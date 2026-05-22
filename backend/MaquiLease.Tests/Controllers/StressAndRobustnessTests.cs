using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using MaquiLease.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MaquiLease.Tests.Controllers
{
    public class StressAndRobustnessTests
    {
        // ═══════════════════════════════════════════════════════════
        // 1. REGISTRO DE CLIENTES — CASOS EXTREMOS Y ENTRADAS CORRUPTAS
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateClient_SpecialCharactersInSector_ShouldNormalizeWithoutCrashing()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var intelligenceMock = new MockIntelligenceService();
            var controller = new ClientsController(context, intelligenceMock);

            var newClientDto = new CreateClientDto
            {
                RUC = "20111111111",
                BusinessName = "Industrial Emojis S.A.",
                ContactName = "Pedro Ortiz",
                Email = "pedro@emojis.com",
                Sector = "  Míneria  Pesáda  y  Siderúrgica!!! 🚀 " // Entrada sucia con acentos, dobles espacios y caracteres especiales
            };

            // Act
            var actionResult = await controller.CreateClient(newClientDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            
            // Verificar normalización en DB
            var clientInDb = await context.Clients
                .Include(c => c.ClientSector)
                .FirstOrDefaultAsync(c => c.RUC == "20111111111");

            Assert.NotNull(clientInDb);
            Assert.NotNull(clientInDb.ClientSector);
            
            // "Míneria  Pesáda  y  Siderúrgica!!! 🚀" -> acentos eliminados, minúsculas, espacios reemplazados por "_"
            Assert.Contains("mineria", clientInDb.Sector);
            Assert.Contains("siderurgica", clientInDb.Sector);
            Assert.Equal("mineria__pesada__y__siderurgica!!!_🚀", clientInDb.Sector); // Normalizado
            Assert.Equal("Míneria  Pesáda  y  Siderúrgica!!! 🚀", clientInDb.ClientSector.Label); // Label original preservado
        }

        // ═══════════════════════════════════════════════════════════
        // 2. GENERACIÓN DE CONTRATOS — MONTOS ANÓMALOS Y CUOTAS EN 0
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateContract_NegativeTotalAmount_ShouldNotCreateInstallments()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20555555555", BusinessName = "Cliente Stress 1" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var badContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "arrendamiento",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                TotalAmount = -5000m, // MONTO NEGATIVO ANÓMALO
                NumberOfInstallments = 3,
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(badContractDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync(c => c.ClientId == client.ClientId);

            Assert.NotNull(contractInDb);
            Assert.Equal(-5000m, contractInDb.TotalAmount);
            Assert.Empty(contractInDb.Installments); // No debería haber generado cuotas al ser el monto <= 0
        }

        [Fact]
        public async Task CreateContract_InitialPaymentExceedsTotalAmount_ShouldNotCreateInstallments()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20444444444", BusinessName = "Cliente Stress 2" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var badContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "arrendamiento",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                TotalAmount = 2000m,
                InitialPayment = 2500m, // CUOTA INICIAL SUPERA AL MONTO TOTAL!
                NumberOfInstallments = 3,
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(badContractDto);

            // Assert
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync(c => c.ClientId == client.ClientId);

            Assert.NotNull(contractInDb);
            Assert.Empty(contractInDb.Installments); // amountToFinance = 2000 - 2500 = -500 <= 0 (No se generan cuotas)
        }

        [Fact]
        public async Task CreateContract_ZeroInstallments_ShouldNotGenerateSchedule()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20333333333", BusinessName = "Cliente Stress 3" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var badContractDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "arrendamiento",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                TotalAmount = 3000m,
                NumberOfInstallments = 0, // CERO CUOTAS ANÓMALO
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(badContractDto);

            // Assert
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync(c => c.ClientId == client.ClientId);

            Assert.NotNull(contractInDb);
            Assert.Empty(contractInDb.Installments); // 0 cuotas
        }

        // ═══════════════════════════════════════════════════════════
        // 3. REGISTRO DE PAGOS — ABONOS INVÁLIDOS Y DOBLE PAGO COMPLETO
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task RegisterPayment_ZeroOrNegativeAmount_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var contract = new Contract { ContractNumber = "CTR-STRESS-PAY-1", TotalAmount = 5000m, Currency = "PEN" };
            context.Contracts.Add(contract);

            var installment = new Installment 
            { 
                Amount = 1000m, 
                PenaltyAmount = 0m, 
                Status = "pendiente",
                Contract = contract
            };
            context.Installments.Add(installment);
            await context.SaveChangesAsync();

            var zeroPaymentDto = new CreatePaymentDto { InstallmentId = installment.InstallmentId, Amount = 0m };
            var negativePaymentDto = new CreatePaymentDto { InstallmentId = installment.InstallmentId, Amount = -100m };

            // Act
            var resultZero = await controller.RegisterPayment(zeroPaymentDto);
            var resultNegative = await controller.RegisterPayment(negativePaymentDto);

            // Assert
            var badZero = Assert.IsType<BadRequestObjectResult>(resultZero);
            var badNeg = Assert.IsType<BadRequestObjectResult>(resultNegative);

            Assert.Equal("El monto debe ser mayor a 0.", badZero.Value);
            Assert.Equal("El monto debe ser mayor a 0.", badNeg.Value);
        }

        [Fact]
        public async Task RegisterPayment_PayOnAlreadyFullyPaidInstallment_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var contract = new Contract { ContractNumber = "CTR-STRESS-PAY-2", TotalAmount = 5000m, Currency = "PEN" };
            context.Contracts.Add(contract);

            var installment = new Installment
            {
                Amount = 1000m,
                PenaltyAmount = 0m,
                PaidAmount = 1000m, // YA PAGADO COMPLETO
                Status = "pagado",
                Contract = contract
            };
            context.Installments.Add(installment);
            await context.SaveChangesAsync();

            var paymentDto = new CreatePaymentDto
            {
                InstallmentId = installment.InstallmentId,
                Amount = 200m // Intento de abonar a cuota ya saldada
            };

            // Act
            var actionResult = await controller.RegisterPayment(paymentDto);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal("El monto supera el saldo pendiente de la cuota.", badResult.Value);
        }

        // ═══════════════════════════════════════════════════════════
        // 4. BI Y DASHBOARD — CÁLCULOS SOBRE BASE DE DATOS TOTALMENTE VACÍA
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetKpis_EmptyDatabase_ShouldNotDivideByZeroOrCrash()
        {
            // Arrange
            using var context = TestDbContextFactory.Create(); // DB TOTALMENTE VACÍA
            var intelligenceMock = new MockIntelligenceService();
            var controller = new DashboardController(context, intelligenceMock);

            // Act
            var actionResult = await controller.GetKpis();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var kpiDto = Assert.IsType<DashboardKpiDto>(okResult.Value);

            Assert.Equal(0, kpiDto.TotalAssets);
            Assert.Equal(0, kpiDto.ActiveContracts);
            Assert.Equal(0m, kpiDto.TotalExpectedRevenue);
            Assert.Equal(0m, kpiDto.TotalCollectedRevenue);
            Assert.Equal(0.0, kpiDto.DefaultRatePercentage); // Evitó la división por cero con éxito
        }

        // ═══════════════════════════════════════════════════════════
        // 5. NUEVOS CASOS NO CONVENCIONALES Y ERRORES ADICIONALES
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateAsset_DuplicateCode_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new AssetsController(context);

            var existingAsset = new Asset { Code = "ACT-DUP-99", Name = "Excavadora Existente", Category = "excavadoras", Brand = "CAT", Status = "disponible" };
            context.Assets.Add(existingAsset);
            await context.SaveChangesAsync();

            var newAssetDto = new CreateAssetDto
            {
                Code = "ACT-DUP-99", // CÓDIGO DUPLICADO
                Name = "Excavadora Nueva",
                Category = "excavadoras",
                Brand = "CAT",
                Status = "disponible"
            };

            // Act
            var actionResult = await controller.CreateAsset(newAssetDto);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Equal("Ya existe un activo con este Codigo.", badResult.Value);
        }

        [Fact]
        public async Task CreateAsset_SpecialCharactersAndSpacesInBrandCategory_ShouldNormalizeSuccessfully()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new AssetsController(context);

            var newAssetDto = new CreateAssetDto
            {
                Code = "ACT-SPEC-01",
                Name = "Cargador Frontal",
                Category = "   Excavadoras Pesadas 🌊   ", // Espacios, mayúsculas y emoji
                Brand = "   Caterpillar!!! 🚜💨   ", // Espacios y emojis múltiples
                Status = "disponible",
                PurchasePriceUSD = 85000m
            };

            // Act
            var actionResult = await controller.CreateAsset(newAssetDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var assetInDb = await context.Assets
                .Include(a => a.AssetCategory)
                .Include(a => a.AssetBrand)
                .FirstOrDefaultAsync(a => a.Code == "ACT-SPEC-01");

            Assert.NotNull(assetInDb);
            Assert.NotNull(assetInDb.AssetCategory);
            Assert.NotNull(assetInDb.AssetBrand);

            // Normalizaciones aplicadas por el AppDbContext
            // Trim() y NormalizeString() para Category:
            Assert.Equal("excavadoras_pesadas_🌊", assetInDb.Category);
            Assert.Equal("Excavadoras Pesadas 🌊", assetInDb.AssetCategory.Label);

            // Trim() y capitalización para Brand (el diseño conserva los espacios en las marcas, solo hace trim):
            Assert.Equal("Caterpillar!!! 🚜💨", assetInDb.Brand);
            Assert.Equal("Caterpillar!!! 🚜💨", assetInDb.AssetBrand.Label);
        }

        [Fact]
        public async Task CreateContract_StartDateAfterEndDate_ShouldCreateSuccessfullyWithoutThrowing()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ContractsController(context);

            var client = new Client { RUC = "20999999999", BusinessName = "Cliente Fechas Invertidas" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var badDatesDto = new CreateContractDto
            {
                ClientId = client.ClientId,
                ContractType = "arrendamiento",
                StartDate = DateTime.UtcNow.AddYears(1), // FECHA INICIO EN EL FUTURO
                EndDate = DateTime.UtcNow,               // FECHA FIN EN EL PASADO (Invertidas!)
                TotalAmount = 3000m,
                NumberOfInstallments = 3,
                Currency = "PEN"
            };

            // Act
            var actionResult = await controller.CreateContract(badDatesDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var contractInDb = await context.Contracts
                .Include(c => c.Installments)
                .FirstOrDefaultAsync(c => c.ClientId == client.ClientId);

            Assert.NotNull(contractInDb);
            Assert.Equal(3, contractInDb.Installments.Count); // El procesador de cronogramas generó las cuotas igualmente sin crashear.
            
            // La primera cuota vence en StartDate + 1 mes
            var firstInstallment = contractInDb.Installments.OrderBy(i => i.InstallmentNumber).First();
            Assert.True(firstInstallment.DueDate > contractInDb.EndDate); // Vence después del fin de contrato
        }

        [Fact]
        public async Task RegisterPayment_AstronomicalAmount_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var contract = new Contract { ContractNumber = "CTR-OVERFLOW", TotalAmount = 5000m, Currency = "PEN" };
            context.Contracts.Add(contract);

            var installment = new Installment
            {
                Amount = 1000m,
                PenaltyAmount = 0m,
                Status = "pendiente",
                Contract = contract
            };
            context.Installments.Add(installment);
            await context.SaveChangesAsync();

            var overflowPaymentDto = new CreatePaymentDto
            {
                InstallmentId = installment.InstallmentId,
                Amount = 999999999999999999m // MONTO GIGANTESCO / DESBORDAMIENTO
            };

            // Act
            var actionResult = await controller.RegisterPayment(overflowPaymentDto);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal("El monto supera el saldo pendiente de la cuota.", badResult.Value);
        }

        [Fact]
        public async Task SaveEntities_SqlInjectionInputs_ShouldBeParametizedAndSavedLiterally()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            var sqlInjectionDto = new CreateClientDto
            {
                RUC = "20777777777",
                BusinessName = "'; DROP TABLE Clients; --", // SQL INJECTION INTENTADO
                ContactName = "Juan Malicioso",
                Email = "malicioso@sqli.com",
                Sector = "Construccion"
            };

            // Act
            var actionResult = await controller.CreateClient(sqlInjectionDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            
            // Verificar que el registro se guardó literalmente y la tabla sigue intacta
            var clientInDb = await context.Clients.FirstOrDefaultAsync(c => c.RUC == "20777777777");
            Assert.NotNull(clientInDb);
            Assert.Equal("'; DROP TABLE Clients; --", clientInDb.BusinessName); // Guardado seguro literal
            
            var allClientsCount = await context.Clients.CountAsync();
            Assert.True(allClientsCount > 0); // La tabla no fue borrada
        }

        // ═══════════════════════════════════════════════════════════
        // 6. ERRORES DE TIPO DE DATOS Y ERRORES DE DESERIALIZACIÓN (JSON)
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void JsonDeserialization_InvalidNumberForClientId_ShouldThrowJsonException()
        {
            // Arrange
            // Simular un JSON enviado por el cliente con texto "XYZ" en vez de un número entero en clientId
            string badJson = "{\"clientId\": \"XYZ\", \"contractType\": \"arrendamiento\", \"totalAmount\": 5000}";

            // Act & Assert
            // Probar que el deserializador integrado de .NET Core (System.Text.Json) lanza una excepción de deserialización
            Assert.Throws<System.Text.Json.JsonException>(() => 
                System.Text.Json.JsonSerializer.Deserialize<CreateContractDto>(badJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            );
        }

        [Fact]
        public void JsonDeserialization_InvalidDecimalForTotalAmount_ShouldThrowJsonException()
        {
            // Arrange
            // Simular JSON con texto "mil-soles" en vez de un número decimal en totalAmount
            string badJson = "{\"clientId\": 1, \"contractType\": \"arrendamiento\", \"totalAmount\": \"mil-soles\"}";

            // Act & Assert
            Assert.Throws<System.Text.Json.JsonException>(() => 
                System.Text.Json.JsonSerializer.Deserialize<CreateContractDto>(badJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            );
        }

        [Fact]
        public void JsonDeserialization_InvalidDateFormatForStartDate_ShouldThrowJsonException()
        {
            // Arrange
            // Simular JSON con texto "hoy-en-la-tarde" en vez de una fecha válida en startDate
            string badJson = "{\"clientId\": 1, \"startDate\": \"hoy-en-la-tarde\", \"totalAmount\": 5000}";

            // Act & Assert
            Assert.Throws<System.Text.Json.JsonException>(() => 
                System.Text.Json.JsonSerializer.Deserialize<CreateContractDto>(badJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            );
        }

        [Fact]
        public async Task CreateClient_NumericBusinessNameAndAlphanumericRuc_ShouldSaveLiterallyWithoutCrashing()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new ClientsController(context, new MockIntelligenceService());

            // Datos poco convencionales: 
            // - RUC con texto (letras "ABC")
            // - BusinessName compuesto únicamente por números ("1234567890") en vez de letras
            var newClientDto = new CreateClientDto
            {
                RUC = "20A123B45C9", // Alfanumérico poco convencional
                BusinessName = "1234567890", // Solo números donde debería ir texto
                ContactName = "999888777", // Teléfono o número donde debería ir nombre
                Email = "abc@def.ghi"
            };

            // Act
            var actionResult = await controller.CreateClient(newClientDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var clientInDb = await context.Clients.FirstOrDefaultAsync(c => c.RUC == "20A123B45C9");

            Assert.NotNull(clientInDb);
            // Comprobar que los tipos string guardan la representación literal del dato de forma correcta sin caerse
            Assert.Equal("1234567890", clientInDb.BusinessName);
            Assert.Equal("20A123B45C9", clientInDb.RUC);
            Assert.Equal("999888777", clientInDb.ContactName);
        }

        [Fact]
        public async Task CreateAsset_NumbersAsNotesAndTextsAsPrices_ShouldRejectOrHandleGracefully()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new AssetsController(context);

            // Intentar crear un activo con números en campos de texto descriptivos
            var badAssetDto = new CreateAssetDto
            {
                Code = "ACT-NUMS-01",
                Name = "999999", // Números donde va texto
                Category = "excavadoras",
                Brand = "CAT",
                Notes = "88888888", // Notas con solo números
                PurchasePriceUSD = 0m
            };

            // Act
            var actionResult = await controller.CreateAsset(badAssetDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var assetInDb = await context.Assets.FirstOrDefaultAsync(a => a.Code == "ACT-NUMS-01");

            Assert.NotNull(assetInDb);
            Assert.Equal("999999", assetInDb.Name);
            Assert.Equal("88888888", assetInDb.Notes);
        }
    }
}
