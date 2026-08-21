namespace SPIC.Core.DTOs
{
    public class WelfareApplicationSummaryDto
    {
        public int Id { get; set; }
        public string? ApplicationNumber { get; set; }
        public int SchemeType { get; set; }
        public string SchemeName { get; set; } = string.Empty;
        public string? BeneficiaryName { get; set; }
        public string? BeneficiaryGroup { get; set; }
        public DateTime ApplicationDate { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime LastUpdatedAt { get; set; }
        public int DocumentCount { get; set; }
    }

    public class WelfareApplicationDocumentDto
    {
        public int Id { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentName { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public bool IsVerified { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class WelfareApprovalStepDto
    {
        public string ApprovalLevel { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class WelfareApplicationDetailDto
    {
        public int Id { get; set; }
        public string? ApplicationNumber { get; set; }
        public int SchemeType { get; set; }
        public string SchemeName { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;

        // Dealer snapshot
        public string? DealerCode { get; set; }
        public string? DealerName { get; set; }
        public string? DealershipNature { get; set; }
        public string? MobileNumber { get; set; }
        public string? Region { get; set; }
        public string? District { get; set; }
        public int? QuantityLifted { get; set; }

        // Beneficiary information
        public string? BeneficiaryName { get; set; }
        public string? Relationship { get; set; }
        public DateTime? BeneficiaryDateOfBirth { get; set; }
        public string? NomineeName { get; set; }
        public string? NomineeRelationship { get; set; }
        public string? BeneficiaryNameAsInCheque { get; set; }
        public string? LeafOrBankPassbook { get; set; }

        // Scheme-specific: Wedding Gift
        public DateTime? MarriageDate { get; set; }

        // Scheme-specific: Grahapravesam / Sathabhishekam
        public DateTime? EventDate { get; set; }
        public string? OwnershipType { get; set; }
        public string? EventVenue { get; set; }

        // Scheme-specific: Educational Assistance
        public string? Course { get; set; }
        public int? EduYear { get; set; }
        public string? CollegeName { get; set; }
        public int? TotalNumberOfCourses { get; set; }
        public bool? IsFirstApplication { get; set; }

        // Scheme-specific: Medical Assistance
        public string? MedicalTreatmentType { get; set; }

        // Scheme-specific: Death Relief
        public DateTime? DateOfDeath { get; set; }
        public string? LegalHeirName { get; set; }
        public string? DeathCause { get; set; }

        // Scheme-specific: Merit Award
        public string? MeritCandidateName { get; set; }
        public string? MeritFatherName { get; set; }
        public string? ExaminationAppeared { get; set; }
        public string? BoardName { get; set; }
        public int? MaximumMarks { get; set; }
        public int? MarksObtained { get; set; }
        public double? MeritPercentage { get; set; }

        // Scheme-specific: Distinction Award
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

        // Beneficiary group
        public string? BeneficiaryGroup { get; set; }
        public string? SubDealerName { get; set; }
        public string? EmployeeName { get; set; }

        // Sales history
        public decimal? AverageQuantityLifted3Years { get; set; }
        public decimal? LastYearQuantityLifted { get; set; }

        public bool IsDeclarationConfirmed { get; set; }

        public List<WelfareApplicationDocumentDto> Documents { get; set; } = new();
        public List<WelfareApprovalStepDto> Approvals { get; set; } = new();
    }
}
