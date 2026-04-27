using MaquiLease.API.Data;
using MaquiLease.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("kpis")]
        public async Task<ActionResult<DashboardKpiDto>> GetKpis()
        {
            var totalAssets = await _context.Assets.CountAsync();
            var activeContracts = await _context.Contracts.CountAsync(c => c.Status == "activo" || c.Status == "ejecucion");
            
            var totalExpectedRevenue = await _context.Installments.SumAsync(i => i.Amount);
            var totalCollectedRevenue = await _context.Installments.SumAsync(i => i.PaidAmount);

            var totalInstallments = await _context.Installments.CountAsync();
            var lateInstallments = await _context.Installments.CountAsync(i => i.Status == "pendiente" && i.DueDate < DateTime.Now);
            
            double defaultRate = totalInstallments > 0 
                ? Math.Round(((double)lateInstallments / totalInstallments) * 100, 2) 
                : 0;

            return Ok(new DashboardKpiDto
            {
                TotalAssets = totalAssets,
                ActiveContracts = activeContracts,
                TotalExpectedRevenue = totalExpectedRevenue,
                TotalCollectedRevenue = totalCollectedRevenue,
                DefaultRatePercentage = defaultRate
            });
        }

        [HttpGet("asset-status")]
        public async Task<ActionResult<AssetDistributionDto>> GetAssetStatus()
        {
            var disponibles = await _context.Assets.CountAsync(a => a.Status.ToLower() == "disponible");
            var arrendados = await _context.Assets.CountAsync(a => a.Status.ToLower() == "arrendado");
            var mantenimiento = await _context.Assets.CountAsync(a => a.Status.ToLower() == "mantenimiento");

            return Ok(new AssetDistributionDto
            {
                Available = disponibles,
                Rented = arrendados,
                Maintenance = mantenimiento
            });
        }

        [HttpGet("revenue-forecast")]
        public async Task<ActionResult<IEnumerable<ForecastPointDto>>> GetRevenueForecast()
        {
            // Histórico (Últimos 3 meses cobrados reales) y Proyección simple Lineal (próximos 6 meses)
            // Agrupamos Installments por su mes de vencimiento.
            
            var baseQuery = await _context.Installments
                .GroupBy(i => new { i.DueDate.Year, i.DueDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAmountExpected = g.Sum(x => x.Amount),
                    TotalAmountPaid = g.Sum(x => x.PaidAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            List<ForecastPointDto> forecast = new List<ForecastPointDto>();
            string[] monthsNames = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

            // Calcular promedio incremental histórico de los cobros como "Trend factor"
            decimal averageRevenue = baseQuery.Any() ? baseQuery.Average(b => b.TotalAmountExpected) : 5000; 
            decimal trendMultiplier = 1.05m; // 5% growth simple predictivo simulado

            // Obtenemos los últimos 4 meses de data histórica (si existen)
            var currentMonth = DateTime.Now;
            for (int i = 3; i >= 0; i--)
            {
                var targetDate = currentMonth.AddMonths(-i);
                var historyRecord = baseQuery.FirstOrDefault(b => b.Year == targetDate.Year && b.Month == targetDate.Month);

                forecast.Add(new ForecastPointDto
                {
                    Month = $"{monthsNames[targetDate.Month]} {targetDate.ToString("yy")}",
                    RealRevenue = historyRecord?.TotalAmountPaid ?? 0,
                    PredictedRevenue = historyRecord?.TotalAmountExpected ?? 0 // Reales vs Esperados
                });
            }

            // Proyección a 4 meses futuros
            decimal lastPrediction = averageRevenue;
            for (int i = 1; i <= 4; i++)
            {
                var targetDate = currentMonth.AddMonths(i);
                lastPrediction = lastPrediction * trendMultiplier; 
                
                var futureRecord = baseQuery.FirstOrDefault(b => b.Year == targetDate.Year && b.Month == targetDate.Month);

                forecast.Add(new ForecastPointDto
                {
                    Month = $"{monthsNames[targetDate.Month]} {targetDate.ToString("yy")}",
                    RealRevenue = 0, // Como es futuro, 0 rentabilidad real aún
                    PredictedRevenue = futureRecord != null && futureRecord.TotalAmountExpected > 0 ? futureRecord.TotalAmountExpected : Math.Round(lastPrediction, 2)
                });
            }

            return Ok(forecast);
        }
    }
}
