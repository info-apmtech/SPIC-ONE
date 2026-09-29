using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Client wrapper for the Metrics page (<c>api/Metrics/...</c>; contract and route list in
/// SPIC.Core/DTOs/MetricsDtos.cs). Same conventions as <see cref="SasPaymentApi"/>: a failure raises
/// ONE toast and returns null, enums travel as integers. <see cref="LastStatus"/> tells the page a
/// 403 ("no access") from a failed call. The summary and live calls stay quiet on failure: the page
/// shows its own empty state, and the live strip refreshes every minute.
///
/// Registered with <c>services.AddScoped&lt;MetricsApi&gt;()</c> in both hosts.
/// </summary>
public sealed class MetricsApi
{
    private const string Root = "api/Metrics";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ToastService _toast;

    public MetricsApi(HttpClient http, ToastService toast)
    {
        _http = http;
        _toast = toast;
    }

    /// <summary>Status of the last call (null when it never reached the API).</summary>
    public HttpStatusCode? LastStatus { get; private set; }

    public Task<MetricsSummaryDto?> GetSummaryAsync(int days, CancellationToken ct = default) =>
        GetAsync<MetricsSummaryDto>($"{Root}/summary?days={days}", "the metrics summary", ct, quiet: true);

    public Task<MetricsLiveDto?> GetLiveAsync(CancellationToken ct = default) =>
        GetAsync<MetricsLiveDto>($"{Root}/live", "the live numbers", ct, quiet: true);

    public Task<MetricsSeriesDto?> GetSeriesAsync(int days, TelemetryApp? app, CancellationToken ct = default) =>
        GetAsync<MetricsSeriesDto>($"{Root}/series?days={days}{App(app)}", "the daily chart", ct);

    public Task<List<MetricsRouteRowDto>?> GetPagesAsync(int days, TelemetryApp? app, int top = 20, CancellationToken ct = default) =>
        GetAsync<List<MetricsRouteRowDto>>($"{Root}/pages?days={days}{App(app)}&top={top}", "the pages", ct);

    /// <param name="sort">hits | slow | errors</param>
    public Task<List<MetricsRouteRowDto>?> GetEndpointsAsync(int days, TelemetryApp? app, string sort, int top = 20,
        CancellationToken ct = default) =>
        GetAsync<List<MetricsRouteRowDto>>($"{Root}/endpoints?days={days}{App(app)}&top={top}&sort={Uri.EscapeDataString(sort)}",
            "the API endpoints", ct);

    public Task<PageResult<MetricsUserRowDto>?> GetUsersAsync(int days, string? q, string? role, TelemetryApp? app,
        int page, int pageSize, CancellationToken ct = default) =>
        GetAsync<PageResult<MetricsUserRowDto>>(
            $"{Root}/users?days={days}{Text("q", q)}{Text("role", role)}{App(app)}&page={Math.Max(1, page)}&pageSize={pageSize}",
            "the users", ct);

    /// <param name="resolved">true = resolved only, false = open only, null = all.</param>
    public Task<PageResult<MetricsErrorGroupDto>?> GetErrorsAsync(int days, string? q, TelemetrySource? source,
        TelemetryApp? app, bool? resolved, int page, int pageSize, CancellationToken ct = default)
    {
        var url = $"{Root}/errors?days={days}{Text("q", q)}{App(app)}&page={Math.Max(1, page)}&pageSize={pageSize}";
        if (source is { } s) url += $"&source={(int)s}";
        if (resolved is { } r) url += $"&resolved={(r ? "true" : "false")}";
        return GetAsync<PageResult<MetricsErrorGroupDto>>(url, "the errors", ct);
    }

    public Task<MetricsErrorDetailDto?> GetErrorAsync(long id, CancellationToken ct = default) =>
        GetAsync<MetricsErrorDetailDto>($"{Root}/errors/{id}", "this error", ct);

    public Task<MetricsErrorGroupDto?> ResolveAsync(string fingerprint, bool resolved, CancellationToken ct = default) =>
        SendJsonAsync<MetricsErrorGroupDto>(HttpMethod.Post, $"{Root}/errors/{Uri.EscapeDataString(fingerprint)}/resolve",
            new MetricsResolveDto { Resolved = resolved }, resolved ? "resolve this error" : "reopen this error", ct);

    // ---------------------------------------------------------------- plumbing

    private static string App(TelemetryApp? app) => app is { } a ? $"&app={(int)a}" : "";

    private static string Text(string key, string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : $"&{key}={Uri.EscapeDataString(value.Trim())}";

    private async Task<T?> GetAsync<T>(string url, string what, CancellationToken ct, bool quiet = false) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url, ct);
            LastStatus = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                if (!quiet) await ToastFailureAsync(response, $"load {what}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            LastStatus = null;
            if (!quiet) ToastOffline($"load {what}");
            return null;
        }
    }

    private async Task<T?> SendJsonAsync<T>(HttpMethod method, string url, object? body, string what,
        CancellationToken ct) where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);

            var response = await _http.SendAsync(request, ct);
            LastStatus = response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, what);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            LastStatus = null;
            ToastOffline(what);
            return null;
        }
    }

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
            HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            HttpStatusCode.Forbidden => $"You are not allowed to {what}.",
            HttpStatusCode.NotFound => "That record is no longer available.",
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
