using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.Entities
{
    public class Client
    {
        [Key]
        public int ClientId { get; set; }
        
        [Required]
        [MaxLength(11)]
        public string RUC { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string BusinessName { get; set; } = string.Empty;
        
        [MaxLength(150)]
        public string? ContactName { get; set; }
        
        [MaxLength(200)]
        public string? Email { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(300)]
        public string? Address { get; set; }
        
        [MaxLength(50)]
        public string? Sector { get; set; }
        
        public decimal? RiskScore { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;

        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public ICollection<PredictionLog> PredictionLogs { get; set; } = new List<PredictionLog>();
    }
}
