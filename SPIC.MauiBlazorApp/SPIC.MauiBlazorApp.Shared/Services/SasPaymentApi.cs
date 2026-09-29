using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Client wrapper for the SAS Payment Approval / Verification API (<c>api/Sas/payments/...</c>;
/// contract and route list in SPIC.Core/DTOs/SasPaymentDtos.cs). Same conventions as
/// <see cref="SasApi"/>: every failure raises ONE toast and returns null / false, enums travel as
/// integers, <see cref="GetMeAsync"/> is cached for the circuit.
///
/// Registered with <c>services.AddScoped&lt;SasPaymentApi&gt;()</c> in both hosts
/// (SPIC.MauiBlazorApp.Web/Program.cs and SPIC.MauiBlazorApp/MauiProgram.cs).
/// </summary>
public sealed class SasPaymentApi
{
    private const string Root = "api/Sas/payments";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly LoginState _login;
    private readonly ToastService _toast;

    private SasPaymentModeDto? _me;
    private readonly Dictionary<int, List<SasPaymentRegionOption>> _regions = new();

    public SasPaymentApi(HttpClient http, LoginState login, ToastService toast)
    {
        _http = http;
        _login = login;
        _toast = toast;
    }

    // ---------------------------------------------------------------- mode / stats / list

    /// <summary>Which mode the caller gets (admin / finance / farmer / read-only). Cached per circuit.</summary>
    public async Task<SasPaymentModeDto?> GetMeAsync(CancellationToken ct = default)
    {
        if (_me is not null) return _me;
        _me = await GetAsync<SasPaymentModeDto>($"{Root}/me", "your payment access", ct);
        return _me;
    }

    public Task<SasPaymentStatsDto?> GetStatsAsync(CancellationToken ct = default) =>
        GetAsync<SasPaymentStatsDto>($"{Root}/stats", "the payment statistics", ct);

    public Task<PageResult<SasPaymentRowDto>?> GetPaymentsAsync(SasPaymentQuery query, CancellationToken ct = default)
    {
        var parts = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        Add("tab", query.Tab);
        if (query.StateId is > 0) Add("stateId", query.StateId.Value.ToString());
        if (query.RegionId is > 0) Add("regionId", query.RegionId.Value.ToString());
        if (query.PaymentMode is { } mode) Add("mode", ((int)mode).ToString());
        if (query.AdminStatus is { } admin) Add("adminStatus", ((int)admin).ToString());
        if (query.FinanceStatus is { } finance) Add("financeStatus", ((int)finance).ToString());
        Add("crop", query.Crop);
        Add("q", query.Search);
        Add("from", query.From);
        Add("to", query.To);
        parts.Add($"page={Math.Max(1, query.Page)}");
        parts.Add($"pageSize={Math.Max(1, query.PageSize)}");

        return GetAsync<PageResult<SasPaymentRowDto>>($"{Root}?{string.Join("&", parts)}", "the payments", ct);
    }

    /// <summary>One payment. <c>NotFound</c> separates "it is gone / not yours" from "the call failed".</summary>
    public async Task<(SasPaymentDetailDto? Item, bool NotFound)> GetPaymentAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/{id}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound) return (null, true);

            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "load this payment");
                return (null, false);
            }

            return (await response.Content.ReadFromJsonAsync<SasPaymentDetailDto>(Json, ct), false);
        }
        catch (OperationCanceledException)
        {
            return (null, false);
        }
        catch (Exception)
        {
            ToastOffline("load this payment");
            return (null, false);
        }
    }

    // ---------------------------------------------------------------- actions

    public Task<SasPaymentDetailDto?> ApproveAsync(int id, SasPaymentApproveDto body, CancellationToken ct = default) =>
        SendJsonAsync<SasPaymentDetailDto>(HttpMethod.Post, $"{Root}/{id}/approve", body, "approve this payment", ct);

    public Task<SasPaymentDetailDto?> RejectAsync(int id, string reason, CancellationToken ct = default) =>
        SendJsonAsync<SasPaymentDetailDto>(HttpMethod.Post, $"{Root}/{id}/reject",
            new SasPaymentRejectDto { Reason = reason }, "reject this payment", ct);

    public Task<SasPaymentDetailDto?> VerifyAsync(int id, SasPaymentVerifyDto body, CancellationToken ct = default) =>
        SendJsonAsync<SasPaymentDetailDto>(HttpMethod.Post, $"{Root}/{id}/verify", body, "verify this payment", ct);

    public Task<SasPaymentDetailDto?> MarkMismatchAsync(int id, string reason, CancellationToken ct = default) =>
        SendJsonAsync<SasPaymentDetailDto>(HttpMethod.Post, $"{Root}/{id}/mismatch",
            new SasPaymentRejectDto { Reason = reason }, "mark this payment as a mismatch", ct);

    // ---------------------------------------------------------------- proof

    /// <summary>
    /// Absolute URL of the receipt (<c>api/Sas/payments/{id}/proof</c>) with <c>access_token</c>, so an
    /// &lt;img&gt; or a new tab can open it without the Authorization header.
    /// </summary>
    public string ProofUrl(int paymentId)
    {
        var baseUrl = _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
        var url = $"{baseUrl}/{Root}/{paymentId}/proof";
        var token = _login.Token;

        if (string.IsNullOrEmpty(token)) return url;

        var raw = token.StartsWith("Bearer ") ? token["Bearer ".Length..] : token;
        return $"{url}?access_token={Uri.EscapeDataString(raw)}";
    }

    /// <summary>The receipt's bytes and content type (Download on the farmer drawer); null after a toast.</summary>
    public async Task<(byte[] Bytes, string ContentType)?> DownloadProofAsync(int paymentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/{paymentId}/proof", ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "download the payment proof");
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return (bytes, type);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            ToastOffline("download the payment proof");
            return null;
        }
    }

    // ---------------------------------------------------------------- filter lookups

    /// <summary>Regions of a state for the Region filter (api/Region/byState/{id}); cached per circuit.</summary>
    public async Task<List<SasPaymentRegionOption>> GetRegionsAsync(int stateId, CancellationToken ct = default)
    {
        if (stateId <= 0) return new List<SasPaymentRegionOption>();
        if (_regions.TryGetValue(stateId, out var cached)) return cached;

        var list = await GetAsync<List<SasPaymentRegionOption>>($"api/Region/byState/{stateId}", "the regions", ct);
        if (list is null) return new List<SasPaymentRegionOption>();

        list = list.Where(r => r.IsActive != false).OrderBy(r => r.RegionName).ToList();
        _regions[stateId] = list;
        return list;
    }

    // ---------------------------------------------------------------- plumbing

    private async Task<T?> GetAsync<T>(string url, string what, CancellationToken ct) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, $"load {what}");
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
            ToastOffline($"load {what}");
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

/// <summary>Everything the payment list can filter on (mirrors the page URL).</summary>
public sealed class SasPaymentQuery
{
    public string? Tab { get; set; }
    public int? StateId { get; set; }
    public int? RegionId { get; set; }
    public SamplePaymentMode? PaymentMode { get; set; }
    public SamplePaymentStatus? AdminStatus { get; set; }
    public SampleFinanceStatus? FinanceStatus { get; set; }
    public string? Crop { get; set; }
    public string? Search { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 16;
}

/// <summary>One row of api/Region/byState/{id} (only the fields the filter needs).</summary>
public sealed class SasPaymentRegionOption
{
    public int Id { get; set; }
    public string RegionName { get; set; } = "";
    public bool? IsActive { get; set; }
}
