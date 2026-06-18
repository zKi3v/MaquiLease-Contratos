using MaquiLease.API.Intelligence;
using MaquiLease.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

        /// <summary>
        /// Obtiene el historial temporal de risk scores para un cliente.
        /// </summary>
        [HttpGet("client/{clientId}/risk-history")]
        public async Task<ActionResult<List<RiskHistoryDto>>> GetRiskHistory(int clientId)
        {
            var result = await _intelligence.GetRiskHistory(clientId);
            return Ok(result);
        }

        /// <summary>
        /// Genera una auditoría crediticia cualitativa con IA (OpenCode Go LLM) para el cliente.
        /// </summary>
        [HttpGet("audit/{clientId}")]
        [EnableRateLimiting("ai-per-user")]
        public async Task<ActionResult<object>> GetAuditReport(int clientId)
        {
            try
            {
                var result = await _intelligence.GetClientAuditReport(clientId);
                return Ok(new { report = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Error al generar auditoría con IA: {ex.Message}" });
            }
        }

        /// <summary>
        /// Redacta términos y cláusulas contractuales personalizadas basadas en IA.
        /// </summary>
        [HttpPost("draft-terms")]
        [EnableRateLimiting("ai-per-user")]
        public async Task<ActionResult<DraftTermsResponseDto>> DraftTerms([FromBody] DraftTermsRequestDto request)
        {
            try
            {
                var result = await _intelligence.DraftContractTerms(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Error al redactar términos con IA: {ex.Message}" });
            }
        }

        /// <summary>
        /// Consulta al Asistente IA Global (chatbot) con contexto consolidado.
        /// </summary>
        [HttpPost("chat-assistant")]
        [EnableRateLimiting("ai-per-user")]
        public async Task<ActionResult<ChatAssistantResponseDto>> ChatAssistant([FromBody] ChatAssistantRequestDto request)
        {
            try
            {
                if (request.History.Count == 0 || request.History.Count > 12)
                {
                    return BadRequest(new { message = "El historial del chat debe tener entre 1 y 12 mensajes." });
                }

                if (request.History.Any(m =>
                        string.IsNullOrWhiteSpace(m.Content) ||
                        m.Content.Length > 1000 ||
                        (m.Role != "user" && m.Role != "assistant")))
                {
                    return BadRequest(new { message = "Cada mensaje debe tener rol user/assistant y un contenido de máximo 1000 caracteres." });
                }

                var result = await _intelligence.ChatAssistant(request);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Error en el asistente de chat: {ex.Message}" });
            }
        }
    }
}
