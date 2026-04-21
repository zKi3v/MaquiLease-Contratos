using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaquiLease.API.Models.Entities
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [MaxLength(30)]
        public string ServiceType { get; set; } = "tecnico"; // tecnico, profesional
        
        [MaxLength(50)]
        public string? Category { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal BasePrice { get; set; }
        
        [MaxLength(20)]
        public string PriceUnit { get; set; } = "por_proyecto"; // por_hora, por_dia, por_proyecto, mensual
        
        [MaxLength(3)]
        public string Currency { get; set; } = "PEN";
        
        [MaxLength(50)]
        public string? EstimatedDuration { get; set; }
        
        public bool RequiresAsset { get; set; } = false;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
