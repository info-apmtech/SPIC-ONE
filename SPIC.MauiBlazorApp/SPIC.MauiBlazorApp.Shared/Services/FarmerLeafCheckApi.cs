using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// HTTP surface of farmer portal Task 6 (docs/farmer-portal.md): the leaf-photo check. Like the other farmer APIs it
/// never toasts: every call returns the value or a plain-words message the page shows inline (with field errors of a
/// 400); a 429 comes back as <see cref="Result{T}.Capped"/>. Registered with AddScoped in both hosts.
/// </summary>
public sealed class FarmerLeafCheckApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private const string OfflineMessage = "We could not reach SPIC. Please check your internet connection and try again.";

    private readonly HttpClient _http;

    public FarmerLeafCheckApi(HttpClient http) => _http = http;

    public sealed record Result<T>(T? Value, string? Message, IReadOnlyDictionary<string, string>? Errors = null, bool Capped = false)
    {
        public bool Ok => Value is not null && Message is null;
    }

    public Task<Result<LeafCheckStatusDto>> StatusAsync(CancellationToken ct = default) =>
        SendAsync<LeafCheckStatusDto>(new HttpRequestMessage(HttpMethod.Get, "api/Farmer/leaf-check/status"), ct);

    public Task<Result<List<LeafCheckSummaryDto>>> HistoryAsync(CancellationToken ct = default) =>
        SendAsync<List<LeafCheckSummaryDto>>(new HttpRequestMessage(HttpMethod.Get, "api/Farmer/leaf-check/history"), ct);

    public Task<Result<LeafCheckResultDto>> GetAsync(long id, CancellationToken ct = default) =>
        SendAsync<LeafCheckResultDto>(new HttpRequestMessage(HttpMethod.Get, $"api/Farmer/leaf-check/{id}"), ct);

    /// <summary>Sends the photo (multipart "photo", "crop", "leafAge").</summary>
    public Task<Result<LeafCheckResultDto>> CheckAsync(byte[] photo, string contentType, string crop, string leafAge, CancellationToken ct = default)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(photo);
        file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var type) ? type : new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "photo", contentType == "image/png" ? "leaf.png" : contentType == "image/webp" ? "leaf.webp" : "leaf.jpg");
        form.Add(new StringContent(crop), "crop");
        form.Add(new StringContent(leafAge), "leafAge");
        return SendAsync<LeafCheckResultDto>(new HttpRequestMessage(HttpMethod.Post, "api/Farmer/leaf-check") { Content = form }, ct);
    }

    /// <summary>The stored photo of one check (JPEG bytes; <paramref name="thumb"/> = 240 px for lists), or null.</summary>
    public async Task<byte[]?> ImageAsync(long id, bool thumb = false, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"api/Farmer/leaf-check/{id}/image{(thumb ? "?thumb=true" : "")}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(ct) : null;
        }
        catch (Exception) { return null; }
    }

    /// <summary>Null when deleted, else the message to show.</summary>
    public async Task<string?> DeleteAsync(long id, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.DeleteAsync($"api/Farmer/leaf-check/{id}", ct);
            if (response.IsSuccessStatusCode) return null;
            return (await ReadErrorAsync(response, ct)).Message;
        }
        catch (Exception) { return OfflineMessage; }
    }

    private async Task<Result<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken ct) where T : class
    {
        try
        {
            using (request)
            using (var response = await _http.SendAsync(request, ct))
            {
                if (response.IsSuccessStatusCode)
                {
                    var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
                    return value is null ? new(null, "Something went wrong. Please try again.") : new(value, null);
                }
                var (message, errors) = await ReadErrorAsync(response, ct);
                return new(null, message, errors, response.StatusCode == HttpStatusCode.TooManyRequests);
            }
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
                var parsed = JsonSerializer.Deserialize<LeafCheckErrorDto>(body, Json);
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
            HttpStatusCode.RequestEntityTooLarge => "This photo is too large (over 8 MB). Please take a new photo.",
            HttpStatusCode.TooManyRequests => "You have used today's leaf checks. Please try again after 12 midnight.",
            _ => "Something went wrong. Please try again."
        }, null);
    }
}
