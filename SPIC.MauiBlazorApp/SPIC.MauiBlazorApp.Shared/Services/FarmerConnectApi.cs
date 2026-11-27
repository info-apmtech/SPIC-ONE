using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;
using SPIC.Core.FarmerPortal;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// HTTP surface of farmer portal Task 2 (docs/farmer-portal.md): dealers near me, my SPIC team,
/// call-back requests (farmer side, api/Farmer/...) and the staff call-back page (api/FarmerRequests).
/// Like <see cref="FarmerApi"/> these never toast: every call returns the result or a plain-words
/// message the page shows inline. Registered with AddScoped in both hosts.
/// </summary>
public sealed class FarmerConnectApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private const string OfflineMessage = "We could not reach SPIC. Please check your internet connection and try again.";

    private readonly HttpClient _http;

    public FarmerConnectApi(HttpClient http) => _http = http;

    /// <summary>A loaded value, or the message to show instead.</summary>
    public sealed record Result<T>(T? Value, string? Message, IReadOnlyDictionary<string, string>? Errors = null)
    {
        public bool Ok => Value is not null && Message is null;
    }

    /// <summary>
    /// GET api/Farmer/dealers. The GPS point is rounded to about 110 m (FarmerGeo.Coarse) before it is
    /// sent: enough to sort by distance, never the exact position.
    /// </summary>
    public Task<Result<FarmerDealerListDto>> DealersAsync(double? lat, double? lng, int radiusKm, string? search, int page, int pageSize = 20, CancellationToken ct = default)
    {
        var query = new List<string> { $"radiusKm={radiusKm}", $"page={page}", $"pageSize={pageSize}" };
        if (FarmerGeo.IsValid(lat, lng))
        {
            query.Add("lat=" + FarmerGeo.Coarse(lat!.Value).ToString(CultureInfo.InvariantCulture));
            query.Add("lng=" + FarmerGeo.Coarse(lng!.Value).ToString(CultureInfo.InvariantCulture));
        }
        if (!string.IsNullOrWhiteSpace(search)) query.Add("q=" + Uri.EscapeDataString(search.Trim()));
        return GetAsync<FarmerDealerListDto>("api/Farmer/dealers?" + string.Join("&", query), ct);
    }

    public Task<Result<FarmerTeamDto>> TeamAsync(CancellationToken ct = default) =>
        GetAsync<FarmerTeamDto>("api/Farmer/contacts", ct);

    public Task<Result<List<FarmerContactRequestDto>>> MyRequestsAsync(CancellationToken ct = default) =>
        GetAsync<List<FarmerContactRequestDto>>("api/Farmer/contacts/requests", ct);

    public Task<Result<FarmerContactRequestDto>> RequestCallBackAsync(string contactId, FarmerContactRequestCreateDto dto, CancellationToken ct = default) =>
        SendAsync<FarmerContactRequestDto>(HttpMethod.Post, $"api/Farmer/contacts/{Uri.EscapeDataString(contactId)}/request", dto, ct);

    // ------------------------------------------------------------------ staff

    public Task<Result<StaffFarmerRequestListDto>> StaffRequestsAsync(SPIC.Core.Entities.FarmerContactRequestStatus? status, int page, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<StaffFarmerRequestListDto>($"api/FarmerRequests?page={page}&pageSize={pageSize}" + (status is { } s ? "&status=" + s : ""), ct);

    public Task<Result<StaffFarmerRequestDto>> UpdateRequestAsync(int id, FarmerRequestUpdateDto dto, CancellationToken ct = default) =>
        SendAsync<StaffFarmerRequestDto>(HttpMethod.Put, $"api/FarmerRequests/{id}", dto, ct);

    // ------------------------------------------------------------------ plumbing

    private async Task<Result<T>> GetAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
                return value is null ? new(null, "Something went wrong. Please try again.") : new(value, null);
            }
            var error = await ReadErrorAsync(response, ct);
            return new(null, error.Message, error.Errors);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(null, null); }
        catch (Exception) { return new(null, OfflineMessage); }
    }

    private async Task<Result<T>> SendAsync<T>(HttpMethod method, string url, object body, CancellationToken ct) where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body, options: Json) };
            using var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
                return value is null ? new(null, "Something went wrong. Please try again.") : new(value, null);
            }
            var error = await ReadErrorAsync(response, ct);
            return new(null, error.Message, error.Errors);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(null, null); }
        catch (Exception) { return new(null, OfflineMessage); }
    }

    private static async Task<(string Message, IReadOnlyDictionary<string, string>? Errors)> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (body.TrimStart().StartsWith('{'))
            {
                var parsed = JsonSerializer.Deserialize<FarmerProfileErrorDto>(body, Json);
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Message))
                    return (parsed.Message, parsed.Errors.Count > 0 ? parsed.Errors : null);
            }
        }
        catch (Exception) { /* fall back to the status text */ }
        return (Fallback(response.StatusCode), null);
    }

    private static string Fallback(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Your session has ended. Please sign in again.",
        HttpStatusCode.Forbidden => "This page is not available for your account.",
        HttpStatusCode.NotFound => "This was not found. Please refresh the page.",
        HttpStatusCode.TooManyRequests => "Please wait a little before trying again.",
        _ => "Something went wrong. Please try again."
    };
}
