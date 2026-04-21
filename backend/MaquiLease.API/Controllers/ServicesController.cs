using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using MaquiLease.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServices()
        {
            var svcs = await _context.Services
                .Where(s => s.IsActive)
                .Select(s => new ServiceDto
                {
                    ServiceId = s.ServiceId,
                    Code = s.Code,
                    Name = s.Name,
                    Description = s.Description,
                    ServiceType = s.ServiceType,
                    Category = s.Category,
                    BasePrice = s.BasePrice,
                    PriceUnit = s.PriceUnit,
                    Currency = s.Currency,
                    EstimatedDuration = s.EstimatedDuration,
                    RequiresAsset = s.RequiresAsset,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(svcs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceDto>> GetService(int id)
        {
            var s = await _context.Services.FindAsync(id);

            if (s == null || !s.IsActive)
                return NotFound();

            return new ServiceDto
            {
                ServiceId = s.ServiceId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                ServiceType = s.ServiceType,
                Category = s.Category,
                BasePrice = s.BasePrice,
                PriceUnit = s.PriceUnit,
                Currency = s.Currency,
                EstimatedDuration = s.EstimatedDuration,
                RequiresAsset = s.RequiresAsset,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            };
        }

        [HttpPost]
        public async Task<ActionResult<ServiceDto>> CreateService(CreateServiceDto dto)
        {
            if (await _context.Services.AnyAsync(s => s.Code == dto.Code))
                return BadRequest("Ya existe un servicio con este Código.");

            var s = new Service
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                ServiceType = dto.ServiceType,
                Category = dto.Category,
                BasePrice = dto.BasePrice,
                PriceUnit = dto.PriceUnit,
                Currency = dto.Currency,
                EstimatedDuration = dto.EstimatedDuration,
                RequiresAsset = dto.RequiresAsset,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Services.Add(s);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetService), new { id = s.ServiceId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateService(int id, CreateServiceDto dto)
        {
            var s = await _context.Services.FindAsync(id);
            if (s == null) return NotFound();

            s.Code = dto.Code;
            s.Name = dto.Name;
            s.Description = dto.Description;
            s.ServiceType = dto.ServiceType;
            s.Category = dto.Category;
            s.BasePrice = dto.BasePrice;
            s.PriceUnit = dto.PriceUnit;
            s.Currency = dto.Currency;
            s.EstimatedDuration = dto.EstimatedDuration;
            s.RequiresAsset = dto.RequiresAsset;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var s = await _context.Services.FindAsync(id);
            if (s == null) return NotFound();

            s.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
