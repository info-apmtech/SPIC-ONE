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
}
