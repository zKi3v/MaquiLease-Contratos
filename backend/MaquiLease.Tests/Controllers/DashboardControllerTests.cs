using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using MaquiLease.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MaquiLease.Tests.Controllers
{
    public class DashboardControllerTests
    {
        [Fact]
        public async Task GetKpis_ShouldCalculateCorrectAggregates()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var intelligenceMock = new MockIntelligenceService();
            var controller = new DashboardController(context, intelligenceMock);

            // 1. Población de Activos (3 activos)
            context.Assets.Add(new Asset { Code = "A1", Name = "Activo 1", Status = "disponible" });
            context.Assets.Add(new Asset { Code = "A2", Name = "Activo 2", Status = "arrendado" });
            context.Assets.Add(new Asset { Code = "A3", Name = "Activo 3", Status = "mantenimiento" });

            // 2. Población de Contratos (2 activos + 1 borrador que no cuenta)
            context.Contracts.Add(new Contract { ContractNumber = "CTR-ACT-1", Status = "activo" });
            context.Contracts.Add(new Contract { ContractNumber = "CTR-ACT-2", Status = "ejecucion" });
            context.Contracts.Add(new Contract { ContractNumber = "CTR-ACT-3", Status = "borrador" }); // No debería contar

            // 3. Población de Cuotas (4 cuotas de 1000 c/u, Total Esperado: 4000)
            // - Cuota 1: Pagada a tiempo (Cobrado: 1000)
            context.Installments.Add(new Installment 
            { 
                InstallmentNumber = 1, 
                Amount = 1000m, 
                PaidAmount = 1000m, 
                Status = "pagado", 
                DueDate = DateTime.Now.AddMonths(-1), 
                PaidDate = DateTime.Now.AddMonths(-1) 
            });
            // - Cuota 2: Pagada a tiempo (Cobrado: 1000, Total Cobrado: 2000)
            context.Installments.Add(new Installment 
            { 
                InstallmentNumber = 2, 
                Amount = 1000m, 
                PaidAmount = 1000m, 
                Status = "pagado", 
                DueDate = DateTime.Now.AddDays(-15), 
                PaidDate = DateTime.Now.AddDays(-15) 
            });
            // - Cuota 3: Pendiente y Vencida (DueDate en el pasado, morosa)
            context.Installments.Add(new Installment 
            { 
                InstallmentNumber = 3, 
                Amount = 1000m, 
                PaidAmount = 0m, 
                Status = "pendiente", 
                DueDate = DateTime.Now.AddDays(-5) // Vencida
            });
            // - Cuota 4: Pendiente en el Futuro (A tiempo)
            context.Installments.Add(new Installment 
            { 
                InstallmentNumber = 4, 
                Amount = 1000m, 
                PaidAmount = 0m, 
                Status = "pendiente", 
                DueDate = DateTime.Now.AddMonths(1) // Futura
            });

            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetKpis();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var kpiDto = Assert.IsType<DashboardKpiDto>(okResult.Value);

            Assert.Equal(3, kpiDto.TotalAssets);
            Assert.Equal(2, kpiDto.ActiveContracts);
            Assert.Equal(4000m, kpiDto.TotalExpectedRevenue);
            Assert.Equal(2000m, kpiDto.TotalCollectedRevenue);
            
            // Tasa de morosidad: 1 cuota vencida de 4 cuotas totales = 25%
            Assert.Equal(25.0, kpiDto.DefaultRatePercentage);
        }

        [Fact]
        public async Task GetKpis_EmptyDatabase_ShouldNotDivideByZeroOrCrash()
        {
            // Arrange
            using var context = TestDbContextFactory.Create(); // Vacía
            var controller = new DashboardController(context, new MockIntelligenceService());

            // Act
            var actionResult = await controller.GetKpis();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var kpiDto = Assert.IsType<DashboardKpiDto>(okResult.Value);

            Assert.Equal(0, kpiDto.TotalAssets);
            Assert.Equal(0, kpiDto.ActiveContracts);
            Assert.Equal(0m, kpiDto.TotalExpectedRevenue);
            Assert.Equal(0m, kpiDto.TotalCollectedRevenue);
            Assert.Equal(0.0, kpiDto.DefaultRatePercentage); // Asegura robustez ante división por cero
        }

        [Fact]
        public async Task GetAssetStatus_ShouldReturnCorrectDistribution()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new DashboardController(context, new MockIntelligenceService());

            context.Assets.Add(new Asset { Code = "A1", Name = "A", Status = "disponible" });
            context.Assets.Add(new Asset { Code = "A2", Name = "B", Status = "DISPONIBLE" }); // Diferente mayúscula/minúscula
            context.Assets.Add(new Asset { Code = "A3", Name = "C", Status = "arrendado" });
            context.Assets.Add(new Asset { Code = "A4", Name = "D", Status = "mantenimiento" });
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetAssetStatus();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var distribution = Assert.IsType<AssetDistributionDto>(okResult.Value);

            Assert.Equal(2, distribution.Available);
            Assert.Equal(1, distribution.Rented);
            Assert.Equal(1, distribution.Maintenance);
        }

        [Fact]
        public async Task GetRevenueForecast_WithPreexistingInstallments_ShouldCalculateCorrectPredictions()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new DashboardController(context, new MockIntelligenceService());

            var currentMonth = DateTime.Now;
            // Registrar cuota del mes pasado para tener datos históricos
            context.Installments.Add(new Installment
            {
                Amount = 1000m,
                PaidAmount = 800m,
                DueDate = currentMonth.AddMonths(-1)
            });
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetRevenueForecast();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var forecast = Assert.IsAssignableFrom<IEnumerable<ForecastPointDto>>(okResult.Value).ToList();

            // Debería retornar 4 meses históricos y 4 meses de proyección (8 puntos en total)
            Assert.Equal(8, forecast.Count);
            
            // Primeros 4 meses son histórico (Real vs Expected)
            Assert.Equal(800m, forecast[2].RealRevenue); // Mes -1
            Assert.Equal(1000m, forecast[2].PredictedRevenue);
        }

        [Fact]
        public async Task GetOverdueRate_WithVaryingLateInstallments_ShouldReturnCorrectMonthlyRates()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new DashboardController(context, new MockIntelligenceService());

            var now = DateTime.UtcNow;
            
            // Cuota vencida en el mes actual
            context.Installments.Add(new Installment
            {
                Amount = 1000m,
                PaidAmount = 0m,
                Status = "vencido",
                DueDate = now
            });

            // Cuota pagada a tiempo en el mes actual
            context.Installments.Add(new Installment
            {
                Amount = 1000m,
                PaidAmount = 1000m,
                Status = "pagado",
                DueDate = now,
                PaidDate = now.AddDays(-1)
            });

            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetOverdueRate();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var rates = Assert.IsAssignableFrom<IEnumerable<MonthlyOverdueDto>>(okResult.Value).ToList();

            // 12 meses históricos
            Assert.Equal(12, rates.Count);
            
            // La última tasa evaluada (el mes actual) debe tener 1 vencida de 2 totales = 50%
            var currentMonthRate = rates.Last();
            Assert.Equal(50.0m, currentMonthRate.OverdueRate);
        }

        [Fact]
        public async Task GetContractDistribution_ShouldGroupByStatusAndType()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new DashboardController(context, new MockIntelligenceService());

            context.Contracts.Add(new Contract { ContractNumber = "C1", Status = "activo", ContractType = "arrendamiento" });
            context.Contracts.Add(new Contract { ContractNumber = "C2", Status = "activo", ContractType = "servicio" });
            context.Contracts.Add(new Contract { ContractNumber = "C3", Status = "finalizado", ContractType = "arrendamiento" });
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetContractDistribution();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var distribution = Assert.IsType<ContractDistributionDto>(okResult.Value);

            Assert.Equal(2, distribution.ByStatus["activo"]);
            Assert.Equal(1, distribution.ByStatus["finalizado"]);
            
            Assert.Equal(2, distribution.ByType["arrendamiento"]);
            Assert.Equal(1, distribution.ByType["servicio"]);
        }

        [Fact]
        public async Task GetClientSegments_ShouldReturnSummaryFromService()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var intelligenceMock = new MockIntelligenceService();
            var controller = new DashboardController(context, intelligenceMock);

            // Act
            var actionResult = await controller.GetClientSegments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var segments = Assert.IsType<SegmentationSummaryDto>(okResult.Value);
            Assert.NotNull(segments);
        }

        [Fact]
        public async Task GetKpis_WithVaryingContractStatus_ShouldExcludeBorradorAndCalculateMorosity()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new DashboardController(context, new MockIntelligenceService());

            // Contratos
            context.Contracts.Add(new Contract { ContractNumber = "CTR-ACT", Status = "activo" });
            context.Contracts.Add(new Contract { ContractNumber = "CTR-BOR", Status = "borrador" }); // Excluido
            context.Contracts.Add(new Contract { ContractNumber = "CTR-EJE", Status = "ejecucion" });

            // Cuotas
            // 1 Cuota normal
            context.Installments.Add(new Installment { Amount = 1000m, DueDate = DateTime.Now.AddDays(10), Status = "pendiente" });
            // 1 Cuota vencida
            context.Installments.Add(new Installment { Amount = 2000m, DueDate = DateTime.Now.AddDays(-10), Status = "pendiente" });

            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetKpis();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var kpis = Assert.IsType<DashboardKpiDto>(okResult.Value);

            Assert.Equal(2, kpis.ActiveContracts); // Solo activo y ejecucion
            Assert.Equal(3000m, kpis.TotalExpectedRevenue); // 1000 + 2000
            Assert.Equal(50.0, kpis.DefaultRatePercentage); // 1 vencida de 2 totales = 50%
        }
    }
}
