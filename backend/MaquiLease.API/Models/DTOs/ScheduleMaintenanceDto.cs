namespace MaquiLease.API.Models.DTOs
{
    public class ScheduleMaintenanceDto
    {
        public int ServiceId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
