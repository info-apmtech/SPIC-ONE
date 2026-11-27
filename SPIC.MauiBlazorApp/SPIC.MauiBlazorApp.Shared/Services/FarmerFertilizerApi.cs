using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// HTTP surface of farmer portal Task 4 (docs/farmer-portal.md): fertilizer recommendation. Like
/// <see cref="FarmerAreaApi"/> it never toasts: every call returns the value or a plain-words message the page
/// shows inline (with the field errors of a 400). Registered with AddScoped in both hosts.
/// </summary>
public sealed class FarmerFertilizerApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private const string OfflineMessage = "We could not reach SPIC. Please check your internet connection and try again.";

    private readonly HttpClient _http;

    public FarmerFertilizerApi(HttpClient http) => _http = http;

    public sealed record Result<T>(T? Value, string? Message, IReadOnlyDictionary<string, string>? Errors = null)
    {
        public bool Ok => Value is not null && Message is null;
    }

    public Task<Result<FertilizerOptionsDto>> OptionsAsync(CancellationToken ct = default) =>
        SendAsync<FertilizerOptionsDto>(HttpMethod.Get, "api/Farmer/fertilizer/options", null, ct);

    public Task<Result<FertilizerSoilReportDto>> SoilReportAsync(CancellationToken ct = default) =>
        SendAsync<FertilizerSoilReportDto>(HttpMethod.Get, "api/Farmer/fertilizer/soil-report", null, ct);

    public Task<Result<FertilizerRecommendationDto>> RecommendAsync(FertilizerRecommendRequest request, CancellationToken ct = default) =>
        SendAsync<FertilizerRecommendationDto>(HttpMethod.Post, "api/Farmer/fertilizer/recommend", request, ct);

    public Task<Result<List<FertilizerRunSummaryDto>>> HistoryAsync(CancellationToken ct = default) =>
        SendAsync<List<FertilizerRunSummaryDto>>(HttpMethod.Get, "api/Farmer/fertilizer/history", null, ct);

    public Task<Result<FertilizerRecommendationDto>> RunAsync(long id, CancellationToken ct = default) =>
        SendAsync<FertilizerRecommendationDto>(HttpMethod.Get, $"api/Farmer/fertilizer/history/{id}", null, ct);

    private async Task<Result<T>> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct) where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = JsonContent.Create(body, options: Json);
            using var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
                return value is null ? new(null, "Something went wrong. Please try again.") : new(value, null);
            }
            var (message, errors) = await ReadErrorAsync(response, ct);
            return new(null, message, errors);
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
                var parsed = JsonSerializer.Deserialize<FertilizerErrorDto>(body, Json);
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Message))
                    return (parsed.Message, parsed.Errors.Count > 0 ? parsed.Errors : null);
            }
        }
        catch (Exception) { /* fall back to the status text */ }

        return (response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has ended. Please sign in again.",
            HttpStatusCode.Forbidden => "This page is not available for your account.",
            HttpStatusCode.NotFound => "This was not found. Please refresh the page.",
            HttpStatusCode.TooManyRequests => "Please wait a little before trying again.",
            _ => "Something went wrong. Please try again."
        }, null);
    }
}
