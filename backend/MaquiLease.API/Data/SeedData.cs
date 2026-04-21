using MaquiLease.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            // Si ya hay datos, no sembrar
            if (context.Clients.Any()) return;

            // ═══════════════════════════════════════════════
            // USUARIOS
            // ═══════════════════════════════════════════════
            var users = new List<User>
            {
                new User { UserId = 1, Username = "admin", Email = "admin@maquilease.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), FullName = "Carlos Mendoza", Role = "admin", IsActive = true },
                new User { UserId = 2, Username = "operador", Email = "operador@maquilease.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("op123"), FullName = "María López", Role = "operador", IsActive = true },
                new User { UserId = 3, Username = "gerente", Email = "gerente@maquilease.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("ger123"), FullName = "Luis Vargas", Role = "gerente", IsActive = true }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // ═══════════════════════════════════════════════
            // CLIENTES
            // ═══════════════════════════════════════════════
            var clients = new List<Client>
            {
                new Client { ClientId = 1, RUC = "20100130204", BusinessName = "AgroExport SAC", ContactName = "Roberto Flores", Email = "rflores@agroexport.pe", Phone = "01-2345678", Address = "Av. La Molina 456, Lima", Sector = "agroindustrial", RiskScore = 15, IsActive = true },
                new Client { ClientId = 2, RUC = "20512345678", BusinessName = "Minera del Sur SA", ContactName = "Ana Gutiérrez", Email = "agutierrez@minerasur.pe", Phone = "054-234567", Address = "Jr. Arequipa 789, Arequipa", Sector = "mineria", RiskScore = 42, IsActive = true },
                new Client { ClientId = 3, RUC = "20601234567", BusinessName = "Constructora Lima Norte EIRL", ContactName = "Pedro Castañeda", Email = "pcastaneda@clnorte.pe", Phone = "01-9876543", Address = "Calle Los Olivos 123, Lima", Sector = "construccion", RiskScore = 68, IsActive = true },
                new Client { ClientId = 4, RUC = "20451234560", BusinessName = "Transportes Andinos SAC", ContactName = "Diana Quispe", Email = "dquispe@transportesandinos.pe", Phone = "064-567890", Address = "Av. Huancavelica 321, Huancayo", Sector = "transporte", RiskScore = 22, IsActive = true },
                new Client { ClientId = 5, RUC = "20789012345", BusinessName = "Alimentos del Pacífico SA", ContactName = "Jorge Ramírez", Email = "jramirez@alpacifico.pe", Phone = "01-4567890", Address = "Av. Argentina 1050, Callao", Sector = "agroindustrial", RiskScore = 85, IsActive = true },
                new Client { ClientId = 6, RUC = "20345678901", BusinessName = "Textiles Modernos SAC", ContactName = "Lucía Fernández", Email = "lfernandez@textmod.pe", Phone = "01-3334567", Address = "Jr. Gamarra 550, La Victoria", Sector = "manufactura", RiskScore = 30, IsActive = true }
            };
            context.Clients.AddRange(clients);
            context.SaveChanges();

            // ═══════════════════════════════════════════════
            // ACTIVOS (MAQUINARIA)
            // ═══════════════════════════════════════════════
            var assets = new List<Asset>
            {
                new Asset { AssetId = 1, Code = "EXC-001", Name = "Excavadora Hidráulica CAT 320", Category = "maquinaria_pesada", Brand = "Caterpillar", Model = "320 GC", PurchaseDate = new DateTime(2023, 3, 15), PurchasePriceCNY = 580000, PurchasePriceUSD = 82000, CurrentValue = 68000, Status = "alquilado", Currency = "PEN" },
                new Asset { AssetId = 2, Code = "RET-001", Name = "Retroexcavadora Komatsu WB93", Category = "maquinaria_pesada", Brand = "Komatsu", Model = "WB93R-8", PurchaseDate = new DateTime(2023, 6, 20), PurchasePriceCNY = 420000, PurchasePriceUSD = 59500, CurrentValue = 52000, Status = "disponible", Currency = "PEN" },
                new Asset { AssetId = 3, Code = "GRU-001", Name = "Grúa Torre Liebherr 150 EC-B", Category = "maquinaria_pesada", Brand = "Liebherr", Model = "150 EC-B 6", PurchaseDate = new DateTime(2022, 11, 10), PurchasePriceCNY = 1200000, PurchasePriceUSD = 170000, CurrentValue = 135000, Status = "alquilado", Currency = "PEN" },
                new Asset { AssetId = 4, Code = "CAR-001", Name = "Cargador Frontal SDLG L958F", Category = "maquinaria_pesada", Brand = "SDLG", Model = "L958F", PurchaseDate = new DateTime(2024, 1, 5), PurchasePriceCNY = 350000, PurchasePriceUSD = 49500, CurrentValue = 46000, Status = "disponible", Currency = "PEN" },
                new Asset { AssetId = 5, Code = "ROD-001", Name = "Rodillo Compactador XCMG XS203", Category = "maquinaria_pesada", Brand = "XCMG", Model = "XS203J", PurchaseDate = new DateTime(2024, 4, 18), PurchasePriceCNY = 280000, PurchasePriceUSD = 39800, CurrentValue = 37500, Status = "mantenimiento", Currency = "PEN" },
                new Asset { AssetId = 6, Code = "MOT-001", Name = "Motoniveladora Shantui SG21-3", Category = "maquinaria_pesada", Brand = "Shantui", Model = "SG21-3", PurchaseDate = new DateTime(2023, 8, 22), PurchasePriceCNY = 650000, PurchasePriceUSD = 92000, CurrentValue = 78000, Status = "alquilado", Currency = "PEN" },
                new Asset { AssetId = 7, Code = "MXC-001", Name = "Mezcladora de Concreto Sicoma", Category = "equipo_construccion", Brand = "Sicoma", Model = "MAO 3000", PurchaseDate = new DateTime(2024, 2, 14), PurchasePriceCNY = 95000, PurchasePriceUSD = 13500, CurrentValue = 12800, Status = "disponible", Currency = "PEN" },
                new Asset { AssetId = 8, Code = "GEN-001", Name = "Generador Eléctrico Weichai 200kW", Category = "generadores", Brand = "Weichai", Model = "WPG275", PurchaseDate = new DateTime(2023, 12, 1), PurchasePriceCNY = 180000, PurchasePriceUSD = 25500, CurrentValue = 22000, Status = "alquilado", Currency = "PEN" },
                new Asset { AssetId = 9, Code = "BOM-001", Name = "Bomba de Concreto Sany HBT6016C", Category = "equipo_construccion", Brand = "Sany", Model = "HBT6016C-5S", PurchaseDate = new DateTime(2024, 5, 10), PurchasePriceCNY = 520000, PurchasePriceUSD = 73500, CurrentValue = 71000, Status = "disponible", Currency = "PEN" },
                new Asset { AssetId = 10, Code = "MNT-001", Name = "Montacargas Heli CPCD30", Category = "equipo_logistico", Brand = "Heli", Model = "CPCD30", PurchaseDate = new DateTime(2024, 3, 8), PurchasePriceCNY = 110000, PurchasePriceUSD = 15600, CurrentValue = 14800, Status = "disponible", Currency = "PEN" }
            };
            context.Assets.AddRange(assets);
            context.SaveChanges();

            // ═══════════════════════════════════════════════
            // SERVICIOS
            // ═══════════════════════════════════════════════
            var services = new List<Service>
            {
                new Service { ServiceId = 1, Code = "SRV-MNT", Name = "Mantenimiento Preventivo", Description = "Inspección general, cambio de aceite, filtros y calibración de sistemas hidráulicos.", ServiceType = "tecnico", Category = "mantenimiento", BasePrice = 2500, PriceUnit = "por_proyecto", Currency = "PEN", EstimatedDuration = "1 día", RequiresAsset = true, IsActive = true },
                new Service { ServiceId = 2, Code = "SRV-REP", Name = "Reparación Mecánica Mayor", Description = "Reparación de motor, sistema hidráulico o transmisión con repuestos originales.", ServiceType = "tecnico", Category = "reparacion", BasePrice = 8500, PriceUnit = "por_proyecto", Currency = "PEN", EstimatedDuration = "3-5 días", RequiresAsset = true, IsActive = true },
                new Service { ServiceId = 3, Code = "SRV-INS", Name = "Instalación y Puesta en Marcha", Description = "Transporte, montaje, calibración y pruebas de funcionamiento del equipo en sitio.", ServiceType = "tecnico", Category = "instalacion", BasePrice = 4500, PriceUnit = "por_proyecto", Currency = "PEN", EstimatedDuration = "2 días", RequiresAsset = true, IsActive = true },
                new Service { ServiceId = 4, Code = "SRV-SOP", Name = "Soporte Técnico Mensual", Description = "Asistencia técnica remota y presencial, con visitas programadas de supervisión.", ServiceType = "tecnico", Category = "soporte", BasePrice = 1800, PriceUnit = "mensual", Currency = "PEN", EstimatedDuration = "Mensual", RequiresAsset = false, IsActive = true },
                new Service { ServiceId = 5, Code = "SRV-CON", Name = "Consultoría en Gestión de Flota", Description = "Análisis de eficiencia operativa, optimización del uso de activos y reducción de costos.", ServiceType = "profesional", Category = "consultoria", BasePrice = 12000, PriceUnit = "por_proyecto", Currency = "PEN", EstimatedDuration = "2 semanas", RequiresAsset = false, IsActive = true },
                new Service { ServiceId = 6, Code = "SRV-CAP", Name = "Capacitación de Operadores", Description = "Programa de formación para operadores de maquinaria pesada con certificación.", ServiceType = "profesional", Category = "capacitacion", BasePrice = 6500, PriceUnit = "por_proyecto", Currency = "PEN", EstimatedDuration = "5 días", RequiresAsset = false, IsActive = true },
                new Service { ServiceId = 7, Code = "SRV-ASE", Name = "Asesoría Técnica Especializada", Description = "Evaluación técnica de proyectos de construcción o minería para selección óptima de maquinaria.", ServiceType = "profesional", Category = "asesoria", BasePrice = 8000, PriceUnit = "por_proyecto", Currency = "PEN", EstimatedDuration = "1 semana", RequiresAsset = false, IsActive = true },
                new Service { ServiceId = 8, Code = "SRV-AUD", Name = "Auditoría de Seguridad Industrial", Description = "Inspección de cumplimiento de normativas de seguridad en operación de maquinaria.", ServiceType = "profesional", Category = "asesoria", BasePrice = 5500, PriceUnit = "por_proyecto", Currency = "PEN", EstimatedDuration = "3 días", RequiresAsset = false, IsActive = true }
            };
            context.Services.AddRange(services);
            context.SaveChanges();

            // ═══════════════════════════════════════════════
            // CONTRATOS
            // ═══════════════════════════════════════════════
            var now = DateTime.UtcNow;
            var contracts = new List<Contract>
            {
                // Contratos Activos
                new Contract { ContractId = 1, ContractNumber = "CTR-2026-001", ClientId = 1, AssetId = 1, ContractType = "alquiler", StartDate = now.AddMonths(-4), EndDate = now.AddMonths(8), TotalAmount = 96000, Currency = "PEN", NumberOfInstallments = 12, InterestRate = 2.5m, PenaltyRate = 1.5m, Status = "activo", SignedAt = now.AddMonths(-4), CreatedById = 1 },
                new Contract { ContractId = 2, ContractNumber = "CTR-2026-002", ClientId = 2, AssetId = 3, ContractType = "alquiler", StartDate = now.AddMonths(-3), EndDate = now.AddMonths(9), TotalAmount = 180000, Currency = "PEN", NumberOfInstallments = 12, InterestRate = 3.0m, PenaltyRate = 2.0m, Status = "activo", SignedAt = now.AddMonths(-3), CreatedById = 1 },
                new Contract { ContractId = 3, ContractNumber = "CTR-2026-003", ClientId = 3, AssetId = 6, ServiceId = 1, ContractType = "alquiler", StartDate = now.AddMonths(-2), EndDate = now.AddMonths(4), TotalAmount = 54000, Currency = "PEN", NumberOfInstallments = 6, InterestRate = 2.0m, PenaltyRate = 1.5m, Status = "activo", SignedAt = now.AddMonths(-2), CreatedById = 2 },
                new Contract { ContractId = 4, ContractNumber = "CTR-2026-004", ClientId = 4, AssetId = 8, ContractType = "alquiler", StartDate = now.AddMonths(-5), EndDate = now.AddMonths(1), TotalAmount = 36000, Currency = "PEN", NumberOfInstallments = 6, InterestRate = 2.0m, PenaltyRate = 1.0m, Status = "activo", SignedAt = now.AddMonths(-5), CreatedById = 1 },
                // Contrato de Servicio Puro
                new Contract { ContractId = 5, ContractNumber = "CTR-2026-005", ClientId = 1, ServiceId = 5, ContractType = "servicio", StartDate = now.AddMonths(-1), EndDate = now.AddMonths(1), TotalAmount = 12000, Currency = "PEN", NumberOfInstallments = 2, InterestRate = 0, PenaltyRate = 1.0m, Status = "activo", SignedAt = now.AddMonths(-1), CreatedById = 2 },
                // Contrato Completado
                new Contract { ContractId = 6, ContractNumber = "CTR-2025-010", ClientId = 6, AssetId = 7, ContractType = "venta", StartDate = now.AddMonths(-8), EndDate = now.AddMonths(-2), TotalAmount = 15000, Currency = "PEN", NumberOfInstallments = 6, InterestRate = 0, PenaltyRate = 1.0m, Status = "completado", SignedAt = now.AddMonths(-8), CreatedById = 1 },
                // Contrato con mora (cliente problemático)
                new Contract { ContractId = 7, ContractNumber = "CTR-2025-008", ClientId = 5, AssetId = 2, ContractType = "alquiler", StartDate = now.AddMonths(-6), EndDate = now.AddMonths(6), TotalAmount = 72000, Currency = "PEN", NumberOfInstallments = 12, InterestRate = 3.0m, PenaltyRate = 2.5m, Status = "activo", SignedAt = now.AddMonths(-6), CreatedById = 1 },
                // Borrador
                new Contract { ContractId = 8, ContractNumber = "CTR-2026-006", ClientId = 4, ServiceId = 6, ContractType = "servicio", StartDate = now.AddDays(10), EndDate = now.AddMonths(1), TotalAmount = 6500, Currency = "PEN", NumberOfInstallments = 1, InterestRate = 0, PenaltyRate = 0, Status = "borrador", CreatedById = 2 }
            };
            context.Contracts.AddRange(contracts);
            context.SaveChanges();

            // ═══════════════════════════════════════════════
            // CUOTAS (INSTALLMENTS)
            // ═══════════════════════════════════════════════
            var installments = new List<Installment>();
            int instId = 1;

            // Generar cuotas para cada contrato (excepto borrador)
            foreach (var contract in contracts.Where(c => c.Status != "borrador"))
            {
                decimal cuotaMonto = contract.TotalAmount / contract.NumberOfInstallments;
                for (int i = 1; i <= contract.NumberOfInstallments; i++)
                {
                    var dueDate = contract.StartDate.AddMonths(i);
                    string status = "pendiente";
                    decimal paidAmount = 0;
                    DateTime? paidDate = null;

                    // Cuotas pasadas: marcar como pagadas o vencidas
                    if (dueDate < now.AddDays(-5))
                    {
                        if (contract.ClientId == 5 && i > 3) // Cliente problemático - mora
                        {
                            status = "vencido";
                        }
                        else
                        {
                            status = "pagado";
                            paidAmount = cuotaMonto;
                            paidDate = dueDate.AddDays(new Random(instId).Next(-2, 5));
                        }
                    }
                    else if (dueDate < now)
                    {
                        status = contract.ClientId == 5 ? "vencido" : "pendiente";
                    }

                    installments.Add(new Installment
                    {
                        InstallmentId = instId,
                        ContractId = contract.ContractId,
                        InstallmentNumber = i,
                        DueDate = dueDate,
                        Amount = cuotaMonto,
                        Status = status,
                        PaidAmount = paidAmount,
                        PaidDate = paidDate,
                        PenaltyAmount = status == "vencido" ? cuotaMonto * 0.02m * (decimal)(now - dueDate).TotalDays / 30m : 0,
                        NotifiedApproaching = dueDate < now.AddDays(10),
                        NotifiedOverdue = status == "vencido"
                    });
                    instId++;
                }
            }
            context.Installments.AddRange(installments);
            context.SaveChanges();

            // ═══════════════════════════════════════════════
            // PAGOS
            // ═══════════════════════════════════════════════
            var payments = new List<Payment>();
            int payId = 1;
            string[] methods = { "transferencia", "efectivo", "cheque", "transferencia" };
            
            foreach (var inst in installments.Where(i => i.Status == "pagado"))
            {
                payments.Add(new Payment
                {
                    PaymentId = payId,
                    InstallmentId = inst.InstallmentId,
                    Amount = inst.PaidAmount,
                    PaymentDate = inst.PaidDate ?? inst.DueDate,
                    PaymentMethod = methods[payId % methods.Length],
                    ReferenceNumber = $"REF-{payId:D6}",
                    DocumentType = payId % 3 == 0 ? "factura" : "boleta",
                    DocumentNumber = $"B001-{payId:D5}",
                    CreatedById = 1
                });
                payId++;
            }
            context.Payments.AddRange(payments);
            context.SaveChanges();

            // ═══════════════════════════════════════════════
            // ALERTAS
            // ═══════════════════════════════════════════════
            var alerts = new List<Alert>
            {
                new Alert { AlertId = 1, ContractId = 7, AlertType = "cuota_vencida", Message = "El cliente Alimentos del Pacífico SA tiene 3 cuotas vencidas del contrato CTR-2025-008.", SentAt = now.AddDays(-10), SentVia = "sistema", IsRead = false },
                new Alert { AlertId = 2, ContractId = 7, AlertType = "riesgo_alto", Message = "Riesgo crítico: Alimentos del Pacífico SA tiene un score de riesgo de 85/100.", SentAt = now.AddDays(-7), SentVia = "sistema", IsRead = false },
                new Alert { AlertId = 3, ContractId = 4, AlertType = "vencimiento_proximo", Message = "El contrato CTR-2026-004 vence en 30 días. Contactar a Transportes Andinos SAC.", SentAt = now.AddDays(-3), SentVia = "email", IsRead = true },
                new Alert { AlertId = 4, ContractId = 3, AlertType = "vencimiento_proximo", Message = "Cuota #3 del contrato CTR-2026-003 vence en 5 días.", SentAt = now.AddDays(-1), SentVia = "sistema", IsRead = false },
            };
            context.Alerts.AddRange(alerts);
            context.SaveChanges();
        }
    }
}
