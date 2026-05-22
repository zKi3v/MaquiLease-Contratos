using MaquiLease.API.Controllers;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using MaquiLease.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MaquiLease.Tests.Controllers
{
    public class PaymentsControllerTests
    {
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
    }
}
