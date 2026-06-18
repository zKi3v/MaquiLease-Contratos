using System.Security.Claims;
using MaquiLease.API.Data;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin.Auth;

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
            else if (!user.IsActive)
            {
                return StatusCode(403, new { message = "Este usuario ha sido desactivado en la plataforma." });
            }

            return Ok(new
            {
                user.UserId,
                user.Email,
                user.FullName,
                user.Role,
                user.IsActive
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!await IsAdminAsync())
            {
                return StatusCode(403, new { message = "Acceso denegado. Se requieren privilegios de Administrador." });
            }

            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> UpdateRole(int userId, [FromBody] UpdateRoleRequest request)
        {
            if (!await IsAdminAsync())
            {
                return StatusCode(403, new { message = "Acceso denegado. Se requieren privilegios de Administrador." });
            }

            if (string.IsNullOrEmpty(request.Role) || 
                (request.Role != "admin" && request.Role != "operador" && request.Role != "gerente"))
            {
                return BadRequest("Rol inválido. Los roles permitidos son: admin, operador, gerente.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // Evitar que el administrador se quite el rol de admin a sí mismo (seguridad básica)
            var currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (user.Email == currentEmail && request.Role != "admin")
            {
                return BadRequest("No puedes remover tu propio rol de Administrador.");
            }

            user.Role = request.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rol actualizado correctamente.", role = user.Role });
        }

        [HttpPut("users/{userId}/status")]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            if (!await IsAdminAsync())
            {
                return StatusCode(403, new { message = "Acceso denegado. Se requieren privilegios de Administrador." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            var currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (user.Email == currentEmail)
            {
                return BadRequest("No puedes desactivar tu propia cuenta de administrador.");
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Estado de usuario actualizado correctamente.", isActive = user.IsActive });
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (!await IsAdminAsync())
            {
                return StatusCode(403, new { message = "Acceso denegado. Se requieren privilegios de Administrador." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            var currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (user.Email == currentEmail)
            {
                return BadRequest("No puedes eliminar tu propia cuenta de administrador.");
            }

            try
            {
                var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(user.Email);
                if (firebaseUser != null)
                {
                    await FirebaseAuth.DefaultInstance.DeleteUserAsync(firebaseUser.Uid);
                }
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode != AuthErrorCode.UserNotFound)
                {
                    return BadRequest(new { message = $"Error al eliminar en Firebase: {ex.Message}" });
                }
            }
            catch (InvalidOperationException)
            {
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario eliminado correctamente." });
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!await IsAdminAsync())
            {
                return StatusCode(403, new { message = "Acceso denegado. Se requieren privilegios de Administrador." });
            }

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.FullName))
            {
                return BadRequest("El email y el nombre completo son obligatorios.");
            }

            var emailNormalized = request.Email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Email == emailNormalized))
            {
                return BadRequest("Ya existe un usuario registrado con este correo electrónico.");
            }

            var role = request.Role?.ToLowerInvariant() ?? "operador";
            if (role != "admin" && role != "operador" && role != "gerente")
            {
                return BadRequest("Rol inválido. Los roles permitidos son: admin, operador, gerente.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "La contraseña es obligatoria." });
            }

            var password = request.Password.Trim();
            if (password.Length < 12)
            {
                return BadRequest(new { message = "La contraseña debe tener al menos 12 caracteres." });
            }

            try
            {
                var userArgs = new UserRecordArgs
                {
                    Email = emailNormalized,
                    EmailVerified = true,
                    Password = password,
                    DisplayName = request.FullName.Trim(),
                    Disabled = false
                };
                await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = $"El SDK de Firebase no está inicializado. Detalle: {ex.Message}" });
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode != AuthErrorCode.EmailAlreadyExists)
                {
                    return BadRequest(new { message = $"Error al registrar en Firebase: {ex.Message}" });
                }
            }

            var newUser = new User
            {
                Email = emailNormalized,
                Username = emailNormalized.Split('@')[0],
                FullName = request.FullName.Trim(),
                Role = role,
                PasswordHash = "firebase-auth",
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                newUser.UserId,
                newUser.Email,
                newUser.FullName,
                newUser.Role,
                newUser.IsActive
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("El token de Firebase no contiene un email.");
            }

            var emailNormalized = email.Trim().ToLowerInvariant();
            var localUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailNormalized);
            if (localUser == null)
            {
                return NotFound("Usuario no encontrado en la base de datos local.");
            }

            if (!localUser.IsActive)
            {
                return StatusCode(403, new { message = "Este usuario ha sido desactivado en la plataforma." });
            }

            if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 12)
            {
                return BadRequest(new { message = "La contraseña debe tener al menos 12 caracteres." });
            }

            try
            {
                var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(emailNormalized);
                
                var updateArgs = new UserRecordArgs
                {
                    Uid = firebaseUser.Uid
                };

                if (!string.IsNullOrEmpty(request.FullName))
                {
                    updateArgs.DisplayName = request.FullName.Trim();
                }

                if (!string.IsNullOrEmpty(request.Password))
                {
                    updateArgs.Password = request.Password;
                }

                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = $"Error al actualizar en Firebase: {ex.Message}" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = $"El SDK de Firebase no está inicializado. Detalle: {ex.Message}" });
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                localUser.FullName = request.FullName.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                localUser.UserId,
                localUser.Email,
                localUser.FullName,
                localUser.Role,
                localUser.IsActive
            });
        }

        private async Task<bool> IsAdminAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (string.IsNullOrEmpty(email)) return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && user.Role == "admin" && user.IsActive;
        }
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "operador";
        public string? Password { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? Password { get; set; }
    }
}
