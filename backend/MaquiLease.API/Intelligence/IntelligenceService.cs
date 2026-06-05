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
        // 5. ASSET HEALTH & PREDICTIVE MAINTENANCE
        // ═══════════════════════════════════════════════════════════
        public async Task<List<AssetHealthDto>> GetAssetHealthAnalysis()
        {
            var assets = await _context.Assets
                .Include(a => a.Contracts)
                .ToListAsync();
            var allAlerts = await _context.Alerts.ToListAsync();

            var result = new List<AssetHealthDto>();

            foreach (var asset in assets)
            {
                decimal healthIndex = 100m;

                // Factor 1: Antigüedad de compra (Max -30 pts)
                double ageMonths = asset.PurchaseDate.HasValue
                    ? (DateTime.UtcNow - asset.PurchaseDate.Value).TotalDays / 30.0
                    : 0;
                decimal ageDeduction = Math.Min((decimal)ageMonths * 0.5m, 30m);
                healthIndex -= ageDeduction;

                // Factor 2: Uso comercial (Contratos históricos) (Max -20 pts)
                int contractsCount = asset.Contracts.Count;
                decimal contractsDeduction = Math.Min(contractsCount * 5m, 20m);
                healthIndex -= contractsDeduction;

                // Factor 3: Depreciación de valor (Max -20 pts)
                if (asset.PurchasePriceUSD.HasValue && asset.PurchasePriceUSD.Value > 0 && asset.CurrentValue.HasValue)
                {
                    decimal dep = 1m - (asset.CurrentValue.Value / asset.PurchasePriceUSD.Value);
                    if (dep > 0)
                    {
                        decimal depDeduction = Math.Min(dep * 20m, 20m);
                        healthIndex -= depDeduction;
                    }
                }

                // Factor 4: Alertas críticas activas vinculadas a sus contratos (Max -30 pts)
                var assetContractIds = asset.Contracts.Select(c => c.ContractId).ToList();
                var unresolvedAlerts = allAlerts.Count(a => assetContractIds.Contains(a.ContractId) && !a.IsRead);
                decimal alertsDeduction = Math.Min(unresolvedAlerts * 10m, 30m);
                healthIndex -= alertsDeduction;

                // Bounding
                healthIndex = Math.Clamp(healthIndex, 0m, 100m);
                healthIndex = Math.Round(healthIndex, 1);

                decimal wearPercentage = 100m - healthIndex;

                // Recomendación basada en desgaste y categoría
                string recommendation = "Mantenimiento preventivo no requerido. Activo en óptimo estado.";
                if (healthIndex <= 40m)
                {
                    recommendation = asset.Category switch
                    {
                        "mineria" => "CRÍTICO: Requiere inspección urgente de sistemas hidráulicos, filtros y motor principal de inmediato.",
                        "agroindustrial" => "CRÍTICO: Requiere overhaul de transmisión, cambio de aceites y calibración de partes mecánicas.",
                        _ => "CRÍTICO: Se sugiere retirar del servicio activo de inmediato y realizar mantenimiento correctivo mayor."
                    };
                }
                else if (healthIndex <= 75m)
                {
                    recommendation = asset.Category switch
                    {
                        "mineria" => "MODERADO: Agendar cambio preventivo de aceites y lubricación en los próximos 15 días.",
                        "agroindustrial" => "MODERADO: Programar afinamiento menor de motor y revisión de presión en neumáticos/orugas.",
                        _ => "MODERADO: Programar servicio preventivo estándar a la brevedad."
                    };
                }

                result.Add(new AssetHealthDto
                {
                    AssetId = asset.AssetId,
                    AssetName = asset.Name,
                    AssetCode = asset.Code,
                    Category = asset.Category ?? "",
                    HealthIndex = healthIndex,
                    WearPercentage = wearPercentage,
                    ContractsCount = contractsCount,
                    Status = asset.Status,
                    Recommendation = recommendation
                });
            }

            return result.OrderBy(r => r.HealthIndex).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        // 6. SMART MATCHMAKER & CROSS-SELLING
        // ═══════════════════════════════════════════════════════════
        public async Task<List<MatchmakerRecommendationDto>> GetMatchmakerRecommendations()
        {
            var clients = await _context.Clients
                .Include(c => c.Contracts)
                .Where(c => c.IsActive)
                .ToListAsync();

            var availableAssets = await _context.Assets
                .Where(a => a.Status == "disponible")
                .ToListAsync();

            var allContracts = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.Asset)
                .ToListAsync();

            var recommendations = new List<MatchmakerRecommendationDto>();

            // Calcular popularidad de categorías por sector
            var sectorCategoryPopularity = allContracts
                .Where(c => c.Client != null && c.Asset != null)
                .GroupBy(c => new { c.Client.Sector, c.Asset.Category })
                .Select(g => new
                {
                    Sector = g.Key.Sector ?? "",
                    Category = g.Key.Category ?? "",
                    Count = g.Count()
                })
                .ToList();

            foreach (var client in clients)
            {
                // Excluir clientes problemáticos del matchmaker para mitigar riesgo comercial
                if (client.RiskScore > 75) continue;

                var clientLeasedCategories = client.Contracts
                    .Where(c => c.AssetId.HasValue && c.Asset != null)
                    .Select(c => c.Asset.Category)
                    .Distinct()
                    .ToList();

                foreach (var asset in availableAssets)
                {
                    decimal affinityScore = 50m; // Base

                    // 1. Coincidencia de categoría preferida del sector (+30%)
                    var popularity = sectorCategoryPopularity.FirstOrDefault(p => p.Sector == client.Sector && p.Category == asset.Category);
                    if (popularity != null && popularity.Count > 0)
                    {
                        affinityScore += 30m;
                    }

                    // 2. Coincidencia de historial individual del cliente (+20%)
                    if (clientLeasedCategories.Contains(asset.Category))
                    {
                        affinityScore += 20m;
                    }

                    // 3. Ajuste según perfil de riesgo (Max -15%)
                    if (client.RiskScore.HasValue)
                    {
                        decimal riskDeduction = (client.RiskScore.Value / 100m) * 15m;
                        affinityScore -= riskDeduction;
                    }

                    affinityScore = Math.Clamp(affinityScore, 0m, 100m);
                    affinityScore = Math.Round(affinityScore, 1);

                    // Solo recomendar si la afinidad supera el 65%
                    if (affinityScore >= 65m)
                    {
                        // Calcular tarifa sugerida usando motor de precios
                        var pricingRec = await RecommendPrice(new PricingRequestDto
                        {
                            AssetId = asset.AssetId,
                            ClientId = client.ClientId,
                            DurationMonths = 12 // Asumir contrato anual estándar para recomendación
                        });

                        string confidenceLevel = "Baja";
                        if (client.RiskScore.HasValue)
                        {
                            if (client.RiskScore.Value <= 35m)
                            {
                                confidenceLevel = affinityScore >= 80m ? "Alta" : "Media";
                            }
                            else if (client.RiskScore.Value <= 50m)
                            {
                                confidenceLevel = "Media";
                            }
                        }
                        else
                        {
                            confidenceLevel = affinityScore >= 80m ? "Alta" : "Media";
                        }

                        string riskStatusText = client.RiskScore.HasValue
                            ? (client.RiskScore.Value <= 35m ? "historial financiero estable" : "historial financiero moderado")
                            : "historial financiero no registrado";

                        string reasoning = $"El {affinityScore:F0}% de afinidad se debe a que ";
                        if (clientLeasedCategories.Contains(asset.Category))
                        {
                            reasoning += $"el cliente ya arrienda activos de categoría '{asset.Category}' ";
                        }
                        else
                        {
                            reasoning += $"la categoría '{asset.Category}' es la más solicitada en el sector '{client.Sector}' ";
                        }
                        reasoning += $"y posee un {riskStatusText} (Score de Riesgo: {client.RiskScore ?? 0:F0}/100).";

                        recommendations.Add(new MatchmakerRecommendationDto
                        {
                            ClientId = client.ClientId,
                            ClientName = client.BusinessName,
                            Sector = client.Sector ?? "",
                            AssetId = asset.AssetId,
                            AssetName = asset.Name,
                            AssetCategory = asset.Category ?? "",
                            AffinityScore = affinityScore,
                            SuggestedMonthlyRate = Math.Round(pricingRec.SuggestedPrice / 12m, 2), // tarifa mensual
                            ConfidenceLevel = confidenceLevel,
                            Reasoning = reasoning
                        });
                    }
                }
            }

            return recommendations.OrderByDescending(r => r.AffinityScore).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        // 7. INTERACTIVE "WHAT-IF" CREDIT SIMULATOR
        // ═══════════════════════════════════════════════════════════
        public async Task<SimulatedRiskDto> SimulateRiskScore(RiskSimulationRequestDto request)
        {
            // Algoritmo de simulación predictiva "What-If"
            // Factor 1: Historial de Pago Simulado (40%)
            decimal paymentHistoryRaw = 1m - (request.OnTimePaymentRate / 100m);

            // Factor 2: Plazo y apalancamiento (30%)
            // A mayor plazo y menor cuota inicial, mayor riesgo acumulativo
            decimal financementRatio = 1m - (request.DownPayment / (request.TotalAmount > 0 ? request.TotalAmount : 1m));
            decimal termFactor = Math.Min(request.InstallmentsCount / 36m, 1m);
            decimal leverageRaw = (financementRatio * 0.6m) + (termFactor * 0.4m);

            // Factor 3: Riesgo del Sector Económico (15%)
            decimal sectorRiskRaw = request.Sector?.ToLowerInvariant() switch
            {
                "mineria" => 0.45m,       // Alta rentabilidad, volatilidad media
                "construccion" => 0.80m,  // Construcción es sector de alto riesgo/mora
                "agroindustrial" => 0.30m, // Sector muy estable y seguro en el leasing peruano
                "transporte" => 0.60m,    // Desgaste alto
                "manufactura" => 0.35m,   // Estable
                _ => 0.50m
            };

            // Factor 4: Desviación del monto contra la mediana nacional (15%)
            // Mediana estimada nacional de financiamiento: S/ 80,000
            decimal medianNational = 80000m;
            decimal deviationRaw = Math.Min(Math.Abs(request.TotalAmount - medianNational) / medianNational, 1m);

            // Cálculo del Score Ponderado
            decimal score = Math.Round(
                (paymentHistoryRaw * 40m) +
                (leverageRaw * 30m) +
                (sectorRiskRaw * 15m) +
                (deviationRaw * 15m), 1);

            score = Math.Clamp(score, 0m, 100m);

            var (category, color) = score switch
            {
                <= 25m => ("Bajo", "green"),
                <= 50m => ("Medio", "yellow"),
                <= 75m => ("Alto", "orange"),
                _ => ("Crítico", "red")
            };

            // Generar recomendaciones dinámicas de la IA para mitigación de riesgo
            var recs = new List<string>();
            if (score > 75m)
            {
                recs.Add("RECHAZO RECOMENDADO: El perfil simulado expone un riesgo crítico.");
                recs.Add($"Sugerencia 1: Incrementar la cuota inicial al menos al {Math.Max(30m, request.DownPayment / request.TotalAmount * 100 + 15):F0}% del monto total.");
                recs.Add($"Sugerencia 2: Disminuir el plazo del contrato de {request.InstallmentsCount} a {Math.Max(3, request.InstallmentsCount / 2)} meses para mitigar la exposición temporal.");
            }
            else if (score > 50m)
            {
                recs.Add("APROBACIÓN CONDICIONADA: El riesgo simulado es Alto.");
                recs.Add("Sugerencia 1: Solicitar una fianza solidaria o carta fianza bancaria como garantía.");
                recs.Add("Sugerencia 2: Acortar el plazo a un máximo de 12 meses.");
                recs.Add("Sugerencia 3: Incrementar la tasa de interés en +2.5% sobre la tasa base.");
            }
            else if (score > 25m)
            {
                recs.Add("APROBACIÓN RECOMENDADA CON MONITOREO: Riesgo Medio.");
                recs.Add("Sugerencia 1: Ofrecer plazos estándar (12 a 24 meses) sin penalidad adicional.");
                recs.Add("Sugerencia 2: Solicitar un abono inicial mínimo del 10%.");
            }
            else
            {
                recs.Add("APROBACIÓN INMEDIATA: Riesgo Bajo / Excelente perfil.");
                recs.Add("Sugerencia 1: Ofrecer tasa de interés preferencial (-1.5%).");
                recs.Add("Sugerencia 2: Habilitar opción de compra residual flexible al finalizar el contrato.");
            }

            return new SimulatedRiskDto
            {
                Score = score,
                Category = category,
                CategoryColor = color,
                Recommendations = recs
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

        public async Task<List<RiskHistoryDto>> GetRiskHistory(int clientId)
        {
            return await _context.PredictionLogs
                .Where(p => p.ClientId == clientId && p.PredictionType == "riesgo_mora")
                .OrderBy(p => p.GeneratedAt)
                .Select(p => new RiskHistoryDto
                {
                    Score = p.Score,
                    GeneratedAt = p.GeneratedAt
                })
                .ToListAsync();
        }
    }
}
