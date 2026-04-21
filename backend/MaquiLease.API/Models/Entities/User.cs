using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [MaxLength(150)]
        public string? FullName { get; set; }
        
        [MaxLength(20)]
        public string Role { get; set; } = "operador"; // admin, operador, gerente
        
        public bool IsActive { get; set; } = true;
    }
}
