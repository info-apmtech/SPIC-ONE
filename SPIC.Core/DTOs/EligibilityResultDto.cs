namespace SPIC.Core.DTOs
{
    public class EligibilityResultDto
    {
        public bool IsEligible { get; set; }
        public string State { get; set; } = string.Empty;
        public string StateGroup { get; set; } = string.Empty;
        public string DealerType { get; set; } = string.Empty;
        public string DealerName { get; set; } = string.Empty;
        public string DealerCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public List<EligibilityCriterionDto> Criteria { get; set; } = new();
    }

    public class EligibilityCriterionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Required { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public bool IsSatisfied { get; set; }
    }
}
