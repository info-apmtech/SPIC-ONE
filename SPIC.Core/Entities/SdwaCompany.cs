namespace SPIC.Core.Entities;

/// <summary>
/// Master record for SDWA Company Details. Stores company information and maps
/// companies to the guest houses where they are applicable.
/// </summary>
public class SdwaCompany
{
	public int Id { get; set; }                                          // Primary key
	public string CompanyName { get; set; } = string.Empty;             // Full company name
	public string? ShortCode { get; set; }                              // Short name / code (e.g. "GFL")
	public string? GSTIN { get; set; }                                  // GST Identification Number
	public bool IsActive { get; set; } = true;                          // Whether the company is active

	// Audit
	public string? CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public string? UpdatedBy { get; set; }
	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	// Relationships
	public ICollection<SdwaCompanyGuestHouse> CompanyGuestHouses { get; set; } = new List<SdwaCompanyGuestHouse>();
}

/// <summary>
/// Junction table linking a company to one or more guest houses.
/// A company can be associated with multiple guest houses.
/// </summary>
public class SdwaCompanyGuestHouse
{
	public int Id { get; set; }                                          // Primary key
	public int SdwaCompanyId { get; set; }                               // FK to the company
	public SdwaCompany? SdwaCompany { get; set; }                       // Navigation to the company
	public int GuestHouseId { get; set; }                                // FK to the guest house
	public GuestHouse? GuestHouse { get; set; }                          // Navigation to the guest house
}
