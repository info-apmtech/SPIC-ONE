using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// HTTP surface of the farmer portal's sign-in and profile (api/FarmerAuth, api/Farmer/profile;
/// contracts in SPIC.Core/DTOs/FarmerPortalDtos.cs). Unlike ApiClientBase calls these never toast:
/// the farmer screens show every problem inline in plain words, so each call returns a result
/// with the message instead. Registered with AddScoped in both hosts.
/// </summary>
public sealed class FarmerApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private const string OfflineMessage = "We could not reach SPIC. Please check your internet connection and try again.";

    private readonly HttpClient _http;

    public FarmerApi(HttpClient http) => _http = http;

    public sealed record OtpRequestResult(bool Ok, string? Message, int ResendInSeconds, string? DevCode);

    public sealed record OtpVerifyResult(FarmerOtpVerifyResponse? Session, string? Message);

    public sealed record ProfileResult(bool Ok, FarmerProfileDto? Profile, bool SasLinked, string? Phone, string? Message);

    public sealed record SaveResult(FarmerProfileDto? Profile, string? Message, IReadOnlyDictionary<string, string> Errors);

    public async Task<OtpRequestResult> RequestOtpAsync(string phone, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/FarmerAuth/otp/request", new FarmerOtpRequestDto { Phone = phone }, Json, ct);
            if (response.IsSuccessStatusCode)
            {
                var ok = await response.Content.ReadFromJsonAsync<FarmerOtpRequestResponse>(Json, ct);
                return new(true, null, ok?.ResendInSeconds ?? 30, ok?.DevCode);
            }

            var error = await ReadAsync<FarmerAuthError>(response, ct);
            return new(false, error?.Message is { Length: > 0 } m ? m : Fallback(response.StatusCode), error?.RetryAfterSeconds ?? 0, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(false, null, 0, null); }
        catch (Exception) { return new(false, OfflineMessage, 0, null); }
    }

    public async Task<OtpVerifyResult> VerifyOtpAsync(string phone, string code, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/FarmerAuth/otp/verify", new FarmerOtpVerifyDto { Phone = phone, Code = code }, Json, ct);
            if (response.IsSuccessStatusCode)
            {
                var session = await response.Content.ReadFromJsonAsync<FarmerOtpVerifyResponse>(Json, ct);
                return session is null || string.IsNullOrWhiteSpace(session.Token)
                    ? new(null, "Something went wrong. Please try again.")
                    : new(session, null);
            }

            var error = await ReadAsync<FarmerAuthError>(response, ct);
            return new(null, error?.Message is { Length: > 0 } m ? m : Fallback(response.StatusCode));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(null, null); }
        catch (Exception) { return new(null, OfflineMessage); }
    }

    /// <summary>The signed-in farmer's profile. A missing profile is Ok with Profile = null.</summary>
    public async Task<ProfileResult> GetProfileAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync("api/Farmer/profile", ct);
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<FarmerProfileDto>(Json, ct);
                return new(profile is not null, profile, profile?.SasLinked ?? false, profile?.Phone, null);
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var missing = await ReadAsync<FarmerProfileMissingDto>(response, ct);
                return new(true, null, missing?.SasLinked ?? false, missing?.Phone, null);
            }
            return new(false, null, false, null, Fallback(response.StatusCode));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(false, null, false, null, null); }
        catch (Exception) { return new(false, null, false, null, OfflineMessage); }
    }

    public async Task<SaveResult> SaveProfileAsync(FarmerProfileUpsertDto dto, CancellationToken ct = default)
    {
        var none = new Dictionary<string, string>();
        try
        {
            using var response = await _http.PutAsJsonAsync("api/Farmer/profile", dto, Json, ct);
            if (response.IsSuccessStatusCode)
                return new(await response.Content.ReadFromJsonAsync<FarmerProfileDto>(Json, ct), null, none);

            var error = await ReadAsync<FarmerProfileErrorDto>(response, ct);
            return new(null, error?.Message is { Length: > 0 } m ? m : Fallback(response.StatusCode), error?.Errors ?? none);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(null, null, none); }
        catch (Exception) { return new(null, OfflineMessage, none); }
    }

    /// <summary>State / district dropdowns: the existing location master endpoints.</summary>
    public Task<List<LocationOption>?> StatesAsync(CancellationToken ct = default) => OptionsAsync("api/State", "stateName", ct);

    public Task<List<LocationOption>?> DistrictsAsync(int stateId, CancellationToken ct = default) =>
        OptionsAsync($"api/District/byState/{stateId}", "districtName", ct);

    /// <summary>District and state names (and the block, used as taluk) for a pincode; null when unknown.</summary>
    public async Task<PincodePlace?> LookupPincodeAsync(string pincode, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"api/PincodeLookup/{pincode}", ct);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            // api.postalpincode.in: [ { "Status": "Success", "PostOffice": [ { "District", "State", "Block" } ] } ]
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;
            var first = doc.RootElement[0];
            if (!first.TryGetProperty("PostOffice", out var offices) || offices.ValueKind != JsonValueKind.Array || offices.GetArrayLength() == 0)
                return null;
            var office = offices[0];
            return new PincodePlace(Text(office, "State"), Text(office, "District"), Text(office, "Block"));
        }
        catch (Exception) { return null; }
    }

    public sealed record LocationOption(int Id, string Name);

    public sealed record PincodePlace(string? State, string? District, string? Block);

    private async Task<List<LocationOption>?> OptionsAsync(string url, string nameProperty, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var list = new List<LocationOption>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var id) && id.TryGetInt32(out var value))
                    list.Add(new LocationOption(value, Text(item, nameProperty) ?? $"#{value}"));
            }
            return list.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception) { return null; }
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct) where T : class
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return body.TrimStart().StartsWith('{') ? JsonSerializer.Deserialize<T>(body, Json) : null;
        }
        catch (Exception) { return null; }
    }

    private static string Fallback(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Your session has ended. Please sign in again.",
        HttpStatusCode.Forbidden => "This page is only for farmers.",
        HttpStatusCode.TooManyRequests => "Please wait a little before trying again.",
        HttpStatusCode.ServiceUnavailable => "This service is not available right now. Please try later.",
        _ => "Something went wrong. Please try again."
    };
}
