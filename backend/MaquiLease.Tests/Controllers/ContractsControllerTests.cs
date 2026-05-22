using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
    }
}
