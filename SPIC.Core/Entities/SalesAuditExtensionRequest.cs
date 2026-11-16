using System;

namespace SPIC.Core.Entities
{
    public sealed class SalesAuditExtensionRequest
    {
        public int Id { get; set; }
        public int CSRId { get; set; }
        public DateTime PreviousEndDate { get; set; }
        public DateTime NewEndDate { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedDate { get; set; }

        // Stored as integer (1-5) in PostgreSQL.
        public ExtensionReason Reason { get; set; }

        public string? Remarks { get; set; } = "";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public SalesAuditExtensionRequestStatus Status { get; set; }
            = SalesAuditExtensionRequestStatus.Submitted;
    }

    public enum SalesAuditExtensionRequestStatus
    {
        Submitted = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum ExtensionReason
    {
        MarketCondition = 1,
        WeatherCropSeasonDelay = 2,
        StockAvailability = 3,
        DealerOnboardingDelay = 4,
        Other = 5
    }

    public static class ExtensionReasonHelper
    {
        public static string GetLabel(this ExtensionReason reason) => reason switch
        {
            ExtensionReason.MarketCondition => "Market Condition",
            ExtensionReason.WeatherCropSeasonDelay => "Weather / Crop Season Delay",
            ExtensionReason.StockAvailability => "Stock Availability",
            ExtensionReason.DealerOnboardingDelay => "Dealer Onboarding Delay",
            ExtensionReason.Other => "Other",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown extension reason")
        };

        // Accept existing RequestExtension DTO strings; write enum IDs only.
        public static bool TryParseLabel(string? label, out ExtensionReason reason)
        {
            foreach (var value in Enum.GetValues<ExtensionReason>())
            {
                if (string.Equals(value.GetLabel(), label, StringComparison.Ordinal))
                {
                    reason = value;
                    return true;
                }
            }
            reason = default;
            return false;
        }
    }
}
