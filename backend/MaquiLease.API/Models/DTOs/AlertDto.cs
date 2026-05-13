namespace MaquiLease.API.Models.DTOs
{
    public class AlertDto
    {
        public int AlertId { get; set; }
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public int? InstallmentId { get; set; }
        public int? InstallmentNumber { get; set; }
        public string AlertType { get; set; } = string.Empty; // vencimiento_proximo, cuota_vencida, riesgo_alto
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string SentVia { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}
