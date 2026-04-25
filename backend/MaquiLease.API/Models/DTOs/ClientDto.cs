using System;
using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.DTOs
{
    public class ClientDto //Cliente
    {
        public int ClientId { get; set; }
        public string RUC { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Sector { get; set; }
        public decimal? RiskScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateClientDto
    {
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
    }
}
