using System.Security.Claims;
using MaquiLease.API.Data;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere token de Firebase válido
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncUser()
        {
            // Extraer email desde el JWT de Firebase
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("El token de Firebase no contiene un email.");
            }

            // Buscar si el usuario ya existe en nuestra base de datos SQL
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Si no existe (nuevo registro en Firebase), lo creamos en SQL
                user = new User
                {
                    Email = email,
                    Username = email.Split('@')[0],
                    FullName = "Usuario Firebase",
                    Role = "operador",
                    PasswordHash = "firebase-auth", // Ya no usamos contraseñas locales
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                user.UserId,
                user.Email,
                user.FullName,
                user.Role
            });
        }
    }
}
