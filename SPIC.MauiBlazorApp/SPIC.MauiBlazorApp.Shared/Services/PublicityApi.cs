using Microsoft.AspNetCore.Components.Forms;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// The only HTTP surface of Publicity Material Tracking (api/Publicity, routes in PublicityDtos.cs).
/// Also owns the role rules the pages use to decide which buttons to show (the API enforces them).
/// Registered with AddScoped in both hosts.
/// </summary>
public sealed class PublicityApi : ApiClientBase
{
    public const long MaxFileBytes = 10L * 1024 * 1024;
    public const string Accept = ".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx";

    private PubLookupsDto? _lookups;

    public PublicityApi(HttpClient http, LoginState login, ToastService toast) : base(http, login, toast, "api/Publicity") { }

    // ---- who is asking ----------------------------------------------------------------------

    public AppRole? Role => Login.UserRole;
    /// <summary>ADMIN-MD CS is a CommonRole user identified by Designation name, not a dedicated
    /// AppRole (AppRole.MDAdmin is still honoured too - same split as PublicityController).</summary>
    public bool IsAdminMdCs => Role == AppRole.MDAdmin ||
        (Role == AppRole.CommonRole && Login.DesignationName is "ADMIN MD CS" or "ADMIN-MD CS");
    /// <summary>DGM-MD CS: CommonRole + that designation (AppRole.MDHOD still honoured too).</summary>
    public bool IsDgmMdCs => Role == AppRole.MDHOD ||
        (Role == AppRole.CommonRole && Login.DesignationName is "DGM MD CS" or "DGM-MD CS");
    public bool IsDispatcher => IsAdminMdCs || IsDgmMdCs || Role is AppRole.Admin or AppRole.SuperAdmin or AppRole.CorporateAdmin;
    public bool IsApprover => Role is AppRole.RM or AppRole.SMD or AppRole.SMM or AppRole.Admin or AppRole.SuperAdmin || IsAdminMdCs || IsDgmMdCs;
    /// <summary>Route root of the signed-in user's pages (also the PageGuard key).</summary>
    public string Home => Login.CanAccess(nameof(PagePermission.PublicityReview)) && (IsApprover || IsDispatcher) ? "/PublicityReview" : "/PublicityRequest";
    /// <summary>Anyone in the workflow may raise a request, not only the RMDO (RMD or MDO) - a chain
    /// role creating its own request skips every stage at or below its own rung (API:
    /// PublicityController.Submit). Admin/SuperAdmin keep the testing bypass.</summary>
    public bool CanCreate => Role is AppRole.RMD or AppRole.MDO or AppRole.RM or AppRole.SMD or AppRole.SMM or AppRole.Admin or AppRole.SuperAdmin
        || IsAdminMdCs || IsDgmMdCs;
    /// <summary>RM, SMD, SMM: the consolidation reviewers (region / state level).</summary>
    public bool IsFieldApprover => Role is AppRole.RM or AppRole.SMD or AppRole.SMM;
    public PublicityStage? MyStage => Role switch
    {
        AppRole.RM => PublicityStage.RmReview, AppRole.SMD => PublicityStage.SmdReview, AppRole.SMM => PublicityStage.SmmReview,
        _ => IsAdminMdCs ? PublicityStage.AdminMdCsReview : IsDgmMdCs ? PublicityStage.DgmMdCsReview : null
    };
    public string? MyStageLabel => Role switch
    {
        AppRole.RM => "RM Review", AppRole.SMD => "SMD Review", AppRole.SMM => "SMM Review",
        _ => IsAdminMdCs ? "ADMIN-MD CS Review" : IsDgmMdCs ? "DGM-MD CS Review" : null
    };

    // ---- calls ------------------------------------------------------------------------------

    public async Task<PubLookupsDto?> LookupsAsync(int? regionId = null)
    {
        if (regionId is null && _lookups is not null) return _lookups;
        var result = await GetAsync<PubLookupsDto>(regionId is null ? "lookups" : $"lookups?regionId={regionId}", "the lists");
        if (regionId is null) _lookups = result;
        return result;
    }

    public Task<List<PubOption>?> StatesAsync() => GetRawAsync("api/RMDHQMapping/states");
    public Task<List<PubOption>?> RegionsAsync(int stateId) => GetRawAsync($"api/RMDHQMapping/regions/{stateId}");
    public Task<List<PubOption>?> HqsAsync(int regionId) => GetRawAsync($"api/RMDHQMapping/headquarters/{regionId}");

    private async Task<List<PubOption>?> GetRawAsync(string url)
    {
        try { return await System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<List<PubOption>>(Http, url, Json); }
        catch (Exception) { return new List<PubOption>(); }
    }

    public Task<PubStatsDto?> StatsAsync() => GetAsync<PubStatsDto>("requests/stats", "the summary");
    public Task<PubPageDto?> ListAsync(string query, CancellationToken ct = default) => GetAsync<PubPageDto>("requests?" + query, "the requests", ct);
    public Task<PubDetailDto?> GetAsync(int id) => GetAsync<PubDetailDto>($"requests/{id}", "the request");
    public Task<PubConsolidationDto?> ConsolidationAsync() => GetAsync<PubConsolidationDto>("consolidation", "the consolidation");

    public Task<PubSaved?> SaveAsync(int? id, PubUpsertDto dto) => id is > 0
        ? SendAsync<PubSaved>(HttpMethod.Put, $"requests/{id}", dto, "save the request")
        : SendAsync<PubSaved>(HttpMethod.Post, "requests", dto, "save the request");

    public Task<bool> DeleteAsync(int id) => SendAsync(HttpMethod.Delete, $"requests/{id}", null, "delete the draft");
    public Task<bool> ReviewAsync(int id, PubReviewDto dto) => SendAsync(HttpMethod.Post, $"requests/{id}/review", dto, "update the request");
    public Task<bool> ReviewBulkAsync(PubBulkReviewDto dto) => SendAsync(HttpMethod.Post, "requests/review-bulk", dto, "approve the requests");
    public Task<PubSaved?> CreateDispatchAsync(int id, PubDispatchCreateDto dto) => SendAsync<PubSaved>(HttpMethod.Post, $"requests/{id}/dispatches", dto, "create the dispatch");
    public Task<bool> DispatchStatusAsync(int dispatchId, PubDispatchStatusDto dto) => SendAsync(HttpMethod.Post, $"dispatches/{dispatchId}/status", dto, "update the dispatch");
    public Task<bool> ProofAsync(int dispatchId, PubProofDto dto) => SendAsync(HttpMethod.Post, $"dispatches/{dispatchId}/proof", dto, "submit the dispatch proof");
    public Task<bool> AcknowledgeAsync(int dispatchId, PubAckUpsertDto dto) => SendAsync(HttpMethod.Put, $"dispatches/{dispatchId}/ack", dto, "save the acknowledgement");

    public Task<List<PubFileDto>?> UploadAsync(int requestId, PublicityFileKind kind, IReadOnlyList<IBrowserFile> files, int? dispatchId = null) =>
        UploadAsync<List<PubFileDto>>($"requests/{requestId}/files?kind={(int)kind}" + (dispatchId is > 0 ? $"&dispatchId={dispatchId}" : ""),
            "files", files, MaxFileBytes, "upload the file");

    public Task<bool> DeleteFileAsync(int fileId) => SendAsync(HttpMethod.Delete, $"files/{fileId}", null, "remove the file");
    public Task<PubOcrDto?> OcrAsync(IBrowserFile file) => UploadAsync<PubOcrDto>("dispatches/ocr", "file", new[] { file }, MaxFileBytes, "read the receipt", quiet: true);

    public Task<List<PubNotificationDto>?> NotificationsAsync(int? type) =>
        GetAsync<List<PubNotificationDto>>("notifications" + (type is null ? "" : $"?type={type}"), "the notifications");
    public Task<PubNotificationDetailDto?> NotificationAsync(int id) => GetAsync<PubNotificationDetailDto>($"notifications/{id}", "the notification");
    public Task<bool> MarkReadAsync(int id) => SendAsync(HttpMethod.Post, $"notifications/{id}/read", null, "mark as read");

    public sealed class PubSaved
    {
        public int Id { get; set; }
        public string? IndentNo { get; set; }
        public string? DispatchNo { get; set; }
    }
}

/// <summary>Labels and pill tones shared by every Publicity page (tones map to .pub-pill--*).</summary>
public static class PubText
{
    public static string Status(PublicityStatus s) => s switch
    {
        PublicityStatus.Draft => "Draft",
        PublicityStatus.PendingApproval => "Pending Approval",
        PublicityStatus.PendingFinalApproval => "Pending Final Approval",
        PublicityStatus.FinalApproved => "Final Approved",
        PublicityStatus.InDispatch => "In Dispatch",
        PublicityStatus.Delivered => "Delivered",
        PublicityStatus.AcknowledgementPending => "Acknowledgement Pending",
        PublicityStatus.ExceptionRaised => "Exception Raised",
        PublicityStatus.ReInitiationInProgress => "Re-initiation In Progress",
        PublicityStatus.TransferInProgress => "Transfer In Progress",
        PublicityStatus.ExceptionResolved => "Exception Resolved",
        PublicityStatus.Completed => "Completed",
        PublicityStatus.Rejected => "Rejected",
        PublicityStatus.TransferOrderReceived => "Transfer Order Received",
        PublicityStatus.Returned => "Returned",
        _ => s.ToString()
    };

    public static string StatusTone(PublicityStatus s) => s switch
    {
        PublicityStatus.Draft => "grey",
        PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval or PublicityStatus.AcknowledgementPending => "amber",
        PublicityStatus.FinalApproved or PublicityStatus.Completed or PublicityStatus.ExceptionResolved or PublicityStatus.Delivered => "green",
        PublicityStatus.InDispatch => "cyan",
        PublicityStatus.ExceptionRaised or PublicityStatus.Rejected or PublicityStatus.Returned => "red",
        PublicityStatus.ReInitiationInProgress => "orange",
        _ => "violet"
    };

    public static string Stage(PublicityStage s) => s switch
    {
        PublicityStage.IndentCreation => "Indent Creation",
        PublicityStage.RmReview => "RM Review",
        PublicityStage.SmdReview => "SMD Review",
        PublicityStage.SmmReview => "SMM Review",
        PublicityStage.AdminMdCsReview => "ADMIN-MD CS",
        PublicityStage.DgmMdCsReview => "DGM-MD CS",
        PublicityStage.Dispatch => "Ready for Dispatch",
        PublicityStage.Delivery => "Dispatch & Delivery",
        PublicityStage.Acknowledgement => "Acknowledgement",
        PublicityStage.ExceptionHandling => "Exception Handling",
        PublicityStage.ReInitiation => "Re-initiation",
        PublicityStage.InterHqTransfer => "Inter-HQ Transfer",
        PublicityStage.TransferCreation => "Transfer Creation",
        _ => "Completed"
    };

    /// <summary>Who acts after / before a review stage ("Approve & Forward to SMD", "pending from RM").</summary>
    public static string Next(PublicityStage s) => s switch
    {
        PublicityStage.RmReview => "SMD", PublicityStage.SmdReview => "SMM", PublicityStage.SmmReview => "ADMIN - MD CS",
        PublicityStage.AdminMdCsReview => "DGM - MD CS", _ => "Dispatch"
    };
    public static string Prev(PublicityStage s) => s switch
    {
        PublicityStage.SmdReview => "RM", PublicityStage.SmmReview => "SMD", PublicityStage.AdminMdCsReview => "SMM",
        PublicityStage.DgmMdCsReview => "ADMIN - MD CS", _ => "RMDO"
    };
    /// <summary>List-column wording used on the reviewer lists ("RM Approval", "Admin Approval").</summary>
    public static string StageShort(PublicityStage s) => s switch
    {
        PublicityStage.RmReview => "RM Approval", PublicityStage.SmdReview => "SMD Approval", PublicityStage.SmmReview => "SMM Approval",
        PublicityStage.AdminMdCsReview => "Admin Approval", PublicityStage.DgmMdCsReview => "DGM-MD CS Approval",
        PublicityStage.Dispatch or PublicityStage.Delivery => "Dispatch & Delivery", _ => Stage(s)
    };

    public static string StageTone(PublicityStage s) => s switch
    {
        <= PublicityStage.DgmMdCsReview => "blue",
        PublicityStage.Dispatch or PublicityStage.Delivery => "cyan",
        PublicityStage.Acknowledgement or PublicityStage.Completed => "green",
        PublicityStage.ExceptionHandling => "red",
        PublicityStage.ReInitiation => "orange",
        _ => "violet"
    };

    public static string DispatchStatus(PublicityDispatchStatus s, PublicityDispatchKind k) => s switch
    {
        PublicityDispatchStatus.TransferRequested => "Awaiting Dispatch Proof",
        PublicityDispatchStatus.ProofSubmitted => "Proof Submitted to Admin",
        PublicityDispatchStatus.Dispatched => k == PublicityDispatchKind.Transfer ? "Transfer Created" : "Dispatched",
        PublicityDispatchStatus.InTransit => "In Transit",
        PublicityDispatchStatus.Delivered => "Delivered",
        _ => "Completed"
    };

    public static string Kind(PublicityDispatchKind k) => k switch
    {
        PublicityDispatchKind.Original => "Original Dispatch",
        PublicityDispatchKind.ReInitiation => "Re-Initiation Dispatch",
        _ => "Stock Transfer (Shortage / Damage Resolution)"
    };

    public static string FileKind(PublicityFileKind k) => k switch
    {
        PublicityFileKind.Design => "Design File",
        PublicityFileKind.DispatchDocument => "Dispatch Document",
        PublicityFileKind.TransportSlip => "Transport Slip",
        PublicityFileKind.EWayBill => "E-Way Bill",
        PublicityFileKind.AckPhoto => "Acknowledgement Photo",
        PublicityFileKind.CourierProof => "Courier Proof",
        PublicityFileKind.NoteApproval => "Note Approval Document",
        _ => "Document"
    };

    public static string Size(long bytes) => bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";
    // The API stores UTC in "timestamp without time zone" (legacy Npgsql mode), so values arrive
    // unspecified; show them in IST whatever the host's own zone is (prod web runs on UTC).
    /// <summary>Today with no zone: a Local value would carry "+05:30" and land on the previous
    /// day on the UTC API host (see the JMDO attendance fix 38a8630b).</summary>
    public static System.DateTime Today => System.DateTime.SpecifyKind(System.DateTime.Today, DateTimeKind.Unspecified);
    public static System.DateTime Ist(System.DateTime d) => d.Kind == DateTimeKind.Local ? d : System.DateTime.SpecifyKind(d, DateTimeKind.Unspecified).AddMinutes(330);
    public static string Date(System.DateTime? d) => d is null ? "-" : Ist(d.Value).ToString("dd MMM yyyy");
    public static string DateTime(System.DateTime? d) => d is null ? "-" : Ist(d.Value).ToString("dd MMM yyyy, hh:mm tt");
    public static bool IsImage(string name) => name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
}
