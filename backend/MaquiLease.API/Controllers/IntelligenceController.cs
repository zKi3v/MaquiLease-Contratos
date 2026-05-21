using MaquiLease.API.Intelligence;
using MaquiLease.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaquiLease.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IntelligenceController : ControllerBase
    {
        private readonly IIntelligenceService _intelligence;

        public IntelligenceController(IIntelligenceService intelligence)
        {
            _intelligence = intelligence;
        }

        /// <summary>
        /// Calcula el score de riesgo de morosidad para un cliente específico.
        /// </summary>
        [HttpGet("default-risk/{clientId}")]
        public async Task<ActionResult<RiskScoreDto>> GetDefaultRisk(int clientId)
        {
            try
            {
                var result = await _intelligence.CalculateRiskScore(clientId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Genera una recomendación de precio basada en activo/servicio y perfil de riesgo.
        /// </summary>
        [HttpPost("pricing-recommendation")]
        public async Task<ActionResult<PricingResponseDto>> GetPricingRecommendation([FromBody] PricingRequestDto request)
        {
            try
            {
                var result = await _intelligence.RecommendPrice(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Proyección de ingresos con 3 bandas (optimista, esperado, pesimista) para 6 meses.
        /// </summary>
        [HttpGet("revenue-forecast")]
        public async Task<ActionResult<RevenueForecastDto>> GetRevenueForecast()
        {
            var result = await _intelligence.RevenueForecast();
            return Ok(result);
        }

        /// <summary>
        /// Segmentación de clientes en 4 categorías con acciones recomendadas.
        /// </summary>
        [HttpGet("client-scoring")]
        public async Task<ActionResult<SegmentationSummaryDto>> GetClientScoring()
        {
            var result = await _intelligence.SegmentClients();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene el análisis de salud y mantenimiento predictivo para todos los activos.
        /// </summary>
        [HttpGet("asset-health")]
        public async Task<ActionResult<List<AssetHealthDto>>> GetAssetHealth()
        {
            var result = await _intelligence.GetAssetHealthAnalysis();
            return Ok(result);
        }

        /// <summary>
        /// Genera recomendaciones de matchmaker para activos disponibles y clientes estables.
        /// </summary>
        [HttpGet("matchmaker")]
        public async Task<ActionResult<List<MatchmakerRecommendationDto>>> GetMatchmaker()
        {
            var result = await _intelligence.GetMatchmakerRecommendations();
            return Ok(result);
        }

        /// <summary>
        /// Simula el score de riesgo del cliente basado en parámetros hipotéticos de contrato (What-If).
        /// </summary>
        [HttpPost("simulate")]
        public async Task<ActionResult<SimulatedRiskDto>> SimulateRisk([FromBody] RiskSimulationRequestDto request)
        {
            var result = await _intelligence.SimulateRiskScore(request);
            return Ok(result);
        }
    }
}
