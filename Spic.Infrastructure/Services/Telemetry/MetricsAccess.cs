using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Who may read api/Metrics (plan section 4), resolved once per request. The normal CanAccess rule,
/// not the designation-only rule of the lab: role SuperAdmin / Admin / CorporateAdmin, or an ACTIVE
/// designation whose RoleAccess holds the "Metrics" page (RoleAccessPermissions.HasPage, exactly like
/// LibraryController / LabAccess read it).
/// </summary>
public sealed class MetricsAccess
{
    private readonly AppDbContext _db;
    private bool _loaded;

    public MetricsAccess(AppDbContext db) => _db = db;

    public string UserId { get; private set; } = "";
    public string UserName { get; private set; } = "";
    public AppRole? Role { get; private set; }
    public string? DesignationName { get; private set; }

    /// <summary>SuperAdmin / Admin / CorporateAdmin role (no designation needed).</summary>
    public bool IsAdminRole { get; private set; }
    /// <summary>Designation grants the Metrics page.</summary>
    public bool HasMetricsPage { get; private set; }

    public bool CanView => IsAdminRole || HasMetricsPage;

    public async Task LoadAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (_loaded) return;
        _loaded = true;

        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        UserName = user.Identity?.Name ?? "";

        var rawRole = user.FindFirst(ClaimTypes.Role)?.Value;
        Role = Enum.TryParse<AppRole>(rawRole, true, out var role) ? role : null;
        IsAdminRole = Role is AppRole.SuperAdmin or AppRole.Admin or AppRole.CorporateAdmin;

        if (string.IsNullOrWhiteSpace(UserId)) return;

        var designationId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == UserId)
            .Select(u => u.DesignationId)
            .FirstOrDefaultAsync(ct);
        if (designationId is not int id || id <= 0) return;

        var designation = await _db.Designations.AsNoTracking()
            .Where(d => d.Id == id && d.IsActive)
            .Select(d => new { d.Name, d.RoleAccess })
            .FirstOrDefaultAsync(ct);

        DesignationName = designation?.Name;
        HasMetricsPage = RoleAccessPermissions.HasPage(designation?.RoleAccess, PagePermission.Metrics);
    }
}
