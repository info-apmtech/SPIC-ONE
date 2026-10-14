namespace SPIC.Core.Entities;

// ------------------------------------------------------------------------------------------------
// MDO -> JMDO yearly target allocation (TargetAchievement page). Distinct from JmdoTaskAllocation's
// Spcm/SoilSample targets, which are the JMDO's own weekly plan. Table created manually (no
// migration) - one row per (EmployeeLoginId, FinancialYearStart).
// ------------------------------------------------------------------------------------------------

/// <summary>One JMDO's yearly SPCM / Soil Sample target for one April-March financial year.</summary>
public class JmdoYearlyTarget
{
    public int Id { get; set; }

    /// <summary>The JMDO's Employeelogins.Id - the same key api/Team/jmdos returns.</summary>
    public int EmployeeLoginId { get; set; }

    /// <summary>2026 for FY 2026-27 (April to March) - same convention as LabReport.FinancialYearStart.</summary>
    public int FinancialYearStart { get; set; }

    public int SpcmTarget { get; set; }
    public int SoilSampleTarget { get; set; }

    /// <summary>The JMDO's HQ at save time, snapshotted for scoping.</summary>
    public int? HeadquarterId { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
