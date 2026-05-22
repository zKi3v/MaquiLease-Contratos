using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.API.Services;
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
    public class PaymentsControllerTests
    {
        static PaymentsControllerTests()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        [Fact]
        public async Task RegisterPayment_FullPayment_ShouldSetStatusToPagado()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var contract = new Contract
            {
                ContractNumber = "CTR-TEST-PAY",
                TotalAmount = 5000m,
                Currency = "PEN"
            };
            context.Contracts.Add(contract);

            var installment = new Installment
            {
                InstallmentNumber = 1,
                Amount = 1000m,
                PenaltyAmount = 150m, // 150 de mora acumulada
                PaidAmount = 0m,
                Status = "vencido",
                DueDate = DateTime.UtcNow.AddMonths(-1),
                Contract = contract
            };
            context.Installments.Add(installment);
            await context.SaveChangesAsync();

            var paymentDto = new CreatePaymentDto
            {
                InstallmentId = installment.InstallmentId,
                Amount = 1150m, // Monto exacto pendiente (1000 + 150)
                PaymentMethod = "transferencia",
                ReferenceNumber = "REF-999999",
                DocumentType = "boleta"
            };

            // Act
            var actionResult = await controller.RegisterPayment(paymentDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            
            // Verificar cuota actualizada
            var instInDb = await context.Installments.FindAsync(installment.InstallmentId);
            Assert.NotNull(instInDb);
            Assert.Equal(1150m, instInDb.PaidAmount);
            Assert.Equal("pagado", instInDb.Status);
            Assert.NotNull(instInDb.PaidDate);

            // Verificar Pago en DB
            var paymentInDb = await context.Payments.FirstOrDefaultAsync();
            Assert.NotNull(paymentInDb);
            Assert.Equal(installment.InstallmentId, paymentInDb.InstallmentId);
            Assert.Equal(1150m, paymentInDb.Amount);
            Assert.Equal("transferencia", paymentInDb.PaymentMethod);
            Assert.StartsWith("DOC-", paymentInDb.DocumentNumber);
        }

        [Fact]
        public async Task RegisterPayment_PartialPayment_ShouldSetStatusToParcial()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var contract = new Contract
            {
                ContractNumber = "CTR-TEST-PAY-2",
                TotalAmount = 3000m,
                Currency = "PEN"
            };
            context.Contracts.Add(contract);

            var installment = new Installment
            {
                InstallmentNumber = 1,
                Amount = 1000m,
                PenaltyAmount = 0m,
                PaidAmount = 0m,
                Status = "pendiente",
                DueDate = DateTime.UtcNow.AddMonths(1),
                Contract = contract
            };
            context.Installments.Add(installment);
            await context.SaveChangesAsync();

            var paymentDto = new CreatePaymentDto
            {
                InstallmentId = installment.InstallmentId,
                Amount = 400m, // Abono parcial
                PaymentMethod = "efectivo",
                ReferenceNumber = "REF-1111",
                DocumentType = "recibo"
            };

            // Act
            var actionResult = await controller.RegisterPayment(paymentDto);

            // Assert
            Assert.IsType<OkObjectResult>(actionResult);

            var instInDb = await context.Installments.FindAsync(installment.InstallmentId);
            Assert.NotNull(instInDb);
            Assert.Equal(400m, instInDb.PaidAmount);
            Assert.Equal("parcial", instInDb.Status);
        }

        [Fact]
        public async Task RegisterPayment_Overpayment_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var contract = new Contract
            {
                ContractNumber = "CTR-TEST-PAY-3",
                TotalAmount = 3000m,
                Currency = "PEN"
            };
            context.Contracts.Add(contract);

            var installment = new Installment
            {
                InstallmentNumber = 1,
                Amount = 1000m,
                PenaltyAmount = 0m,
                PaidAmount = 0m,
                Status = "pendiente",
                DueDate = DateTime.UtcNow.AddMonths(1),
                Contract = contract
            };
            context.Installments.Add(installment);
            await context.SaveChangesAsync();

            var paymentDto = new CreatePaymentDto
            {
                InstallmentId = installment.InstallmentId,
                Amount = 1200m, // Pago que supera los 1000 pendientes
                PaymentMethod = "tarjeta"
            };

            // Act
            var actionResult = await controller.RegisterPayment(paymentDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal("El monto supera el saldo pendiente de la cuota.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetPayments_Empty_ShouldReturnEmptyList()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            // Act
            var actionResult = await controller.GetPayments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var payments = Assert.IsAssignableFrom<IEnumerable<PaymentDto>>(okResult.Value);
            Assert.Empty(payments);
        }

        [Fact]
        public async Task GetPayments_Existing_ShouldReturnOrderedByPaymentDate()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var payment1 = new Payment { Amount = 100, PaymentDate = DateTime.UtcNow.AddMinutes(-5), PaymentMethod = "efectivo" };
            var payment2 = new Payment { Amount = 200, PaymentDate = DateTime.UtcNow, PaymentMethod = "transferencia" };

            context.Payments.Add(payment1);
            context.Payments.Add(payment2);
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetPayments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var payments = Assert.IsAssignableFrom<IEnumerable<PaymentDto>>(okResult.Value).ToList();

            Assert.Equal(2, payments.Count);
            // Orden descendente por fecha de pago
            Assert.Equal(200, payments[0].Amount);
            Assert.Equal(100, payments[1].Amount);
        }

        [Fact]
        public async Task RegisterPayment_NonExistentInstallment_ShouldReturnNotFound()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var paymentDto = new CreatePaymentDto
            {
                InstallmentId = 9999, // Inexistente
                Amount = 500
            };

            // Act
            var actionResult = await controller.RegisterPayment(paymentDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Equal("Cuota no encontrada.", notFoundResult.Value);
        }

        [Fact]
        public async Task RegisterPayment_ZeroOrNegativeAmount_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);

            var contract = new Contract { ContractNumber = "CTR-1", TotalAmount = 5000 };
            var installment = new Installment { Amount = 1000, Contract = contract };
            context.Installments.Add(installment);
            await context.SaveChangesAsync();

            var zeroPayment = new CreatePaymentDto { InstallmentId = installment.InstallmentId, Amount = 0 };
            var negativePayment = new CreatePaymentDto { InstallmentId = installment.InstallmentId, Amount = -100 };

            // Act
            var resultZero = await controller.RegisterPayment(zeroPayment);
            var resultNeg = await controller.RegisterPayment(negativePayment);

            // Assert
            var badZero = Assert.IsType<BadRequestObjectResult>(resultZero);
            var badNeg = Assert.IsType<BadRequestObjectResult>(resultNeg);
            Assert.Equal("El monto debe ser mayor a 0.", badZero.Value);
            Assert.Equal("El monto debe ser mayor a 0.", badNeg.Value);
        }

        [Fact]
        public async Task DownloadReceipt_Existing_ShouldReturnPdfFile()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);
            var pdfService = new PdfService();

            var client = new Client { RUC = "20111111111", BusinessName = "Cliente PDF S.A." };
            var contract = new Contract { ContractNumber = "CTR-PDF-1", Client = client, TotalAmount = 5000 };
            var installment = new Installment { Amount = 1000, Contract = contract };
            var payment = new Payment
            {
                Installment = installment,
                Amount = 1000,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "transferencia",
                DocumentNumber = "DOC-12345"
            };

            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.DownloadReceipt(payment.PaymentId, pdfService);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(actionResult);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("Comprobante_DOC-12345.pdf", fileResult.FileDownloadName);
            Assert.NotEmpty(fileResult.FileContents); // El byte[] del PDF no está vacío
        }

        [Fact]
        public async Task DownloadReceipt_NonExistentPayment_ShouldReturnNotFound()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var controller = new PaymentsController(context);
            var pdfService = new PdfService();

            // Act
            var actionResult = await controller.DownloadReceipt(9999, pdfService);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Equal("Pago no encontrado", notFoundResult.Value);
        }
    }
}
