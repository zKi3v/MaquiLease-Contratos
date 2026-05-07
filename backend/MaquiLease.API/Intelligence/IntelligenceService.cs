using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Intelligence
{
    public class IntelligenceService : IIntelligenceService
    {
        private readonly AppDbContext _context;

        public IntelligenceService(AppDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════
        // 1. RISK SCORE — Predicción de morosidad (0-100)
        // ═══════════════════════════════════════════════════════════
        public async Task<RiskScoreDto> CalculateRiskScore(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new KeyNotFoundException($"Cliente {clientId} no encontrado.");

            var contracts = await _context.Contracts
                .Include(c => c.Installments)
                .Where(c => c.ClientId == clientId)
                .ToListAsync();

            var allInstallments = contracts.SelectMany(c => c.Installments).ToList();
            var now = DateTime.UtcNow;

            // Factor 1: Historial de pagos (40%)
            var resolvedInstallments = allInstallments
                .Where(i => i.Status == "pagado" || i.Status == "vencido").ToList();
            var paidOnTime = resolvedInstallments
                .Count(i => i.Status == "pagado" && i.PaidDate.HasValue && i.PaidDate.Value <= i.DueDate.AddDays(3));
            decimal paymentHistoryRaw = resolvedInstallments.Count > 0
                ? 1m - ((decimal)paidOnTime / resolvedInstallments.Count)
                : 0m;

            // Factor 2: Promedio días de atraso (25%)
            var overdueInstallments = allInstallments
                .Where(i => i.Status == "vencido" || (i.Status == "pagado" && i.PaidDate.HasValue && i.PaidDate.Value > i.DueDate))
                .ToList();
            double avgDaysLate = 0;
            if (overdueInstallments.Any())
            {
                avgDaysLate = overdueInstallments.Average(i =>
                {
                    var referenceDate = i.PaidDate ?? now;
                    return Math.Max(0, (referenceDate - i.DueDate).TotalDays);
                });
            }
            decimal avgDaysLateRaw = Math.Min((decimal)avgDaysLate / 30m, 1m);

            // Factor 3: Cuotas vencidas actuales (20%)
            var currentOverdue = allInstallments.Count(i => i.Status == "vencido");
            decimal currentOverdueRaw = Math.Min((decimal)currentOverdue / 5m, 1m);

            // Factor 4: Valor contrato vs sector (15%)
            var clientTotalValue = contracts.Sum(c => c.TotalAmount);
            var sectorMedian = await GetSectorMedianContractValue(client.Sector ?? "");
            decimal sectorDeviationRaw = sectorMedian > 0
                ? Math.Min(Math.Abs(clientTotalValue - sectorMedian) / sectorMedian, 1m)
                : 0m;

            // Cálculo final ponderado
            decimal score = Math.Round(
                (paymentHistoryRaw * 40m) +
                (avgDaysLateRaw * 25m) +
                (currentOverdueRaw * 20m) +
                (sectorDeviationRaw * 15m), 2);

            score = Math.Min(score, 100m);

            // Categorización
            var (category, color) = score switch
            {
                <= 25 => ("Bajo", "green"),
                <= 50 => ("Medio", "yellow"),
                <= 75 => ("Alto", "orange"),
                _ => ("Crítico", "red")
            };

            // Persistir en PredictionLogs
            _context.PredictionLogs.Add(new PredictionLog
            {
                ClientId = clientId,
                PredictionType = "riesgo_mora",
                Score = score,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    paymentHistoryRaw,
                    avgDaysLateRaw,
                    currentOverdueRaw,
                    sectorDeviationRaw,
                    resolvedCount = resolvedInstallments.Count,
                    paidOnTimeCount = paidOnTime,
                    currentOverdueCount = currentOverdue,
                    avgDaysLate
                }),
                GeneratedAt = DateTime.UtcNow
            });

            // Actualizar Client.RiskScore
            client.RiskScore = (decimal?)score;
            await _context.SaveChangesAsync();

            return new RiskScoreDto
            {
                ClientId = clientId,
                ClientName = client.BusinessName,
                Score = score,
                Category = category,
                CategoryColor = color,
                CalculatedAt = DateTime.UtcNow,
                Factors = new List<RiskFactorDto>
                {
                    new()
                    {
                        Name = "Historial de Pagos",
                        Weight = 40,
                        RawValue = Math.Round(paymentHistoryRaw * 100, 1),
                        WeightedScore = Math.Round(paymentHistoryRaw * 40, 2),
                        Description = $"{paidOnTime} de {resolvedInstallments.Count} cuotas pagadas a tiempo"
                    },
                    new()
                    {
                        Name = "Promedio Días Atraso",
                        Weight = 25,
                        RawValue = Math.Round(avgDaysLateRaw * 100, 1),
                        WeightedScore = Math.Round(avgDaysLateRaw * 25, 2),
                        Description = $"Promedio de {avgDaysLate:F1} días de atraso"
                    },
                    new()
                    {
                        Name = "Cuotas Vencidas Actuales",
                        Weight = 20,
                        RawValue = Math.Round(currentOverdueRaw * 100, 1),
                        WeightedScore = Math.Round(currentOverdueRaw * 20, 2),
                        Description = $"{currentOverdue} cuotas vencidas actualmente"
                    },
                    new()
                    {
                        Name = "Valor vs Sector",
                        Weight = 15,
                        RawValue = Math.Round(sectorDeviationRaw * 100, 1),
                        WeightedScore = Math.Round(sectorDeviationRaw * 15, 2),
                        Description = $"Valor total S/ {clientTotalValue:N0} vs mediana sector S/ {sectorMedian:N0}"
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════
        // 2. PRICING RECOMMENDATION
        // ═══════════════════════════════════════════════════════════
        public async Task<PricingResponseDto> RecommendPrice(PricingRequestDto request)
        {
            var factors = new List<PricingFactorDto>();
            decimal basePrice = 0;
            string itemName = "";

            // Obtener precio base del activo o servicio
            if (request.AssetId.HasValue)
            {
                var asset = await _context.Assets.FindAsync(request.AssetId.Value);
                if (asset == null) throw new KeyNotFoundException("Activo no encontrado.");

                // Base: valor mensual estimado = CurrentValue / 36 meses de vida útil aprox.
                basePrice = (asset.CurrentValue ?? 0m) / 36m;
                itemName = asset.Name;

                // Factor depreciación
                var ageMonths = asset.PurchaseDate.HasValue
                    ? (DateTime.UtcNow - asset.PurchaseDate.Value).TotalDays / 30.0
                    : 0;
                var depreciationRate = Math.Min(ageMonths / 120.0, 0.5); // Max 50% depreciación
                factors.Add(new PricingFactorDto
                {
                    Name = "Depreciación del activo",
                    Impact = depreciationRate > 0.3 ? "negativo" : "neutro",
                    Description = $"Activo con {ageMonths:F0} meses de antigüedad ({depreciationRate * 100:F0}% depreciado)"
                });

                // Histórico de contratos similares
                var similarContracts = await _context.Contracts
                    .Include(c => c.Asset)
                    .Where(c => c.Asset != null && c.Asset.Category == asset.Category && c.Status != "borrador")
                    .ToListAsync();

                if (similarContracts.Any())
                {
                    var avgMonthlyRate = similarContracts.Average(c =>
                        c.NumberOfInstallments > 0 ? c.TotalAmount / c.NumberOfInstallments : c.TotalAmount);
                    basePrice = (basePrice + avgMonthlyRate) / 2m; // Promedio entre cálculo y mercado

                    factors.Add(new PricingFactorDto
                    {
                        Name = "Contratos similares",
                        Impact = "positivo",
                        Description = $"Basado en {similarContracts.Count} contratos de categoría '{asset.Category}' con cuota promedio S/ {avgMonthlyRate:N0}"
                    });
                }
            }
            else if (request.ServiceId.HasValue)
            {
                var service = await _context.Services.FindAsync(request.ServiceId.Value);
                if (service == null) throw new KeyNotFoundException("Servicio no encontrado.");

                basePrice = service.BasePrice;
                itemName = service.Name;

                factors.Add(new PricingFactorDto
                {
                    Name = "Precio base del servicio",
                    Impact = "neutro",
                    Description = $"Tarifa estándar: S/ {service.BasePrice:N0} ({service.PriceUnit})"
                });
            }
            else
            {
                throw new ArgumentException("Debe especificar un AssetId o ServiceId.");
            }

            // Factor: riesgo del cliente
            decimal riskAdjustment = 1.0m;
            if (request.ClientId.HasValue)
            {
                var client = await _context.Clients.FindAsync(request.ClientId.Value);
                if (client != null)
                {
                    if ((client.RiskScore ?? 0) > 70)
                    {
                        riskAdjustment = 1.15m; // +15% para clientes de alto riesgo
                        factors.Add(new PricingFactorDto
                        {
                            Name = "Riesgo del cliente",
                            Impact = "negativo",
                            Description = $"Score {client.RiskScore}/100 (crítico) — se sugiere +15% y plazos cortos"
                        });
                    }
                    else if ((client.RiskScore ?? 0) > 40)
                    {
                        riskAdjustment = 1.05m;
                        factors.Add(new PricingFactorDto
                        {
                            Name = "Riesgo del cliente",
                            Impact = "negativo",
                            Description = $"Score {client.RiskScore}/100 (medio) — recargo moderado +5%"
                        });
                    }
                    else
                    {
                        riskAdjustment = 0.95m; // Descuento para clientes fiables
                        factors.Add(new PricingFactorDto
                        {
                            Name = "Riesgo del cliente",
                            Impact = "positivo",
                            Description = $"Score {client.RiskScore}/100 (bajo riesgo) — descuento del 5% por buen historial"
                        });
                    }
                }
            }

            // Factor: duración del contrato
            if (request.DurationMonths >= 12)
            {
                riskAdjustment *= 0.95m; // 5% descuento por contrato largo
                factors.Add(new PricingFactorDto
                {
                    Name = "Duración del contrato",
                    Impact = "positivo",
                    Description = $"Contrato de {request.DurationMonths} meses — descuento por compromiso a largo plazo"
                });
            }

            decimal suggested = Math.Round(basePrice * riskAdjustment * request.DurationMonths, 2);
            decimal min = Math.Round(suggested * 0.85m, 2);
            decimal max = Math.Round(suggested * 1.20m, 2);

            string explanation = $"Precio sugerido para '{itemName}' basado en "
                + $"{factors.Count} factores de análisis. "
                + (request.ClientId.HasValue ? $"Ajustado por perfil de riesgo del cliente. " : "")
                + $"Rango: S/ {min:N0} – S/ {max:N0} por {request.DurationMonths} meses.";

            return new PricingResponseDto
            {
                MinPrice = min,
                SuggestedPrice = suggested,
                MaxPrice = max,
                Currency = "PEN",
                Explanation = explanation,
                Factors = factors
            };
        }

        // ═══════════════════════════════════════════════════════════
        // 3. REVENUE FORECAST (3 bandas: optimista, esperado, pesimista)
        // ═══════════════════════════════════════════════════════════
        public async Task<RevenueForecastDto> RevenueForecast()
        {
            var now = DateTime.UtcNow;
            var allInstallments = await _context.Installments.ToListAsync();

            // Calcular tasa global de cobro a tiempo
            var resolved = allInstallments.Where(i => i.Status == "pagado" || i.Status == "vencido").ToList();
            var paidOnTime = resolved.Count(i => i.Status == "pagado");
            decimal collectionRate = resolved.Count > 0 ? (decimal)paidOnTime / resolved.Count : 0.85m;

            var points = new List<ForecastBandPointDto>();
            string[] monthNames = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

            // Últimos 3 meses (históricos)
            for (int i = 3; i >= 1; i--)
            {
                var target = now.AddMonths(-i);
                var monthInstallments = allInstallments
                    .Where(inst => inst.DueDate.Year == target.Year && inst.DueDate.Month == target.Month)
                    .ToList();

                decimal totalExpected = monthInstallments.Sum(inst => inst.Amount);
                decimal totalPaid = monthInstallments.Sum(inst => inst.PaidAmount);

                points.Add(new ForecastBandPointDto
                {
                    Month = $"{monthNames[target.Month]} {target:yy}",
                    Optimistic = totalExpected,
                    Expected = totalPaid,
                    Pessimistic = totalPaid,
                    IsHistorical = true
                });
            }

            // Próximos 6 meses (proyección)
            for (int i = 0; i <= 5; i++)
            {
                var target = now.AddMonths(i);
                var monthInstallments = allInstallments
                    .Where(inst => inst.DueDate.Year == target.Year && inst.DueDate.Month == target.Month)
                    .ToList();

                decimal totalExpected = monthInstallments.Sum(inst => inst.Amount);

                // Si no hay cuotas futuras, usar promedio histórico
                if (totalExpected == 0 && i > 0)
                {
                    var avgMonthly = allInstallments
                        .Where(inst => inst.DueDate >= now.AddMonths(-6) && inst.DueDate < now)
                        .GroupBy(inst => new { inst.DueDate.Year, inst.DueDate.Month })
                        .Select(g => g.Sum(x => x.Amount))
                        .DefaultIfEmpty(0)
                        .Average();
                    totalExpected = (decimal)avgMonthly;
                }

                decimal optimistic = totalExpected;
                decimal expected = Math.Round(totalExpected * collectionRate, 2);
                decimal pessimistic = Math.Round(expected * 0.80m, 2);

                // Mes actual: combinar real + proyectado
                if (i == 0)
                {
                    var alreadyPaid = monthInstallments.Sum(inst => inst.PaidAmount);
                    if (alreadyPaid > 0)
                    {
                        expected = Math.Max(expected, alreadyPaid);
                        pessimistic = Math.Max(pessimistic, alreadyPaid * 0.9m);
                    }
                }

                points.Add(new ForecastBandPointDto
                {
                    Month = $"{monthNames[target.Month]} {target:yy}",
                    Optimistic = optimistic,
                    Expected = expected,
                    Pessimistic = pessimistic,
                    IsHistorical = false
                });
            }

            // Resumen textual
            var futureExpected = points.Where(p => !p.IsHistorical).Sum(p => p.Expected);
            var futurePessimistic = points.Where(p => !p.IsHistorical).Sum(p => p.Pessimistic);

            return new RevenueForecastDto
            {
                Points = points,
                HistoricalCollectionRate = Math.Round(collectionRate * 100, 1),
                Summary = $"Con una tasa de cobro histórica del {collectionRate * 100:F1}%, " +
                          $"se proyectan ingresos de S/ {futureExpected:N0} (esperado) a S/ {futurePessimistic:N0} (pesimista) en los próximos 6 meses."
            };
        }

        // ═══════════════════════════════════════════════════════════
        // 4. CLIENT SEGMENTATION
        // ═══════════════════════════════════════════════════════════
        public async Task<SegmentationSummaryDto> SegmentClients()
        {
            var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
            var contracts = await _context.Contracts.ToListAsync();

            var segments = new List<ClientSegmentDto>();

            foreach (var client in clients)
            {
                var clientContracts = contracts.Where(c => c.ClientId == client.ClientId).ToList();
                var totalValue = clientContracts.Sum(c => c.TotalAmount);
                var firstContract = clientContracts.OrderBy(c => c.StartDate).FirstOrDefault();
                var monthsSinceFirst = firstContract != null
                    ? (DateTime.UtcNow - firstContract.StartDate).TotalDays / 30.0
                    : 0;

                var overdueInstallments = await _context.Installments
                    .Where(i => clientContracts.Select(c => c.ContractId).Contains(i.ContractId) && i.Status == "vencido")
                    .CountAsync();

                // Clasificación según PLAN.md sección 8.4
                string segment, color, icon, action;

                if ((client.RiskScore ?? 0) > 70 || overdueInstallments >= 2)
                {
                    segment = "Problemático";
                    color = "red";
                    icon = "pi-exclamation-triangle";
                    action = "Endurecer condiciones, limitar crédito, cobro inmediato";
                }
                else if ((client.RiskScore ?? 0) > 40)
                {
                    segment = "En Riesgo";
                    color = "orange";
                    icon = "pi-exclamation-circle";
                    action = "Contactar proactivamente, renegociar condiciones";
                }
                else if (monthsSinceFirst < 6 && (client.RiskScore ?? 0) <= 40)
                {
                    segment = "Crecimiento";
                    color = "blue";
                    icon = "pi-arrow-up-right";
                    action = "Monitorear, ofrecer expansión de servicios";
                }
                else
                {
                    segment = "Premium";
                    color = "emerald";
                    icon = "pi-star";
                    action = "Mejores condiciones, programas de fidelización";
                }

                segments.Add(new ClientSegmentDto
                {
                    ClientId = client.ClientId,
                    ClientName = client.BusinessName,
                    Sector = client.Sector ?? "",
                    Segment = segment,
                    SegmentColor = color,
                    SegmentIcon = icon,
                    RiskScore = client.RiskScore ?? 0m,
                    OverdueInstallments = overdueInstallments,
                    TotalContractValue = totalValue,
                    SuggestedAction = action
                });
            }

            var counts = segments.GroupBy(s => s.Segment).ToDictionary(g => g.Key, g => g.Count());

            return new SegmentationSummaryDto
            {
                Clients = segments.OrderByDescending(s => s.RiskScore).ToList(),
                SegmentCounts = counts
            };
        }

        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════
        private async Task<decimal> GetSectorMedianContractValue(string sector)
        {
            var sectorValues = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Client.Sector == sector && c.Status != "borrador")
                .Select(c => c.TotalAmount)
                .OrderBy(v => v)
                .ToListAsync();

            if (!sectorValues.Any()) return 0;

            int mid = sectorValues.Count / 2;
            return sectorValues.Count % 2 == 0
                ? (sectorValues[mid - 1] + sectorValues[mid]) / 2m
                : sectorValues[mid];
        }
    }
}
