using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using MaquiLease.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MaquiLease.Tests.Controllers
{
    public class ClientsControllerTests
    {
        [Fact]
        public async Task CreateClient_Success_ShouldSaveAndResolveSectors()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var intelligenceMock = new MockIntelligenceService();
            var controller = new ClientsController(context, intelligenceMock);

            var newClientDto = new CreateClientDto
            {
                RUC = "20123456789",
                BusinessName = "Maquinarias del Norte S.A.C.",
                ContactName = "Juan Perez",
                Email = "juan@norte.com",
                Phone = "999888777",
                Address = "Av. Industrial 123, Piura",
                Sector = "Construcción" // Debería normalizarse
            };

            // Act
            var actionResult = await controller.CreateClient(newClientDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var returnedDto = Assert.IsType<CreateClientDto>(createdResult.Value);
            Assert.Equal("20123456789", returnedDto.RUC);

            // Verificar DB
            var clientInDb = await context.Clients
                .Include(c => c.ClientSector)
                .FirstOrDefaultAsync(c => c.RUC == "20123456789");

            Assert.NotNull(clientInDb);
            Assert.True(clientInDb.IsActive);
            Assert.Equal("Juan Perez", clientInDb.ContactName);
            
            // Verificar resolución de catálogo (ResolveCatalogKeys)
            Assert.NotNull(clientInDb.ClientSector);
            Assert.Equal("construccion", clientInDb.Sector); // Normalizado a lowercase snake_case
            Assert.Equal("Construcción", clientInDb.ClientSector.Label); // Capitalizado y con tilde preservado en el label
        }

        [Fact]
        public async Task CreateClient_DuplicateRuc_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var intelligenceMock = new MockIntelligenceService();
            var controller = new ClientsController(context, intelligenceMock);

            // Insertar cliente existente
            var existingClient = new Client
            {
                RUC = "20888888888",
                BusinessName = "Existente S.A.",
                ContactName = "Pedro",
                Email = "pedro@exist.com",
                IsActive = true
            };
            context.Clients.Add(existingClient);
            await context.SaveChangesAsync();

            var duplicateClientDto = new CreateClientDto
            {
                RUC = "20888888888", // Duplicado
                BusinessName = "Intento Duplicado S.A.",
                ContactName = "Maria",
                Email = "maria@dup.com"
            };

            // Act
            var actionResult = await controller.CreateClient(duplicateClientDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Equal("Ya existe un cliente con este RUC.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetClients_ActiveOnly_ShouldReturnOnlyActiveClients()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            context.Clients.Add(new Client { RUC = "20100000001", BusinessName = "Activo A", IsActive = true });
            context.Clients.Add(new Client { RUC = "20100000002", BusinessName = "Inactivo B", IsActive = false });
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetClients();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var clients = Assert.IsAssignableFrom<IEnumerable<ClientDto>>(okResult.Value).ToList();

            Assert.Single(clients);
            Assert.Equal("20100000001", clients[0].RUC);
        }

        [Fact]
        public async Task GetClient_ExistingActive_ShouldReturnClientDto()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            var client = new Client { RUC = "20111111111", BusinessName = "Cliente Activo S.A.", IsActive = true };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetClient(client.ClientId);

            // Assert
            var okResult = Assert.IsType<ClientDto>(actionResult.Value);
            Assert.Equal("20111111111", okResult.RUC);
        }

        [Fact]
        public async Task GetClient_NonExistentOrInactive_ShouldReturnNotFound()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            var inactiveClient = new Client { RUC = "20222222222", BusinessName = "Cliente Inactivo", IsActive = false };
            context.Clients.Add(inactiveClient);
            await context.SaveChangesAsync();

            // Act
            var actionResult1 = await controller.GetClient(9999); // Inexistente
            var actionResult2 = await controller.GetClient(inactiveClient.ClientId); // Inactivo

            // Assert
            Assert.IsType<NotFoundResult>(actionResult1.Result);
            Assert.IsType<NotFoundResult>(actionResult2.Result);
        }

        [Fact]
        public async Task UpdateClient_Existing_ShouldModifyFieldsAndSave()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            var client = new Client
            {
                RUC = "20333333333",
                BusinessName = "Cliente Original",
                ContactName = "Original Contact",
                IsActive = true
            };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var updateDto = new CreateClientDto
            {
                RUC = "20333333333",
                BusinessName = "Cliente Modificado",
                ContactName = "Nuevo Contacto",
                Email = "nuevo@email.com",
                Sector = "Minería"
            };

            // Act
            var actionResult = await controller.UpdateClient(client.ClientId, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);

            var clientInDb = await context.Clients.FindAsync(client.ClientId);
            Assert.NotNull(clientInDb);
            Assert.Equal("Cliente Modificado", clientInDb.BusinessName);
            Assert.Equal("Nuevo Contacto", clientInDb.ContactName);
            Assert.Equal("nuevo@email.com", clientInDb.Email);
            Assert.Equal("mineria", clientInDb.Sector); // Sector normalizado automáticamente
        }

        [Fact]
        public async Task DeleteClient_Existing_ShouldPerformLogicalDeletion()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            var client = new Client { RUC = "20444444444", BusinessName = "Cliente A Eliminar", IsActive = true };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.DeleteClient(client.ClientId);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);

            var clientInDb = await context.Clients.FindAsync(client.ClientId);
            Assert.NotNull(clientInDb);
            Assert.False(clientInDb.IsActive); // Verificación del Borrado Lógico
        }

        [Fact]
        public async Task GetRiskScore_Success_ShouldReturnRiskScoreFromService()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var intelligenceMock = new MockIntelligenceService();
            var controller = new ClientsController(context, intelligenceMock);

            // Act
            var actionResult = await controller.GetRiskScore(123);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var riskScore = Assert.IsType<RiskScoreDto>(okResult.Value);
            Assert.Equal(123, riskScore.ClientId);
            Assert.Equal(15m, riskScore.Score);
            Assert.Equal("Bajo", riskScore.Category);
        }

        [Fact]
        public async Task CreateClient_UnconventionalData_ShouldSaveLiterallyWithoutCrashing()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            // Usando datos poco convencionales: números en campos de texto, etc.
            var badDataDto = new CreateClientDto
            {
                RUC = "ABC-1234-999", // RUC alfanumérico no convencional
                BusinessName = "999999999", // Solo números donde va Razón Social (texto)
                ContactName = "12345", // Solo números donde va Nombre de Contacto (texto)
                Email = "999@999.999",
                Phone = "no-tengo-telefono", // Texto donde va número telefónico
                Address = "0", // Un solo dígito
                Sector = "Construccion"
            };

            // Act
            var actionResult = await controller.CreateClient(badDataDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var clientInDb = await context.Clients.FirstOrDefaultAsync(c => c.RUC == "ABC-1234-999");

            Assert.NotNull(clientInDb);
            Assert.Equal("999999999", clientInDb.BusinessName);
            Assert.Equal("12345", clientInDb.ContactName);
            Assert.Equal("no-tengo-telefono", clientInDb.Phone);
        }
    }
}
