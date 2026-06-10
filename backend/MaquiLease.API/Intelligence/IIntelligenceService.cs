using MaquiLease.API.Models.DTOs;

namespace MaquiLease.API.Intelligence
{
    public interface IIntelligenceService
    {
        Task<RiskScoreDto> CalculateRiskScore(int clientId);
        Task<PricingResponseDto> RecommendPrice(PricingRequestDto request);
        Task<RevenueForecastDto> RevenueForecast();
        Task<SegmentationSummaryDto> SegmentClients();
        Task<List<AssetHealthDto>> GetAssetHealthAnalysis();
        Task<List<MatchmakerRecommendationDto>> GetMatchmakerRecommendations();
        Task<SimulatedRiskDto> SimulateRiskScore(RiskSimulationRequestDto request);
        Task<List<RiskHistoryDto>> GetRiskHistory(int clientId);
        Task<string> GetClientAuditReport(int clientId);
        Task<DraftTermsResponseDto> DraftContractTerms(DraftTermsRequestDto request);
        Task<ChatAssistantResponseDto> ChatAssistant(ChatAssistantRequestDto request);
    }
}
