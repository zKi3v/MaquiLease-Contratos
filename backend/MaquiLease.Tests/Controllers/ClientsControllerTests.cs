using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using MaquiLease.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    }
}
