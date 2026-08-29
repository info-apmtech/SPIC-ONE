using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

public partial class SubDealerFormModel : IValidatableObject
{
	public int Id { get; set; }
	public string? SubDealerCode { get; set; }

	[Range(1, int.MaxValue, ErrorMessage = "State is required.")]
	public int StateId { get; set; }

	[Range(1, int.MaxValue, ErrorMessage = "Region is required.")]
	public int Region { get; set; }

	[Range(1, int.MaxValue, ErrorMessage = "HQ is required.")]
	public int HQ { get; set; }
	public SubDealerStatus Status { get; set; } = SubDealerStatus.Active;

	[Required(ErrorMessage = "Firm Name is required.")]
	public string FirmName { get; set; } = string.Empty;

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

	// mFMS / PAN
	public string? WholesaleMFMSId { get; set; }
	public string? RetailMFMSId { get; set; }

	// PAN format is validated in Validate() (trim/uppercase-aware and only
	// when a value is actually entered), matching backend and frontend rules.
	public string? PANNo { get; set; }

	// Kept because your current DTO already contains these properties.
	// They do not change the current Sub Dealer UI.
	public decimal? SpicTradeDepositAmount { get; set; }
	public string? SpicTradeDepositReceiptNo { get; set; }
	public DateTime? SpicTradeDepositDate { get; set; }

	public decimal? GreenstarTradeDepositAmount { get; set; }
	public string? GreenstarTradeDepositReceiptNo { get; set; }
	public DateTime? GreenstarTradeDepositDate { get; set; }

	public string? GSTNumber { get; set; }
	public string? GSTLegalName { get; set; }
	public string? GSTTradeName { get; set; }
	public string? GSTConstitutionofBusiness { get; set; }
	public string? GSTFilePath { get; set; }

	public string? CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? UpdatedBy { get; set; }
	public DateTime UpdatedAt { get; set; }

	// UI-to-API fallback when role claims are unavailable in local/dev.
	// The server prefers the authenticated role claim when present.
	public string? SubmittedByRole { get; set; }
}

// Implement conditional and cross-field validation for shared client/server use.
public partial class SubDealerFormModel
{
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (Status is not SubDealerStatus.Active and not SubDealerStatus.InActive)
			yield return new ValidationResult("Only Active or Inactive status is allowed.", new[] { nameof(Status) });

		if (Status == SubDealerStatus.Active)
		{
			if (string.IsNullOrWhiteSpace(RetailMFMSId))
				yield return new ValidationResult("Retail mFMS ID is required for Active Sub Dealer.", new[] { nameof(RetailMFMSId) });

			if (string.IsNullOrWhiteSpace(PANNo))
				yield return new ValidationResult("PAN No is required for Active Sub Dealer.", new[] { nameof(PANNo) });
		}

		if (!string.IsNullOrWhiteSpace(PANNo))
		{
			var pan = PANNo!.Trim().ToUpperInvariant();
			if (!Regex.IsMatch(pan, "^[A-Z]{5}[0-9]{4}[A-Z]$"))
				yield return new ValidationResult("Invalid PAN format (e.g., ABCDE1234F).", new[] { nameof(PANNo) });
		}
	}
}

public class SubDealerLookupDto
{
	public int Id { get; set; }
	public string SubDealerCode { get; set; } = string.Empty;
	public string FirmName { get; set; } = string.Empty;
	public int StateId { get; set; }
	public int RegionId { get; set; }
	public int HQId { get; set; }
}

public class SubDealerFileUploadResponse
{
	public string FilePath { get; set; } = string.Empty;
}

public class GstOcrExtractResult
{
	public string? GST { get; set; }
	public string? GSTLegalName { get; set; }
	public string? GSTTradeName { get; set; }
	public string? GSTConstitutionofBusiness { get; set; }
}

public class SubDealerExcelRowDto
{
	public int RowNumber { get; set; }
	public string SubDealerCode { get; set; } = string.Empty;
	public string SubDealerName { get; set; } = string.Empty;
	public string HQ { get; set; } = string.Empty;
	public string Region { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
}

public class SubDealerExcelParseResponse
{
	public int TotalRows { get; set; }
	public List<SubDealerExcelRowDto> Rows { get; set; } = new();
	public List<string> Errors { get; set; } = new();
}

public class SubDealerBulkImportRowDto
{
	public int ExcelRowNumber { get; set; }
	public string SubDealerCode { get; set; } = string.Empty;
	public string FirmName { get; set; } = string.Empty;
	public int StateId { get; set; }
	public int RegionId { get; set; }
	public int HQId { get; set; }
}

public class SubDealerBulkImportRequest
{
	public string? ImportedBy { get; set; }
	public List<SubDealerBulkImportRowDto> Rows { get; set; } = new();
}

public class SubDealerBulkImportResponse
{
	public int Inserted { get; set; }
	public int Updated { get; set; }
	public List<string> Errors { get; set; } = new();
}


public class SubDealerListItemDto
{
	public int Id { get; set; }
	public string SubDealerCode { get; set; } = string.Empty;
	public string FirmName { get; set; } = string.Empty;

	public int StateId { get; set; }
	public int RegionId { get; set; }
	public int HQId { get; set; }

	public SubDealerStatus Status { get; set; }

	public string? OfficialContactNumber { get; set; }
	public string? WhatsAppNumber { get; set; }

	public string? GSTNumber { get; set; }
	public string? PANNo { get; set; }

	public string? WholesaleMFMSId { get; set; }
	// -------------------------------------------------
	// Export Location Details
	// -------------------------------------------------
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }

	public string? ShopNoORRoomNoOrBlockNo { get; set; }
	public string? Street { get; set; }
	public string? SubVillage { get; set; }
	public string? Village { get; set; }
	public string? Block { get; set; }
	public string? Taluk { get; set; }
	public string? PinCode { get; set; }
	public string? RetailMFMSId { get; set; }

	public DateTime UpdatedAt { get; set; }
}

public class SubDealerPagedListResponse
{
	public List<SubDealerListItemDto> Items { get; set; } = new();

	public int Page { get; set; }
	public int PageSize { get; set; }

	public int TotalCount { get; set; }
	public int TotalPages { get; set; }

	public int ActiveCount { get; set; }
	public int InactiveCount { get; set; }
}