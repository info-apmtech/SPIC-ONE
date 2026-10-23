using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
    public string? WholesaleMFMSId { get; set; }
    public string? RetailMFMSId { get; set; }
    public string? PANNo { get; set; }
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
    // Legacy payload compatibility ONLY. Never use this untrusted value for authorization.
    public string? SubmittedByRole { get; set; }
}

public partial class SubDealerFormModel
{
    // Shared profile validation. Bulk master DTOs intentionally do NOT use Active profile requirements.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();
        void Check(bool invalid, string message, string field)
        {
            if (invalid) errors.Add(new ValidationResult(message, new[] { field }));
        }
        if (CodeError(SubDealerCode) is string codeError)
            errors.Add(new ValidationResult(codeError, new[] { nameof(SubDealerCode) }));
        Check(StateId <= 0, "State is required.", nameof(StateId));
        Check(Region <= 0, "Region is required.", nameof(Region));
        Check(HQ <= 0, "HQ is required.", nameof(HQ));
        Check(string.IsNullOrWhiteSpace(FirmName), "Firm Name is required.", nameof(FirmName));
        Check(Status is not SubDealerStatus.Active and not SubDealerStatus.InActive,
            "Only Active or Inactive status is allowed.", nameof(Status));

        if (Status == SubDealerStatus.Active)
        {
            foreach (var item in new (string? Value, string Field, string Label)[]
            {
                (ShopNoORRoomNoOrBlockNo, nameof(ShopNoORRoomNoOrBlockNo), "Shop No / Room No / Block No"),
                (Street, nameof(Street), "Street"), (SubVillage, nameof(SubVillage), "Sub Village"),
                (Village, nameof(Village), "Village"), (PinCode, nameof(PinCode), "Pincode"),
                (Block, nameof(Block), "Block"), (Taluk, nameof(Taluk), "Taluk"),
                (GoogleMapURL, nameof(GoogleMapURL), "Google Map URL"),
                (OfficialContactNumber, nameof(OfficialContactNumber), "Official Contact Number"),
                (WhatsAppNumber, nameof(WhatsAppNumber), "WhatsApp Number"),
                (AlternativeNumber, nameof(AlternativeNumber), "Alternative Number"),
                (RetailMFMSId, nameof(RetailMFMSId), "Retail mFMS ID"),
                (PANNo, nameof(PANNo), "PAN No"), (GSTNumber, nameof(GSTNumber), "GST Number"),
                (GSTFilePath, nameof(GSTFilePath), "GST Certificate PDF")
            }) Check(string.IsNullOrWhiteSpace(item.Value), item.Label + " is required for Active Sub Dealer.", item.Field);
            Check(!Latitude.HasValue, "Latitude is required.", nameof(Latitude));
            Check(!Longitude.HasValue, "Longitude is required.", nameof(Longitude));
            Check(DistrictId.GetValueOrDefault() <= 0, "District is required in Primary Location.", nameof(DistrictId));
            Check(DealerStateId.GetValueOrDefault() <= 0, "State is required in Primary Location.", nameof(DealerStateId));
        }

        // Inactive: no Active-only requirements, but entered values must still be valid.
        if (!string.IsNullOrWhiteSpace(PANNo))
            Check(!Regex.IsMatch(PANNo.Trim().ToUpperInvariant(), @"\A[A-Z]{5}[0-9]{4}[A-Z]\z"),
                "Invalid PAN format (e.g., ABCDE1234F).", nameof(PANNo));
        if (!string.IsNullOrWhiteSpace(GSTNumber))
            Check(!Regex.IsMatch(GSTNumber.Trim().ToUpperInvariant(), @"\A[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]\z"),
                "Invalid GST format (e.g., 22AAAAA0000A1Z5).", nameof(GSTNumber));
        if (!string.IsNullOrWhiteSpace(PinCode))
            Check(!Regex.IsMatch(PinCode.Trim(), @"\A[0-9]{6}\z"), "Pincode must contain exactly 6 digits.", nameof(PinCode));
        foreach (var item in new (string? Value, string Field)[]
        {
            (OfficialContactNumber, nameof(OfficialContactNumber)),
            (WhatsAppNumber, nameof(WhatsAppNumber)), (AlternativeNumber, nameof(AlternativeNumber))
        })
            if (!string.IsNullOrWhiteSpace(item.Value))
                Check(!Regex.IsMatch(item.Value.Trim(), @"\A[0-9]{10}\z"), "Contact number must contain exactly 10 digits.", item.Field);
        Check(Latitude.HasValue && (!double.IsFinite(Latitude.Value) || Latitude.Value < -90 || Latitude.Value > 90),
            "Latitude must be a finite value from -90 to 90.", nameof(Latitude));
        Check(Longitude.HasValue && (!double.IsFinite(Longitude.Value) || Longitude.Value < -180 || Longitude.Value > 180),
            "Longitude must be a finite value from -180 to 180.", nameof(Longitude));
        Check(Latitude.HasValue != Longitude.HasValue, "Enter both latitude and longitude together.", nameof(Latitude));
        Check(DistrictId.HasValue && DistrictId.Value <= 0, "Select a valid District or leave it empty.", nameof(DistrictId));
        Check(DealerStateId.HasValue && DealerStateId.Value <= 0, "Select a valid State or leave it empty.", nameof(DealerStateId));
        return errors;
    }

    public const int MaximumCodeLength = 50;

    // Identity comparison only. Do not persist this key over an existing master code.
    public static string CodeKey(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();
    public static string CodeValue(string? code) => (code ?? string.Empty).Trim();

    public static string? CodeError(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return "Sub Dealer Code is required. Enter the actual master code; this row was not saved.";

        var code = rawCode.Trim();
        if (code.Length > MaximumCodeLength)
            return "Sub Dealer Code must not exceed 50 characters; this row was not saved.";

        var hasVisibleBase = false;
        foreach (var rune in code.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            var value = rune.Value;
            if (category is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate ||
                value == 0xFFFD || value is 0x034F or 0x115F or 0x1160 or 0x17B4 or 0x17B5 or 0x3164 or 0xFFA0 or 0x2800 ||
                (value >= 0xFE00 && value <= 0xFE0F) || (value >= 0xE0100 && value <= 0xE01EF))
            {
                return "Sub Dealer Code contains hidden/invisible characters. Re-enter the actual master code; this row was not saved.";
            }

            if (!Rune.IsWhiteSpace(rune) &&
                category is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or
                                 UnicodeCategory.EnclosingMark or UnicodeCategory.OtherNotAssigned))
                hasVisibleBase = true;
        }

        return hasVisibleBase ? null :
            "Sub Dealer Code is visually empty. Enter the actual master code; this row was not saved.";
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

public class SubDealerImportRejectedRowDto
{
    public int ExcelRowNumber { get; set; }
    public string SubDealerCode { get; set; } = string.Empty;
    public string SubDealerName { get; set; } = string.Empty;
    public string Category { get; set; } = SubDealerImportRejectCategory.Validation;
    public string Reason { get; set; } = string.Empty;
}

public class SubDealerBulkImportResponse
{
    // Actual save counts when PreviewOnly=false. During preview these are planned counts.
    public int Inserted { get; set; }
    public int Updated { get; set; }

    // Existing rows whose FirmName/State/Region/HQ already exactly match the Excel master.
    // These rows are intentionally NOT written again, so UpdatedAt is not changed unnecessarily.
    public int Unchanged { get; set; }

    // PreviewOnly responses never mutate the database. The UI must explicitly call the
    // endpoint again with Confirm=true before any valid rows are saved.
    public bool PreviewOnly { get; set; }
    public bool RequiresConfirmation { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<SubDealerImportRejectedRowDto> Rejected { get; set; } = new();
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
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ShopNoORRoomNoOrBlockNo { get; set; }
    public string? Street { get; set; }
    public string? SubVillage { get; set; }
    public string? Village { get; set; }
    public string? Block { get; set; }
    public string? Taluk { get; set; }
    public int? DistrictId { get; set; }
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

public static class SubDealerImportRejectCategory
{
    public const string Duplicate = "Duplicate";
    public const string Validation = "Validation";
    public const string DatabaseError = "Database error";
}
