using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Every Digital Library call the pages make, in one scoped service over the injected
    /// <see cref="HttpClient"/> (the same client the rest of the app injects, so the bearer
    /// token set at login is already on it).
    ///
    /// Failure policy: nothing throws at the caller. Read methods return null / an empty page
    /// and write methods return null / false; the reason is left in <see cref="LastError"/> so
    /// the page can raise a toast and fall back to an EmptyState.
    ///
    /// Routes and rules are the contract in SPIC.Core/DTOs/LibraryDtos.cs.
    /// </summary>
    public sealed class DigitalLibraryApi
    {
        private const string Root = "api/Library";

        private readonly HttpClient _http;
        private readonly LoginState _login;

        public DigitalLibraryApi(HttpClient http, LoginState login)
        {
            _http = http;
            _login = login;
        }

        /// <summary>Why the last call failed, or null when it succeeded.</summary>
        public string? LastError { get; private set; }

        // ------------------------------------------------------------------ content

        public Task<LibraryStatsDto?> GetStatsAsync(CancellationToken ct = default)
            => GetAsync<LibraryStatsDto>($"{Root}/stats", ct);

        public Task<LibraryLookupsDto?> GetLookupsAsync(CancellationToken ct = default)
            => GetAsync<LibraryLookupsDto>($"{Root}/lookups", ct);

        /// <summary>One page of the list. Never null: an empty page is returned on failure.</summary>
        public async Task<PageResult<LibraryContentSummaryDto>> ListAsync(
            LibraryContentKind? kind = null,
            LibraryContentStatus? status = null,
            string? q = null,
            int page = 1,
            int pageSize = 12,
            CancellationToken ct = default)
        {
            var parts = new List<string>();
            if (kind is not null) parts.Add("kind=" + kind);
            if (status is not null) parts.Add("status=" + status);
            if (!string.IsNullOrWhiteSpace(q)) parts.Add("q=" + Uri.EscapeDataString(q));
            parts.Add("page=" + page);
            parts.Add("pageSize=" + pageSize);

            var result = await GetAsync<PageResult<LibraryContentSummaryDto>>($"{Root}?{string.Join("&", parts)}", ct);
            return result ?? new PageResult<LibraryContentSummaryDto> { Page = page, PageSize = pageSize };
        }

        /// <summary>One page of the list already mapped to the card view model.</summary>
        public async Task<(List<DigitalLibraryItem> Items, int Total, bool HasMore)> ListItemsAsync(
            LibraryContentKind? kind = null,
            LibraryContentStatus? status = null,
            string? q = null,
            int page = 1,
            int pageSize = 12,
            CancellationToken ct = default)
        {
            var result = await ListAsync(kind, status, q, page, pageSize, ct);
            return (result.Items.Select(ToItem).ToList(), result.Total, result.HasMore);
        }

        public Task<LibraryContentDetailDto?> GetAsync(int id, CancellationToken ct = default)
            => GetAsync<LibraryContentDetailDto>($"{Root}/{id}", ct);

        public Task<LibraryContentDetailDto?> CreateAsync(LibraryContentUpsertDto body, CancellationToken ct = default)
            => SendAsync<LibraryContentDetailDto>(HttpMethod.Post, Root, body, ct);

        public Task<LibraryContentDetailDto?> UpdateAsync(int id, LibraryContentUpsertDto body, CancellationToken ct = default)
            => SendAsync<LibraryContentDetailDto>(HttpMethod.Put, $"{Root}/{id}", body, ct);

        public Task<LibraryContentSummaryDto?> SetStatusAsync(int id, LibraryContentStatus status, CancellationToken ct = default)
            => SendAsync<LibraryContentSummaryDto>(HttpMethod.Patch, $"{Root}/{id}/status?status={status}", null, ct);

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            LastError = null;
            try
            {
                using var response = await _http.DeleteAsync($"{Root}/{id}", ct);
                if (response.IsSuccessStatusCode) return true;
                LastError = await DescribeAsync(response);
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastError = Describe(ex);
                return false;
            }
        }

        // ------------------------------------------------------------------- files

        // The ceilings the API enforces (LibraryController.MaxCoverBytes / MaxVideoBytes / MaxDocumentBytes).
        public const long MaxCoverBytes = 5L * 1024 * 1024;
        public const long MaxVideoBytes = 200L * 1024 * 1024;
        public const long MaxDocumentBytes = 25L * 1024 * 1024;

        public Task<LibraryFileDto?> UploadCoverAsync(int id, IBrowserFile file, CancellationToken ct = default)
            => UploadAsync(id, "cover", file, MaxCoverBytes, ct);

        /// <summary>
        /// Uploads a cover the page already has in memory. An <see cref="IBrowserFile"/> stream can
        /// only be read once, so a cover that was read to build the preview must be sent as bytes.
        /// </summary>
        public async Task<LibraryFileDto?> UploadCoverAsync(int id, byte[] bytes, string fileName, string contentType, CancellationToken ct = default)
        {
            LastError = null;
            try
            {
                using var content = new MultipartFormDataContent();
                var part = new ByteArrayContent(bytes);
                part.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
                content.Add(part, "file", fileName);

                using var response = await _http.PostAsync($"{Root}/{id}/cover", content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await DescribeAsync(response);
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<LibraryFileDto>(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastError = Describe(ex);
                return null;
            }
        }

        public Task<LibraryFileDto?> UploadVideoAsync(int id, IBrowserFile file, CancellationToken ct = default)
            => UploadAsync(id, "video", file, MaxVideoBytes, ct);

        public Task<LibraryFileDto?> UploadDocumentAsync(int id, IBrowserFile file, CancellationToken ct = default)
            => UploadAsync(id, "document", file, MaxDocumentBytes, ct);

        private async Task<LibraryFileDto?> UploadAsync(int id, string slot, IBrowserFile file, long maxBytes, CancellationToken ct)
        {
            LastError = null;
            try
            {
                using var content = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream(maxBytes, ct);
                var part = new StreamContent(stream);
                part.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
                content.Add(part, "file", file.Name);

                using var response = await _http.PostAsync($"{Root}/{id}/{slot}", content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await DescribeAsync(response);
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<LibraryFileDto>(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastError = Describe(ex);
                return null;
            }
        }

        /// <summary>
        /// Absolute URL for a stored file, usable straight from &lt;img&gt; / &lt;video&gt; (the token
        /// travels in the query string because those elements cannot send a header) - the same
        /// shape Pages/BookingDetails.razor builds for guest-house images.
        /// Returns "" for a missing path and passes absolute / _content URLs through unchanged.
        /// </summary>
        public string FileUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("_content/", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var baseUrl = _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
            var relative = Uri.EscapeDataString(path).Replace("%2F", "/").TrimStart('/');
            var token = _login.Token;
            if (!string.IsNullOrEmpty(token))
            {
                var raw = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token["Bearer ".Length..] : token;
                return $"{baseUrl}/{Root}/file/{relative}?access_token={Uri.EscapeDataString(raw)}";
            }

            return $"{baseUrl}/{Root}/file/{relative}";
        }

        /// <summary>Relative URL for FileDownloadService (the bearer header is added by the client).</summary>
        public static string FilePath(string path) =>
            $"{Root}/file/{Uri.EscapeDataString(path).Replace("%2F", "/").TrimStart('/')}";

        // --------------------------------------------------------------- assistant

        public async Task<List<AssistantConversationDto>> GetConversationsAsync(CancellationToken ct = default)
            => await GetAsync<List<AssistantConversationDto>>($"{Root}/assistant/conversations", ct) ?? new();

        public Task<AssistantConversationDetailDto?> GetConversationAsync(int id, CancellationToken ct = default)
            => GetAsync<AssistantConversationDetailDto>($"{Root}/assistant/conversations/{id}", ct);

        public Task<AssistantAskResponse?> AskAsync(AssistantAskRequest request, CancellationToken ct = default)
            => SendAsync<AssistantAskResponse>(HttpMethod.Post, $"{Root}/assistant/ask", request, ct);

        public async Task<bool> DeleteConversationAsync(int id, CancellationToken ct = default)
        {
            LastError = null;
            try
            {
                using var response = await _http.DeleteAsync($"{Root}/assistant/conversations/{id}", ct);
                if (response.IsSuccessStatusCode) return true;
                LastError = await DescribeAsync(response);
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastError = Describe(ex);
                return false;
            }
        }

        // ----------------------------------------------------------------- mapping

        /// <summary>Summary DTO to the card view model, with the cover resolved to a usable URL.</summary>
        public DigitalLibraryItem ToItem(LibraryContentSummaryDto dto)
            => DigitalLibraryItem.FromDto(dto, FileUrl);

        public List<DigitalLibraryItem> ToItems(IEnumerable<LibraryContentSummaryDto> dtos)
            => dtos.Select(ToItem).ToList();

        // ------------------------------------------------------------------ plumbing

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
        {
            LastError = null;
            try
            {
                using var response = await _http.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await DescribeAsync(response);
                    return default;
                }
                return await response.Content.ReadFromJsonAsync<T>(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastError = Describe(ex);
                return default;
            }
        }

        private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
        {
            LastError = null;
            try
            {
                using var request = new HttpRequestMessage(method, url);
                if (body is not null) request.Content = JsonContent.Create(body, body.GetType());

                using var response = await _http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await DescribeAsync(response);
                    return default;
                }

                if (response.Content.Headers.ContentLength == 0) return default;
                return await response.Content.ReadFromJsonAsync<T>(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastError = Describe(ex);
                return default;
            }
        }

        private static string Describe(Exception ex) => ex switch
        {
            HttpRequestException => "The library service is not reachable. Check your connection and try again.",
            TaskCanceledException => "The library service took too long to answer.",
            _ => ex.Message
        };

        private static async Task<string> DescribeAsync(HttpResponseMessage response)
        {
            var status = (int)response.StatusCode;
            if (status == 401) return "Your session has expired. Sign in again.";
            if (status == 403) return "You do not have permission to do that.";
            if (status == 404) return "That library item no longer exists.";

            try
            {
                var text = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // { "success": false, "message": "..." } is the API's write envelope.
                    if (text.TrimStart().StartsWith("{"))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(text);
                        foreach (var key in new[] { "message", "Message", "title", "detail" })
                        {
                            if (doc.RootElement.TryGetProperty(key, out var v) && v.GetString() is { Length: > 0 } m)
                                return m;
                        }
                    }
                    else if (text.Length <= 200)
                    {
                        return text;
                    }
                }
            }
            catch { /* fall through to the generic message */ }

            return $"The library service returned {status} ({response.ReasonPhrase}).";
        }
    }
}
