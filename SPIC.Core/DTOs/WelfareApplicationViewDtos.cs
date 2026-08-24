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

        // Rejection details (populated when Status is Rejected or ReturnedToDealer)
        public string? RejectedByLevel { get; set; }      // e.g. "RM"
        public string? RejectedByName { get; set; }       // officer name who rejected
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }      // rejection reason/remarks

        // True when the application was returned to the dealer for correction (Status == ReturnedToDealer)
        public bool CanResubmit { get; set; }
    }

    // =====================================================================
    //  SDWA Welfare Scheme Approval workflow (MO -> RM -> SM -> AVP)
    // =====================================================================

    public class WelfareApprovalApplicationDto
    {
        public int Id { get; set; }
        public string? ApplicationNumber { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;

        // Dealer snapshot
        public string? DealerCode { get; set; }
        public string? DealerName { get; set; }
        public string? Region { get; set; }
        public string? District { get; set; }

        // Welfare scheme
        public int SchemeType { get; set; }
        public string SchemeName { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; }

        // Beneficiary / eligibility snapshot
        public string? BeneficiaryName { get; set; }
        public string? BeneficiaryGroup { get; set; }

        // Approval history summary (per level, in workflow order MO -> RM -> SM -> AVP)
        public List<WelfareApprovalStepDto> Approvals { get; set; } = new();
    }

    public class WelfareApprovalStatsDto
    {
        public int TotalApplications { get; set; }
        public int PendingMyStage { get; set; }       // applications currently waiting on the logged-in approver
        public int ValidatedByMO { get; set; }        // approved by MO and still moving through the flow
        public int Rejected { get; set; }
        public int Completed { get; set; }            // finally approved by AVP
    }

    public class WelfareApprovalTabDto
    {
        public string Key { get; set; } = string.Empty;       // pending | validatedmo | recommendedrm | recommendedsm | rejected | completed
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class WelfareApprovalPageDto
    {
        public WelfareApprovalStatsDto Stats { get; set; } = new();
        public List<WelfareApprovalTabDto> Tabs { get; set; } = new();
        public string ActiveTab { get; set; } = "pending";
        public List<WelfareApprovalApplicationDto> Applications { get; set; } = new();
    }

    public class WelfareApprovalActionRequest
    {
        public string? Reason { get; set; }     // structured reason (reject modal dropdown)
        public string? Remarks { get; set; }    // free text - mandatory for reject
        public string? Recommendation { get; set; }  // MO approval dropdown: "Recommended" / "Not Recommended"
        public string? Comment { get; set; }         // MO approval comment - mandatory for MO approve
    }

    public class WelfareApprovalActionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ApplicationId { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
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
        public string? Recommendation { get; set; }   // MO's recommendation recorded at approval
        public string? Comment { get; set; }          // MO's comment recorded at approval
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
        public int? SubDealerId { get; set; }
        public string? SubDealerName { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        // Sales history
        public decimal? AverageQuantityLifted3Years { get; set; }
        public decimal? LastYearQuantityLifted { get; set; }

        public bool IsDeclarationConfirmed { get; set; }

        // Resubmission (reverse rejection flow)
        public bool CanResubmit { get; set; }                 // True when Status == ReturnedToDealer and the dealer may correct & resubmit
        public int ResubmissionCount { get; set; }            // Number of dealer resubmissions so far
        public DateTime? LastResubmittedAt { get; set; }      // When the dealer last resubmitted

        public List<WelfareApplicationDocumentDto> Documents { get; set; } = new();
        public List<WelfareApprovalStepDto> Approvals { get; set; } = new();
    }
}
