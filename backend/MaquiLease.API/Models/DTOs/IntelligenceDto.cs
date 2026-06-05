namespace MaquiLease.API.Models.DTOs
{
    // ── Risk Score ──────────────────────────────────
    public class RiskScoreDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string Category { get; set; } = string.Empty;   // Bajo, Medio, Alto, Crítico
        public string CategoryColor { get; set; } = string.Empty; // green, yellow, orange, red
        public List<RiskFactorDto> Factors { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
    }

    public class RiskFactorDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal RawValue { get; set; }
        public decimal WeightedScore { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // ── Pricing Recommendation ──────────────────────
    public class PricingRequestDto
    {
        public int? AssetId { get; set; }
        public int? ServiceId { get; set; }
        public int? ClientId { get; set; }
        public int DurationMonths { get; set; } = 6;
    }

    public class PricingResponseDto
    {
        public decimal MinPrice { get; set; }
        public decimal SuggestedPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string Currency { get; set; } = "PEN";
        public string Explanation { get; set; } = string.Empty;
        public List<PricingFactorDto> Factors { get; set; } = new();
    }

    public class PricingFactorDto
    {
        public string Name { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;  // positivo, negativo, neutro
        public string Description { get; set; } = string.Empty;
    }

    // ── Revenue Forecast (3 bandas) ─────────────────
    public class RevenueForecastDto
    {
        public List<ForecastBandPointDto> Points { get; set; } = new();
        public decimal HistoricalCollectionRate { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public class ForecastBandPointDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Optimistic { get; set; }
        public decimal Expected { get; set; }
        public decimal Pessimistic { get; set; }
        public bool IsHistorical { get; set; }
    }

    // ── Client Segmentation ─────────────────────────
    public class ClientSegmentDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;        // Premium, Crecimiento, En Riesgo, Problemático
        public string SegmentColor { get; set; } = string.Empty;   // emerald, blue, orange, red
        public string SegmentIcon { get; set; } = string.Empty;
        public decimal RiskScore { get; set; }
        public int OverdueInstallments { get; set; }
        public decimal TotalContractValue { get; set; }
        public string SuggestedAction { get; set; } = string.Empty;
    }

    public class SegmentationSummaryDto
    {
        public List<ClientSegmentDto> Clients { get; set; } = new();
        public Dictionary<string, int> SegmentCounts { get; set; } = new();
    }

    // ── Asset Health Analysis ───────────────────────
    public class AssetHealthDto
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal HealthIndex { get; set; } // 0 - 100
        public decimal WearPercentage { get; set; } // 0 - 100
        public int ContractsCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    // ── Matchmaker Recommendations ──────────────────
    public class MatchmakerRecommendationDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetCategory { get; set; } = string.Empty;
        public decimal AffinityScore { get; set; } // 0 - 100
        public decimal SuggestedMonthlyRate { get; set; }
        public string ConfidenceLevel { get; set; } = string.Empty; // Alta, Media
        public string Reasoning { get; set; } = string.Empty;
    }

    // ── Risk Simulation ─────────────────────────────
    public class RiskSimulationRequestDto
    {
        public string Sector { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }
        public int InstallmentsCount { get; set; }
        public decimal OnTimePaymentRate { get; set; } // 0 - 100
    }

    public class SimulatedRiskDto
    {
        public decimal Score { get; set; }
        public string Category { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    public class RiskHistoryDto
    {
        public decimal Score { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
