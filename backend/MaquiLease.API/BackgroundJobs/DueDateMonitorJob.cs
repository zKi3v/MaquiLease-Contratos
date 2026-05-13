using MaquiLease.API.Data;
using MaquiLease.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.BackgroundJobs
{
    public class DueDateMonitorJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DueDateMonitorJob> _logger;

        public DueDateMonitorJob(IServiceProvider serviceProvider, ILogger<DueDateMonitorJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DueDateMonitorJob is starting.");

            // Para pruebas: evaluar cada 1 minuto
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("DueDateMonitorJob is stopping.");
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DueDateMonitorJob is checking installments...");
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var soon = now.AddDays(5);

            // 1. Alertas Preventivas: Vencen en <= 5 días y > now
            var approaching = await context.Installments
                .Include(i => i.Contract)
                .Where(i => i.Status == "pendiente" && !i.NotifiedApproaching && i.DueDate <= soon && i.DueDate > now)
                .ToListAsync(stoppingToken);

            foreach (var inst in approaching)
            {
                inst.NotifiedApproaching = true;
                
                var alert = new Alert
                {
                    ContractId = inst.ContractId,
                    InstallmentId = inst.InstallmentId,
                    AlertType = "vencimiento_proximo",
                    Message = $"La cuota {inst.InstallmentNumber} del contrato {inst.Contract.ContractNumber} vence el {inst.DueDate:dd/MM/yyyy}.",
                    SentAt = now,
                    SentVia = "sistema",
                    IsRead = false
                };
                context.Alerts.Add(alert);
            }

            // 2. Cuotas Vencidas: DueDate < now
            var overdue = await context.Installments
                .Include(i => i.Contract)
                .Where(i => i.Status == "pendiente" && i.DueDate < now)
                .ToListAsync(stoppingToken);

            foreach (var inst in overdue)
            {
                // Cambiar estado
                inst.Status = "vencido";
                
                // Calcular penalidad (ejemplo: 5% del monto original por mora)
                var daysLate = (now - inst.DueDate).Days;
                if (daysLate < 1) daysLate = 1;
                
                var penaltyRate = inst.Contract.PenaltyRate / 100m;
                inst.PenaltyAmount = Math.Round(inst.Amount * penaltyRate * daysLate, 2);

                if (!inst.NotifiedOverdue)
                {
                    inst.NotifiedOverdue = true;
                    
                    var alert = new Alert
                    {
                        ContractId = inst.ContractId,
                        InstallmentId = inst.InstallmentId,
                        AlertType = "cuota_vencida",
                        Message = $"La cuota {inst.InstallmentNumber} del contrato {inst.Contract.ContractNumber} ha vencido. Penalidad actual: {inst.PenaltyAmount} {inst.Contract.Currency}.",
                        SentAt = now,
                        SentVia = "sistema",
                        IsRead = false
                    };
                    context.Alerts.Add(alert);
                }
            }

            if (approaching.Any() || overdue.Any())
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"DueDateMonitorJob: Processed {approaching.Count} approaching and {overdue.Count} overdue installments.");
            }
        }
    }
}
