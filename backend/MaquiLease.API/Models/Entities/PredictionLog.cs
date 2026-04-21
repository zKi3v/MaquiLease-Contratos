using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaquiLease.API.Models.Entities
{
    public class PredictionLog
    {
        [Key]
        public int LogId { get; set; }
        
        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Client Client { get; set; } = null!;
        
        public int? ContractId { get; set; }
        [ForeignKey("ContractId")]
        public Contract? Contract { get; set; }
        
        [Required]
        [MaxLength(30)]
        public string PredictionType { get; set; } = string.Empty; // riesgo_mora, forecast_ingresos, precio_sugerido
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; }
        
        public string? Details { get; set; } // JSON details
        
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
