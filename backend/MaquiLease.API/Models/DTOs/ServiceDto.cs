using System;
using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.DTOs
{
    public class ServiceDto
    {
        public int ServiceId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal BasePrice { get; set; }
        public string PriceUnit { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string? EstimatedDuration { get; set; }
        public bool RequiresAsset { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateServiceDto
    {
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [MaxLength(30)]
        public string ServiceType { get; set; } = "tecnico";
        
        [MaxLength(50)]
        public string? Category { get; set; }
        
        public decimal BasePrice { get; set; }
        
        [MaxLength(20)]
        public string PriceUnit { get; set; } = "por_proyecto";
        
        [MaxLength(3)]
        public string Currency { get; set; } = "PEN";
        
        [MaxLength(50)]
        public string? EstimatedDuration { get; set; }
        
        public bool RequiresAsset { get; set; } = false;
        
        public bool IsActive { get; set; } = true;
    }
}
