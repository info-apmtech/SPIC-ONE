using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Reusable HTTP plumbing for a module's API client (same conventions as SasApi): every call is
/// defensive - a network problem or a non-2xx answer raises ONE toast and returns null / false,
/// so pages never catch; enums travel as integers; file URLs carry ?access_token= so &lt;img&gt;
/// and target="_blank" work. Derive, pass the route root, and write only the typed calls.
/// </summary>
public abstract class ApiClientBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    protected readonly HttpClient Http;
    protected readonly LoginState Login;
    protected readonly ToastService Toast;
    private readonly string _root;

    protected ApiClientBase(HttpClient http, LoginState login, ToastService toast, string root)
    {
        Http = http;
        Login = login;
        Toast = toast;
        _root = root.TrimEnd('/');
    }

    /// <summary>Absolute URL of {root}/file/{path} with the access token appended.</summary>
    public string FileUrl(string? path, bool download = false)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var baseUrl = Http.BaseAddress?.ToString().TrimEnd('/') ?? "";
        var relative = Uri.EscapeDataString(path).Replace("%2F", "/").TrimStart('/');
        var token = Login.Token;
        var query = new List<string>();
        if (!string.IsNullOrEmpty(token))
            query.Add("access_token=" + Uri.EscapeDataString(token.StartsWith("Bearer ") ? token["Bearer ".Length..] : token));
        if (download) query.Add("download=true");
        return $"{baseUrl}/{_root}/file/{relative}" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
    }

    protected string Url(string relative) => $"{_root}/{relative.TrimStart('/')}";

    protected async Task<T?> GetAsync<T>(string relative, string what, CancellationToken ct = default) where T : class
    {
        try
        {
            var response = await Http.GetAsync(Url(relative), ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, $"load {what}");
                return null;
            }
            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { ToastOffline($"load {what}"); return null; }
    }

    protected async Task<T?> SendAsync<T>(HttpMethod method, string relative, object? body, string what, CancellationToken ct = default) where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(method, Url(relative));
            if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
            var response = await Http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, what);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { ToastOffline(what); return null; }
    }

    protected async Task<bool> SendAsync(HttpMethod method, string relative, object? body, string what, CancellationToken ct = default) =>
        await SendAsync<object>(method, relative, body, what, ct) is not null;

    protected async Task<T?> UploadAsync<T>(string relative, string field, IReadOnlyList<IBrowserFile> files, long maxBytes, string what,
        bool quiet = false, CancellationToken ct = default) where T : class
    {
        using var content = new MultipartFormDataContent();
        var streams = new List<Stream>();
        try
        {
            foreach (var file in files)
            {
                var stream = file.OpenReadStream(maxBytes, ct);
                streams.Add(stream);
                var part = new StreamContent(stream);
                if (!string.IsNullOrWhiteSpace(file.ContentType) && MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
                    part.Headers.ContentType = mediaType;
                content.Add(part, field, file.Name);
            }
            var response = await Http.PostAsync(Url(relative), content, ct);
            if (!response.IsSuccessStatusCode)
            {
                if (!quiet) await ToastFailureAsync(response, what);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (Exception)
        {
            if (!quiet) ToastOffline(what);
            return null;
        }
        finally
        {
            foreach (var stream in streams) stream.Dispose();
        }
    }

    private async Task ToastFailureAsync(HttpResponseMessage response, string what)
    {
        var message = await ReadMessageAsync(response);
        Toast.Error(!string.IsNullOrWhiteSpace(message) ? message! : response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            HttpStatusCode.Forbidden => $"You are not allowed to {what}.",
            HttpStatusCode.NotFound => "That record is no longer available.",
            _ => $"We could not {what}. Please try again."
        });
    }

    private void ToastOffline(string what) => Toast.Error($"We could not {what}. Please check your connection and try again.");

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body) || !body.TrimStart().StartsWith("{")) return null;
            using var document = JsonDocument.Parse(body);
            foreach (var name in new[] { "message", "Message", "title", "detail" })
                if (document.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
                    value.GetString() is { Length: > 0 } text)
                    return text;
        }
        catch (Exception) { }
        return null;
    }
}
