using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaquiLease.API.Models.Entities
{
    public class Installment
    {
        [Key]
        public int InstallmentId { get; set; }
        
        public int ContractId { get; set; }
        [ForeignKey("ContractId")]
        public Contract Contract { get; set; } = null!;
        
        public int InstallmentNumber { get; set; }
        
        public DateTime DueDate { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal PenaltyAmount { get; set; } = 0;
        
        [MaxLength(20)]
        public string Status { get; set; } = "pendiente"; // pendiente, pagado, parcial, vencido
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal PaidAmount { get; set; } = 0;
        
        public DateTime? PaidDate { get; set; }
        
        public bool NotifiedApproaching { get; set; } = false;
        
        public bool NotifiedOverdue { get; set; } = false;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
