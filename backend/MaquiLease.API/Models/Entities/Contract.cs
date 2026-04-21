using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaquiLease.API.Models.Entities
{
    public class Contract
    {
        [Key]
        public int ContractId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string ContractNumber { get; set; } = string.Empty;
        
        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Client Client { get; set; } = null!;
        
        public int? AssetId { get; set; }
        [ForeignKey("AssetId")]
        public Asset? Asset { get; set; }
        
        public int? ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }
        
        [Required]
        [MaxLength(15)]
        public string ContractType { get; set; } = "alquiler"; // alquiler, venta, servicio
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        [Column(TypeName = "decimal(14,2)")]
        public decimal TotalAmount { get; set; }
        
        [MaxLength(3)]
        public string Currency { get; set; } = "PEN";
        
        public int NumberOfInstallments { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal PenaltyRate { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "borrador"; // borrador, activo, completado, cancelado, vencido
        
        public DateTime? SignedAt { get; set; }
        
        [MaxLength(256)]
        public string? SignatureHash { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int? CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public User? CreatedBy { get; set; }

        public ICollection<Installment> Installments { get; set; } = new List<Installment>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
