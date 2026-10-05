namespace SPIC.Core.DTOs
{
    public class SDWADashboardDealerDto
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public string DealerCode { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int ProfileCompletion { get; set; }
        public string State { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string HQ { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? EntityType { get; set; }
    }
}
