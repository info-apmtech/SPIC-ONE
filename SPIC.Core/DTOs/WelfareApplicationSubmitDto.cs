namespace SPIC.Core.DTOs;

public class WelfareApplicationSubmitDto
{
    public string SchemeName { get; set; } = string.Empty;

    // Beneficiary
    public string? BeneficiaryName { get; set; }
    public string? Relationship { get; set; }
    public DateTime? BeneficiaryDateOfBirth { get; set; }
    public string? NomineeName { get; set; }
    public string? NomineeRelationship { get; set; }
    public string? BeneficiaryNameAsInCheque { get; set; }
    public string? LeafOrBankPassbook { get; set; }

    // Quantity Lifted
    public int? QuantityLifted { get; set; }

    // Wedding / Sathabhishekam / Grahapravesam
    public DateTime? EventDate { get; set; }
    public string? EventVenue { get; set; }

    // Grahapravesam
    public string? OwnershipType { get; set; }

    // Death Relief
    public DateTime? DateOfDeath { get; set; }
    public string? LegalHeirName { get; set; }
    public string? DeathCause { get; set; }

    // Educational Assistance
    public string? Course { get; set; }
    public int? EduYear { get; set; }
    public string? CollegeName { get; set; }
    public int? TotalNumberOfCourses { get; set; }
    public bool? IsFirstApplication { get; set; }

    // Medical Assistance
    public string? MedicalTreatmentType { get; set; }

    // Merit Award
    public string? MeritCandidateName { get; set; }
    public string? MeritFatherName { get; set; }
    public string? ExaminationAppeared { get; set; }
    public string? BoardName { get; set; }
    public int? MaximumMarks { get; set; }
    public int? MarksObtained { get; set; }
    public double? MeritPercentage { get; set; }

    // Distinction Award
    public string? DistinctionCandidateName { get; set; }
    public string? DistinctionFatherName { get; set; }
    public string? ProfessionalCourseName { get; set; }
    public string? CourseCompletionYear { get; set; }
    public string? UniversityName { get; set; }
    public int? DistinctionMaximumMarks { get; set; }
    public int? DistinctionMarksObtained { get; set; }
    public double? DistinctionAggregatePercentage { get; set; }
    public bool? HasArrears { get; set; }
    public bool? IsWholesaleDealerEmployee { get; set; }

    // Declaration
    public bool IsDeclarationConfirmed { get; set; }
}
