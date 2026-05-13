using MaquiLease.API.Intelligence;
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
        private readonly IIntelligenceService _intelligenceService;

        public DashboardController(AppDbContext context, IIntelligenceService intelligenceService)
        {
            _context = context;
            _intelligenceService = intelligenceService;
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

        [HttpGet("overdue-rate")]
        public async Task<ActionResult<IEnumerable<MonthlyOverdueDto>>> GetOverdueRate()
        {
            var result = new List<MonthlyOverdueDto>();
            var now = DateTime.UtcNow;
            string[] monthsNames = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

            // We calculate over the last 12 months
            for (int i = 11; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var startDate = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Cuotas que vencían en ese mes
                var installmentsInMonth = await _context.Installments
                    .Where(inst => inst.DueDate >= startDate && inst.DueDate <= endDate)
                    .ToListAsync();

                int totalInstallments = installmentsInMonth.Count;
                int lateInstallments = installmentsInMonth.Count(inst => 
                    inst.Status == "vencido" || 
                    (inst.Status == "pagado" && inst.PaidDate.HasValue && inst.PaidDate.Value > inst.DueDate));

                decimal rate = totalInstallments > 0 
                    ? Math.Round(((decimal)lateInstallments / totalInstallments) * 100, 2) 
                    : 0;

                result.Add(new MonthlyOverdueDto
                {
                    Month = $"{monthsNames[targetMonth.Month]} {targetMonth:yy}",
                    OverdueRate = rate
                });
            }

            return Ok(result);
        }

        [HttpGet("contract-distribution")]
        public async Task<ActionResult<ContractDistributionDto>> GetContractDistribution()
        {
            var contracts = await _context.Contracts.ToListAsync();

            var byStatus = contracts
                .GroupBy(c => c.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var byType = contracts
                .GroupBy(c => c.ContractType)
                .ToDictionary(g => g.Key, g => g.Count());

            return Ok(new ContractDistributionDto
            {
                ByStatus = byStatus,
                ByType = byType
            });
        }

        [HttpGet("client-segments")]
        public async Task<ActionResult<SegmentationSummaryDto>> GetClientSegments()
        {
            var summary = await _intelligenceService.SegmentClients();
            return Ok(summary);
        }
    }
}
