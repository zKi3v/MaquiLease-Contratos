using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using MaquiLease.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using System;
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
    }
}
