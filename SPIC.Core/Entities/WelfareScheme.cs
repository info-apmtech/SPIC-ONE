namespace SPIC.Core.Entities;


public class WelfareScheme
{
	public int Id { get; set; }                                          // Primary key
	public string SchemeName { get; set; } = string.Empty;               // Name of the scheme (e.g. "Grahapravesam")
	public string? Description { get; set; }                             // Short summary shown on the scheme card
	public string? Category { get; set; }                                // Grouping used by the filter tabs (e.g. "Family Welfare", "Medical", "Education", "Emergency Support")
	public int? DocumentsRequired { get; set; }                          // Number of documents displayed on the card (e.g. "2 required")
	public string? EligibilityDescription { get; set; }                  // Eligibility conditions text shown at the top of the apply form
	public bool IsActive { get; set; } = true;                           // Whether the scheme is visible/enabled for applications

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the scheme record
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the scheme record was created
	public string? UpdatedBy { get; set; }                               // User who last updated the scheme record
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the scheme record was last updated

	// Relationships
	public ICollection<WelfareApplication> Applications { get; set; } = new List<WelfareApplication>();                        // All welfare applications raised against this scheme
	public ICollection<WelfareSchemeDocumentRequirement> DocumentRequirements { get; set; } = new List<WelfareSchemeDocumentRequirement>(); // List of documents a dealer must upload for this scheme
}

public class WelfareSchemeDocumentRequirement
{
	public int Id { get; set; }                                          // Primary key
	public int WelfareSchemeId { get; set; }                             // FK to the WelfareScheme this requirement belongs to
	public WelfareScheme? WelfareScheme { get; set; }                    // Navigation to the parent scheme

	public string? DocumentType { get; set; }                            // Category of the document (e.g. "Invitation", "Proof", "ID Card")
	public string? DocumentName { get; set; }                            // Display name shown on the upload UI (e.g. "House Ownership Proof")
	public bool IsMandatory { get; set; } = true;                        // Whether the dealer must upload this document to submit
	public string? AllowedFileTypes { get; set; }                        // Accepted formats, comma separated (e.g. "PDF,JPG,PNG")
	public long? MaxFileSize { get; set; }                               // Maximum file size in bytes (e.g. 5MB = 5242880)
	public bool IsActive { get; set; } = true;                           // Whether this requirement is currently enforced

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the requirement
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the requirement was created
	public string? UpdatedBy { get; set; }                               // User who last updated the requirement
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the requirement was last updated
}

public class WelfareApplication
{
	public int Id { get; set; }                                          // Primary key
	public int DealerId { get; set; }                                    // FK to the DealerRegistration applying for the scheme
	public DealerRegistration? Dealer { get; set; }                      // Navigation to the applying dealer
	public int WelfareSchemeId { get; set; }                             // FK to the WelfareScheme being applied for
	public WelfareScheme? WelfareScheme { get; set; }                    // Navigation to the applied scheme

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

	// Quantity / Eligibility
	public int? FinancialYearId { get; set; }                            // FK to the FinancialYear selected on the form
	public FinancialYear? FinancialYear { get; set; }                    // Navigation to the selected financial year
	public decimal? QuantityLifted { get; set; }                         // Cases/quantity lifted in the selected financial year

	// Beneficiary Information
	public string? BeneficiaryName { get; set; }                         // Name of the person receiving the benefit
	public string? Relationship { get; set; }                            // Beneficiary's relationship to the dealer (e.g. Spouse, Child, Parent)
	public DateTime? BeneficiaryDateOfBirth { get; set; }                // Beneficiary's date of birth (for schemes like Sathabhishekam)
	public string? NomineeName { get; set; }                             // Name of the nominee for the benefit
	public string? NomineeRelationship { get; set; }                     // Nominee's relationship to the dealer
	public string? BeneficiaryNameAsInCheque { get; set; }               // Name exactly as printed on the cheque leaf / bank passbook
	public string? LeafOrBankPassbook { get; set; }                      // Which document the name was taken from (e.g. "Cheque Leaf" / "Bank Passbook")

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

