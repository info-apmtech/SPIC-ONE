namespace SPIC.Core.Entities;


public class WelfareApplication
{
	public int Id { get; set; }                                          // Primary key
	public int DealerId { get; set; }                                    // FK to the DealerRegistration applying for the scheme
	public DealerRegistration? Dealer { get; set; }                      // Navigation to the applying dealer
	public WelfareSchemeType SchemeName { get; set; }                      // Which scheme this application is for

	// Application Information
	public string? ApplicationNumber { get; set; }                       // Generated reference shown to user/office (e.g. "SDWA-GH-2025-00847")
	public DateTime ApplicationDate { get; set; } = DateTime.Now;        // Date the application was created/submitted
	public WelfareApplicationStatus Status { get; set; } = WelfareApplicationStatus.Draft; // Current stage in the approval workflow

	// Dealer Details (snapshot of the submitted application)
	public string? DealerCode { get; set; }                              // Dealer code copied from the dealer master at submission
	public string? DealerName { get; set; }                              // Dealer/firm name copied at submission
	public string? DealershipNature { get; set; }                        // Nature of dealership (e.g. "Retail Fertilizer Outlet")
	public string? MobileNumber { get; set; }                            // Dealer mobile number copied at submission
	public string? Region { get; set; }                                  // Region name copied at submission
	public string? District { get; set; }                                // District name copied at submission
	public int? QuantityLifted { get; set; }                         // Cases/quantity lifted in the selected financial year

	// Beneficiary Information
	public string? BeneficiaryName { get; set; }                         // Name of the person receiving the benefit
	public string? Relationship { get; set; }                            // Beneficiary's relationship to the dealer (e.g. Spouse, Child, Parent)
	public DateTime? BeneficiaryDateOfBirth { get; set; }                // Beneficiary's date of birth (for schemes like Sathabhishekam)
	public string? NomineeName { get; set; }                             // Name of the nominee for the benefit
	public string? NomineeRelationship { get; set; }                     // Nominee's relationship to the dealer
	public string? BeneficiaryNameAsInCheque { get; set; }               // Name exactly as printed on the cheque leaf / bank passbook
	public string? LeafOrBankPassbook { get; set; }                      // Which document the name was taken from (e.g. "Cheque Leaf" / "Bank Passbook")

	// Scheme-specific: Wedding Gift
	public DateTime? MarriageDate { get; set; }                           // Date of marriage for Wedding Gift scheme

	// Scheme-specific: Grahapravesam
	public DateTime? EventDate { get; set; }                              // Date of function/event (Grahapravesam, Sathabhishekam)
	public string? OwnershipType { get; set; }                            // Owned / Rented for Grahapravesam
	public string? EventVenue { get; set; }                               // Venue if different from house address

	// Scheme-specific: Educational Assistance
	public string? Course { get; set; }                                   // Course name
	public int? EduYear { get; set; }                                  // Year of study (1st, 2nd, 3rd, 4th)
	public string? CollegeName { get; set; }                              // Name of the college
	public int? TotalNumberOfCourses { get; set; }                        // Total number of years/duration of the course
	public bool? IsFirstApplication { get; set; }                         // First-time or renewal (null = unknown, true = first, false = renewal)

	// Scheme-specific: Medical Assistance
	public string? MedicalTreatmentType { get; set; }                    // Type of treatment / medical condition (e.g. Surgery, Hospitalization, Cancer treatment)

	// Scheme-specific: Merit Award
	public string? MeritCandidateName { get; set; }                       // Name of the candidate
	public string? MeritFatherName { get; set; }                          // Father's name of the candidate
	public string? ExaminationAppeared { get; set; }                      // 10th or 12th
	public string? BoardName { get; set; }                                // Name of the examination board
	public int? MaximumMarks { get; set; }                                // Maximum marks for the examination
	public int? MarksObtained { get; set; }                               // Marks obtained by the candidate
	public double? MeritPercentage { get; set; }                          // Calculated percentage (Marks Obtained / Maximum Marks * 100)

	// Scheme-specific: Distinction Award
	public string? DistinctionCandidateName { get; set; }                // Name of the candidate
	public string? DistinctionFatherName { get; set; }                   // Father's name of the candidate
	public string? ProfessionalCourseName { get; set; }                  // Name of the professional course completed
	public string? CourseCompletionYear { get; set; }                    // Year of course completion
	public string? UniversityName { get; set; }                          // University / institution name
	public int? DistinctionMaximumMarks { get; set; }                    // Total / maximum marks for the course
	public int? DistinctionMarksObtained { get; set; }                   // Marks obtained in the course
	public double? DistinctionAggregatePercentage { get; set; }          // Calculated aggregate percentage
	public bool? HasArrears { get; set; }                                // Whether the candidate has any arrears (true = has arrears, false = no arrears)
	public bool? IsWholesaleDealerEmployee { get; set; }                 // Whether the candidate is an approved employee of a wholesale dealer (TN only)

	// Beneficiary Group (Direct Dealer / Sub Dealer / Approved Employee)
	public string? BeneficiaryGroup { get; set; }                        // "Direct Dealer", "Sub Dealer", or "Approved Employee"
	public int? SubDealerId { get; set; }                                 // FK to SubDealerRegistration (when BeneficiaryGroup = Sub Dealer)
	public string? SubDealerName { get; set; }                            // Sub Dealer name snapshot at submission
	public int? EmployeeId { get; set; }                                  // Employee ID reference (when BeneficiaryGroup = Approved Employee)
	public string? EmployeeName { get; set; }                             // Employee name snapshot at submission

	// Sales history (calculated from CreditLimitSales, stored at submission)
	public decimal? AverageQuantityLifted3Years { get; set; }             // 3-year average quantity in MT
	public decimal? LastYearQuantityLifted { get; set; }                  // Last financial year quantity in MT

	// Declaration
	public bool IsDeclarationConfirmed { get; set; }                     // Whether the dealer ticked the declaration checkbox before submitting

	// Audit
	public string? CreatedBy { get; set; }                               // User/dealer who created the application
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the application record was created
	public string? UpdatedBy { get; set; }                               // User who last updated the application
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the application record was last updated

	// Relationships
	public ICollection<WelfareApplicationDocument> Documents { get; set; } = new List<WelfareApplicationDocument>(); // Uploaded documents for this application
	public ICollection<WelfareApplicationApproval> Approvals { get; set; } = new List<WelfareApplicationApproval>(); // Approval history (MO / RM / SMM) for this application
}


public class WelfareApplicationDocument
{
	public int Id { get; set; }                                          // Primary key
	public int WelfareApplicationId { get; set; }                        // FK to the application this document belongs to
	public WelfareApplication? WelfareApplication { get; set; }          // Navigation to the parent application

	public string? DocumentType { get; set; }                            // Category of the uploaded document (e.g. "Invitation", "Aadhaar")
	public string? DocumentName { get; set; }                            // Display name of the uploaded document
	public string? FileName { get; set; }                                // Original name of the uploaded file
	public string? FilePath { get; set; }                                // Stored path/location of the file on the server
	public string? ContentType { get; set; }                             // MIME type of the file (e.g. "application/pdf", "image/jpeg")
	public long? FileSize { get; set; }                                  // Size of the file in bytes
	public bool IsVerified { get; set; }                                 // Whether the approving officer marked this document as verified
	public string? UploadedBy { get; set; }                              // User who uploaded the document
	public DateTime UploadedAt { get; set; } = DateTime.Now;             // When the document was uploaded
}

public class WelfareApplicationApproval
{
	public int Id { get; set; }                                          // Primary key
	public int WelfareApplicationId { get; set; }                        // FK to the application being approved
	public WelfareApplication? WelfareApplication { get; set; }          // Navigation to the parent application

	// Reuses the existing AppRole enum (MO / RM / SMM levels)
	public AppRole ApprovalLevel { get; set; }                           // Which role performed/owns this step (MO, RM or SMM)
	public WelfareApprovalStatus ApprovalStatus { get; set; } = WelfareApprovalStatus.Pending; // Outcome of this step (Pending/Approved/Rejected)
	public string? Remarks { get; set; }                                 // Validation/rejection remarks entered by the officer
	public string? ApprovedBy { get; set; }                              // Name/Id of the officer who acted on this step
	public DateTime? ApprovedAt { get; set; }                            // When the officer approved/rejected this step

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the approval record
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the approval record was created
	public string? UpdatedBy { get; set; }                               // User who last updated the approval record
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the approval record was last updated
}

public enum WelfareApplicationStatus
{
    Draft = 0,        // Started but not yet submitted by the dealer
    Submitted = 1,    // Submitted by dealer, awaiting MO review
    MOReview = 2,     // Under Marketing Officer review
    RMReview = 3,     // Under Regional Manager review
    SMReview = 4,     // Under Senior Manager (SMM) review
    Approved = 5,     // Fully approved, benefit can be released
    Rejected = 6,     // Rejected at any approval level, process stopped
    Cancelled = 7     // Cancelled by dealer/office before approval
}

public enum WelfareApprovalStatus
{
    Pending = 0,   // Approval step not yet acted upon
    Approved = 1,  // Level approved/recommended and forwarded
    Rejected = 2   // Level rejected the application
}

public enum WelfareSchemeType
{
    MedicalAssistance = 0,
    Wedding = 1,
    Grahapravesam = 2,
    EducationalAssistance = 3,
    Sathabhishekam = 4,
    DeathRelief = 5,
    MeritAward = 6,
    DistinctionAward = 7
}

