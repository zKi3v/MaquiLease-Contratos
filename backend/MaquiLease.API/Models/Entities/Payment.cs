using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaquiLease.API.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        
        public int InstallmentId { get; set; }
        [ForeignKey("InstallmentId")]
        public Installment Installment { get; set; } = null!;
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = "transferencia"; // transferencia, efectivo, cheque
        
        [MaxLength(50)]
        public string? ReferenceNumber { get; set; }
        
        [MaxLength(10)]
        public string DocumentType { get; set; } = "boleta"; // boleta o factura
        
        [MaxLength(20)]
        public string? DocumentNumber { get; set; }
        
        public int? CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public User? CreatedBy { get; set; }
    }
}
