using MaquiLease.API.Models.DTOs;

namespace MaquiLease.API.Intelligence
{
    public interface IIntelligenceService
    {
        Task<RiskScoreDto> CalculateRiskScore(int clientId);
        Task<PricingResponseDto> RecommendPrice(PricingRequestDto request);
        Task<RevenueForecastDto> RevenueForecast();
        Task<SegmentationSummaryDto> SegmentClients();
    }
}
