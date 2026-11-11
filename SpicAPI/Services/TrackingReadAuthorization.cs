using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers;

/// <summary>
/// Read-only permission check for an individual approved CSR2 record.
/// Do not use to authorize data mutations. No DB migration required.
/// </summary>
public static class TrackingReadAuthorization
{
    public static async Task<bool> CanViewAsync(
        AppDbContext db, ClaimsPrincipal principal,
        SalesAuditEligibilityOptions options, int csr2Id,
        CancellationToken cancellationToken = default)
    {
        if (csr2Id <= 0) return false;
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var employee = await db.Set<Employeelogin>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (employee is null) return false;

        var role = ResolveRole(employee, principal);
        var geography = role switch
        {
            "RM" or "RMM" => "Region",
            "SMM" => "HQ",
            "SMDO" or "SMD" => "State",
            _ => ""
        };
        if (geography.Length == 0) return false;
        var allowedIds = geography switch
        {
            "Region" => ReadIds(employee, "RegionIds", "RegionId"),
            "HQ" => ReadIds(employee, "HeadquartersIds", "HeadquartersId", "HQId"),
            "State" => ReadIds(employee, "StateIds", "StateId"),
            _ => new HashSet<int>()
        };
        if (allowedIds.Count == 0) return false;
        var eligiblePrograms = (options.EligibleProgramIds ?? Array.Empty<int>())
            .Where(x => x > 0).Distinct().ToArray();

        var selected = await (from c2 in db.Set<CSR2>().AsNoTracking()
                              join c1 in db.Set<CSR1>().AsNoTracking()
                                  on c2.CSR1Id equals c1.Id
                              where c2.Id == csr2Id && c2.Status == CSR2Status.Approved
                                  && c1.Status == CSR1Status.Approved && c1.IsActive
                              select new { c1.ProgramId, c1.HeadquarterId })
            .FirstOrDefaultAsync(cancellationToken);
        if (selected is null || !selected.HeadquarterId.HasValue) return false;
        if (eligiblePrograms.Length > 0 && !eligiblePrograms.Contains(selected.ProgramId))
            return false;
        var hqId = selected.HeadquarterId.Value;
        if (geography == "HQ") return allowedIds.Contains(hqId);
        if (geography == "Region")
        {
            foreach (var regionId in allowedIds)
            {
                var exists = await db.Database.SqlQuery<int>($@"
                    SELECT h.""Id"" AS ""Value""
                    FROM ""Headquarters"" h
                    WHERE h.""Id"" = {hqId} AND h.""RegionId"" = {regionId}")
                    .AnyAsync(cancellationToken);
                if (exists) return true;
            }
            return false;
        }
        foreach (var stateId in allowedIds)
        {
            var exists = await db.Database.SqlQuery<int>($@"
                SELECT h.""Id"" AS ""Value""
                FROM ""Headquarters"" h
                JOIN ""Regions"" r ON r.""Id"" = h.""RegionId""
                WHERE h.""Id"" = {hqId} AND r.""StateId"" = {stateId}")
                .AnyAsync(cancellationToken);
            if (exists) return true;
        }
        return false;
    }

    private static HashSet<int> ReadIds(object source, params string[] names)
    {
        var result = new HashSet<int>();
        foreach (var name in names)
        {
            var value = source.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(source);
            if (value is null) continue;
            if (value is IEnumerable entries && value is not string)
            {
                foreach (var entry in entries)
                {
                    if (int.TryParse(Convert.ToString(entry, CultureInfo.InvariantCulture),
                        out var id) && id > 0) result.Add(id);
                }
            }
            else if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                out var id) && id > 0) result.Add(id);
        }
        return result;
    }

    private static string ResolveRole(object employee, ClaimsPrincipal principal)
    {
        static string Normalize(object? value) =>
            (Convert.ToString(value, CultureInfo.InvariantCulture) ?? "")
                .Trim().ToUpperInvariant();
        static bool Supported(string role) => role is
            "RM" or "RMM" or "SMM" or "SMDO" or "SMD";
        foreach (var propertyName in new[] { "AppRole", "Role", "UserRole", "RoleName", "EmployeeRole" })
        {
            var value = employee.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(employee);
            var role = Normalize(value);
            if (Supported(role)) return role;
        }
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
