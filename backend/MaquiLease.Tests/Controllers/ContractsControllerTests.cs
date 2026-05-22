using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MaquiLease.Tests.Controllers
{
    public class ContractsControllerTests
    {
        [Fact]
        public async Task CreateContract_Success_Leasing_GeneratesInstallmentsAndUpdatesAsset()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            // Crear cliente de prueba
            var client = new Client
            {
                RUC = "20999999999",
                BusinessName = "Cliente Construcciones",
                IsActive = true
            };
            context.Clients.Add(client);

            // Crear activo disponible
            var asset = new Asset
            {
                Code = "ACT-001",
                Name = "Excavadora CAT 320",
                Status = "disponible"
            };
            context.Assets.Add(asset);
            await context.SaveChangesAsync();

            var newContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                AssetId = asset.AssetId,
                ContractType = "arrendamiento_financiero",
                StartDate = new DateTime(2026, 06, 01),
                EndDate = new DateTime(2026, 09, 01),
                TotalAmount = 6000m,
                InitialPayment = 0m,
                NumberOfInstallments = 3,
                Currency = "USD",
                SignatureHash = "signature_hash_test_123"
            };

            // Act
            var actionResult = await controller.CreateContract(newContractDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            
            // Verificar Contrato en DB
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync();

            Assert.NotNull(contractInDb);
            Assert.StartsWith("CTR-", contractInDb.ContractNumber);
            Assert.Equal("activo", contractInDb.Status);
            Assert.Equal("signature_hash_test_123", contractInDb.SignatureHash);

            // Verificar Cronograma (3 Cuotas)
            Assert.Equal(3, contractInDb.Installments.Count);
            
            var insts = contractInDb.Installments.OrderBy(i => i.InstallmentNumber).ToList();
            Assert.Equal(1, insts[0].InstallmentNumber);
            Assert.Equal(2000m, insts[0].Amount);
            Assert.Equal(new DateTime(2026, 07, 01), insts[0].DueDate);
            Assert.Equal("pendiente", insts[0].Status);

            Assert.Equal(2, insts[1].InstallmentNumber);
            Assert.Equal(2000m, insts[1].Amount);
            Assert.Equal(new DateTime(2026, 08, 01), insts[1].DueDate);

            Assert.Equal(3, insts[2].InstallmentNumber);
            Assert.Equal(2000m, insts[2].Amount);
            Assert.Equal(new DateTime(2026, 09, 01), insts[2].DueDate);

            // Verificar actualización de Estado del Activo (alquilado)
            var assetInDb = await context.Assets.FindAsync(asset.AssetId);
            Assert.NotNull(assetInDb);
            Assert.Equal("alquilado", assetInDb.Status);
        }

        [Fact]
        public async Task CreateContract_CentavosAdjustment_CorrectLastInstallment()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client
            {
                RUC = "20777777777",
                BusinessName = "Cliente Centavos",
                IsActive = true
            };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var newContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "servicio_mantenimiento", // Sin Activo
                StartDate = new DateTime(2026, 06, 01),
                EndDate = new DateTime(2026, 09, 01),
                TotalAmount = 1000m, // 1000 dividido entre 3 = 333.33, última cuota debe ser 333.34
                InitialPayment = 0m,
                NumberOfInstallments = 3,
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(newContractDto);

            // Assert
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync();

            Assert.NotNull(contractInDb);
            var insts = contractInDb.Installments.OrderBy(i => i.InstallmentNumber).ToList();
            
            Assert.Equal(3, insts.Count);
            Assert.Equal(333.33m, insts[0].Amount);
            Assert.Equal(333.33m, insts[1].Amount);
            Assert.Equal(333.34m, insts[2].Amount); // Ajustado de centavos: 1000 - (333.33 * 2)
            
            Assert.Equal(1000m, insts.Sum(i => i.Amount)); // Sumatoria cuadra exactamente con el TotalAmount
        }

        [Fact]
        public async Task GetContracts_List_ShouldReturnOrderedByCreatedAtDescending()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20111111111", BusinessName = "Cliente A" };
            context.Clients.Add(client);

            var contract1 = new Contract { Client = client, ContractNumber = "CTR-1", TotalAmount = 5000, CreatedAt = DateTime.UtcNow.AddMinutes(-10), Status = "activo" };
            var contract2 = new Contract { Client = client, ContractNumber = "CTR-2", TotalAmount = 10000, CreatedAt = DateTime.UtcNow, Status = "activo" };
            context.Contracts.Add(contract1);
            context.Contracts.Add(contract2);
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetContracts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var contracts = Assert.IsAssignableFrom<IEnumerable<ContractDto>>(okResult.Value).ToList();

            Assert.Equal(2, contracts.Count);
            // Orden descendente de creación
            Assert.Equal("CTR-2", contracts[0].ContractNumber);
            Assert.Equal("CTR-1", contracts[1].ContractNumber);
        }

        [Fact]
        public async Task GetContract_Existing_ShouldReturnDetailsAndInstallments()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20222222222", BusinessName = "Cliente B" };
            context.Clients.Add(client);

            var contract = new Contract { Client = client, ContractNumber = "CTR-3", TotalAmount = 3000, CreatedAt = DateTime.UtcNow, Status = "activo" };
            contract.Installments.Add(new Installment { InstallmentNumber = 1, Amount = 1500, Status = "pendiente", DueDate = DateTime.UtcNow.AddMonths(1) });
            contract.Installments.Add(new Installment { InstallmentNumber = 2, Amount = 1500, Status = "pendiente", DueDate = DateTime.UtcNow.AddMonths(2) });

            context.Contracts.Add(contract);
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetContract(contract.ContractId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var detailDto = Assert.IsType<ContractDto>(okResult.Value);

            Assert.Equal("CTR-3", detailDto.ContractNumber);
            Assert.Equal(2, detailDto.Installments.Count);
            Assert.Equal(1, detailDto.Installments[0].InstallmentNumber);
            Assert.Equal(2, detailDto.Installments[1].InstallmentNumber);
        }

        [Fact]
        public async Task GetContract_NonExistent_ShouldReturnNotFound()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            // Act
            var actionResult = await controller.GetContract(9999);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task CreateContract_WithInitialPayment_ShouldFinanceRemainingAmountOnly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20444444444", BusinessName = "Cliente C" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var newContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "arrendamiento",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(2),
                TotalAmount = 5000m,
                InitialPayment = 2000m, // Cuota inicial
                NumberOfInstallments = 2,
                Currency = "USD"
            };

            // Act
            var actionResult = await controller.CreateContract(newContractDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync();

            Assert.NotNull(contractInDb);
            // Saldo a financiar = 5000 - 2000 = 3000
            // 2 cuotas de 1500 c/u
            Assert.Equal(2, contractInDb.Installments.Count);
            Assert.All(contractInDb.Installments, i => Assert.Equal(1500m, i.Amount));
        }

        [Fact]
        public async Task CreateContract_WithServiceAndNoAsset_ShouldCreateSuccessfully()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20555555555", BusinessName = "Cliente D" };
            var service = new Service { Code = "SRV-TEST", Name = "Mantenimiento Preventivo Mensual", BasePrice = 1200 };
            context.Clients.Add(client);
            context.Services.Add(service);
            await context.SaveChangesAsync();

            var newContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ServiceId = service.ServiceId,
                ContractType = "servicio_mantenimiento", // Contrato de servicio
                AssetId = null, // Sin maquinaria
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(2),
                TotalAmount = 1200m,
                InitialPayment = 0m,
                NumberOfInstallments = 2,
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(newContractDto);

            // Assert
            Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var contractInDb = await context.Contracts.FirstOrDefaultAsync();
            Assert.NotNull(contractInDb);
            Assert.Null(contractInDb.AssetId);
            Assert.Equal(service.ServiceId, contractInDb.ServiceId);
        }

        [Fact]
        public async Task CreateContract_ZeroInstallments_ShouldNotGenerateCronograma()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20666666666", BusinessName = "Cliente E" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var newContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "arrendamiento",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                TotalAmount = 4000m,
                InitialPayment = 0m,
                NumberOfInstallments = 0, // Cero cuotas
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(newContractDto);

            // Assert
            Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync();

            Assert.NotNull(contractInDb);
            Assert.Empty(contractInDb.Installments); // Sin cronograma
        }

        [Fact]
        public async Task CreateContract_NegativeInstallments_ShouldNotGenerateCronograma()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20888888888", BusinessName = "Cliente F" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var newContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "arrendamiento",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                TotalAmount = 4000m,
                InitialPayment = 0m,
                NumberOfInstallments = -5, // Cuotas negativas (dato erróneo / de riesgo)
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(newContractDto);

            // Assert
            Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync();

            Assert.NotNull(contractInDb);
            Assert.Empty(contractInDb.Installments); // Manejado de forma segura (sin cronograma / no crashea)
        }
    }
}
