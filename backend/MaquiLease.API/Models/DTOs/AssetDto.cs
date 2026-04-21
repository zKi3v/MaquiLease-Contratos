using System;
using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.DTOs
{
    public class AssetDto
    {
        public int AssetId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchasePriceCNY { get; set; }
        public decimal? PurchasePriceUSD { get; set; }
        public decimal? CurrentValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateAssetDto
    {
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
        public decimal? PurchasePriceCNY { get; set; }
        public decimal? PurchasePriceUSD { get; set; }
        public decimal? CurrentValue { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "disponible";
        
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        
        public string? Notes { get; set; }
    }
}
