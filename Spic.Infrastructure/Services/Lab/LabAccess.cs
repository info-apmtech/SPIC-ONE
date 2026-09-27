using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// Who the caller is in the SAS Lab portal (docs/sas-lab-portal-plan.md section 3), resolved once
/// per request from the designation RoleAccess exactly like LibraryController does
/// (RoleAccessPermissions.HasPage on an ACTIVE designation).
///
/// DESIGNATION ONLY (product decision 2026-09-27): the Admin / CorporateAdmin ROLES get no lab
/// rights by themselves; an admin reaches the lab only through a designation holding the keys
/// below, like every other user. (IsV1ReviewRole only feeds the v1 CanReview flag.)
///
///   coordinator pages                    : LabConsignments or LabAnalysis, or LabDashboard /
///                                          LabReports WITHOUT LabTestEntry (the Lab Analyst
///                                          designation also holds LabDashboard and LabReports
///                                          for its own dashboard and report pages)
///   analyst page                         : LabTestEntry; sees only the batches assigned to them
///   LabTracking (admin tracking page)    : reads every batch, writes nothing
///   anyone else                          : no lab access (403 from LabController)
/// </summary>
public sealed class LabAccess
{
    private readonly AppDbContext _db;
    private bool _loaded;

    public LabAccess(AppDbContext db) => _db = db;

    public string UserId { get; private set; } = "";
    public string UserName { get; private set; } = "";
    /// <summary>Display name (AppUsers.Name, else the user name).</summary>
    public string Name { get; private set; } = "";
    public string? DesignationName { get; private set; }
    public AppRole? Role { get; private set; }

    /// <summary>Role Admin / CorporateAdmin: the v1 review roles (SasController). Grants NO lab
    /// rights; only passed on as the v1 CanReview flag of the embedded v1 consignment detail.</summary>
    public bool IsV1ReviewRole { get; private set; }
    public bool IsFarmer => Role == AppRole.Farmer;
    /// <summary>Holds a coordinator page (designation only).</summary>
    public bool IsCoordinator { get; private set; }
    /// <summary>Holds the LabTestEntry page.</summary>
    public bool IsAnalyst { get; private set; }
    /// <summary>Holds LabTracking (read-only view of every batch).</summary>
    public bool HasTracking { get; private set; }

    /// <summary>Create batches, receive consignments, documents, assign, complete.</summary>
    public bool CanWrite => IsCoordinator;
    public bool CanRead => IsCoordinator || IsAnalyst || HasTracking;
    /// <summary>An analyst without coordinator / tracking rights: every list, stat and
    /// dashboard is limited to the batches assigned to them.</summary>
    public bool AnalystOnly => IsAnalyst && !IsCoordinator && !HasTracking;

    /// <summary>"Lab Coordinator" style label for the activity log (designation, else role).</summary>
    public string? RoleLabel => DesignationName ?? Role?.ToString();

    public async Task LoadAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (_loaded) return;
        _loaded = true;

        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        UserName = user.Identity?.Name ?? "";

        var rawRole = user.FindFirst(ClaimTypes.Role)?.Value;
        Role = Enum.TryParse<AppRole>(rawRole, true, out var role) ? role : null;
        IsV1ReviewRole = Role == AppRole.Admin || Role == AppRole.CorporateAdmin;

        if (string.IsNullOrWhiteSpace(UserId))
        {
            Name = UserName;
            return;
        }

        var profile = await _db.Users.AsNoTracking()
            .Where(u => u.Id == UserId)
            .Select(u => new { u.Name, u.UserName, u.DesignationId })
            .FirstOrDefaultAsync(ct);

        Name = !string.IsNullOrWhiteSpace(profile?.Name) ? profile!.Name!
             : !string.IsNullOrWhiteSpace(profile?.UserName) ? profile!.UserName!
             : UserName;

        string? roleAccess = null;
        if (profile?.DesignationId is int designationId && designationId > 0)
        {
            var designation = await _db.Designations.AsNoTracking()
                .Where(d => d.Id == designationId && d.IsActive)
                .Select(d => new { d.Name, d.RoleAccess })
                .FirstOrDefaultAsync(ct);

            DesignationName = designation?.Name;
            roleAccess = designation?.RoleAccess;
        }

        var dashboard = RoleAccessPermissions.HasPage(roleAccess, PagePermission.LabDashboard);
        var consignments = RoleAccessPermissions.HasPage(roleAccess, PagePermission.LabConsignments);
        var analysis = RoleAccessPermissions.HasPage(roleAccess, PagePermission.LabAnalysis);
        var reports = RoleAccessPermissions.HasPage(roleAccess, PagePermission.LabReports);
        var testEntry = RoleAccessPermissions.HasPage(roleAccess, PagePermission.LabTestEntry);

        IsAnalyst = testEntry;
        HasTracking = RoleAccessPermissions.HasPage(roleAccess, PagePermission.LabTracking);
        IsCoordinator = consignments || analysis || ((dashboard || reports) && !testEntry);
    }

    /// <summary>Designation ids whose RoleAccess grants LabTestEntry (the analyst pool).</summary>
    public static async Task<List<int>> AnalystDesignationIdsAsync(AppDbContext db, CancellationToken ct = default)
    {
        var designations = await db.Designations.AsNoTracking()
            .Where(d => d.IsActive && d.RoleAccess != null && d.RoleAccess.Contains("LabTestEntry"))
            .Select(d => new { d.Id, d.RoleAccess })
            .ToListAsync(ct);

        return designations
            .Where(d => RoleAccessPermissions.HasPage(d.RoleAccess, PagePermission.LabTestEntry))
            .Select(d => d.Id)
            .ToList();
    }
}
