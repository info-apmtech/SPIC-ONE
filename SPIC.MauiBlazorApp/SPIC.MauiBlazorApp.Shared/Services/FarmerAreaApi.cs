using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// HTTP surface of farmer portal Task 3 (docs/farmer-portal.md): "Trends in my area", the home weather
/// strip and "Community near me". Like <see cref="FarmerConnectApi"/> these never toast: every call
/// returns the value or a plain-words message the page shows inline. Registered with AddScoped in both
/// hosts. The app never sends a location here: the API reads the GPS saved on the profile.
/// </summary>
public sealed class FarmerAreaApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private const string OfflineMessage = "We could not reach SPIC. Please check your internet connection and try again.";

    private readonly HttpClient _http;

    public FarmerAreaApi(HttpClient http) => _http = http;

    public sealed record Result<T>(T? Value, string? Message)
    {
        public bool Ok => Value is not null && Message is null;
    }

    public Task<Result<FarmerTrendsDto>> TrendsAsync(CancellationToken ct = default) =>
        GetAsync<FarmerTrendsDto>("api/Farmer/trends", ct);

    public Task<Result<FarmerWeatherDto>> WeatherAsync(CancellationToken ct = default) =>
        GetAsync<FarmerWeatherDto>("api/Farmer/weather", ct);

    public Task<Result<CommunityNearPageDto>> CommunityNearAsync(int page, int pageSize = 10, CancellationToken ct = default) =>
        GetAsync<CommunityNearPageDto>($"api/Community/near?page={Math.Max(1, page)}&pageSize={Math.Clamp(pageSize, 1, 50)}", ct);

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
            return new(null, await ReadErrorAsync(response, ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(null, null); }
        catch (Exception) { return new(null, OfflineMessage); }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (body.TrimStart().StartsWith('{'))
            {
                var parsed = JsonSerializer.Deserialize<FarmerProfileErrorDto>(body, Json);
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Message)) return parsed.Message;
            }
        }
        catch (Exception) { /* fall back to the status text */ }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has ended. Please sign in again.",
            HttpStatusCode.Forbidden => "This page is not available for your account.",
            HttpStatusCode.NotFound => "This was not found. Please refresh the page.",
            HttpStatusCode.TooManyRequests => "Please wait a little before trying again.",
            _ => "Something went wrong. Please try again."
        };
    }
}
