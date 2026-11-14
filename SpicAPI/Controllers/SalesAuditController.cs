using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Collections;
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
