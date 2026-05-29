using System;
using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.DTOs
{
    public class ClientDto
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
        [Required(ErrorMessage = "El RUC es obligatorio.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe constar de exactamente 11 dígitos numéricos.")]
        public string RUC { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La Razón Social es obligatoria.")]
        [MinLength(3, ErrorMessage = "La Razón Social debe tener al menos 3 caracteres.")]
        [MaxLength(200, ErrorMessage = "La Razón Social no puede exceder los 200 caracteres.")]
        public string BusinessName { get; set; } = string.Empty;
        
        [MinLength(3, ErrorMessage = "El nombre del contacto debe tener al menos 3 caracteres.")]
        [MaxLength(150, ErrorMessage = "El nombre del contacto no puede exceder los 150 caracteres.")]
        public string? ContactName { get; set; }
        
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [MaxLength(200, ErrorMessage = "El correo electrónico no puede exceder los 200 caracteres.")]
        public string? Email { get; set; }
        
        [RegularExpression(@"^\+?[\d\s\-()]{7,20}$", ErrorMessage = "El formato del teléfono no es válido (mínimo 7 y máximo 20 dígitos numéricos u operadores +, -, (), espacios).")]
        public string? Phone { get; set; }
        
        [MinLength(5, ErrorMessage = "La dirección debe tener al menos 5 caracteres.")]
        [MaxLength(300, ErrorMessage = "La dirección no puede exceder los 300 caracteres.")]
        public string? Address { get; set; }
        
        [MinLength(2, ErrorMessage = "El sector debe tener al menos 2 caracteres.")]
        [MaxLength(50, ErrorMessage = "El sector no puede exceder los 50 caracteres.")]
        public string? Sector { get; set; }
    }
}
