using Microsoft.AspNetCore.Components.Forms;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// The only HTTP surface of Publicity Material Tracking (api/Publicity, routes in PublicityDtos.cs).
/// Also owns the role rules the pages use to decide which buttons to show (the API enforces them).
/// Registered with AddScoped in both hosts.
/// </summary>
public sealed class PublicityApi : ApiClientBase, IDisposable
{
    public const long MaxFileBytes = 10L * 1024 * 1024;
    public const string Accept = ".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx";

    private PubLookupsDto? _lookups;

    public PublicityApi(HttpClient http, LoginState login, ToastService toast) : base(http, login, toast, "api/Publicity")
    {
        // This service is AddScoped, but a scope can outlive a single login (MAUI keeps one scope
        // for the whole app session; a Web circuit survives client-side navigation) - without this,
        // logging out and back in as someone else would keep serving the previous user's cached
        // People list / MyUserId from LookupsAsync() below, silently defaulting "Requested By" on
        // PublicityForm.razor to the wrong person. Same pattern as ClientTelemetry's own
        // Login.OnChange subscription.
        Login.OnChange += OnLoginChanged;
    }

    private void OnLoginChanged() => _lookups = null;

    public void Dispose() => Login.OnChange -= OnLoginChanged;

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
    /// <summary>Publicity Material Master (2026-11-27 authorization fix): a normal Designation-grid
    /// permission (PagePermission.PublicityMaterialMaster) - deliberately NOT IsAdminMdCs (that's a
    /// hardcoded CommonRole + Designation-NAME match, invisible to and unmanageable from Designation
    /// Management). Login.CanAccess already covers the Admin/SuperAdmin bypass (PageAccessModel
    /// .RoleBypass) and the CommonRole -> Designation.RoleAccess grant (PageAccessModel.DesignationOnly)
    /// on its own; AppRole.MDAdmin is added explicitly here because that's a dedicated ROLE bypass
    /// (not a designation-name string match) that must keep working even before anyone grants its own
    /// Designation the new permission via Designation Management - matches
    /// PublicityController.CanManageMaterialMasterAsync()'s exact two bypasses + grid check.</summary>
    public bool CanManageMaterials => Role == AppRole.MDAdmin || Login.CanAccess(PagePermission.PublicityMaterialMaster);
    /// <summary>Delivery Address Management (ADMIN-MD CS only, same gate/exclusions as
    /// CanManageMaterials) - matches PublicityController's delivery-addresses/admin + PUT/DELETE
    /// endpoints' IsAdminMdCsAsync()/IsSuper() check exactly.</summary>
    public bool CanManageDeliveryAddresses => IsAdminMdCs || Role is AppRole.Admin or AppRole.SuperAdmin;
    public bool IsApprover => Role is AppRole.RM or AppRole.SMD or AppRole.SMM or AppRole.Admin or AppRole.SuperAdmin || IsAdminMdCs || IsDgmMdCs;
    /// <summary>Route root of the signed-in user's pages (also the PageGuard key).</summary>
    public string Home => Login.CanAccess(nameof(PagePermission.PublicityReview)) && (IsApprover || IsDispatcher) ? "/PublicityReview" : "/PublicityRequest";
    /// <summary>Anyone in the workflow may raise a request, not only the RMDO (RMD or MDO) - a chain
    /// role creating its own request skips every stage at or below its own rung (API:
    /// PublicityController.Submit). Admin/SuperAdmin keep the testing bypass. ADMIN-MD CS / DGM-MD CS
    /// are deliberately excluded here (they review/approve, they don't raise requests) - the server's
    /// own Create() endpoint still allows them independently of this flag, so this only controls
    /// whether the button is shown, never the actual authorization to create one.</summary>
    public bool CanCreate => Role is AppRole.RMD or AppRole.MDO or AppRole.RM or AppRole.SMD or AppRole.SMM or AppRole.Admin or AppRole.SuperAdmin;
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

    // ---- material master (ADMIN-MD CS only - IsAdminMdCs above decides whether the menu/page show;
    // the API independently enforces the same rule, see PublicityController's IsAdminMdCsAsync) -----

    public Task<List<PubMaterialAdminDto>?> MaterialsAdminAsync() => GetAsync<List<PubMaterialAdminDto>>("materials", "the material list");
    public Task<PubMaterialAdminDto?> CreateMaterialAsync(PubMaterialUpsertDto dto) => SendAsync<PubMaterialAdminDto>(HttpMethod.Post, "materials", dto, "save the material");
    public Task<PubMaterialAdminDto?> UpdateMaterialAsync(int id, PubMaterialUpsertDto dto) => SendAsync<PubMaterialAdminDto>(HttpMethod.Put, $"materials/{id}", dto, "save the material");
    public Task<bool> DeleteMaterialAsync(int id) => SendAsync(HttpMethod.Delete, $"materials/{id}", null, "delete the material");

    public Task<List<PubOption>?> StatesAsync() => GetRawAsync("api/RMDHQMapping/states");
    public Task<List<PubOption>?> RegionsAsync(int stateId) => GetRawAsync($"api/RMDHQMapping/regions/{stateId}");
    public Task<List<PubOption>?> HqsAsync(int regionId) => GetRawAsync($"api/RMDHQMapping/headquarters/{regionId}");

    private async Task<List<PubOption>?> GetRawAsync(string url)
    {
        try { return await System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<List<PubOption>>(Http, url, Json); }
        catch (Exception) { return new List<PubOption>(); }
    }

    public Task<PubStatsDto?> StatsAsync(int? category = null) => GetAsync<PubStatsDto>(category.HasValue ? $"requests/stats?category={category.Value}" : "requests/stats", "the summary");
    public Task<PubPageDto?> ListAsync(string query, CancellationToken ct = default) => GetAsync<PubPageDto>("requests?" + query, "the requests", ct);
    public Task<PubDetailDto?> GetAsync(int id) => GetAsync<PubDetailDto>($"requests/{id}", "the request");
    public Task<List<PubExcessDto>?> ExcessSourcesForProductAsync(int id, int productId) =>
        GetAsync<List<PubExcessDto>>($"requests/{id}/excess-sources?productId={productId}", "the available stock");
    // Only ever called when the list above comes back empty, to distinguish "nothing matches
    // anywhere" from "a match exists at another HQ but hasn't been verified yet" - see ExcessSourcesPending.
    public Task<PubPendingExcessDto?> ExcessSourcesPendingAsync(int id, int productId) =>
        GetAsync<PubPendingExcessDto>($"requests/{id}/excess-sources/pending?productId={productId}", "the pending excess check");
    public Task<PubConsolidationDto?> ConsolidationAsync() => GetAsync<PubConsolidationDto>("consolidation", "the consolidation");

    public Task<PubSaved?> SaveAsync(int? id, PubUpsertDto dto) => id is > 0
        ? SendAsync<PubSaved>(HttpMethod.Put, $"requests/{id}", dto, "save the request")
        : SendAsync<PubSaved>(HttpMethod.Post, "requests", dto, "save the request");

    public Task<bool> DeleteAsync(int id) => SendAsync(HttpMethod.Delete, $"requests/{id}", null, "delete the draft");
    public Task<bool> ReviewAsync(int id, PubReviewDto dto) => SendAsync(HttpMethod.Post, $"requests/{id}/review", dto, "update the request");
    public Task<bool> ReviewBulkAsync(PubBulkReviewDto dto) => SendAsync(HttpMethod.Post, "requests/review-bulk", dto, "approve the requests");
    // Same endpoint, typed response - callers that need to report the server-confirmed Approved
    // count (never the submitted id-list length, so a partial failure is never shown as "all done").
    public Task<PubBulkResultDto?> ReviewBulkDetailedAsync(PubBulkReviewDto dto) =>
        SendAsync<PubBulkResultDto>(HttpMethod.Post, "requests/review-bulk", dto, "approve the requests");
    public Task<bool> MarkRequestReviewedAsync(int id, bool reviewed) =>
        SendAsync(HttpMethod.Post, $"requests/{id}/mark-reviewed", new PubMarkReviewedDto { Reviewed = reviewed }, "update the review status");
    public Task<bool> ApproveRegionAsync(int stateId, int regionId, string? remarks) =>
        SendAsync(HttpMethod.Post, $"regions/{stateId}/{regionId}/approve", new PubRegionActionDto { Remarks = remarks }, "approve the Region");
    public Task<bool> FinalizeZeroRegionAsync(int stateId, int regionId, string? remarks) =>
        SendAsync(HttpMethod.Post, $"regions/{stateId}/{regionId}/finalize-zero", new PubRegionActionDto { Remarks = remarks }, "finalize zero requirement");
    public Task<bool> ConfirmZeroRegionAsync(int stateId, int regionId, string? remarks) =>
        SendAsync(HttpMethod.Post, $"regions/{stateId}/{regionId}/confirm-zero", new PubRegionActionDto { Remarks = remarks }, "confirm zero requirement");
    public Task<PubSaved?> CreateDispatchAsync(int id, PubDispatchCreateDto dto) => SendAsync<PubSaved>(HttpMethod.Post, $"requests/{id}/dispatches", dto, "create the dispatch");
    public Task<bool> VerifyExcessAsync(int id) => SendAsync(HttpMethod.Post, $"requests/{id}/verify-excess", null, "verify excess stock");
    public Task<List<PubBulkImportRowDto>?> ImportExcelAsync(IBrowserFile file) =>
        UploadAsync<List<PubBulkImportRowDto>>("requests/import-excel", "file", new[] { file }, MaxFileBytes, "import the Excel file");
    public Task<PubBulkCreatedDto?> CreateBulkAsync(PubBulkUpsertDto dto) => SendAsync<PubBulkCreatedDto>(HttpMethod.Post, "requests/bulk", dto, "save the requests");
    public Task<PubBulkCreatedDto?> UpdateItemsAsync(int id, PubItemsUpdateDto dto) => SendAsync<PubBulkCreatedDto>(HttpMethod.Put, $"requests/{id}/items", dto, "save the request items");
    // Deliberately NOT cached like LookupsAsync() - the dropdown must reflect a just-added address
    // immediately, and the list is small/cheap enough that a fresh fetch every time is simplest.
    public Task<List<PubDeliveryAddressDto>?> DeliveryAddressesAsync() => GetAsync<List<PubDeliveryAddressDto>>("delivery-addresses", "the saved addresses");
    public Task<PubSaved?> CreateDeliveryAddressAsync(PubDeliveryAddressCreateDto dto) => SendAsync<PubSaved>(HttpMethod.Post, "delivery-addresses", dto, "save the address");
    // Delivery Address Management (ADMIN-MD CS only) - admin list includes inactive rows too, unlike
    // DeliveryAddressesAsync() above which the dispatch dropdown uses and which stays active-only.
    public Task<List<PubDeliveryAddressDto>?> DeliveryAddressesAdminAsync() => GetAsync<List<PubDeliveryAddressDto>>("delivery-addresses/admin", "the address list");
    public Task<PubSaved?> UpdateDeliveryAddressAsync(int id, PubDeliveryAddressUpdateDto dto) => SendAsync<PubSaved>(HttpMethod.Put, $"delivery-addresses/{id}", dto, "save the address");
    public Task<bool> DeleteDeliveryAddressAsync(int id) => SendAsync(HttpMethod.Delete, $"delivery-addresses/{id}", null, "delete the address");
    public Task<bool> DispatchStatusAsync(int dispatchId, PubDispatchStatusDto dto) => SendAsync(HttpMethod.Post, $"dispatches/{dispatchId}/status", dto, "update the dispatch");
    public Task<bool> ProofAsync(int dispatchId, PubProofDto dto) => SendAsync(HttpMethod.Post, $"dispatches/{dispatchId}/proof", dto, "submit the dispatch proof");
    public Task<bool> AcknowledgeAsync(int dispatchId, PubAckUpsertDto dto) => SendAsync(HttpMethod.Put, $"dispatches/{dispatchId}/ack", dto, "save the acknowledgement");

    public Task<List<PubFileDto>?> UploadAsync(int requestId, PublicityFileKind kind, IReadOnlyList<IBrowserFile> files, int? dispatchId = null, int? requestItemId = null) =>
        UploadAsync<List<PubFileDto>>($"requests/{requestId}/files?kind={(int)kind}" + (dispatchId is > 0 ? $"&dispatchId={dispatchId}" : "") + (requestItemId is > 0 ? $"&requestItemId={requestItemId}" : ""),
            "files", files, MaxFileBytes, "upload the file");

    public Task<bool> DeleteFileAsync(int fileId) => SendAsync(HttpMethod.Delete, $"files/{fileId}", null, "remove the file");
    public Task<PubOcrDto?> OcrAsync(IBrowserFile file) => UploadAsync<PubOcrDto>("dispatches/ocr", "file", new[] { file }, MaxFileBytes, "read the receipt", quiet: true);

    public Task<List<PubNotificationDto>?> NotificationsAsync(int? type) =>
        GetAsync<List<PubNotificationDto>>("notifications" + (type is null ? "" : $"?type={type}"), "the notifications");
    public Task<PubNotificationDetailDto?> NotificationAsync(int id) => GetAsync<PubNotificationDetailDto>($"notifications/{id}", "the notification");
    public Task<PubUnreadCountDto?> MarkReadAsync(int id) => SendAsync<PubUnreadCountDto>(HttpMethod.Post, $"notifications/{id}/read", null, "mark as read");
    // Marks every notification about a given request as read for the signed-in user, WITHOUT the user
    // ever opening Notifications - called once a request's details page has confirmed it loaded
    // successfully (PublicityDetails.razor).
    public Task<PubUnreadCountDto?> MarkReadByRequestAsync(int requestId) => SendAsync<PubUnreadCountDto>(HttpMethod.Post, $"notifications/by-request/{requestId}/read", null, "mark as read");
    public Task<PubUnreadCountDto?> UnreadCountAsync() => GetAsync<PubUnreadCountDto>("notifications/unread-count", "the unread count");

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
        PublicityStatus.ExcessVerificationPending => "Excess Verification Pending",
        _ => s.ToString()
    };

    public static string ExceptionStatus(string? exceptionType) => exceptionType switch
    {
        "Excess" => "Excess Reported",
        "Shortage" => "Shortage Reported",
        "Damage" => "Damage Reported",
        not null and not "" and not "None" => exceptionType,
        _ => "Exception Raised"
    };

    public static string StatusTone(PublicityStatus s) => s switch
    {
        PublicityStatus.Draft => "grey",
        PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval or PublicityStatus.AcknowledgementPending
            or PublicityStatus.ExcessVerificationPending => "amber",
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
        PublicityStage.ExcessVerification => "Excess Verification",
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
        PublicityStage.ExcessVerification => "amber",
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

    /// <summary>Region-level status on the consolidation screen reflecting the workflow state.</summary>
    public static string RegionStatus(PubRegionDto g) => g.Status switch
    {
        PubRegionStatus.Approved => "Finalized",
        PubRegionStatus.ZeroConfirmed => "Finalized",
        PubRegionStatus.ZeroSubmittedByRm => "Zero Submitted by RM",
        PubRegionStatus.Reviewed => "Reviewed by SMD",
        PubRegionStatus.InReview => "In SMD Review",
        PubRegionStatus.Submitted => "Submitted to SMD",
        PubRegionStatus.SubmittedToSmm => "Submitted to SMM",
        _ => "Pending RM Submission"
    };

    public static string RegionStatusTone(PubRegionDto g) => g.Status switch
    {
        PubRegionStatus.Approved or PubRegionStatus.ZeroConfirmed or PubRegionStatus.SubmittedToSmm => "green",
        PubRegionStatus.ZeroSubmittedByRm => "violet",
        PubRegionStatus.Reviewed => "cyan",
        PubRegionStatus.InReview => "amber",
        PubRegionStatus.Submitted => "blue",
        _ => "grey"
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
