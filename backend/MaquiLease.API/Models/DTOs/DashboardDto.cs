namespace MaquiLease.API.Models.DTOs
{
    public class DashboardKpiDto
    {
        public int TotalAssets { get; set; }
        public int ActiveContracts { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public decimal TotalCollectedRevenue { get; set; }
        public double DefaultRatePercentage { get; set; }
    }

    public class ForecastPointDto
    {
        public required string Month { get; set; } // Ej: "Enero"
        public decimal RealRevenue { get; set; }
        public decimal PredictedRevenue { get; set; }
    }

    public class AssetDistributionDto
    {
        public int Available { get; set; }
        public int Rented { get; set; }
        public int Maintenance { get; set; }
    }
}
