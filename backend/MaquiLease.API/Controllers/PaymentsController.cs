using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments()
        {
            var payments = await _context.Payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    InstallmentId = p.InstallmentId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod,
                    ReferenceNumber = p.ReferenceNumber,
                    DocumentType = p.DocumentType,
                    DocumentNumber = p.DocumentNumber
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterPayment(CreatePaymentDto dto)
        {
            var installment = await _context.Installments
                .Include(i => i.Contract)
                .FirstOrDefaultAsync(i => i.InstallmentId == dto.InstallmentId);

            if (installment == null) return NotFound("Cuota no encontrada.");

            decimal pendingAmount = (installment.Amount + installment.PenaltyAmount) - installment.PaidAmount;
            
            if (dto.Amount <= 0) return BadRequest("El monto debe ser mayor a 0.");
            if (dto.Amount > pendingAmount) return BadRequest("El monto supera el saldo pendiente de la cuota.");

            var payment = new Payment
            {
                InstallmentId = dto.InstallmentId,
                Amount = dto.Amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = dto.PaymentMethod,
                ReferenceNumber = dto.ReferenceNumber,
                DocumentType = dto.DocumentType,
                DocumentNumber = "DOC-" + DateTime.UtcNow.Ticks.ToString().Substring(8)
            };

            _context.Payments.Add(payment);

            installment.PaidAmount += dto.Amount;
            installment.PaidDate = DateTime.UtcNow;

            if (installment.PaidAmount >= (installment.Amount + installment.PenaltyAmount))
            {
                installment.Status = "pagado";
            }
            else
            {
                installment.Status = "parcial";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Pago registrado exitosamente", paymentId = payment.PaymentId });
        }

        [HttpGet("{id}/receipt")]
        public async Task<IActionResult> DownloadReceipt(int id, [FromServices] Services.PdfService pdfService)
        {
            var payment = await _context.Payments
                .Include(p => p.Installment)
                    .ThenInclude(i => i.Contract)
                        .ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound("Pago no encontrado");

            var clientName = payment.Installment.Contract.Client.BusinessName;
            var contractNumber = payment.Installment.Contract.ContractNumber;

            var pdfBytes = pdfService.GeneratePaymentReceipt(
                clientName, 
                contractNumber, 
                payment.PaymentMethod, 
                payment.Amount, 
                payment.PaymentDate, 
                payment.DocumentNumber ?? "DOC-UNKNOWN"
            );

            return File(pdfBytes, "application/pdf", $"Comprobante_{payment.DocumentNumber}.pdf");
        }
    }
}
