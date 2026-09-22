using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// The ONLY HTTP surface of the SAS module (<c>api/Sas/...</c>; the route list lives at the top
/// of SPIC.Core/DTOs/SasDtos.cs). Collections, payments and consignments all go through here so
/// the three client agents share one set of conventions.
///
/// Conventions:
///  * every call is defensive - a network problem or a non-2xx answer raises ONE toast and
///    returns null / false / an empty list, so no page has to catch anything;
///  * enums travel as INTEGERS (the API has no string enum converter), so <see cref="Json"/>
///    deliberately does NOT register JsonStringEnumConverter;
///  * lookups are cached for the lifetime of the circuit (one scoped instance per user session).
///
/// Registered with <c>services.AddScoped&lt;SasApi&gt;()</c> in both hosts
/// (SPIC.MauiBlazorApp.Web/Program.cs and SPIC.MauiBlazorApp/MauiProgram.cs).
/// </summary>
public sealed class SasApi
{
    private const string Root = "api/Sas";

    /// <summary>Largest payment proof / consignment photo the drop zones accept (server enforces it too).</summary>
    public const long MaxImageBytes = 5L * 1024 * 1024;

    /// <summary>Integers on the wire, web naming, tolerant reader.</summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly LoginState _login;
    private readonly ToastService _toast;

    private SasLookupsDto? _lookups;
    private Task<SasLookupsDto?>? _lookupsInFlight;

    public SasApi(HttpClient http, LoginState login, ToastService toast)
    {
        _http = http;
        _login = login;
        _toast = toast;
    }

    // ---------------------------------------------------------------- who is asking

    /// <summary>
    /// May create / edit / submit / pay / consign. The API enforces the same rule; this only
    /// decides whether a button is worth rendering.
    /// </summary>
    public bool CanWrite =>
        _login.UserRole is AppRole.MDO or AppRole.JMDO or AppRole.Admin or AppRole.CorporateAdmin;

    /// <summary>May approve payments, move a consignment on and enter lab results.</summary>
    public bool CanReview => _login.UserRole is AppRole.Admin or AppRole.CorporateAdmin;

    public string? CurrentUserId => _login.UserId;

    /// <summary>Display name from the JWT - the read-only "Collected By" box on the New page.</summary>
    public string CurrentUserName => NameFromToken() ?? "Me";

    /// <summary>"MDO", "JMDO", ... for the "Collected By (MO)" caption.</summary>
    public string CurrentUserRole => _login.UserRole?.ToString() ?? "";

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
            // a malformed token just means the generic caption
        }

        return null;
    }

    // ---- lookups & farmers -------------------------------------------------------------

    /// <summary>
    /// Couriers, charges, crops, states/districts, packaging types and the UPI details.
    /// Cached per circuit; concurrent callers share the one in-flight request.
    /// </summary>
    public Task<SasLookupsDto?> GetLookupsAsync(CancellationToken ct = default)
    {
        if (_lookups is not null) return Task.FromResult<SasLookupsDto?>(_lookups);

        return _lookupsInFlight ??= LoadLookupsAsync(ct);
    }

    private async Task<SasLookupsDto?> LoadLookupsAsync(CancellationToken ct)
    {
        var dto = await GetAsync<SasLookupsDto>($"{Root}/lookups", "the sample lists", ct);
        _lookupsInFlight = null;
        if (dto is not null) _lookups = dto;
        return dto;
    }

    /// <summary>Price per sample for a type + category; free collections never call this.</summary>
    public decimal ChargeFor(SampleType type, SamplePaidCategory? category)
    {
        if (category is null || _lookups is null) return 0m;

        return _lookups.Charges
            .FirstOrDefault(c => c.SampleType == type && c.Category == category.Value)?
            .AmountPerSample ?? 0m;
    }

    public int TestsFor(SampleType type, SamplePaidCategory? category)
    {
        if (category is null || _lookups is null) return 1;

        return _lookups.Charges
            .FirstOrDefault(c => c.SampleType == type && c.Category == category.Value)?
            .NoOfTests ?? 1;
    }

    /// <summary>Name / mobile search behind the Farmer Name box (debounced by the caller).</summary>
    public async Task<List<SasFarmerDto>> SearchFarmersAsync(string? query, int take = 10,
        CancellationToken ct = default)
    {
        var page = await GetAsync<PageResult<SasFarmerDto>>(
            $"{Root}/farmers?q={Uri.EscapeDataString(query ?? "")}&page=1&pageSize={take}",
            "the farmer list", ct);

        return page?.Items ?? new List<SasFarmerDto>();
    }

    public Task<SasFarmerDto?> GetFarmerAsync(int id, CancellationToken ct = default) =>
        GetAsync<SasFarmerDto>($"{Root}/farmers/{id}", "this farmer", ct);

    public Task<SasFarmerDto?> CreateFarmerAsync(SasFarmerUpsertDto farmer, CancellationToken ct = default) =>
        SendJsonAsync<SasFarmerDto>(HttpMethod.Post, $"{Root}/farmers", farmer, "save the farmer", ct);

    public Task<SasFarmerDto?> UpdateFarmerAsync(int id, SasFarmerUpsertDto farmer, CancellationToken ct = default) =>
        SendJsonAsync<SasFarmerDto>(HttpMethod.Put, $"{Root}/farmers/{id}", farmer, "save the farmer", ct);

    // ---- collections -------------------------------------------------------------------

    public Task<SampleCollectionStatsDto?> GetStatsAsync(CancellationToken ct = default) =>
        GetAsync<SampleCollectionStatsDto>($"{Root}/collections/stats", "the sample statistics", ct);

    /// <summary>The filtered, sorted, paged list behind <c>/SampleCollection</c>.</summary>
    public Task<PageResult<SampleCollectionSummaryDto>?> GetCollectionsAsync(SasCollectionQuery query,
        CancellationToken ct = default)
    {
        var parts = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        // Enums go out as their integer value: the API has no string enum converter.
        if (query.Status is { } status) Add("status", ((int)status).ToString());
        if (query.PaymentType is { } paymentType) Add("paymentType", ((int)paymentType).ToString());
        if (query.PaymentStatus is { } paymentStatus) Add("paymentStatus", ((int)paymentStatus).ToString());
        Add("location", query.Location);
        Add("from", query.From);
        Add("to", query.To);
        Add("q", query.Search);
        Add("sort", query.Sort);
        parts.Add($"page={Math.Max(1, query.Page)}");
        parts.Add($"pageSize={Math.Max(1, query.PageSize)}");

        return GetAsync<PageResult<SampleCollectionSummaryDto>>(
            $"{Root}/collections?{string.Join("&", parts)}", "the sample collections", ct);
    }

    /// <summary>Collections of mine that are ReadyForConsignment (the consignment picker).</summary>
    public async Task<List<SampleCollectionSummaryDto>> GetReadyAsync(string paymentType = "both",
        CancellationToken ct = default)
    {
        var list = await GetAsync<List<SampleCollectionSummaryDto>>(
            $"{Root}/collections/ready?paymentType={Uri.EscapeDataString(paymentType)}",
            "the collections ready to send", ct);

        return list ?? new List<SampleCollectionSummaryDto>();
    }

    /// <summary>
    /// One collection. <c>NotFound</c> separates "it is gone" (the page says so) from
    /// "the call failed" (toast + Try again), exactly like CommunityApi.GetDiscussionAsync.
    /// </summary>
    public async Task<(SampleCollectionDetailDto? Item, bool NotFound)> GetCollectionAsync(int id,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/collections/{id}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound) return (null, true);

            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "load this collection");
                return (null, false);
            }

            return (await response.Content.ReadFromJsonAsync<SampleCollectionDetailDto>(Json, ct), false);
        }
        catch (OperationCanceledException)
        {
            return (null, false);
        }
        catch (Exception)
        {
            ToastOffline("load this collection");
            return (null, false);
        }
    }

    public Task<SampleCollectionDetailDto?> CreateCollectionAsync(SampleCollectionUpsertDto body,
        CancellationToken ct = default) =>
        SendJsonAsync<SampleCollectionDetailDto>(HttpMethod.Post, $"{Root}/collections", body,
            "save this collection", ct);

    public Task<SampleCollectionDetailDto?> UpdateCollectionAsync(int id, SampleCollectionUpsertDto body,
        CancellationToken ct = default) =>
        SendJsonAsync<SampleCollectionDetailDto>(HttpMethod.Put, $"{Root}/collections/{id}", body,
            "save this collection", ct);

    public Task<bool> DeleteCollectionAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"{Root}/collections/{id}", null, "delete this draft", ct);

    /// <summary>Draft -> PendingPayment (paid) or ReadyForConsignment (free).</summary>
    public Task<SampleCollectionDetailDto?> SubmitCollectionAsync(int id, CancellationToken ct = default) =>
        SendJsonAsync<SampleCollectionDetailDto>(HttpMethod.Post, $"{Root}/collections/{id}/submit", null,
            "submit this collection", ct);

    // ---- payments ----------------------------------------------------------------------

    public Task<SamplePaymentDto?> SavePaymentAsync(int collectionId, SamplePaymentUpsertDto body,
        CancellationToken ct = default) =>
        SendJsonAsync<SamplePaymentDto>(HttpMethod.Post, $"{Root}/collections/{collectionId}/payment", body,
            "save this payment", ct);

    /// <summary>
    /// Uploads the payment screenshot as multipart "file". Called after
    /// <see cref="SavePaymentAsync"/> succeeded, so a failure here costs only the proof.
    /// </summary>
    public Task<SasFileDto?> UploadPaymentProofAsync(int collectionId, IBrowserFile file,
        CancellationToken ct = default) =>
        UploadAsync<SasFileDto>($"{Root}/collections/{collectionId}/payment/proof", "file",
            new[] { file }, MaxImageBytes, "upload the payment proof", ct);

    /// <summary>Approve or reject a recorded payment (review roles).</summary>
    public Task<SamplePaymentDto?> SetPaymentStatusAsync(int paymentId, SamplePaymentStatus status,
        string? reason = null, CancellationToken ct = default) =>
        SendJsonAsync<SamplePaymentDto>(HttpMethod.Patch,
            $"{Root}/payments/{paymentId}/status?status={status}" +
            (string.IsNullOrWhiteSpace(reason) ? "" : $"&reason={Uri.EscapeDataString(reason)}"),
            null, "update this payment", ct);

    // ---- files -------------------------------------------------------------------------

    /// <summary>
    /// Absolute URL for a stored SAS file. <c>GET api/Sas/file/{*path}</c> is on the
    /// <c>access_token</c> allowlist, so &lt;img&gt; and target="_blank" work without the
    /// Authorization header (same pattern as the guest-house images in BookingDetails.razor).
    /// </summary>
    public string FileUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
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

    // ---- consignments (owned by Client-Consign) ----------------------------------------
    // Append consignment calls here; never edit the members above.

    /// <summary>The five cards on top of /ConsignmentHistory.</summary>
    public Task<ConsignmentStatsDto?> GetConsignmentStatsAsync(CancellationToken ct = default) =>
        GetAsync<ConsignmentStatsDto>($"{Root}/consignments/stats", "the consignment statistics", ct);

    /// <summary>
    /// The filtered, paged consignment list behind /ConsignmentHistory. Enum filters travel as
    /// their integer value, like <see cref="GetCollectionsAsync"/>.
    /// </summary>
    public Task<PageResult<ConsignmentSummaryDto>?> GetConsignmentsAsync(
        ConsignmentStatus? status = null,
        string? courier = null,
        SamplePaymentType? paymentType = null,
        string? from = null,
        string? to = null,
        string? search = null,
        int page = 1,
        int pageSize = 16,
        CancellationToken ct = default)
    {
        var parts = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        if (status is { } s) Add("status", ((int)s).ToString());
        Add("courier", courier);
        if (paymentType is { } pt) Add("paymentType", ((int)pt).ToString());
        Add("from", from);
        Add("to", to);
        Add("q", search);
        parts.Add($"page={Math.Max(1, page)}");
        parts.Add($"pageSize={Math.Max(1, pageSize)}");

        return GetAsync<PageResult<ConsignmentSummaryDto>>(
            $"{Root}/consignments?{string.Join("&", parts)}", "the consignments", ct);
    }

    /// <summary>
    /// One consignment. <c>NotFound</c> separates "it is gone" (the page says so) from "the call
    /// failed" (toast + Try again), exactly like <see cref="GetCollectionAsync"/>.
    /// </summary>
    public async Task<(ConsignmentDetailDto? Item, bool NotFound)> GetConsignmentAsync(int id,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/consignments/{id}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound) return (null, true);

            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "load this consignment");
                return (null, false);
            }

            return (await response.Content.ReadFromJsonAsync<ConsignmentDetailDto>(Json, ct), false);
        }
        catch (OperationCanceledException)
        {
            return (null, false);
        }
        catch (Exception)
        {
            ToastOffline("load this consignment");
            return (null, false);
        }
    }

    /// <summary>Bundles the chosen ReadyForConsignment collections into one dispatch.</summary>
    public Task<ConsignmentDetailDto?> CreateConsignmentAsync(ConsignmentUpsertDto body,
        CancellationToken ct = default) =>
        SendJsonAsync<ConsignmentDetailDto>(HttpMethod.Post, $"{Root}/consignments", body,
            "submit this consignment", ct);

    /// <summary>
    /// Uploads one drop zone's pictures as multipart "files". Called after
    /// <see cref="CreateConsignmentAsync"/> succeeded, so a failure here costs only the photos:
    /// the consignment itself is already dispatched.
    /// </summary>
    public Task<List<ConsignmentPhotoDto>?> UploadConsignmentPhotosAsync(int consignmentId,
        ConsignmentPhotoKind kind, IReadOnlyList<IBrowserFile> files, string? title = null,
        CancellationToken ct = default)
    {
        if (files.Count == 0) return Task.FromResult<List<ConsignmentPhotoDto>?>(new List<ConsignmentPhotoDto>());

        var url = $"{Root}/consignments/{consignmentId}/photos?kind={kind}" +
                  (string.IsNullOrWhiteSpace(title) ? "" : $"&title={Uri.EscapeDataString(title)}");

        return UploadAsync<List<ConsignmentPhotoDto>>(url, "files", files, MaxImageBytes,
            "upload the consignment photos", ct);
    }

    public Task<bool> DeleteConsignmentPhotoAsync(int photoId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"{Root}/consignments/photos/{photoId}", null,
            "remove this photo", ct);

    /// <summary>
    /// Moves a consignment (and every collection inside it) forward: InTransit, Delivered or
    /// Completed. Review roles only; the API enforces the same rule.
    /// </summary>
    public Task<ConsignmentDetailDto?> SetConsignmentStatusAsync(int id, ConsignmentStatus status,
        CancellationToken ct = default) =>
        SendJsonAsync<ConsignmentDetailDto>(HttpMethod.Patch,
            $"{Root}/consignments/{id}/status?status={status}", null, "update this consignment", ct);

    // ---- details (owned by Client-Details) ---------------------------------------------
    // Append lab-result / timeline calls here; never edit the members above.
    //
    // The details page reads a collection with GetCollectionAsync, its lookups with
    // GetLookupsAsync, its payment proof with FileUrl and reviews a payment with
    // SetPaymentStatusAsync; only the lab-result pair is new.

    /// <summary>
    /// Saved lab results for one sample. The detail DTO already carries them
    /// (<c>SampleItemDto.Results</c>); this refreshes one sample without re-reading the
    /// whole collection. Never null: an empty list means "no results yet".
    /// </summary>
    public async Task<List<LabResultDto>> GetResultsAsync(int itemId, CancellationToken ct = default) =>
        await GetAsync<List<LabResultDto>>($"{Root}/samples/{itemId}/results", "the lab results", ct)
        ?? new List<LabResultDto>();

    /// <summary>
    /// Replaces the result rows of one sample (review roles). The API moves the collection to
    /// TestInProgress, or to Completed once every sample of it has results, so the caller should
    /// re-read the collection afterwards to refresh the status pill and the timeline.
    /// Returns null after one toast when the save failed.
    /// </summary>
    public Task<List<LabResultDto>?> SaveResultsAsync(int itemId, IReadOnlyList<LabResultUpsertDto> rows,
        CancellationToken ct = default) =>
        SendJsonAsync<List<LabResultDto>>(HttpMethod.Put, $"{Root}/samples/{itemId}/results",
            rows as object ?? new List<LabResultUpsertDto>(), "save these lab results", ct);

    /// <summary>
    /// Approve or reject the payment behind a collection, then hand back the refreshed
    /// collection so the details page can redraw the pill, the buttons and the timeline in one go.
    /// </summary>
    public async Task<SampleCollectionDetailDto?> ReviewPaymentAsync(int paymentId, int collectionId,
        SamplePaymentStatus status, string? reason = null, CancellationToken ct = default)
    {
        var payment = await SetPaymentStatusAsync(paymentId, status, reason, ct);
        if (payment is null) return null;

        var (item, _) = await GetCollectionAsync(collectionId, ct);
        return item;
    }

    // ---- plumbing ----------------------------------------------------------------------

    private async Task<T?> GetAsync<T>(string url, string what, CancellationToken ct)
        where T : class
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

    /// <summary>POST/PUT/PATCH that expects a body back; null after one toast on failure.</summary>
    private async Task<T?> SendJsonAsync<T>(HttpMethod method, string url, object? body, string what,
        CancellationToken ct)
        where T : class
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

    private async Task<bool> SendAsync(HttpMethod method, string url, object? body, string what,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);

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

    /// <summary>One multipart POST; every stream is disposed whatever happens.</summary>
    private async Task<T?> UploadAsync<T>(string url, string field, IReadOnlyList<IBrowserFile> files,
        long maxBytes, string what, CancellationToken ct)
        where T : class
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
                if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                    MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
                {
                    part.Headers.ContentType = mediaType;
                }

                content.Add(part, field, file.Name);
            }

            var response = await _http.PostAsync(url, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, what);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (Exception)
        {
            ToastOffline(what);
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
