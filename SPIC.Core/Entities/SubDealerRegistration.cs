using System;

namespace SPIC.Core.Entities;

public enum SubDealerStatus
{
    Active = 0,
    InActive = 1
}

public class SubDealerRegistration
{
    public int Id { get; set; }
    // Actual Admin-imported code. NEVER generate SD+Id. Nullable only for reading legacy
    // invalid records so SuperAdmin can repair them; all application writes validate the code.
    // Apply the database guard separately after the preflight in README_DEPLOYMENT.md.
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
    public string? WholesaleMFMSId { get; set; }
    public string? RetailMFMSId { get; set; }
    public string? PANNo { get; set; }
    public string? GSTNumber { get; set; }
    public string? GSTLegalName { get; set; }
    public string? GSTTradeName { get; set; }
    public string? GSTConstitutionofBusiness { get; set; }
    public string? GSTFilePath { get; set; }
    // The API assigns audit fields. Original creation audit is preserved on every edit/reset.
    // Initial CreatedAt == UpdatedAt is expected; later edits update only UpdatedAt/UpdatedBy.
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
