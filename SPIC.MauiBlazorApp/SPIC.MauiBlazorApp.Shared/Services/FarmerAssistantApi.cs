using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// HTTP surface of farmer portal Task 5 (docs/farmer-portal.md): SPIC AI for farmers (api/Farmer/assistant).
/// Like <see cref="FarmerFertilizerApi"/> it never toasts: every call returns the value or a plain-words
/// message the page shows inline; a 429 comes back as <see cref="Result{T}.Capped"/> with the reset time.
/// Registered with AddScoped in both hosts.
/// </summary>
public sealed class FarmerAssistantApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private const string OfflineMessage = "We could not reach SPIC. Please check your internet connection and try again.";

    private readonly HttpClient _http;

    public FarmerAssistantApi(HttpClient http) => _http = http;

    public sealed record Result<T>(T? Value, string? Message, bool Capped = false, DateTime? ResetsAtUtc = null)
    {
        public bool Ok => Value is not null && Message is null;
    }

    public Task<Result<FarmerAssistantStatusDto>> StatusAsync(CancellationToken ct = default) =>
        SendAsync<FarmerAssistantStatusDto>(HttpMethod.Get, "api/Farmer/assistant/status", null, ct);

    public Task<Result<List<FarmerChatConversationDto>>> ConversationsAsync(CancellationToken ct = default) =>
        SendAsync<List<FarmerChatConversationDto>>(HttpMethod.Get, "api/Farmer/assistant/conversations", null, ct);

    public Task<Result<FarmerChatDetailDto>> ConversationAsync(long id, CancellationToken ct = default) =>
        SendAsync<FarmerChatDetailDto>(HttpMethod.Get, $"api/Farmer/assistant/conversations/{id}", null, ct);

    public Task<Result<FarmerAskResponse>> AskAsync(FarmerAskRequest request, CancellationToken ct = default) =>
        SendAsync<FarmerAskResponse>(HttpMethod.Post, "api/Farmer/assistant/ask", request, ct);

    /// <summary>Null when deleted, else the problem in plain words.</summary>
    public async Task<string?> DeleteAsync(long id, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.DeleteAsync($"api/Farmer/assistant/conversations/{id}", ct);
            if (response.IsSuccessStatusCode) return null;
            var (message, _, _) = await ReadErrorAsync(response, ct);
            return message;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return null; }
        catch (Exception) { return OfflineMessage; }
    }

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
            var (message, capped, resets) = await ReadErrorAsync(response, ct);
            return new(null, message, capped, resets);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new(null, null); }
        catch (Exception) { return new(null, OfflineMessage); }
    }

    private static async Task<(string Message, bool Capped, DateTime? ResetsAtUtc)> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var capped = response.StatusCode == HttpStatusCode.TooManyRequests;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (body.TrimStart().StartsWith('{'))
            {
                var parsed = JsonSerializer.Deserialize<FarmerAssistantErrorDto>(body, Json);
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Message))
                    return (parsed.Message, capped, parsed.ResetsAtUtc);
            }
        }
        catch (Exception) { /* fall back to the status text */ }

        return (response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has ended. Please sign in again.",
            HttpStatusCode.Forbidden => "SPIC AI is for farmers. This account cannot use it.",
            HttpStatusCode.NotFound => "This chat was not found. Please start a new chat.",
            HttpStatusCode.TooManyRequests => "You have asked the most questions for today. Please ask again after 12 midnight.",
            _ => "SPIC AI could not answer just now. Please try again."
        }, capped, null);
    }
}
