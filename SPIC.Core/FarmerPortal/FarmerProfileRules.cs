using System.Net.Mail;
using System.Text.RegularExpressions;
using SPIC.Core.DTOs;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Farmer profile rules shared by the API (authoritative) and the onboarding form: field
/// validation, the "profile complete" rule, the language list and the crop chips. Whether the
/// state / district ids exist is a database question and stays in FarmerProfileController.
/// </summary>
public static partial class FarmerProfileRules
{
    public const int NameMin = 2;
    public const int NameMax = 100;
    public const int VillageMax = 100;
    public const int TalukMax = 100;
    public const int EmailMax = 256;
    public const int CropMax = 40;
    public const int MaxCrops = 15;
    public const decimal LandAcresMax = 10000m;

    /// <summary>Language code -> name shown in its own script. AI answers follow this choice.</summary>
    public static readonly IReadOnlyList<(string Code, string Name)> Languages = new[]
    {
        ("en", "English"),
        ("ta", "தமிழ் (Tamil)"),
        ("te", "తెలుగు (Telugu)"),
        ("hi", "हिन्दी (Hindi)"),
        ("mr", "मराठी (Marathi)"),
        ("kn", "ಕನ್ನಡ (Kannada)"),
        ("ml", "മലയാളം (Malayalam)")
    };

    public const string DefaultLanguage = "en";

    /// <summary>Crop chips offered at onboarding (a farmer can add others).</summary>
    public static readonly IReadOnlyList<string> CommonCrops = new[]
    {
        "Paddy", "Sugarcane", "Banana", "Coconut", "Cotton", "Groundnut", "Maize", "Millets",
        "Pulses", "Turmeric", "Chilli", "Onion", "Tomato", "Vegetables", "Wheat"
    };

    [GeneratedRegex(@"^[0-9]{6}$")]
    private static partial Regex PinPattern();

    public static bool IsKnownLanguage(string? code) =>
        !string.IsNullOrWhiteSpace(code) && Languages.Any(l => l.Code == code);

    /// <summary>Complete = Name, State, District, Village and PinCode are all set.</summary>
    public static bool IsComplete(string? name, int stateId, int districtId, string? village, string? pinCode) =>
        !string.IsNullOrWhiteSpace(name) && stateId > 0 && districtId > 0
        && !string.IsNullOrWhiteSpace(village) && !string.IsNullOrWhiteSpace(pinCode);

    /// <summary>
    /// Field errors in plain words, keyed by the camelCase DTO property name. Empty when valid.
    /// </summary>
    public static Dictionary<string, string> Validate(FarmerProfileUpsertDto dto)
    {
        var errors = new Dictionary<string, string>();

        var name = dto.Name?.Trim() ?? "";
        if (name.Length < NameMin || name.Length > NameMax)
            errors["name"] = $"Please enter your name ({NameMin} to {NameMax} letters).";

        var email = dto.Email?.Trim();
        if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
            errors["email"] = "This email does not look right. You can leave it empty.";

        if (dto.StateId <= 0) errors["stateId"] = "Please choose your state.";
        if (dto.DistrictId <= 0) errors["districtId"] = "Please choose your district.";

        var village = dto.Village?.Trim() ?? "";
        if (village.Length == 0) errors["village"] = "Please enter your village.";
        else if (village.Length > VillageMax) errors["village"] = $"Village name is too long (up to {VillageMax} letters).";

        if ((dto.Taluk?.Trim().Length ?? 0) > TalukMax)
            errors["taluk"] = $"Taluk name is too long (up to {TalukMax} letters).";

        if (!PinPattern().IsMatch(dto.PinCode?.Trim() ?? ""))
            errors["pinCode"] = "Pincode must be 6 digits.";

        if (dto.LandAcres is { } acres && (acres < 0 || acres > LandAcresMax))
            errors["landAcres"] = $"Land must be between 0 and {LandAcresMax:0} acres.";

        if (dto.Latitude.HasValue != dto.Longitude.HasValue)
            errors["latitude"] = "Location is incomplete. Please tap \"Use my location\" again.";
        else if (dto.Latitude is { } lat && dto.Longitude is { } lng
                 && (lat < -90 || lat > 90 || lng < -180 || lng > 180 || double.IsNaN(lat) || double.IsNaN(lng)))
            errors["latitude"] = "Location is not valid. Please tap \"Use my location\" again.";

        if (dto.IrrigationType is { } irrigation && !Enum.IsDefined(irrigation))
            errors["irrigationType"] = "Please choose a water source from the list.";

        if (!string.IsNullOrWhiteSpace(dto.PreferredLanguage) && !IsKnownLanguage(dto.PreferredLanguage.Trim()))
            errors["preferredLanguage"] = "Please choose a language from the list.";

        var crops = CleanCrops(dto.PrimaryCrops);
        if (crops.Count > MaxCrops) errors["primaryCrops"] = $"Please choose up to {MaxCrops} crops.";
        else if (crops.Any(c => c.Length > CropMax)) errors["primaryCrops"] = $"A crop name is too long (up to {CropMax} letters).";

        return errors;
    }

    /// <summary>Trimmed, comma-free, de-duplicated (case-insensitive) crop names, in the given order.</summary>
    public static List<string> CleanCrops(IEnumerable<string?>? crops)
    {
        var result = new List<string>();
        if (crops is null) return result;
        foreach (var raw in crops)
        {
            var crop = (raw ?? "").Replace(",", " ").Trim();
            if (crop.Length == 0) continue;
            if (!result.Contains(crop, StringComparer.OrdinalIgnoreCase)) result.Add(crop);
        }
        return result;
    }

    public static string? JoinCrops(IEnumerable<string?>? crops)
    {
        var clean = CleanCrops(crops);
        return clean.Count == 0 ? null : string.Join(",", clean);
    }

    public static List<string> SplitCrops(string? stored) =>
        CleanCrops(stored?.Split(',', StringSplitOptions.RemoveEmptyEntries));

    private static bool IsValidEmail(string email)
    {
        if (email.Length > EmailMax || email.Contains(' ')) return false;
        try
        {
            var address = new MailAddress(email);
            var at = email.IndexOf('@');
            return address.Address == email && at > 0 && email.IndexOf('.', at) > at + 1 && !email.EndsWith('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
