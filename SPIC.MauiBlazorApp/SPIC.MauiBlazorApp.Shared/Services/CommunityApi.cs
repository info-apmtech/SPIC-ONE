using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Client for <c>api/Community</c> (see the route list at the top of SPIC.Core/DTOs/CommunityDtos.cs).
///
/// Every call is defensive: a network problem or a non-2xx response raises a toast and returns
/// null / false / an empty list, so a page never has to catch anything and can render an
/// <c>EmptyState</c> instead. Author identity always comes from the server (the JWT), never
/// from the client.
///
/// Registered with <c>services.AddScoped&lt;CommunityApi&gt;()</c> in both hosts
/// (SPIC.MauiBlazorApp.Web/Program.cs and SPIC.MauiBlazorApp/MauiProgram.cs).
/// </summary>
public sealed class CommunityApi
{
    private const string Root = "api/Community";

    /// <summary>Largest file the drop zone accepts (also enforced server side).</summary>
    public const long MaxFileBytes = 10L * 1024 * 1024;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly LoginState _login;
    private readonly ToastService _toast;

    public CommunityApi(HttpClient http, LoginState login, ToastService toast)
    {
        _http = http;
        _login = login;
        _toast = toast;
    }

    /// <summary>Identity id of the signed-in user, used only to decide which rows offer Delete.</summary>
    public string? CurrentUserId => _login.UserId;

    /// <summary>
    /// Display-only stand-in for the signed-in user: the avatar beside a composer, nothing else.
    /// Authorship is always decided by the server from the JWT; this never travels to the API.
    /// </summary>
    public CommunityMember ComposerIdentity => new()
    {
        UserId = _login.UserId ?? "",
        Name = NameFromToken() ?? "You",
        Role = _login.UserRole switch
        {
            AppRole.Dealer => "Dealer",
            AppRole.Farmer => "Farmer",
            null => "Farmer",
            _ => "SPIC Expert"
        }
    };

    /// <summary>First name claim in the JWT (same claims the chat header and MainLayout read).</summary>
    private string? NameFromToken()
    {
        try
        {
            var parts = _login.Token?.Split('.');
            if (parts is not { Length: >= 2 }) return null;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            foreach (var key in new[]
                     {
                         "name", "unique_name",
                         "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
                         "given_name", "sub"
                     })
            {
                if (document.RootElement.TryGetProperty(key, out var value) &&
                    value.GetString() is { Length: > 0 } name)
                {
                    return name;
                }
            }
        }
        catch (Exception)
        {
            // a malformed token just means the generic avatar
        }

        return null;
    }

    // ------------------------------------------------------------------ landing

    public Task<CommunityStats?> GetStatsAsync(CancellationToken ct = default) =>
        GetAsync($"{Root}/stats", (CommunityStatsDto d) => CommunityStats.FromDto(d), "community statistics", ct);

    public async Task<List<CommunityDiscussion>?> GetRecentAsync(int take = 6, CancellationToken ct = default)
    {
        var dtos = await GetAsync($"{Root}/recent?take={take}",
            (List<DiscussionSummaryDto> d) => d, "the recent discussions", ct);
        return dtos?.Select(d => CommunityDiscussion.FromDto(d, FileUrl)).ToList();
    }

    public async Task<List<CommunityProduct>?> GetProductsAsync(CancellationToken ct = default)
    {
        var dtos = await GetAsync($"{Root}/products",
            (List<CommunityProductDto> d) => d, "the popular products", ct);
        return dtos?.Select(p => CommunityProduct.FromDto(p, FileUrl)).ToList();
    }

    public Task<ReactionResultDto?> ToggleJoinAsync(string productName, CancellationToken ct = default) =>
        PostReactionAsync($"{Root}/products/{Uri.EscapeDataString(productName)}/join", ct);

    public Task<CommunityLookups?> GetLookupsAsync(CancellationToken ct = default) =>
        GetAsync($"{Root}/lookups", (CommunityLookupsDto d) => CommunityLookups.FromDto(d), "the category lists", ct);

    // ------------------------------------------------------------------ list

    public async Task<DiscussionPage?> GetDiscussionsAsync(DiscussionQuery query, CancellationToken ct = default)
    {
        var parts = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        Add("tab", query.Tab);
        Add("category", query.Category);
        Add("product", query.Product);
        Add("crop", query.Crop);
        Add("status", query.Status);
        if (query.Tags.Count > 0) Add("tags", string.Join(",", query.Tags));
        Add("from", query.From);
        Add("to", query.To);
        Add("q", query.Search);
        Add("sort", query.Sort);
        parts.Add($"page={Math.Max(1, query.Page)}");
        parts.Add($"pageSize={Math.Max(1, query.PageSize)}");

        var page = await GetAsync($"{Root}/discussions?{string.Join("&", parts)}",
            (PageResult<DiscussionSummaryDto> p) => p, "the discussions", ct);

        if (page is null) return null;

        return new DiscussionPage
        {
            Items = (page.Items ?? new List<DiscussionSummaryDto>())
                .Select(d => CommunityDiscussion.FromDto(d, FileUrl)).ToList(),
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    /// <summary>
    /// Detail. The server counts the view, so never increment <c>Views</c> in the client.
    /// <c>NotFound</c> separates "this discussion is gone" (no toast, the page says so) from
    /// "the call failed" (toast + a Try again button).
    /// </summary>
    public async Task<(CommunityDiscussion? Item, bool NotFound)> GetDiscussionAsync(int id, CancellationToken ct = default, bool countView = true)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/discussions/{id}{(countView ? "" : "?countView=false")}", ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return (null, true);

            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "load this discussion");
                return (null, false);
            }

            var dto = await response.Content.ReadFromJsonAsync<DiscussionDetailDto>(Json, ct);
            return dto is null ? (null, true) : (CommunityDiscussion.FromDto(dto, FileUrl), false);
        }
        catch (OperationCanceledException)
        {
            return (null, false);
        }
        catch (Exception)
        {
            ToastOffline("load this discussion");
            return (null, false);
        }
    }

    public async Task<List<CommunityDiscussion>> GetSimilarAsync(string title, string? body, int max = 3, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{Root}/discussions/similar",
                new SimilarRequest { Title = title ?? "", Body = body, Max = max }, Json, ct);

            if (!response.IsSuccessStatusCode) return new List<CommunityDiscussion>();

            var dtos = await response.Content.ReadFromJsonAsync<List<DiscussionSummaryDto>>(Json, ct);
            return (dtos ?? new List<DiscussionSummaryDto>())
                .Select(d => CommunityDiscussion.FromDto(d, FileUrl)).ToList();
        }
        catch (Exception)
        {
            // The similar check is a courtesy; if it cannot run, the post simply goes ahead.
            return new List<CommunityDiscussion>();
        }
    }

    // ------------------------------------------------------------------ writes

    public async Task<CommunityDiscussion?> CreateAsync(DiscussionDraft draft, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{Root}/discussions", draft.ToDto(), Json, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "post your question");
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<DiscussionDetailDto>(Json, ct);
            return dto is null ? null : CommunityDiscussion.FromDto(dto, FileUrl);
        }
        catch (Exception)
        {
            ToastOffline("post your question");
            return null;
        }
    }

    public Task<bool> UpdateAsync(int id, DiscussionDraft draft, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"{Root}/discussions/{id}", draft.ToDto(), "save your changes", ct);

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"{Root}/discussions/{id}", null, "delete this discussion", ct);

    public Task<bool> SetStatusAsync(int id, DiscussionStatus status, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Patch,
            $"{Root}/discussions/{id}/status?status={CommunityDiscussion.ToStatus(status)}",
            null, "change the status", ct);

    public async Task<CommunityReply?> AddReplyAsync(int discussionId, string body, int? parentReplyId = null,
        string? mentionName = null, CancellationToken ct = default)
    {
        try
        {
            var payload = new ReplyCreateDto
            {
                Body = body.Trim(),
                ParentReplyId = parentReplyId,
                MentionName = mentionName
            };

            var response = await _http.PostAsJsonAsync($"{Root}/discussions/{discussionId}/replies", payload, Json, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "post your reply");
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<ReplyDto>(Json, ct);
            return dto is null ? null : CommunityReply.FromDto(dto, FileUrl);
        }
        catch (Exception)
        {
            ToastOffline("post your reply");
            return null;
        }
    }

    public Task<bool> DeleteReplyAsync(int replyId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"{Root}/replies/{replyId}", null, "delete this reply", ct);

    // ------------------------------------------------------------------ reactions

    public Task<ReactionResultDto?> ToggleLikeAsync(int discussionId, CancellationToken ct = default) =>
        PostReactionAsync($"{Root}/discussions/{discussionId}/like", ct);

    public Task<ReactionResultDto?> ToggleSaveAsync(int discussionId, CancellationToken ct = default) =>
        PostReactionAsync($"{Root}/discussions/{discussionId}/save", ct);

    public Task<ReactionResultDto?> ToggleFollowAsync(int discussionId, CancellationToken ct = default) =>
        PostReactionAsync($"{Root}/discussions/{discussionId}/follow", ct);

    public Task<ReactionResultDto?> ToggleReplyLikeAsync(int replyId, CancellationToken ct = default) =>
        PostReactionAsync($"{Root}/replies/{replyId}/like", ct);

    // ------------------------------------------------------------------ attachments

    /// <summary>
    /// Uploads the chosen files as multipart "files". Returns null when the upload failed, so
    /// the caller can tell the author the discussion was posted but the files were not.
    /// </summary>
    public async Task<List<CommunityAttachment>?> UploadAttachmentsAsync(int discussionId,
        IReadOnlyList<IBrowserFile> files, CancellationToken ct = default)
    {
        if (files.Count == 0) return new List<CommunityAttachment>();

        using var content = new MultipartFormDataContent();
        var streams = new List<Stream>();

        try
        {
            foreach (var file in files)
            {
                var stream = file.OpenReadStream(MaxFileBytes, ct);
                streams.Add(stream);

                var part = new StreamContent(stream);
                if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                    MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
                {
                    part.Headers.ContentType = mediaType;
                }

                content.Add(part, "files", file.Name);
            }

            var response = await _http.PostAsync($"{Root}/discussions/{discussionId}/attachments", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "upload the attachments");
                return null;
            }

            var dtos = await response.Content.ReadFromJsonAsync<List<AttachmentDto>>(Json, ct);
            return (dtos ?? new List<AttachmentDto>())
                .Select(a => CommunityAttachment.FromDto(a, FileUrl)).ToList();
        }
        catch (Exception)
        {
            ToastOffline("upload the attachments");
            return null;
        }
        finally
        {
            foreach (var stream in streams) stream.Dispose();
        }
    }

    public Task<bool> DeleteAttachmentAsync(int attachmentId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"{Root}/attachments/{attachmentId}", null, "remove this attachment", ct);

    /// <summary>
    /// Absolute URL for a stored file. The community file endpoint is on the <c>access_token</c>
    /// allowlist, so &lt;img&gt; / target="_blank" work without the Authorization header
    /// (same pattern as the guest-house image URLs in BookingDetails.razor).
    /// </summary>
    public string FileUrl(string path)
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
            var rawToken = token.StartsWith("Bearer ") ? token["Bearer ".Length..] : token;
            return $"{baseUrl}/{Root}/file/{relative}?access_token={Uri.EscapeDataString(rawToken)}";
        }

        return $"{baseUrl}/{Root}/file/{relative}";
    }

    // ------------------------------------------------------------------ plumbing

    private async Task<TResult?> GetAsync<TDto, TResult>(string url, Func<TDto, TResult> map, string what,
        CancellationToken ct)
        where TResult : class
    {
        try
        {
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, $"load {what}");
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<TDto>(Json, ct);
            return dto is null ? null : map(dto);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            ToastOffline($"load {what}");
            return null;
        }
    }

    private async Task<ReactionResultDto?> PostReactionAsync(string url, CancellationToken ct)
    {
        try
        {
            var response = await _http.PostAsync(url, null, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "save that");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ReactionResultDto>(Json, ct);
        }
        catch (Exception)
        {
            ToastOffline("save that");
            return null;
        }
    }

    private async Task<bool> SendAsync(HttpMethod method, string url, object? body, string what, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = JsonContent.Create(body, options: Json);

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, what);
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            ToastOffline(what);
            return false;
        }
    }

    /// <summary>Turns a failed response into one readable toast (the API's Message when it sent one).</summary>
    private async Task ToastFailureAsync(HttpResponseMessage response, string what)
    {
        var message = await ReadMessageAsync(response);

        if (!string.IsNullOrWhiteSpace(message))
        {
            _toast.Error(message!);
            return;
        }

        _toast.Error(response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            System.Net.HttpStatusCode.Forbidden => $"You are not allowed to {what}.",
            System.Net.HttpStatusCode.NotFound => "That item is no longer in the community.",
            _ => $"We could not {what}. Please try again."
        });
    }

    private void ToastOffline(string what) =>
        _toast.Error($"We could not {what}. Please check your connection and try again.");

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body) || !body.TrimStart().StartsWith("{")) return null;

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;

            foreach (var name in new[] { "message", "Message", "title", "detail" })
            {
                if (document.RootElement.TryGetProperty(name, out var value) &&
                    value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
        }
        catch (Exception)
        {
            // an unparsable body just falls back to the status-code message
        }

        return null;
    }
}
