using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaquiLease.API.Models.Entities
{
    public class Alert
    {
        [Key]
        public int AlertId { get; set; }
        
        public int ContractId { get; set; }
        [ForeignKey("ContractId")]
        public Contract Contract { get; set; } = null!;
        
        public int? InstallmentId { get; set; }
        [ForeignKey("InstallmentId")]
        public Installment? Installment { get; set; }
        
        [Required]
        [MaxLength(30)]
        public string AlertType { get; set; } = string.Empty; // vencimiento_proximo, cuota_vencida, riesgo_alto
        
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(20)]
        public string SentVia { get; set; } = "sistema"; // email o sistema
        
        public bool IsRead { get; set; } = false;
    }
}
