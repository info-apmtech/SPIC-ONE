using SPIC.Core.Entities;
using static SPIC.Core.DTOs.AdminViewModel;

namespace SPIC.Core.DTOs;

// Farmer Portal contracts (docs/farmer-portal.md). Routes:
//   POST api/FarmerAuth/otp/request   FarmerOtpRequestDto  -> FarmerOtpRequestResponse (429 / 409 / 503 carry { message })
//   POST api/FarmerAuth/otp/verify    FarmerOtpVerifyDto   -> FarmerOtpVerifyResponse  (400 / 403 / 409 carry { message })
//   GET  api/Farmer/profile                                -> FarmerProfileDto, or 404 FarmerProfileMissingDto
//   PUT  api/Farmer/profile           FarmerProfileUpsertDto -> FarmerProfileDto (400 carries { message, errors })

public class FarmerOtpRequestDto
{
    public string? Phone { get; set; }
}

public class FarmerOtpRequestResponse
{
    public bool Success { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendInSeconds { get; set; }
    /// <summary>Only in the Development environment with Sms:Provider = Log, so a developer can sign in.</summary>
    public string? DevCode { get; set; }
}

public class FarmerOtpVerifyDto
{
    public string? Phone { get; set; }
    public string? Code { get; set; }
}

/// <summary>The normal login response (same JWT + refresh token pair) plus the farmer flags.</summary>
public class FarmerOtpVerifyResponse : LoginResponseModel
{
    public bool ProfileComplete { get; set; }
    public bool IsNewUser { get; set; }

    public static FarmerOtpVerifyResponse From(LoginResponseModel login, bool profileComplete, bool isNewUser) => new()
    {
        Token = login.Token,
        User = login.User,
        Expiration = login.Expiration,
        RoleAccess = login.RoleAccess,
        DesignationName = login.DesignationName,
        RefreshToken = login.RefreshToken,
        ProfileComplete = profileComplete,
        IsNewUser = isNewUser
    };
}

/// <summary>Error body of the OTP endpoints: a plain-words message, plus the wait for a 429.</summary>
public class FarmerAuthError
{
    public string Message { get; set; } = string.Empty;
    public int? RetryAfterSeconds { get; set; }
}

public class FarmerProfileUpsertDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int StateId { get; set; }
    public int DistrictId { get; set; }
    public string? Village { get; set; }
    public string? Taluk { get; set; }
    public string? PinCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string>? PrimaryCrops { get; set; }
    public decimal? LandAcres { get; set; }
    public FarmerIrrigationType? IrrigationType { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class FarmerProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string? StateName { get; set; }
    public int DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public string Village { get; set; } = string.Empty;
    public string? Taluk { get; set; }
    public string PinCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> PrimaryCrops { get; set; } = new();
    public decimal? LandAcres { get; set; }
    public FarmerIrrigationType? IrrigationType { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public bool ProfileComplete { get; set; }
    /// <summary>True when a SAS farmer record (soil samples / reports) is linked to this account.</summary>
    public bool SasLinked { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>404 body of GET api/Farmer/profile: no profile yet.</summary>
public class FarmerProfileMissingDto
{
    public bool ProfileComplete { get; set; }
    public bool SasLinked { get; set; }
    /// <summary>The verified mobile, so onboarding can show it.</summary>
    public string? Phone { get; set; }
}

/// <summary>400 body of PUT api/Farmer/profile: field name (camelCase DTO name) -> plain-words message.</summary>
public class FarmerProfileErrorDto
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> Errors { get; set; } = new();
}
