using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;

namespace SPIC.Core.DTOs;

// Farmer Portal Task 2 contracts (docs/farmer-portal.md): dealer locator, "My SPIC team" and
// call-back requests.
//   GET  api/Farmer/dealers?lat&lng&radiusKm&districtId&q&page&pageSize -> FarmerDealerListDto
//   GET  api/Farmer/contacts                                          -> FarmerTeamDto
//   POST api/Farmer/contacts/{contactId}/request  FarmerContactRequestCreateDto -> FarmerContactRequestDto
//                                                 (400 / 409 carry FarmerProfileErrorDto)
//   GET  api/Farmer/contacts/requests                                 -> List<FarmerContactRequestDto>
//   GET  api/FarmerRequests?status&page&pageSize  (staff)             -> StaffFarmerRequestListDto
//   PUT  api/FarmerRequests/{id}  FarmerRequestUpdateDto  (addressed staff) -> StaffFarmerRequestDto

/// <summary>One dealer card: public business details only (never PAN, bank, GST or owner data).</summary>
public class FarmerDealerDto
{
    public int Id { get; set; }
    public string FirmName { get; set; } = string.Empty;
    /// <summary>Shop / door number and street.</summary>
    public string? AddressLine1 { get; set; }
    /// <summary>Sub-village, village and taluk.</summary>
    public string? AddressLine2 { get; set; }
    public string? Village { get; set; }
    public string? DistrictName { get; set; }
    public string? PinCode { get; set; }
    /// <summary>The official contact number, normalised for dialling (null when unusable).</summary>
    public string? Phone { get; set; }
    /// <summary>10-digit WhatsApp mobile (null when none / not a mobile).</summary>
    public string? WhatsApp { get; set; }
    /// <summary>The dealer's own Google Maps link (http / https only).</summary>
    public string? GoogleMapUrl { get; set; }
    /// <summary>Ready-made directions link (GPS, the map link, or a maps search).</summary>
    public string? DirectionsUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DistanceKm { get; set; }
}

public class FarmerDealerListDto
{
    /// <summary>Which search answered: Nearby (within RadiusKm), District, State or None.</summary>
    public FarmerDealerScope Scope { get; set; }
    public int RadiusKm { get; set; }
    /// <summary>True when the search used a GPS point (distances are filled).</summary>
    public bool UsedLocation { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public string? StateName { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<FarmerDealerDto> Items { get; set; } = new();
}

/// <summary>One person on the farmer's SPIC team.</summary>
public class FarmerContactDto
{
    /// <summary>Opaque id used in POST api/Farmer/contacts/{contactId}/request (a staff user id).</summary>
    public string ContactId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AppRole Role { get; set; }
    public string RoleLabel { get; set; } = string.Empty;
    /// <summary>Headquarter name (JMDO / MDO) or region name (RM / RMD).</summary>
    public string? AreaName { get; set; }
    /// <summary>"Headquarter" or "Region".</summary>
    public string? AreaKind { get; set; }
    /// <summary>Only when FarmerPortal:ShowStaffPhoneNumbers is true.</summary>
    public string? Phone { get; set; }
    /// <summary>Only when FarmerPortal:ShowStaffPhoneNumbers is true and the number is a mobile.</summary>
    public string? WhatsApp { get; set; }
    /// <summary>This farmer's Open requests to this person.</summary>
    public int OpenRequests { get; set; }
}

public class FarmerTeamDto
{
    public FarmerTeamBasis Basis { get; set; }
    public string? DistrictName { get; set; }
    public string? StateName { get; set; }
    /// <summary>Distance to the dealer used when Basis = NearestDealer.</summary>
    public double? NearestDealerKm { get; set; }
    /// <summary>FarmerPortal:ShowStaffPhoneNumbers.</summary>
    public bool PhonesShown { get; set; }
    public int MaxOpenPerStaff { get; set; } = FarmerContactRequestRules.MaxOpenPerStaff;
    public List<FarmerContactDto> Members { get; set; } = new();
}

public class FarmerContactRequestCreateDto
{
    public FarmerContactTopic? Topic { get; set; }
    public string? Message { get; set; }
    public string? PreferredTime { get; set; }
}

/// <summary>A request as the farmer sees it (the staff note is not shown to the farmer).</summary>
public class FarmerContactRequestDto
{
    public int Id { get; set; }
    public string ContactId { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public AppRole StaffRole { get; set; }
    public string StaffRoleLabel { get; set; } = string.Empty;
    public FarmerContactTopic Topic { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PreferredTime { get; set; }
    public FarmerContactRequestStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>A request as staff see it: the farmer asked to be called, so their phone and village are shown.</summary>
public class StaffFarmerRequestDto
{
    public int Id { get; set; }
    public string FarmerName { get; set; } = string.Empty;
    /// <summary>The full number for the addressed officer; masked ("98xxxxxx21") for anyone else.</summary>
    public string? FarmerPhone { get; set; }
    /// <summary>True when <see cref="FarmerPhone"/> is masked (no call link).</summary>
    public bool FarmerPhoneMasked { get; set; }
    public string? Village { get; set; }
    public string? DistrictName { get; set; }
    public string? StateName { get; set; }
    public FarmerContactTopic Topic { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PreferredTime { get; set; }
    public FarmerContactRequestStatus Status { get; set; }
    public string? StaffNote { get; set; }
    /// <summary>Who the request is addressed to (shown in the Admin view).</summary>
    public string StaffName { get; set; } = string.Empty;
    public AppRole StaffRole { get; set; }
    public string StaffRoleLabel { get; set; } = string.Empty;
    /// <summary>True only for the addressed staff member.</summary>
    public bool CanUpdate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class StaffFarmerRequestListDto
{
    /// <summary>Admin / SuperAdmin: every request, read-only.</summary>
    public bool AdminView { get; set; }
    public int Open { get; set; }
    public int Contacted { get; set; }
    public int Closed { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    /// <summary>Rows matching the status filter.</summary>
    public int Total { get; set; }
    public List<StaffFarmerRequestDto> Items { get; set; } = new();
}

public class FarmerRequestUpdateDto
{
    public FarmerContactRequestStatus? Status { get; set; }
    public string? Note { get; set; }
}
