namespace SPIC.Core.Entities;

public enum SubDealerStatus
{
	Active = 0,
	InActive = 1
}

public class SubDealerRegistration
{
	public int Id { get; set; }

	// Generated immediately on create because Sub Dealer has no approval workflow.
	public string? SubDealerCode { get; set; }

	// Basic Information
	public int StateId { get; set; }
	public int Region { get; set; }
	public int HQ { get; set; }
	public SubDealerStatus Status { get; set; } = SubDealerStatus.Active;

	// Business Information
	public string FirmName { get; set; } = string.Empty;

	// Primary Location
	public string? GoogleMapURL { get; set; }
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
	public string ShopNoORRoomNoOrBlockNo { get; set; } = string.Empty;
	public string? Street { get; set; }
	public string? SubVillage { get; set; }
	public string Village { get; set; } = string.Empty;
	public string PinCode { get; set; } = string.Empty;
	public string? Block { get; set; }
	public string? Taluk { get; set; }
	public int? DistrictId { get; set; }
	public int? DealerStateId { get; set; }
	public string OfficialContactNumber { get; set; } = string.Empty;
	public string WhatsAppNumber { get; set; } = string.Empty;
	public string? AlternativeNumber { get; set; }



	// GST only - no PAN / Aadhaar
	public string? GSTNumber { get; set; }
	public string? GSTLegalName { get; set; }
	public string? GSTTradeName { get; set; }
	public string? GSTConstitutionofBusiness { get; set; }
	public string? GSTFilePath { get; set; }

	// Audit
	public string? CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public string? UpdatedBy { get; set; }
	public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
