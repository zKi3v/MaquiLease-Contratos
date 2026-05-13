using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlertsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AlertsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AlertDto>>> GetAlerts([FromQuery] bool? unreadOnly = null)
        {
            var query = _context.Alerts
                .Include(a => a.Contract)
                .Include(a => a.Installment)
                .AsQueryable();

            if (unreadOnly.HasValue && unreadOnly.Value)
            {
                query = query.Where(a => !a.IsRead);
            }

            var alerts = await query
                .OrderByDescending(a => a.SentAt)
                .Select(a => new AlertDto
                {
                    AlertId = a.AlertId,
                    ContractId = a.ContractId,
                    ContractNumber = a.Contract.ContractNumber,
                    InstallmentId = a.InstallmentId,
                    InstallmentNumber = a.Installment != null ? a.Installment.InstallmentNumber : null,
                    AlertType = a.AlertType,
                    Message = a.Message,
                    SentAt = a.SentAt,
                    SentVia = a.SentVia,
                    IsRead = a.IsRead
                })
                .ToListAsync();

            return Ok(alerts);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null) return NotFound();

            alert.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
