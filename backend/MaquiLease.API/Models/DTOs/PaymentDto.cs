using System;

namespace MaquiLease.API.Models.DTOs
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int InstallmentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string? DocumentNumber { get; set; }
    }

    public class CreatePaymentDto
    {
        public int InstallmentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "transferencia";
        public string? ReferenceNumber { get; set; }
        public string DocumentType { get; set; } = "boleta";
    }
}
