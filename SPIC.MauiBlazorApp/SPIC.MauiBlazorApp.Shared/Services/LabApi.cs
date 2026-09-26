using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Client wrapper for the SAS Lab portal API (contract: SPIC.Core/DTOs/LabDtos.cs). One method per
/// route; every failure shows ONE toast and returns null / false so pages never throw. Shared by
/// the coordinator, analyst, admin and farmer pages. Append-only for the page agents: add methods
/// you need at the END of the matching region; never change existing signatures.
/// </summary>
public sealed class LabApi
{
    private const string Root = "api/Lab";
    public const int DefaultPageSize = 16;
    public const long MaxDocumentBytes = 20L * 1024 * 1024;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly LoginState _login;
    private readonly ToastService _toast;
    private LabMeDto? _me;

    public LabApi(HttpClient http, LoginState login, ToastService toast)
    {
        _http = http;
        _login = login;
        _toast = toast;
    }

    // ---------------------------------------------------------------- me

    /// <summary>Cached per circuit; decides the dashboard variant and which buttons render.</summary>
    public async Task<LabMeDto?> GetMeAsync(CancellationToken ct = default)
    {
        if (_me is not null) return _me;
        _me = await GetAsync<LabMeDto>($"{Root}/me", "your lab access", ct);
        return _me;
    }

    public void ForgetMe() => _me = null;

    public Task<List<LabUserDto>?> GetAnalystsAsync(CancellationToken ct = default)
        => GetAsync<List<LabUserDto>>($"{Root}/analysts", "the analysts", ct);

    public Task<List<LabLanguageDto>?> GetLanguagesAsync(CancellationToken ct = default)
        => GetAsync<List<LabLanguageDto>>($"{Root}/languages", "the report languages", ct);

    public Task<List<int>?> GetFinancialYearsAsync(CancellationToken ct = default)
        => GetAsync<List<int>>($"{Root}/financial-years", "the financial years", ct);

    // ---------------------------------------------------------------- dashboards

    public Task<LabDashboardDto?> GetDashboardAsync(CancellationToken ct = default)
        => GetAsync<LabDashboardDto>($"{Root}/dashboard", "the lab dashboard", ct);

    public Task<LabAnalystDashboardDto?> GetAnalystDashboardAsync(CancellationToken ct = default)
        => GetAsync<LabAnalystDashboardDto>($"{Root}/analyst/dashboard", "your dashboard", ct);

    // ---------------------------------------------------------------- consignments

    public Task<LabConsignmentStatsDto?> GetConsignmentStatsAsync(CancellationToken ct = default)
        => GetAsync<LabConsignmentStatsDto>($"{Root}/consignments/stats", "the consignment totals", ct);

    public Task<PageResult<LabConsignmentRowDto>?> GetConsignmentsAsync(
        string? status = null, int? stateId = null, string? q = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabConsignmentRowDto>>(
            Url($"{Root}/consignments", ("status", status), ("stateId", stateId), ("q", q), ("from", from), ("to", to), ("page", page), ("pageSize", pageSize)),
            "the consignments", ct);

    public Task<LabConsignmentDetailDto?> GetConsignmentAsync(int id, CancellationToken ct = default)
        => GetAsync<LabConsignmentDetailDto>($"{Root}/consignments/{id}", "the consignment", ct);

    public Task<LabConsignmentRowDto?> ReceiveConsignmentAsync(int id, CancellationToken ct = default)
        => SendJsonAsync<LabConsignmentRowDto>(HttpMethod.Post, $"{Root}/consignments/{id}/receive", null, "mark the consignment received", ct);

    // ---------------------------------------------------------------- batches

    public Task<LabBatchStatsDto?> GetBatchStatsAsync(CancellationToken ct = default)
        => GetAsync<LabBatchStatsDto>($"{Root}/batches/stats", "the batch totals", ct);

    public Task<PageResult<LabBatchRowDto>?> GetBatchesAsync(
        string? status = null, string? priority = null, string? assignedTo = null, string? sampleType = null,
        int? financialYear = null, string? q = null, DateTime? from = null, DateTime? to = null,
        int? batchId = null, int? consignmentId = null, int? stateId = null, int? regionId = null, string? stage = null,
        int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabBatchRowDto>>(
            Url($"{Root}/batches", ("status", status), ("priority", priority), ("assignedTo", assignedTo), ("sampleType", sampleType),
                ("financialYear", financialYear), ("q", q), ("from", from), ("to", to), ("batchId", batchId), ("consignmentId", consignmentId),
                ("stateId", stateId), ("regionId", regionId), ("stage", stage), ("page", page), ("pageSize", pageSize)),
            "the batches", ct);

    public Task<LabBatchDetailDto?> CreateBatchAsync(LabBatchCreateDto dto, CancellationToken ct = default)
        => SendJsonAsync<LabBatchDetailDto>(HttpMethod.Post, $"{Root}/batches", dto, "create the batch", ct);

    public Task<LabBatchDetailDto?> GetBatchAsync(int id, CancellationToken ct = default)
        => GetAsync<LabBatchDetailDto>($"{Root}/batches/{id}", "the batch", ct);

    public Task<LabBatchDetailDto?> AssignBatchAsync(int id, LabBatchAssignDto dto, CancellationToken ct = default)
        => SendJsonAsync<LabBatchDetailDto>(HttpMethod.Patch, $"{Root}/batches/{id}/assign", dto, "assign the batch", ct);

    public Task<LabBatchDetailDto?> SetBatchStatusAsync(int id, SampleBatchStatus status, string? remarks = null, CancellationToken ct = default)
        => SendJsonAsync<LabBatchDetailDto>(HttpMethod.Patch,
            Url($"{Root}/batches/{id}/status", ("status", status.ToString()), ("remarks", remarks)), null, "update the batch status", ct);

    // ---------------------------------------------------------------- samples and parameters

    public Task<LabSampleStatsDto?> GetSampleStatsAsync(int batchId, CancellationToken ct = default)
        => GetAsync<LabSampleStatsDto>($"{Root}/batches/{batchId}/samples/stats", "the sample totals", ct);

    public Task<PageResult<LabSampleRowDto>?> GetSamplesAsync(int batchId,
        string? status = null, string? sampleType = null, string? parameter = null, string? q = null,
        DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabSampleRowDto>>(
            Url($"{Root}/batches/{batchId}/samples", ("status", status), ("sampleType", sampleType), ("parameter", parameter),
                ("q", q), ("from", from), ("to", to), ("page", page), ("pageSize", pageSize)),
            "the samples", ct);

    public string SamplesExportUrl(int batchId, string? status = null, string? sampleType = null, string? parameter = null, string? q = null)
        => AuthorizedUrl(Url($"{Root}/batches/{batchId}/samples/export", ("status", status), ("sampleType", sampleType), ("parameter", parameter), ("q", q)));

    public Task<LabParameterStatsDto?> GetParameterStatsAsync(int batchId, CancellationToken ct = default)
        => GetAsync<LabParameterStatsDto>($"{Root}/batches/{batchId}/parameters/stats", "the parameter totals", ct);

    public Task<PageResult<LabSampleParametersDto>?> GetParametersAsync(int batchId,
        string? status = null, string? sampleType = null, string? parameter = null, string? q = null,
        int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabSampleParametersDto>>(
            Url($"{Root}/batches/{batchId}/parameters", ("status", status), ("sampleType", sampleType), ("parameter", parameter),
                ("q", q), ("page", page), ("pageSize", pageSize)),
            "the parameters", ct);

    public string ParametersExportUrl(int batchId)
        => AuthorizedUrl($"{Root}/batches/{batchId}/parameters/export");

    public Task<LabSampleEntryDto?> GetSampleEntryAsync(int sampleItemId, CancellationToken ct = default)
        => GetAsync<LabSampleEntryDto>($"{Root}/samples/{sampleItemId}", "the sample", ct);

    public Task<LabSampleEntryDto?> SaveSampleValuesAsync(LabSampleValuesDto dto, CancellationToken ct = default)
        => SendJsonAsync<LabSampleEntryDto>(HttpMethod.Put, $"{Root}/samples/{dto.SampleItemId}/values", dto,
            dto.Submit ? "submit the test values" : "save the draft", ct);

    public Task<LabAutoResultDto?> PreviewSampleAsync(LabSampleValuesDto dto, CancellationToken ct = default)
        => SendJsonAsync<LabAutoResultDto>(HttpMethod.Post, $"{Root}/samples/preview", dto, "preview the result", ct);

    // ---------------------------------------------------------------- documents and activity

    public Task<PageResult<LabDocumentDto>?> GetDocumentsAsync(int batchId,
        string? kind = null, string? uploader = null, string? q = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabDocumentDto>>(
            Url($"{Root}/batches/{batchId}/documents", ("kind", kind), ("uploader", uploader), ("q", q), ("from", from), ("to", to), ("page", page), ("pageSize", pageSize)),
            "the documents", ct);

    /// <summary>Multipart upload ("files"); the caller opens the browser files and passes streams.</summary>
    public async Task<List<LabDocumentDto>?> UploadDocumentsAsync(int batchId, LabDocumentKind kind, string? description,
        IEnumerable<(string FileName, string ContentType, Stream Content)> files, CancellationToken ct = default)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            foreach (var (name, type, stream) in files)
            {
                var part = new StreamContent(stream);
                part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(type) ? "application/octet-stream" : type);
                form.Add(part, "files", name);
            }
            var response = await _http.PostAsync(Url($"{Root}/batches/{batchId}/documents", ("kind", kind.ToString()), ("description", description)), form, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, "upload the documents");
                return null;
            }
            return await response.Content.ReadFromJsonAsync<List<LabDocumentDto>>(Json, ct);
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception)
        {
            ToastOffline("upload the documents");
            return null;
        }
    }

    public Task<bool> DeleteDocumentAsync(int docId, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"{Root}/documents/{docId}", null, "delete the document", ct);

    public Task<PageResult<LabActivityDto>?> GetActivitiesAsync(int batchId,
        string? kind = null, string? by = null, string? q = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabActivityDto>>(
            Url($"{Root}/batches/{batchId}/activities", ("kind", kind), ("by", by), ("q", q), ("from", from), ("to", to), ("page", page), ("pageSize", pageSize)),
            "the activity log", ct);

    // ---------------------------------------------------------------- reports

    public Task<LabReportStatsDto?> GetReportStatsAsync(int? financialYear = null, CancellationToken ct = default)
        => GetAsync<LabReportStatsDto>(Url($"{Root}/reports/stats", ("financialYear", financialYear)), "the report totals", ct);

    public Task<PageResult<LabReportBatchRowDto>?> GetReportBatchesAsync(
        int? stateId = null, int? regionId = null, int? hqId = null, string? sampleType = null, int? financialYear = null,
        string? q = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabReportBatchRowDto>>(
            Url($"{Root}/reports/batches", ("stateId", stateId), ("regionId", regionId), ("hqId", hqId), ("sampleType", sampleType),
                ("financialYear", financialYear), ("q", q), ("from", from), ("to", to), ("page", page), ("pageSize", pageSize)),
            "the batch reports", ct);

    public Task<LabBatchReportDto?> GetBatchReportAsync(int batchId, CancellationToken ct = default)
        => GetAsync<LabBatchReportDto>($"{Root}/batches/{batchId}/report", "the batch report", ct);

    public string BatchReportDownloadUrl(int batchId, string lang, string format = "pdf")
        => AuthorizedUrl(Url($"{Root}/batches/{batchId}/report/download", ("lang", lang), ("format", format)));

    public Task<PageResult<LabReportRowDto>?> GetReportsAsync(
        int? batchId = null, string? sampleType = null, string? status = null, string? crop = null, string? village = null,
        int? financialYear = null, string? q = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabReportRowDto>>(
            Url($"{Root}/reports", ("batchId", batchId), ("sampleType", sampleType), ("status", status), ("crop", crop), ("village", village),
                ("financialYear", financialYear), ("q", q), ("from", from), ("to", to), ("page", page), ("pageSize", pageSize)),
            "the reports", ct);

    public Task<LabReportDetailDto?> GetReportAsync(int id, CancellationToken ct = default)
        => GetAsync<LabReportDetailDto>($"{Root}/reports/{id}", "the report", ct);

    public string ReportPdfUrl(int id, string lang) => AuthorizedUrl(Url($"{Root}/reports/{id}/pdf", ("lang", lang)));
    public string ReportXlsxUrl(int id, string lang) => AuthorizedUrl(Url($"{Root}/reports/{id}/xlsx", ("lang", lang)));

    public Task<LabReportRowDto?> MarkReportPrintedAsync(int id, CancellationToken ct = default)
        => SendJsonAsync<LabReportRowDto>(HttpMethod.Post, $"{Root}/reports/{id}/printed", null, "mark the report printed", ct);

    // ---------------------------------------------------------------- files

    /// <summary>
    /// Absolute URL for a relative API path with the caller's token as <c>access_token</c>, so
    /// &lt;a download&gt; / &lt;img&gt; / target="_blank" work in both hosts (the API allowlists these routes).
    /// </summary>
    public string AuthorizedUrl(string relativeUrl)
    {
        var baseUrl = _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
        var token = _login.Token ?? "";
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) token = token["Bearer ".Length..];
        var sep = relativeUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}/{relativeUrl.TrimStart('/')}{sep}access_token={Uri.EscapeDataString(token)}";
    }

    /// <summary>Document / image served by the v1 file route (LabDocumentDto.Url is already relative to the API).</summary>
    public string FileUrl(string relativeUrl) => AuthorizedUrl(relativeUrl);

    // ---------------------------------------------------------------- admin + farmer (phase 1f, append-only)

    /// <summary>
    /// Region master (<c>api/Region/all</c>) for the admin Lab Tracking "Region" filter: the v1
    /// SAS lookups carry states and districts only. Rows use the shared LocationItemDto shape
    /// (Id, RegionName, StateId).
    /// </summary>
    public Task<List<LocationItemDto>?> GetAllRegionsAsync(CancellationToken ct = default)
        => GetAsync<List<LocationItemDto>>("api/Region/all", "the regions", ct);

    /// <summary>Avatar URL from a lab DTO (relative API path) made loadable by &lt;img&gt;; null stays null.</summary>
    public string? AvatarSrc(string? relativeUrl)
        => string.IsNullOrWhiteSpace(relativeUrl) ? null
            : relativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? relativeUrl
            : AuthorizedUrl(relativeUrl);

    // ---------------------------------------------------------------- analyst additions (phase 1e, Client-Analyst)

    /// <summary>Id + name of a location master row (State / Region / Head Quarters filters of the report pages).</summary>
    public sealed record LookupItem(int Id, string Name);

    /// <summary>Batch List (screen 14): GET api/Lab/batches with the analyst filters, including assignedBy
    /// (contains-match on the coordinator's name), which GetBatchesAsync does not pass.</summary>
    public Task<PageResult<LabBatchRowDto>?> GetAnalystBatchesAsync(
        string? status = null, string? priority = null, string? sampleType = null, int? financialYear = null,
        string? assignedBy = null, string? q = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetAsync<PageResult<LabBatchRowDto>>(
            Url($"{Root}/batches", ("status", status), ("priority", priority), ("sampleType", sampleType),
                ("financialYear", financialYear), ("assignedBy", assignedBy), ("q", q), ("from", from), ("to", to),
                ("page", page), ("pageSize", pageSize)),
            "the batches", ct);

    /// <summary>Active states (GET api/State/all), by name.</summary>
    public Task<List<LookupItem>?> GetStatesAsync(CancellationToken ct = default)
        => GetLookupAsync("api/State/all", "stateName", "the states", ct);

    /// <summary>Regions of a state (api/Region/byState/{id}) or all regions.</summary>
    public Task<List<LookupItem>?> GetRegionsAsync(int? stateId = null, CancellationToken ct = default)
        => GetLookupAsync(stateId is > 0 ? $"api/Region/byState/{stateId}" : "api/Region/all", "regionName", "the regions", ct);

    /// <summary>Head quarters of a region (api/Headquarter/byRegion/{id}) or all head quarters.</summary>
    public Task<List<LookupItem>?> GetHeadquartersAsync(int? regionId = null, CancellationToken ct = default)
        => GetLookupAsync(regionId is > 0 ? $"api/Headquarter/byRegion/{regionId}" : "api/Headquarter/all", "headquarterName", "the head quarters", ct);

    private async Task<List<LookupItem>?> GetLookupAsync(string url, string nameProperty, string what, CancellationToken ct)
    {
        var rows = await GetAsync<List<JsonElement>>(url, what, ct);
        if (rows is null) return null;

        var items = new List<LookupItem>();
        foreach (var row in rows)
        {
            if (row.ValueKind != JsonValueKind.Object) continue;
            if (!row.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number) continue;
            if (row.TryGetProperty("isActive", out var active) && active.ValueKind == JsonValueKind.False) continue;
            var name = row.TryGetProperty(nameProperty, out var n) ? n.GetString() : null;
            if (!string.IsNullOrWhiteSpace(name)) items.Add(new LookupItem(id.GetInt32(), name.Trim()));
        }
        return items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ---------------------------------------------------------------- end of analyst additions

    // ---------------------------------------------------------------- coordinator additions (phase 1d, Client-Coordinator)

    /// <summary>Report KPIs for the Lab Reports page; a 404 (no reports yet / report service absent) returns null without a toast.</summary>
    public Task<LabReportStatsDto?> GetReportStatsQuietAsync(int? financialYear = null, CancellationToken ct = default)
        => GetQuietAsync<LabReportStatsDto>(Url($"{Root}/reports/stats", ("financialYear", financialYear)), "the report totals", ct);

    /// <summary>Batch-wise report rows for the Lab Reports page; a 404 returns null without a toast.</summary>
    public Task<PageResult<LabReportBatchRowDto>?> GetReportBatchesQuietAsync(
        int? stateId = null, string? sampleType = null, int? financialYear = null,
        string? q = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = DefaultPageSize, CancellationToken ct = default)
        => GetQuietAsync<PageResult<LabReportBatchRowDto>>(
            Url($"{Root}/reports/batches", ("stateId", stateId), ("sampleType", sampleType), ("financialYear", financialYear),
                ("q", q), ("from", from), ("to", to), ("page", page), ("pageSize", pageSize)),
            "the batch reports", ct);

    /// <summary>Lab Report Details drawer; a 404 (report not generated yet) returns null without a toast.</summary>
    public Task<LabBatchReportDto?> GetBatchReportQuietAsync(int batchId, CancellationToken ct = default)
        => GetQuietAsync<LabBatchReportDto>($"{Root}/batches/{batchId}/report", "the batch report", ct);

    /// <summary>GetAsync, except that 404 Not Found is an expected "nothing yet" answer: no toast.</summary>
    private async Task<T?> GetQuietAsync<T>(string url, string what, CancellationToken ct) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
            {
                await ToastFailureAsync(response, $"load {what}");
                return null;
            }
            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception)
        {
            ToastOffline($"load {what}");
            return null;
        }
    }

    // ---------------------------------------------------------------- end of coordinator additions

    // ---------------------------------------------------------------- plumbing

    private static string Url(string path, params (string Key, object? Value)[] query)
    {
        var parts = new List<string>();
        foreach (var (key, value) in query)
        {
            var text = value switch
            {
                null => null,
                string s => string.IsNullOrWhiteSpace(s) ? null : s,
                DateTime d => d.ToString("yyyy-MM-dd"),
                bool b => b ? "true" : "false",
                _ => value.ToString()
            };
            if (text is null) continue;
            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(text)}");
        }
        return parts.Count == 0 ? path : $"{path}?{string.Join("&", parts)}";
    }

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
        catch (OperationCanceledException) { return null; }
        catch (Exception)
        {
            ToastOffline($"load {what}");
            return null;
        }
    }

    private async Task<T?> SendJsonAsync<T>(HttpMethod method, string url, object? body, string what, CancellationToken ct) where T : class
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
        catch (OperationCanceledException) { return null; }
        catch (Exception)
        {
            ToastOffline(what);
            return null;
        }
    }

    private async Task<bool> SendAsync(HttpMethod method, string url, object? body, string what, CancellationToken ct)
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
        catch (OperationCanceledException) { return false; }
        catch (Exception)
        {
            ToastOffline(what);
            return false;
        }
    }

    private async Task ToastFailureAsync(HttpResponseMessage response, string what)
    {
        string? message = null;
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var m)) message = m.GetString();
                    else if (doc.RootElement.TryGetProperty("Message", out var m2)) message = m2.GetString();
                }
            }
        }
        catch (Exception)
        {
            // not JSON; fall through to the status text
        }

        message ??= response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            System.Net.HttpStatusCode.Forbidden => "You do not have permission for this action.",
            System.Net.HttpStatusCode.NotFound => "That record no longer exists.",
            _ => $"Could not {what}. Please try again."
        };
        _toast.Error(message);
    }

    private void ToastOffline(string what)
        => _toast.Error($"Could not {what}. Check your connection and try again.");
}
