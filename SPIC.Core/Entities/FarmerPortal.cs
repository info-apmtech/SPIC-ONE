namespace SPIC.Core.Entities;

// Farmer Portal (docs/farmer-portal.md, 2026-11-27). Self sign-up farmers: phone OTP login and the
// one-time profile every later farmer feature (home, dealer locator, advice, forecasts) reads.

/// <summary>
/// One OTP sent to a mobile number. Only the SHA-256 hash of the code is stored
/// (<see cref="SPIC.Core.FarmerPortal.FarmerOtpRules.HashCode"/>). Rows also drive the send limits
/// (per number and per client IP); rows older than a day are deleted by the next request.
/// All timestamps are UTC.
/// </summary>
public class FarmerOtpChallenge
{
    public long Id { get; set; }
    /// <summary>Normalised 10-digit Indian mobile number.</summary>
    public string Phone { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    /// <summary>Wrong codes entered so far; the challenge is dead at <see cref="SPIC.Core.FarmerPortal.FarmerOtpRules.MaxAttempts"/>.</summary>
    public int Attempts { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public string? RequestIp { get; set; }
}

public enum FarmerIrrigationType
{
    Rainfed,
    Canal,
    Borewell,
    Drip,
    Sprinkler,
    Mixed
}

/// <summary>
/// The farmer's own profile, one row per farmer Identity user (UserId unique). Filled once at
/// onboarding and editable from /farmer/profile. Complete when Name, State, District, Village and
/// PinCode are set (<see cref="SPIC.Core.FarmerPortal.FarmerProfileRules.IsComplete"/>).
/// </summary>
public class FarmerProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    /// <summary>The OTP-verified mobile of the account (never taken from the client).</summary>
    public string Phone { get; set; } = string.Empty;
    public int StateId { get; set; }
    public int DistrictId { get; set; }
    public string Village { get; set; } = string.Empty;
    public string? Taluk { get; set; }
    public string PinCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    /// <summary>Comma separated crop names, e.g. "Paddy,Banana".</summary>
    public string? PrimaryCrops { get; set; }
    public decimal? LandAcres { get; set; }
    public FarmerIrrigationType? IrrigationType { get; set; }
    /// <summary>One of <see cref="SPIC.Core.FarmerPortal.FarmerProfileRules.Languages"/>; "en" by default.</summary>
    public string PreferredLanguage { get; set; } = "en";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>When (UTC) the farmer last accepted the privacy notice (final fix wave, V42): at the first profile
    /// save, and at a sign-in that continues past the login consent line when the version moved on.</summary>
    public DateTime? PrivacyAcceptedAt { get; set; }

    /// <summary>The privacy notice version accepted (FarmerPrivacyRules.Version).</summary>
    public string? PrivacyVersion { get; set; }
}

// Farmer Portal Task 2 (docs/farmer-portal.md, 2026-11-28): call-back requests a farmer sends to one of
// their SPIC field staff (JMDO / MDO / RM / RMD) from /farmer/team; the staff member handles them on
// /farmer-requests.

/// <summary>What a call-back request is about (stored as text).</summary>
public enum FarmerContactTopic
{
    Fertilizer,
    CropProblem,
    SoilTest,
    Product,
    Dealer,
    Other
}

/// <summary>Open (new), Contacted (staff called), Closed (done). Stored as text.</summary>
public enum FarmerContactRequestStatus
{
    Open,
    Contacted,
    Closed
}

/// <summary>
/// One "please call me back" request from a farmer to one staff member. The staff member must be one
/// of the farmer's resolved contacts when the request is made
/// (<see cref="SPIC.Core.FarmerPortal.FarmerTeamRules"/>); at most
/// <see cref="SPIC.Core.FarmerPortal.FarmerContactRequestRules.MaxOpenPerStaff"/> Open requests per farmer
/// per staff member. Only the addressed staff member changes the status / note. Timestamps are UTC.
/// </summary>
public class FarmerContactRequest
{
    public int Id { get; set; }
    /// <summary>The farmer's Identity user id (AppUsers.Id).</summary>
    public string FarmerUserId { get; set; } = string.Empty;
    /// <summary>The staff member's Identity user id (Employeelogins.UserId).</summary>
    public string StaffUserId { get; set; } = string.Empty;
    /// <summary>The staff member's role when the request was made (JMDO / MDO / RM / RMD), for the label.</summary>
    public AppRole StaffRole { get; set; }
    public FarmerContactTopic Topic { get; set; }
    /// <summary>The farmer's own words, at most 500 characters ("" when none).</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>One of <see cref="SPIC.Core.FarmerPortal.FarmerContactRequestRules.PreferredTimes"/> keys, or null.</summary>
    public string? PreferredTime { get; set; }
    public FarmerContactRequestStatus Status { get; set; }
    /// <summary>The staff member's note (not shown to the farmer).</summary>
    public string? StaffNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
