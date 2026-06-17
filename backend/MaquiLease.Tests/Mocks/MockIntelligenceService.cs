using MaquiLease.API.Intelligence;
using MaquiLease.API.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaquiLease.Tests.Mocks
{
    public class MockIntelligenceService : IIntelligenceService
    {
        public Task<RiskScoreDto> CalculateRiskScore(int clientId)
        {
            return Task.FromResult(new RiskScoreDto
            {
                ClientId = clientId,
                Score = 15m,
                Category = "Bajo",
                CategoryColor = "green",
                Factors = new List<RiskFactorDto>()
            });
        }

        public Task<PricingResponseDto> RecommendPrice(PricingRequestDto request) => Task.FromResult(new PricingResponseDto());
        public Task<RevenueForecastDto> RevenueForecast() => Task.FromResult(new RevenueForecastDto());
        public Task<SegmentationSummaryDto> SegmentClients() => Task.FromResult(new SegmentationSummaryDto());
        public Task<List<AssetHealthDto>> GetAssetHealthAnalysis() => Task.FromResult(new List<AssetHealthDto>());
        public Task<List<MatchmakerRecommendationDto>> GetMatchmakerRecommendations() => Task.FromResult(new List<MatchmakerRecommendationDto>());
        public Task<SimulatedRiskDto> SimulateRiskScore(RiskSimulationRequestDto request) => Task.FromResult(new SimulatedRiskDto());
        public Task<List<RiskHistoryDto>> GetRiskHistory(int clientId) => Task.FromResult(new List<RiskHistoryDto>());
        public Task<string> GetClientAuditReport(int clientId) => Task.FromResult(string.Empty);
        public Task<DraftTermsResponseDto> DraftContractTerms(DraftTermsRequestDto request) => Task.FromResult(new DraftTermsResponseDto());
        public Task<ChatAssistantResponseDto> ChatAssistant(ChatAssistantRequestDto request) => Task.FromResult(new ChatAssistantResponseDto());
    }
}
