using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaquiLease.API.Models.Entities
{
    public class Asset
    {
        [Key]
        public int AssetId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? Category { get; set; }
        
        [MaxLength(100)]
        public string? Brand { get; set; }
        
        [MaxLength(100)]
        public string? Model { get; set; }
        
        public DateTime? PurchaseDate { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal? PurchasePriceCNY { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal? PurchasePriceUSD { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal? CurrentValue { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "disponible"; // disponible, alquilado, vendido, mantenimiento

        [MaxLength(10)]
        public string Currency { get; set; } = "PEN";
        
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        
        public string? Notes { get; set; }

        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
