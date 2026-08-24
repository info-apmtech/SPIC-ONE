using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

public class SubDealerFormModel
{
	public int Id { get; set; }
	public string? SubDealerCode { get; set; }

	public int StateId { get; set; }
	public int Region { get; set; }
	public int HQ { get; set; }
	public SubDealerStatus Status { get; set; } = SubDealerStatus.Active;

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