using MaquiLease.API.Data;
using MaquiLease.API.Intelligence;
using MaquiLease.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaquiLease.API.BackgroundJobs
{
    public class RiskScoreRecalcJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RiskScoreRecalcJob> _logger;

        public RiskScoreRecalcJob(IServiceProvider serviceProvider, ILogger<RiskScoreRecalcJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RiskScoreRecalcJob is starting.");

            // Para pruebas: evaluar cada 1 minuto junto con las cuotas
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("RiskScoreRecalcJob is stopping.");
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RiskScoreRecalcJob is recalculating scores...");
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var intelligenceService = scope.ServiceProvider.GetRequiredService<IIntelligenceService>();

            var now = DateTime.UtcNow;

            // Obtener clientes activos
            var clients = await context.Clients
                .Where(c => c.IsActive)
                .ToListAsync(stoppingToken);

            int alertsGenerated = 0;

            foreach (var client in clients)
            {
                // Calcular score
                var scoreResult = await intelligenceService.CalculateRiskScore(client.ClientId);
                
                // Si el riesgo es Crítico (score > 70)
                if (scoreResult != null && (scoreResult.Category == "Crítico" || scoreResult.Score > 70))
                {
                    // Evitar spam: chequear si ya generamos una alerta similar recientemente (ej. últimos 7 días)
                    var recentAlert = await context.Alerts
                        .Where(a => a.Contract.ClientId == client.ClientId 
                                && a.AlertType == "riesgo_alto" 
                                && a.SentAt > now.AddDays(-7))
                        .FirstOrDefaultAsync(stoppingToken);

                    if (recentAlert == null)
                    {
                        // Buscar el contrato más reciente de este cliente para asociar la alerta (requerido por el esquema)
                        var lastContract = await context.Contracts
                            .Where(c => c.ClientId == client.ClientId && c.Status == "vigente")
                            .OrderByDescending(c => c.StartDate)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (lastContract != null)
                        {
                            var alert = new Alert
                            {
                                ContractId = lastContract.ContractId,
                                InstallmentId = null,
                                AlertType = "riesgo_alto",
                                Message = $"El cliente {client.BusinessName} ha sido reclasificado como Riesgo Problemático (Score: {scoreResult.Score}). Se requiere revisión inmediata.",
                                SentAt = now,
                                SentVia = "sistema",
                                IsRead = false
                            };
                            context.Alerts.Add(alert);
                            alertsGenerated++;
                        }
                    }
                }
            }

            if (alertsGenerated > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"RiskScoreRecalcJob: Generated {alertsGenerated} risk alerts.");
            }
        }
    }
}
