using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients()
        {
            var clients = await _context.Clients
                .Where(c => c.IsActive)
                .Select(c => new ClientDto
                {
                    ClientId = c.ClientId,
                    RUC = c.RUC,
                    BusinessName = c.BusinessName,
                    ContactName = c.ContactName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    Sector = c.Sector,
                    RiskScore = c.RiskScore,
                    CreatedAt = c.CreatedAt,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(clients);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetClient(int id)
        {
            var c = await _context.Clients.FindAsync(id);

            if (c == null || !c.IsActive)
                return NotFound();

            return new ClientDto
            {
                ClientId = c.ClientId,
                RUC = c.RUC,
                BusinessName = c.BusinessName,
                ContactName = c.ContactName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                Sector = c.Sector,
                RiskScore = c.RiskScore,
                CreatedAt = c.CreatedAt,
                IsActive = c.IsActive
            };
        }

        [HttpPost]
        public async Task<ActionResult<ClientDto>> CreateClient(CreateClientDto dto)
        {
            if (await _context.Clients.AnyAsync(c => c.RUC == dto.RUC))
                return BadRequest("Ya existe un cliente con este RUC.");

            var client = new Client
            {
                RUC = dto.RUC,
                BusinessName = dto.BusinessName,
                ContactName = dto.ContactName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                Sector = dto.Sector,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClient), new { id = client.ClientId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(int id, CreateClientDto dto)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();

            client.RUC = dto.RUC;
            client.BusinessName = dto.BusinessName;
            client.ContactName = dto.ContactName;
            client.Email = dto.Email;
            client.Phone = dto.Phone;
            client.Address = dto.Address;
            client.Sector = dto.Sector;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();

            client.IsActive = false; // Borrado lógico
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
