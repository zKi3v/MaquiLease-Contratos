using System;
using System.Collections.Generic;

namespace MaquiLease.API.Models.DTOs
{
    public class ContractDto
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int? AssetId { get; set; }
        public string? AssetCode { get; set; }
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentFrequency { get; set; } = string.Empty;
        public decimal InitialPayment { get; set; }
        public string LeasingTerms { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? SignatureHash { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public List<InstallmentDto> Installments { get; set; } = new();
    }

    public class CreateContractDto
    {
        public int ClientId { get; set; }
        public int? AssetId { get; set; }
        public int? ServiceId { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "PEN";
        public string PaymentFrequency { get; set; } = "mensual";
        public int NumberOfInstallments { get; set; }
        public decimal InitialPayment { get; set; }
        public string LeasingTerms { get; set; } = string.Empty;
        public string? SignatureHash { get; set; }
    }
    
    public class InstallmentDto
    {
        public int InstallmentId { get; set; }
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }
    }
}
