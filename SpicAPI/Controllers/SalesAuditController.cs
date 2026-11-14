using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Collections;
using System.Data;
using SPIC.Core.DTOs;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers;

public sealed class SalesAuditEligibilityOptions
{
    public int[] EligibleProgramIds { get; set; } = Array.Empty<int>();
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SalesAuditController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SalesAuditEligibilityOptions _options;

    public SalesAuditController(AppDbContext db, IOptions<SalesAuditEligibilityOptions> options)
    {
        _db = db;
        _options = options.Value;
    }


    // Read-only extension request list for authorized administrators.
    // No updates to SalesAuditRecords or SalesAuditExtensionRequests.
    [HttpGet("extension/requests")]
    public async Task<IActionResult> GetExtensionRequests()
    {
        // Admin accounts may authenticate without an Employeelogin record.
        // Trust only authenticated role claims issued by the configured auth scheme;
        // never accept a role from query parameters or request bodies.
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        bool isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") ||
            User.Claims.Any(c =>
                (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles" ||
                 c.Type == "AppRole" || c.Type == "app_role") &&
                (string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase)));

        // Compatibility for deployments that keep the validated role on Employeelogin.
        if (!isAdmin)
        {
            var employee = await _db.Set<Employeelogin>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted);
            if (employee is not null)
                isAdmin = ResolveTrackingRole(employee, User) is "ADMIN" or "SUPERADMIN";
        }
        if (!isAdmin) return Forbid();

        // Left joins preserve extension records even when their audit or program is missing.
        var rows = await (
            from ext in _db.Set<SalesAuditExtensionRequest>().AsNoTracking()
            join audit0 in _db.Set<SalesAuditRecord>().AsNoTracking()
                on ext.CSRId equals audit0.CSR2Id into audits
            from audit in audits.DefaultIfEmpty()
            join csr20 in _db.Set<CSR2>().AsNoTracking()
                on ext.CSRId equals csr20.Id into csr2s
            from csr2 in csr2s.DefaultIfEmpty()
            join csr10 in _db.Set<CSR1>().AsNoTracking()
                on csr2.CSR1Id equals csr10.Id into csr1s
            from csr1 in csr1s.DefaultIfEmpty()
            join program0 in _db.Set<ProgramMaster>().AsNoTracking()
                on csr1.ProgramId equals program0.Id into programs
            from program in programs.DefaultIfEmpty()
            orderby ext.CreatedDate descending, ext.Id descending
            select new
            {
                ext.Id,
                ext.CSRId,
                ext.CreatedBy,
                ext.CreatedDate,
                ext.PreviousEndDate,
                ext.NewEndDate,
                ext.ApprovedBy,
                ext.Status,
                SanctionNumber = csr1 != null ? (csr1.SanctionNo ?? "") : "",
                ProgramName = program != null ? program.Name : "",
                Location = csr2 != null ? (csr2.ActualLocation ?? "") : "",
                SchemeFrom = audit != null ? audit.SchemeFrom : (DateTime?)null,
                SchemeTo = audit != null ? audit.SchemeTo : (DateTime?)null,
                RequiredTurnover = audit != null ? audit.RequiredTurnover : (decimal?)null,
                ActualTurnover = audit != null ? audit.ActualTurnover : (decimal?)null,
                DocumentStatus = audit != null ? audit.DocumentStatus : null
            }).ToListAsync(HttpContext.RequestAborted);

        var requesterNames = await ResolveExtensionRequesterNamesAsync(
            rows.Select(x => x.CreatedBy), HttpContext.RequestAborted);

        // Status is authoritative. Program Type has no verified separate master field,
        // so continue displaying ProgramName consistently.
        return Ok(rows.Select(x =>
        {
            var required = x.RequiredTurnover ?? 0m;
            var actual = x.ActualTurnover ?? 0m;
            return new ExtensionRequestListDto(
                x.Id, x.CSRId, x.SanctionNumber,
                x.ProgramName, x.ProgramName, x.Location,
                requesterNames.TryGetValue(x.CreatedBy, out var displayName) ? displayName : x.CreatedBy,
                x.CreatedDate, x.SchemeFrom,
                x.SchemeTo ?? x.PreviousEndDate,
                required, actual, x.NewEndDate,
                Math.Max(0, (x.NewEndDate.Date - x.PreviousEndDate.Date).Days),
                required > 0m ? Math.Max(0m, actual / required * 100m) : 0m,
                x.DocumentStatus ?? "Not Started",
                ExtensionStatusLabel(x.Status)
            );
        }).ToArray());
    }

    // Resolve CreatedBy (authenticated user ID) to the employee's visible name.
    // Do not expose a different person's name when the login mapping is absent.
    private async Task<Dictionary<string, string>> ResolveExtensionRequesterNamesAsync(
        IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal).ToArray();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (ids.Length == 0) return result;

        var logins = await _db.Set<Employeelogin>().AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .ToListAsync(cancellationToken);
        var links = new List<(string userId, int employeeId)>();
        foreach (var login in logins)
        {
            // Legacy SPIC branches use EmployeeInformationID or EmployeeInformationId.
            var property = login.GetType().GetProperty("EmployeeInformationID",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null) continue;
            var rawId = property.GetValue(login);
            if (int.TryParse(Convert.ToString(rawId, CultureInfo.InvariantCulture), out var employeeId)
                && employeeId > 0)
                links.Add((login.UserId, employeeId));
        }
        var employeeIds = links.Select(x => x.employeeId).Distinct().ToArray();
        if (employeeIds.Length == 0) return result;
        var employees = await _db.Set<EmployeeInformation>().AsNoTracking()
            .Where(x => employeeIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);
        var names = employees.Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => x.Id, x => x.Name.Trim());
        foreach (var (userId, employeeId) in links)
            if (names.TryGetValue(employeeId, out var name)) result[userId] = name;
        return result;
    }

    // Display-only lookup. Never replace persisted ApprovedBy/RejectedBy IDs with names.
    // Reuse employee master names; Admin/SuperAdmin accounts can exist only in UserInfo.
    private async Task<Dictionary<string, string>> ResolveExtensionReviewerNamesAsync(
        IEnumerable<string?> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length == 0) return new Dictionary<string, string>(StringComparer.Ordinal);

        var result = await ResolveExtensionRequesterNamesAsync(ids, cancellationToken);
        var unresolved = ids.Where(x => !result.ContainsKey(x)).ToArray();
        if (unresolved.Length == 0) return result;

        // UserInfo is the identity entity mapped by this SPIC ONE branch.
        // Do not assume every Admin has an Employeelogin row.
        var identityUsers = await _db.Set<UserInfo>().AsNoTracking()
            .Where(x => unresolved.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.UserName })
            .ToListAsync(cancellationToken);

        foreach (var identityUser in identityUsers)
        {
            var name = !string.IsNullOrWhiteSpace(identityUser.Name)
                ? identityUser.Name.Trim()
                : identityUser.UserName?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
                result[identityUser.Id] = name;
        }
        return result;
    }

    // GET by extension request primary key (not CSR2Id); details must never read sample rows.
    // This endpoint is read-only and uses the same Admin/SuperAdmin policy as the list.
    [HttpGet("extension/requests/{requestId:int}")]
    public async Task<IActionResult> GetExtensionRequestDetails(int requestId)
    {
        if (requestId <= 0) return BadRequest(new { message = "Invalid request ID." });
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        bool isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") ||
            User.Claims.Any(c =>
                (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles" ||
                 c.Type == "AppRole" || c.Type == "app_role") &&
                (string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase)));
        if (!isAdmin)
        {
            var employee = await _db.Set<Employeelogin>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted);
            if (employee is not null)
                isAdmin = ResolveTrackingRole(employee, User) is "ADMIN" or "SUPERADMIN";
        }
        if (!isAdmin) return Forbid();

        var item = await (
            from ext in _db.Set<SalesAuditExtensionRequest>().AsNoTracking()
            where ext.Id == requestId
            join audit0 in _db.Set<SalesAuditRecord>().AsNoTracking()
                on ext.CSRId equals audit0.CSR2Id into audits
            from audit in audits.DefaultIfEmpty()
            join c20 in _db.Set<CSR2>().AsNoTracking()
                on ext.CSRId equals c20.Id into c2s
            from c2 in c2s.DefaultIfEmpty()
            join c10 in _db.Set<CSR1>().AsNoTracking()
                on c2.CSR1Id equals c10.Id into c1s
            from c1 in c1s.DefaultIfEmpty()
            join p0 in _db.Set<ProgramMaster>().AsNoTracking()
                on c1.ProgramId equals p0.Id into programs
            from program in programs.DefaultIfEmpty()
            select new
            {
                ext.Id,
                ext.CSRId,
                ext.CreatedBy,
                ext.CreatedDate,
                ext.PreviousEndDate,
                ext.NewEndDate,
                ext.Reason,
                ext.Remarks,
                ext.ApprovedBy,
                ext.ApprovedDate,
                ext.RejectedBy,
                ext.RejectedDate,
                ext.Status,
                SanctionNumber = c1 != null ? (c1.SanctionNo ?? "") : "",
                ProgramName = program != null ? program.Name : "",
                Location = c2 != null ? (c2.ActualLocation ?? "") : "",
                SchemeFrom = audit != null ? audit.SchemeFrom : (DateTime?)null,
                RequiredTurnover = audit != null ? audit.RequiredTurnover : (decimal?)null,
                ActualTurnover = audit != null ? audit.ActualTurnover : (decimal?)null,
                DocumentStatus = audit != null ? audit.DocumentStatus : null
            }).FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (item is null) return NotFound(new { message = "Extension request not found." });
        var creatorNames = await ResolveExtensionRequesterNamesAsync(
            new[] { item.CreatedBy }, HttpContext.RequestAborted);
        var requestedBy = creatorNames.TryGetValue(item.CreatedBy, out var creatorName)
            ? creatorName : item.CreatedBy;
        var reviewerNames = await ResolveExtensionReviewerNamesAsync(
            new[] { item.ApprovedBy, item.RejectedBy }, HttpContext.RequestAborted);
        string? DisplayReviewer(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return reviewerNames.TryGetValue(id, out var name) ? name : "User not found";
        }
        var required = item.RequiredTurnover ?? 0m;
        var actual = item.ActualTurnover ?? 0m;
        return Ok(new ExtensionRequestDetailsDto(
            item.Id, item.CSRId, item.SanctionNumber, item.ProgramName,
            item.Location, requestedBy, item.CreatedDate,
            item.SchemeFrom, item.PreviousEndDate, item.NewEndDate,
            required, actual,
            required > 0m ? actual / required * 100m : 0m,
            item.DocumentStatus ?? "Not Started",
            item.Reason, item.Remarks, DisplayReviewer(item.ApprovedBy), item.ApprovedDate,
            ExtensionStatusLabel(item.Status))
        {
            RejectedBy = DisplayReviewer(item.RejectedBy),
            RejectedDate = item.RejectedDate
        });
    }

    // Approve an existing request by its extension request PK (not CSRId).
    // Approval is the ONLY point at which SalesAuditRecords.SchemeTo changes.
    [HttpPost("extension/requests/{requestId:int}/approve")]
    public async Task<IActionResult> ApproveExtensionRequest(int requestId)
    {
        if (requestId <= 0)
            return BadRequest(new { message = "Invalid extension request ID." });
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        // Explicit Admin-only WRITE authorization. Do not reuse tracking GET permission.
        // Role claims are trusted only through the server's authentication middleware.
        bool isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") ||
            User.Claims.Any(c =>
                (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles" ||
                 c.Type == "AppRole" || c.Type == "app_role") &&
                (string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase)));
        if (!isAdmin)
        {
            var employee = await _db.Set<Employeelogin>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.UserId == userId,
                    HttpContext.RequestAborted);
            if (employee is not null)
                isAdmin = ResolveTrackingRole(employee, User) is "ADMIN" or "SUPERADMIN";
        }
        if (!isAdmin) return Forbid();

        // SERIALIZABLE ensures competing approvals/revisions cannot silently overwrite
        // each other. Both changes commit or neither does.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, HttpContext.RequestAborted);
        try
        {
            var request = await _db.Set<SalesAuditExtensionRequest>()
                .FirstOrDefaultAsync(x => x.Id == requestId, HttpContext.RequestAborted);
            if (request is null)
                return NotFound(new { message = "Extension request not found." });

            if (request.Status != SalesAuditExtensionRequestStatus.Submitted)
                return Conflict(new { message = "Only submitted extension requests can be approved." });

            // Fail closed on inconsistent legacy approval or rejection metadata.
            if (!string.IsNullOrWhiteSpace(request.ApprovedBy) || request.ApprovedDate.HasValue)
                return Conflict(new { message = "Request already contains approval information. Review migration/status before approving." });
            if (!string.IsNullOrWhiteSpace(request.RejectedBy) || request.RejectedDate.HasValue)
                return Conflict(new { message = "Request contains rejection information. Review migration/status before approving." });

            var audit = await _db.Set<SalesAuditRecord>()
                .FirstOrDefaultAsync(x => x.CSR2Id == request.CSRId, HttpContext.RequestAborted);
            if (audit is null)
                return NotFound(new { message = "Linked Sales Audit record not found." });
            if (!audit.SchemeTo.HasValue)
                return Conflict(new { message = "The current scheme end date is missing." });

            // Prevent applying a stale approval to a different scheme period.
            if (audit.SchemeTo.Value.Date != request.PreviousEndDate.Date)
                return Conflict(new { message = "Scheme end date has changed since this request was submitted. Review the current audit period before approving." });
            if (request.NewEndDate.Date <= audit.SchemeTo.Value.Date)
                return Conflict(new { message = "Requested new end date must be later than the current scheme end date." });

            request.ApprovedBy = userId;
            request.ApprovedDate = DateTime.UtcNow;
            request.Status = SalesAuditExtensionRequestStatus.Approved;
            audit.SchemeTo = request.NewEndDate.Date;

            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return Ok(new
            {
                requestId = request.Id,
                csrId = request.CSRId,
                approvedBy = request.ApprovedBy,
                approvedDate = request.ApprovedDate,
                previousEndDate = request.PreviousEndDate,
                newEndDate = request.NewEndDate,
                status = SalesAuditExtensionRequestStatus.Approved.ToString()
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(HttpContext.RequestAborted);
            return Conflict(new { message = "Approval changed concurrently. Reload and try again." });
        }
        catch (Exception ex) when (HasPostgresSqlState(ex, "40001"))
        {
            await transaction.RollbackAsync(HttpContext.RequestAborted);
            return Conflict(new { message = "Another approval or audit change occurred concurrently. Reload and retry." });
        }
    }

    // Reject submitted extension request without changing SalesAuditRecords.SchemeTo.
    // Persist rejection audit metadata; leave SalesAuditRecords.SchemeTo unchanged.
    [HttpPost("extension/requests/{requestId:int}/reject")]
    public async Task<IActionResult> RejectExtensionRequest(int requestId)
    {
        if (requestId <= 0) return BadRequest(new { message = "Invalid extension request ID." });
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        // Same Admin/SuperAdmin authorization as the approval write endpoint.
        bool isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") ||
            User.Claims.Any(c =>
                (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles" ||
                 c.Type == "AppRole" || c.Type == "app_role") &&
                (string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase)));
        if (!isAdmin)
        {
            var employee = await _db.Set<Employeelogin>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted);
            if (employee is not null)
                isAdmin = ResolveTrackingRole(employee, User) is "ADMIN" or "SUPERADMIN";
        }
        if (!isAdmin) return Forbid();

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, HttpContext.RequestAborted);
        try
        {
            var request = await _db.Set<SalesAuditExtensionRequest>()
                .FirstOrDefaultAsync(x => x.Id == requestId, HttpContext.RequestAborted);
            if (request is null) return NotFound(new { message = "Extension request not found." });
            if (request.Status != SalesAuditExtensionRequestStatus.Submitted)
                return Conflict(new { message = "Only submitted extension requests can be rejected." });
            if (!string.IsNullOrWhiteSpace(request.ApprovedBy) || request.ApprovedDate.HasValue)
                return Conflict(new { message = "Request contains existing approval information. Review status before rejecting." });
            if (!string.IsNullOrWhiteSpace(request.RejectedBy) || request.RejectedDate.HasValue)
                return Conflict(new { message = "Request contains existing rejection information. Review status before rejecting." });

            request.Status = SalesAuditExtensionRequestStatus.Rejected;
            request.RejectedBy = userId;
            request.RejectedDate = DateTime.UtcNow;
            // Never populate ApprovedBy/ApprovedDate or update SalesAuditRecords on rejection.
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return Ok(new
            {
                requestId = request.Id,
                csrId = request.CSRId,
                status = request.Status.ToString(),
                rejectedBy = request.RejectedBy,
                rejectedDate = request.RejectedDate
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(HttpContext.RequestAborted);
            return Conflict(new { message = "Request changed concurrently. Reload and retry." });
        }
        catch (Exception ex) when (HasPostgresSqlState(ex, "40001"))
        {
            await transaction.RollbackAsync(HttpContext.RequestAborted);
            return Conflict(new { message = "Another review occurred concurrently. Reload and retry." });
        }
    }

    private static string ExtensionStatusLabel(SalesAuditExtensionRequestStatus status) => status switch
    {
        SalesAuditExtensionRequestStatus.Submitted => "Pending", // compatible with existing Razor filters/buttons
        SalesAuditExtensionRequestStatus.Approved => "Approved",
        SalesAuditExtensionRequestStatus.Rejected => "Rejected",
        _ => "Unknown"
    };

    // Extension requests live in THIS existing SalesAuditController.
    // Route identifier is CSR2Id, not SalesAuditRecord primary-key ID.
    [HttpGet("extension/{csr2Id:int}")]
    public async Task<IActionResult> GetExtension(int csr2Id)
    {
        if (csr2Id <= 0)
            return BadRequest(new { message = "Invalid CSR2 ID." });

        if (!await CanReadSalesAuditRecordAsync(csr2Id))
            return Forbid();

        var scheme = await (
            from audit in _db.Set<SalesAuditRecord>().AsNoTracking()
            join c2 in _db.Set<CSR2>().AsNoTracking()
                on audit.CSR2Id equals c2.Id
            join c1 in _db.Set<CSR1>().AsNoTracking()
                on c2.CSR1Id equals c1.Id
            join program in _db.Set<ProgramMaster>().AsNoTracking()
                on c1.ProgramId equals program.Id
            where audit.CSR2Id == csr2Id
            select new
            {
                audit.CSR2Id,
                ProgramName = program.Name,
                SanctionNumber = c1.SanctionNo ?? "",
                audit.SchemeFrom,
                audit.SchemeTo,
                audit.RequiredTurnover,
                audit.ActualTurnover
            }
        ).FirstOrDefaultAsync(HttpContext.RequestAborted);

        if (scheme is null)
            return NotFound(new { message = "Sales Audit record not found." });

        if (!scheme.SchemeFrom.HasValue || !scheme.SchemeTo.HasValue)
            return BadRequest(new
            {
                message = "Scheme dates are not configured."
            });

        return Ok(new SchemeExtensionInfoDto(
            scheme.CSR2Id,
            scheme.ProgramName,
            scheme.SanctionNumber,
            scheme.SchemeFrom.Value,
            scheme.SchemeTo.Value,
            scheme.RequiredTurnover ?? 0m,
            scheme.ActualTurnover ?? 0m
        ));
    }

    // Read-only latest extension request for the selected Sales Tracking CSR2.
    // Reuse the existing record-level Sales Audit read authorization; do not broaden it.
    [HttpGet("extension/{csr2Id:int}/latest")]
    public async Task<IActionResult> GetLatestExtensionForTracking(int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest(new { message = "Invalid CSR2 ID." });
        if (!await CanReadSalesAuditRecordAsync(csr2Id)) return Forbid();

        var latest = await _db.Set<SalesAuditExtensionRequest>()
            .AsNoTracking()
            .Where(x => x.CSRId == csr2Id)
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.CSRId,
                x.CreatedBy,
                x.CreatedDate,
                x.PreviousEndDate,
                x.NewEndDate,
                x.Status,
                x.Reason,
                x.Remarks
            })
            .FirstOrDefaultAsync(HttpContext.RequestAborted);

        if (latest is null) return NotFound();
        var hasApprovedExtension = await _db.Set<SalesAuditExtensionRequest>()
            .AsNoTracking().AnyAsync(x => x.CSRId == csr2Id &&
                x.Status == SalesAuditExtensionRequestStatus.Approved, HttpContext.RequestAborted);
        var creatorNames = await ResolveExtensionRequesterNamesAsync(
            new[] { latest.CreatedBy }, HttpContext.RequestAborted);
        var displayName = creatorNames.TryGetValue(latest.CreatedBy, out var name)
            ? name : latest.CreatedBy;

        return Ok(new
        {
            requestId = latest.Id,
            csrId = latest.CSRId,
            previousEndDate = latest.PreviousEndDate,
            newEndDate = latest.NewEndDate,
            requestedOn = latest.CreatedDate,
            requestedBy = displayName,
            status = latest.Status.ToString(),
            reason = latest.Reason,
            remarks = latest.Remarks,
            hasApprovedExtension
        });
    }

    // Existing unapproved request, for the read-only pending summary in the UI.
    [HttpGet("extension/{csr2Id:int}/pending")]
    public async Task<IActionResult> GetPendingExtension(int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest(new { message = "Invalid CSR2 ID." });
        if (!await CanReadSalesAuditRecordAsync(csr2Id)) return Forbid();

        var pending = await _db.Set<SalesAuditExtensionRequest>().AsNoTracking()
            .Where(x => x.CSRId == csr2Id && x.Status == SalesAuditExtensionRequestStatus.Submitted)
            .OrderByDescending(x => x.Id)
            .Select(x => new ExtensionRequestReceiptDto(
                x.Id, x.CreatedDate, x.PreviousEndDate,
                x.NewEndDate, x.Reason, x.Remarks))
            .FirstOrDefaultAsync(HttpContext.RequestAborted);

        return pending is null ? NotFound() : Ok(pending);
    }

    [HttpPost("extension/{csr2Id:int}")]
    public async Task<IActionResult> SubmitExtension(int csr2Id, [FromBody] CreateExtensionRequestDto input)
    {
        if (csr2Id <= 0) return BadRequest(new { message = "Invalid CSR2 ID." });
        if (input is null) return BadRequest(new { message = "Request body is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        // Explicit WRITE authorization. Never rely on CanViewTrackingRecordAsync for mutation.
        if (!await CanRequestExtensionAsync(csr2Id, userId)) return Forbid();

        if (input.NewEndDate == default || input.NewEndDate.TimeOfDay != TimeSpan.Zero)
            return BadRequest(new { message = "Provide a valid date without a time component." });
        if (input.Reason is null || !ExtensionReasons.Contains(input.Reason))
            return BadRequest(new { message = "Select a valid extension reason." });
        var remarks = input.Remarks?.Trim() ?? "";
        if (remarks.Length > 500)
            return BadRequest(new { message = "Remarks cannot exceed 500 characters." });

        // Insert only. Serializable isolation + the partial unique DB index prevent
        // concurrent submissions while a request remains Submitted.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, HttpContext.RequestAborted);
        var pendingExists = await _db.Set<SalesAuditExtensionRequest>()
            .AsNoTracking()
            .AnyAsync(x => x.CSRId == csr2Id && x.Status == SalesAuditExtensionRequestStatus.Submitted,
                HttpContext.RequestAborted);
        if (pendingExists)
            return Conflict(new { message = "An extension request is already pending approval for this CSR. No new request was saved." });

        var auditRecord = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.CSR2Id == csr2Id, HttpContext.RequestAborted);
        if (auditRecord is null) return NotFound(new { message = "Sales Audit record not found." });
        if (!auditRecord.SchemeTo.HasValue)
        {
            return BadRequest(new
            {
                message = "Current scheme end date is not configured."
            });
        }

        DateTime previous = auditRecord.SchemeTo.Value;

        if (input.NewEndDate.Date <= previous.Date)
        {
            return BadRequest(new
            {
                message = "New end date must be later than the current end date."
            });
        }

        var extension = new SalesAuditExtensionRequest
        {
            CSRId = csr2Id,
            PreviousEndDate = previous,
            NewEndDate = input.NewEndDate.Date,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow,
            Reason = input.Reason,
            Remarks = remarks,
            Status = SalesAuditExtensionRequestStatus.Submitted
        };
        _db.Set<SalesAuditExtensionRequest>().Add(extension);

        try
        {
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(HttpContext.RequestAborted);
            return Conflict(new { message = "Concurrent request detected. Reload and retry." });
        }
        catch (Exception ex) when (HasPostgresSqlState(ex, "23505"))
        {
            await transaction.RollbackAsync(HttpContext.RequestAborted);
            return Conflict(new { message = "An extension request is already pending approval for this CSR. No new request was saved." });
        }
        catch (Exception ex) when (HasPostgresSqlState(ex, "40001"))
        {
            await transaction.RollbackAsync(HttpContext.RequestAborted);
            return Conflict(new { message = "The scheme period changed concurrently. Reload and retry." });
        }

        return Ok(new ExtensionRequestReceiptDto(
            extension.Id, extension.CreatedDate, extension.PreviousEndDate,
            extension.NewEndDate, extension.Reason, extension.Remarks));
    }

    private static bool HasPostgresSqlState(Exception ex, string sqlState)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
            if (current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString() == sqlState)
                return true;
        return false;
    }

    private static readonly HashSet<string> ExtensionReasons = new(StringComparer.Ordinal)
    {
        "Market Condition", "Weather / Crop Season Delay", "Stock Availability",
        "Dealer Onboarding Delay", "Other"
    };

    // Write permission: active owner OR explicitly supported tracking role within its
    // actual server-side geographic scope. This does not alter other write endpoints.
    private async Task<bool> CanRequestExtensionAsync(int csr2Id, string userId)
    {
        var employee = await _db.Set<Employeelogin>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted);
        if (employee is null) return false;

        var programIds = (_options.EligibleProgramIds ?? Array.Empty<int>())
            .Where(x => x > 0).Distinct().ToArray();
        var row = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                         join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                         where c2.Id == csr2Id && c2.Status == CSR2Status.Approved
                             && c1.Status == CSR1Status.Approved && c1.IsActive
                         select new { c1.ResponsiblePersonId, c1.ProgramId, c1.HeadquarterId })
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (row is null || (programIds.Length > 0 && !programIds.Contains(row.ProgramId))) return false;
        if (row.ResponsiblePersonId == userId) return true;
        if (!row.HeadquarterId.HasValue) return false;

        var role = ResolveTrackingRole(employee, User);
        var hqId = row.HeadquarterId.Value;
        if (role is "SMM")
            return ReadIds(employee, "HeadquartersIds", "HeadquartersId", "HQId").Contains(hqId);
        if (role is "RM" or "RMM")
        {
            var assigned = ReadIds(employee, "RegionIds", "RegionId");
            if (assigned.Count == 0) return false;
            var ids = assigned.ToArray();
            var matched = await _db.Database.SqlQuery<int>($@"
                SELECT h.""RegionId"" AS ""Value"" FROM ""Headquarters"" AS h
                WHERE h.""Id"" = {hqId}").ToListAsync(HttpContext.RequestAborted);
            return matched.Any(ids.Contains);
        }
        if (role is "SMDO" or "SMD")
        {
            var assigned = ReadIds(employee, "StateIds", "StateId");
            if (assigned.Count == 0) return false;
            var ids = assigned.ToArray();
            var matched = await _db.Database.SqlQuery<int>($@"
                SELECT r.""StateId"" AS ""Value"" FROM ""Headquarters"" AS h
                INNER JOIN ""Regions"" AS r ON r.""Id"" = h.""RegionId""
                WHERE h.""Id"" = {hqId}").ToListAsync(HttpContext.RequestAborted);
            return matched.Any(ids.Contains);
        }
        // Admin/SuperAdmin extensions require a separate explicit business approval rule.
        return false;
    }

    // Existing Sales Audit endpoint: unchanged. This endpoint intentionally
    // remains responsible-person-only to protect existing edit workflows.
    [HttpGet("eligible")]
    public async Task<IActionResult> GetEligible()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var programIds = (_options.EligibleProgramIds ?? Array.Empty<int>())
            .Where(x => x > 0).Distinct().ToArray();

        var activeUser = await _db.Set<Employeelogin>().AsNoTracking()
            .AnyAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted);
        if (!activeUser) return Forbid();

        var query = from c2 in _db.Set<CSR2>().AsNoTracking()
                    join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                    join pm in _db.Set<ProgramMaster>().AsNoTracking() on c1.ProgramId equals pm.Id
                    where c2.Status == CSR2Status.Approved
                          && c1.Status == CSR1Status.Approved && c1.IsActive
                          && c1.ResponsiblePersonId == userId
                    select new { c2, c1, pm };

        if (programIds.Length > 0)
            query = query.Where(x => programIds.Contains(x.c1.ProgramId));

        var rows = await query.OrderByDescending(x => x.c2.Id)
            .Select(x => new {
                CSR2Id = x.c2.Id,
                SanctionNumber = x.c1.SanctionNo ?? "",
                ProgramName = x.pm.Name,
                Location = x.c2.ActualLocation ?? ""
            }).ToListAsync(HttpContext.RequestAborted);

        if (rows.Count == 0) return Ok(Array.Empty<object>());

        var csr2Ids = rows.Select(x => x.CSR2Id).ToArray();
        var savedRecords = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => csr2Ids.Contains(x.CSR2Id))
            .Select(x => new {
                x.CSR2Id,
                x.SchemeFrom,
                x.SchemeTo,
                x.RequiredTurnover,
                x.ActualTurnover,
                x.DocumentStatus
            }).ToListAsync(HttpContext.RequestAborted);
        var byCsr2 = savedRecords.GroupBy(x => x.CSR2Id)
            .ToDictionary(x => x.Key, x => x.First());

        return Ok(rows.Select(x => {
            byCsr2.TryGetValue(x.CSR2Id, out var record);
            var requiredCr = record?.RequiredTurnover ?? 0m;
            var actualCr = record?.ActualTurnover ?? 0m;
            var status = record?.DocumentStatus ?? "Not Started";
            return new
            {
                x.CSR2Id,
                x.SanctionNumber,
                x.ProgramName,
                x.Location,
                SchemeFrom = record?.SchemeFrom,
                SchemeTo = record?.SchemeTo,
                RequiredTurnover = record?.RequiredTurnover,
                ActualTurnover = record?.ActualTurnover,
                DocumentStatus = status,
                ProgressPercent = requiredCr > 0m ? actualCr / requiredCr * 100m : 0m,
                RemainingTurnover = Math.Max(0m, requiredCr - actualCr),
                IsAuditCompleted = status == "Completed",
                CanSaveAchievement = status is "Plan Confirmed" or "In Progress"
            };
        }).ToList());
    }

    // Read-only Sales Tracking list. Do not use this action to authorize writes.
    // Geographic lookup follows the verified Headquarters / Regions database tables.
    [HttpGet("tracking/eligible")]
    public async Task<IActionResult> GetTrackingEligible()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var employee = await _db.Set<Employeelogin>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted);
        if (employee is null)
            return StatusCode(403, new
            {
                reason = "ACTIVE_EMPLOYEE_NOT_FOUND",
                message = "Authenticated account has no active employee login record."
            });

        // Resolve role exclusively from the active employee record / validated identity.
        // Never accept role or geography from the request query string.
        var role = ResolveTrackingRole(employee, User);
        bool isAdmin = role is "ADMIN" or "SUPERADMIN";
        string scopeField = role switch
        {
            "RM" or "RMM" => "RegionId",
            "SMM" => "HeadquartersId",
            "SMDO" or "SMD" => "StateId",
            _ => ""
        };
        if (!isAdmin && scopeField.Length == 0)
            return StatusCode(403, new
            {
                reason = "TRACKING_ROLE_NOT_ALLOWED",
                message = "The authenticated employee role is not permitted to view the tracking list.",
                role = string.IsNullOrWhiteSpace(role) ? "(missing)" : role,
                employeeRolePropertyNames = new[] { "AppRole", "Role", "UserRole", "RoleName", "EmployeeRole" }
                    .Where(n => employee.GetType().GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null)
                    .ToArray()
            });

        var assigned = scopeField switch
        {
            "RegionId" => ReadIds(employee, "RegionIds", "RegionId"),
            "HeadquartersId" => ReadIds(employee, "HeadquartersIds", "HeadquartersId", "HQId"),
            "StateId" => ReadIds(employee, "StateIds", "StateId"),
            _ => new HashSet<int>()
        };
        if (!isAdmin && assigned.Count == 0)
            return StatusCode(403, new
            {
                reason = "LOGIN_GEOGRAPHY_MISSING",
                message = $"No valid assigned {scopeField} was found for this employee role.",
                requiredScope = scopeField
            });

        var programIds = (_options.EligibleProgramIds ?? Array.Empty<int>())
            .Where(x => x > 0).Distinct().ToArray();
        var query = from c2 in _db.Set<CSR2>().AsNoTracking()
                    join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                    join pm in _db.Set<ProgramMaster>().AsNoTracking() on c1.ProgramId equals pm.Id
                    where c2.Status == CSR2Status.Approved && c1.Status == CSR1Status.Approved && c1.IsActive
                    select new { c2, c1, pm };
        if (programIds.Length > 0)
            query = query.Where(x => programIds.Contains(x.c1.ProgramId));

        // Verified geographic hierarchy from PostgreSQL schema:
        // CSR1.HeadquarterId -> Headquarters.Id -> Headquarters.RegionId
        // -> Regions.Id -> Regions.StateId.
        // Resolve permitted HQ IDs from server-side employee assignments only.
        // SQL is parameterized by EF Core interpolation; no client geography input.
        HashSet<int> permittedHeadquarterIds = new();
        if (!isAdmin)
        {
            if (scopeField == "HeadquartersId")
            {
                permittedHeadquarterIds.UnionWith(assigned);
            }
            else if (scopeField == "RegionId")
            {
                foreach (var regionId in assigned)
                {
                    var matchedHeadquarterIds = await _db.Database.SqlQuery<int>($@"
                        SELECT h.""Id"" AS ""Value""
                        FROM ""Headquarters"" AS h
                        WHERE h.""RegionId"" = {regionId}")
                        .ToListAsync(HttpContext.RequestAborted);
                    permittedHeadquarterIds.UnionWith(matchedHeadquarterIds);
                }
            }
            else if (scopeField == "StateId")
            {
                foreach (var stateId in assigned)
                {
                    var matchedHeadquarterIds = await _db.Database.SqlQuery<int>($@"
                        SELECT h.""Id"" AS ""Value""
                        FROM ""Headquarters"" AS h
                        INNER JOIN ""Regions"" AS r ON h.""RegionId"" = r.""Id""
                        WHERE r.""StateId"" = {stateId}")
                        .ToListAsync(HttpContext.RequestAborted);
                    permittedHeadquarterIds.UnionWith(matchedHeadquarterIds);
                }
            }

            // Fail closed: no configured/valid mapped headquarters means no rows.
            if (permittedHeadquarterIds.Count == 0) return Ok(Array.Empty<object>());
            var permittedIds = permittedHeadquarterIds.ToArray();
            query = query.Where(x => x.c1.HeadquarterId.HasValue && permittedIds.Contains(x.c1.HeadquarterId.Value));
        }

        // No ResponsiblePersonId restriction on read-only Tracking List.
        // Apply geography before fetching candidates so unrelated rows aren't materialized.
        var visible = await query.OrderByDescending(x => x.c2.Id)
            .Select(x => new
            {
                CSR2Id = x.c2.Id,
                SanctionNumber = x.c1.SanctionNo ?? "",
                ProgramName = x.pm.Name,
                Location = x.c2.ActualLocation ?? ""
            }).ToListAsync(HttpContext.RequestAborted);

        if (visible.Count == 0) return Ok(Array.Empty<object>());
        var trackingCsr2Ids = visible.Select(x => x.CSR2Id).ToArray();
        var records = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => trackingCsr2Ids.Contains(x.CSR2Id))
            .Select(x => new {
                x.CSR2Id,
                x.SchemeFrom,
                x.SchemeTo,
                x.RequiredTurnover,
                x.ActualTurnover,
                x.DocumentStatus
            })
            .ToListAsync(HttpContext.RequestAborted);
        var byId = records.GroupBy(x => x.CSR2Id)
            .ToDictionary(g => g.Key, g => g.First());

        return Ok(visible.Select(x =>
        {
            byId.TryGetValue(x.CSR2Id, out var record);
            var required = record?.RequiredTurnover ?? 0m;
            var actual = record?.ActualTurnover ?? 0m;
            return new
            {
                x.CSR2Id,
                x.SanctionNumber,
                x.ProgramName,
                x.Location,
                SchemeFrom = record?.SchemeFrom,
                SchemeTo = record?.SchemeTo,
                RequiredTurnover = record?.RequiredTurnover,
                ActualTurnover = record?.ActualTurnover,
                DocumentStatus = record?.DocumentStatus ?? "Not Started",
                ProgressPercent = required > 0m ? actual / required * 100m : 0m
            };
        }).ToList());
    }

    // READ-ONLY tracking detail authorization. Call this from GET overview/details/
    // dealer-mapping/sap-sales BEFORE the existing responsible-person-only Forbid.
    // Never use this helper for POST/PUT/DELETE or Sales Audit plan mutations.
    private async Task<bool> CanViewTrackingRecordAsync(int csr2Id)
    {
        if (csr2Id <= 0) return false;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var employee = await _db.Set<Employeelogin>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.UserId == userId,
                HttpContext.RequestAborted);
        if (employee is null) return false;
        var role = ResolveTrackingRole(employee, User);
        var scopeField = role switch
        {
            "RM" or "RMM" => "RegionId",
            "SMM" => "HeadquartersId",
            "SMDO" or "SMD" => "StateId",
            _ => ""
        };
        // Existing Admin/SuperAdmin access stays governed by existing endpoint rules.
        if (scopeField.Length == 0) return false;

        var assigned = scopeField switch
        {
            "RegionId" => ReadIds(employee, "RegionIds", "RegionId"),
            "HeadquartersId" => ReadIds(employee, "HeadquartersIds", "HeadquartersId", "HQId"),
            "StateId" => ReadIds(employee, "StateIds", "StateId"),
            _ => new HashSet<int>()
        };
        if (assigned.Count == 0) return false;

        var configuredProgramIds = (_options.EligibleProgramIds ?? Array.Empty<int>())
            .Where(x => x > 0).Distinct().ToArray();
        var matching = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                              join c1 in _db.Set<CSR1>().AsNoTracking()
                                  on c2.CSR1Id equals c1.Id
                              where c2.Id == csr2Id
                                  && c2.Status == CSR2Status.Approved
                                  && c1.Status == CSR1Status.Approved
                                  && c1.IsActive
                              select new { c1.ProgramId, c1.HeadquarterId })
                              .FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (matching is null || !matching.HeadquarterId.HasValue) return false;
        if (configuredProgramIds.Length > 0 &&
            !configuredProgramIds.Contains(matching.ProgramId)) return false;
        var hqId = matching.HeadquarterId.Value;

        if (scopeField == "HeadquartersId") return assigned.Contains(hqId);
        if (scopeField == "RegionId")
        {
            var regionIds = assigned.ToArray();
            foreach (var regionId in regionIds)
            {
                var found = await _db.Database.SqlQuery<int>($@"
                    SELECT h.""Id"" AS ""Value""
                    FROM ""Headquarters"" h
                    WHERE h.""Id"" = {hqId} AND h.""RegionId"" = {regionId}")
                    .AnyAsync(HttpContext.RequestAborted);
                if (found) return true;
            }
            return false;
        }
        foreach (var stateId in assigned)
        {
            var found = await _db.Database.SqlQuery<int>($@"
                SELECT h.""Id"" AS ""Value""
                FROM ""Headquarters"" h
                INNER JOIN ""Regions"" r ON r.""Id"" = h.""RegionId""
                WHERE h.""Id"" = {hqId} AND r.""StateId"" = {stateId}")
                .AnyAsync(HttpContext.RequestAborted);
            if (found) return true;
        }
        return false;
    }

    // GET-only: existing owner access plus geographically scoped Sales Tracking viewers.
    // Do not call this method from POST/PUT/PATCH/DELETE actions.
    private async Task<bool> CanReadSalesAuditRecordAsync(int csr2Id)
    {
        if (csr2Id <= 0) return false;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var active = await _db.Set<Employeelogin>().AsNoTracking()
            .AnyAsync(e => e.IsActive && e.UserId == userId, HttpContext.RequestAborted);
        if (!active) return false;

        var isOwner = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                             join c1 in _db.Set<CSR1>().AsNoTracking()
                                 on c2.CSR1Id equals c1.Id
                             where c2.Id == csr2Id && c1.ResponsiblePersonId == userId
                             select c2.Id).AnyAsync(HttpContext.RequestAborted);
        if (isOwner) return true;

        // Approved CSR2, approved/active CSR1, programme filter, role,
        // and employee geography are checked inside this existing method.
        return await CanViewTrackingRecordAsync(csr2Id);
    }

    // Compatibility with scalar RegionId/HeadquartersId and optional integer[]
    // RegionIds/HeadquartersIds on the existing Employeelogin entity.
    // ReadIds intentionally refuses unknown geography rather than returning everything.
    private static HashSet<int> ReadIds(object source, params string[] names)
    {
        var ids = new HashSet<int>();
        foreach (var name in names)
        {
            var value = source.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(source);
            if (value is null) continue;
            if (value is IEnumerable sequence && value is not string)
            {
                foreach (var entry in sequence)
                    if (int.TryParse(Convert.ToString(entry, CultureInfo.InvariantCulture), out var id) && id > 0)
                        ids.Add(id);
            }
            else if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var id) && id > 0)
                ids.Add(id);
        }
        return ids;
    }

    // Read the existing employee login role first. Different deployments expose
    // it as Role rather than AppRole; only explicitly supported roles are accepted.
    // Validated authentication claims are a fallback, never request parameters.
    private static string ResolveTrackingRole(object employee, ClaimsPrincipal principal)
    {
        static string Normalize(object? value) =>
            (Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Trim().ToUpperInvariant();

        static bool Supported(string value) => value is
            "RM" or "RMM" or "SMM" or "SMDO" or "SMD" or "ADMIN" or "SUPERADMIN";

        foreach (var propertyName in new[] { "AppRole", "Role", "UserRole", "RoleName", "EmployeeRole" })
        {
            var property = employee.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var role = Normalize(property?.GetValue(employee));
            if (Supported(role)) return role;
        }

        // This fallback assumes the API's authentication scheme validates the
        // identity and signs/protects role claims (e.g. JWT/cookie middleware).
        if (principal.Identity?.IsAuthenticated == true)
        {
            foreach (var claim in principal.Claims.Where(c =>
                c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles" ||
                c.Type == "AppRole" || c.Type == "app_role" || c.Type == "designation_role"))
            {
                var role = Normalize(claim.Value);
                if (Supported(role)) return role;
            }
        }

        return "";
    }
}
