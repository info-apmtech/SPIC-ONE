using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SpicAPI.Services;

namespace SpicAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LogisticsReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LogisticsReportController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Warehouse Report:
        /// - States: from saved Warehouse records
        /// - As Per SAP: overall COUNT(PVTMasters) — NOT state-wise
        /// - Pending/Completed: from saved Warehouse approval status fields, state-wise
        /// </summary>
        [HttpGet("warehouse")]
        public async Task<IActionResult> GetWarehouseReport()
        {
            var role = CurrentRole();

            var warehouses = await (await ApplyWarehouseRoleFilter(
                    _context.Warehouses
                        .Where(w => w.StateId.HasValue),
                    role))
                .Select(w => new
                {
                    w.Id,

                    // StateId is nullable in entity,
                    // but report only includes records having a State.
                    StateId = w.StateId.Value,

                    w.IsActive,
                    w.IsSubmittedForReview,
                    w.RMApproved,
                    w.SMApproved,
                    w.AVPApproved
                })
                .ToListAsync();

            var sapTotal = await _context.PVTMasters
                .CountAsync(p => p.IsActive);

            var rows = warehouses
                .GroupBy(w => w.StateId)
                .Select(g => new LogisticsReportRowDto
                {
                    StateId = g.Key,
                    TotalCount = g.Count(),
                    Active = g.Count(w => w.IsActive),
                    Inactive = g.Count(w => !w.IsActive),
                    AsPerSap = 0,
                    PendingWithMo = 0,

                    PendingRm = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == null &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingRmActive = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == null &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingRmInactive = g.Count(w =>
                        !w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == null &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingRmTotal = g.Count(w =>
                        w.IsSubmittedForReview &&
                        w.RMApproved == null &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingSmm = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingSmmActive = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingSmmInactive = g.Count(w =>
                        !w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingSmmTotal = g.Count(w =>
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),

                    PendingWithAvp = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == null),

                    PendingWithAvpActive = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == null),

                    PendingWithAvpInactive = g.Count(w =>
                        !w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == null),

                    PendingWithAvpTotal = g.Count(w =>
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == null),

                    Completed = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == true),

                    CompletedActive = g.Count(w =>
                        w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == true),

                    CompletedInactive = g.Count(w =>
                        !w.IsActive &&
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == true),

                    CompletedTotal = g.Count(w =>
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == true)
                })
                .OrderBy(x => x.StateId)
                .ToList();

            await ResolveStateNames(rows);

            return Ok(new LogisticsReportResponseDto
            {
                Rows = rows,
                Total = BuildTotal(rows, sapTotal)
            });
        }

        /// <summary>
        /// Rakepoint Report:
        /// - States: from saved Rakepoint records
        /// - As Per SAP: overall COUNT(RakePointMasters) — NOT state-wise
        /// - Pending/Completed: from saved Rakepoint approval status fields, state-wise
        /// </summary>
        [HttpGet("rakepoint")]
        public async Task<IActionResult> GetRakepointReport()
        {
            var role = CurrentRole();

            var rakepoints = await (await ApplyRakepointRoleFilter(
                    _context.RackPoints
                        .Where(r => r.StateId.HasValue),
                    role))
                .Select(r => new
                {
                    r.Id,

                    // StateId is nullable in entity,
                    // but report only includes records having a State.
                    StateId = r.StateId.Value,

                    r.IsActive,
                    r.IsSubmittedForReview,
                    r.RMApproved,
                    r.SMApproved,
                    r.AVPApproved
                })
                .ToListAsync();

            var sapTotal = await _context.RakePointMasters
                .CountAsync(rp => rp.IsActive);

            var rows = rakepoints
                .GroupBy(r => r.StateId)
                .Select(g => new LogisticsReportRowDto
                {
                    StateId = g.Key,
                    TotalCount = g.Count(),
                    Active = g.Count(r => r.IsActive),
                    Inactive = g.Count(r => !r.IsActive),
                    AsPerSap = 0,
                    PendingWithMo = 0,

                    PendingRm = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == null &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingRmActive = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == null &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingRmInactive = g.Count(r =>
                        !r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == null &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingRmTotal = g.Count(r =>
                        r.IsSubmittedForReview &&
                        r.RMApproved == null &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingSmm = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingSmmActive = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingSmmInactive = g.Count(r =>
                        !r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingSmmTotal = g.Count(r =>
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),

                    PendingWithAvp = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == null),

                    PendingWithAvpActive = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == null),

                    PendingWithAvpInactive = g.Count(r =>
                        !r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == null),

                    PendingWithAvpTotal = g.Count(r =>
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == null),

                    Completed = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == true),

                    CompletedActive = g.Count(r =>
                        r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == true),

                    CompletedInactive = g.Count(r =>
                        !r.IsActive &&
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == true),

                    CompletedTotal = g.Count(r =>
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == true)
                })
                .OrderBy(x => x.StateId)
                .ToList();

            await ResolveStateNames(rows);

            return Ok(new LogisticsReportResponseDto
            {
                Rows = rows,
                Total = BuildTotal(rows, sapTotal)
            });
        }

        private async Task ResolveStateNames(List<LogisticsReportRowDto> rows)
        {
            if (rows.Count == 0) return;

            var stateIds = rows.Select(r => r.StateId).ToList();
            var names = await _context.States
                .Where(s => stateIds.Contains(s.Id))
                .Select(s => new { s.Id, s.StateName })
                .ToDictionaryAsync(x => x.Id, x => x.StateName);

            foreach (var row in rows)
            {
                row.State = names.TryGetValue(row.StateId, out var name)
                    ? name
                    : $"State {row.StateId}";
            }
        }

        private static LogisticsReportTotalDto BuildTotal(List<LogisticsReportRowDto> rows, int sapTotal)
        {
            var totalRm = rows.Sum(r => r.PendingRm);
            var totalSmm = rows.Sum(r => r.PendingSmm);
            var totalAvp = rows.Sum(r => r.PendingWithAvp);
            var totalCompleted = rows.Sum(r => r.Completed);

            return new LogisticsReportTotalDto
            {
                TotalCount = rows.Sum(r => r.TotalCount),
                Active = rows.Sum(r => r.Active),
                Inactive = rows.Sum(r => r.Inactive),
                AsPerSap = sapTotal,
                PendingWithMo = Math.Max(0, sapTotal - (totalRm + totalSmm + totalAvp + totalCompleted)),
                PendingRm = totalRm,
                PendingRmActive = rows.Sum(r => r.PendingRmActive),
                PendingRmInactive = rows.Sum(r => r.PendingRmInactive),
                PendingRmTotal = rows.Sum(r => r.PendingRmActive) + rows.Sum(r => r.PendingRmInactive),
                PendingSmm = totalSmm,
                PendingSmmActive = rows.Sum(r => r.PendingSmmActive),
                PendingSmmInactive = rows.Sum(r => r.PendingSmmInactive),
                PendingSmmTotal = rows.Sum(r => r.PendingSmmActive) + rows.Sum(r => r.PendingSmmInactive),
                PendingWithAvp = totalAvp,
                PendingWithAvpActive = rows.Sum(r => r.PendingWithAvpActive),
                PendingWithAvpInactive = rows.Sum(r => r.PendingWithAvpInactive),
                PendingWithAvpTotal = rows.Sum(r => r.PendingWithAvpActive) + rows.Sum(r => r.PendingWithAvpInactive),
                Completed = totalCompleted,
                CompletedActive = rows.Sum(r => r.CompletedActive),
                CompletedInactive = rows.Sum(r => r.CompletedInactive),
                CompletedTotal = rows.Sum(r => r.CompletedActive) + rows.Sum(r => r.CompletedInactive)
            };
        }

        private async Task<IQueryable<SPIC.Core.Entities.Warehouse>> ApplyWarehouseRoleFilter(
            IQueryable<SPIC.Core.Entities.Warehouse> query, string role)
        {
            if (IsAvpRole(role))
            {
                // AVP sees only Warehouses belonging to the Zone assigned to the logged-in AVP.
                var zoneId = CurrentZoneId();
                if (!zoneId.HasValue || zoneId.Value <= 0)
                    return query.Where(_ => false);

                var stateIdsInZone = await _context.States
                    .AsNoTracking()
                    .Where(s => s.ZoneId == zoneId.Value)
                    .Select(s => s.Id)
                    .ToListAsync();

                return query.Where(w => w.BasicStateId.HasValue && stateIdsInZone.Contains(w.BasicStateId.Value));
            }

            if (IsUnrestrictedRole(role))
                return query;

            // SpecialAdmin: multi-location scope. Data is restricted to every state
            // assigned to this user (from the JWT). All other roles are untouched.
            if (SpecialAdminScope.IsSpecialAdmin(User))
            {
                var assignedStates = SpecialAdminScope.AssignedStateIds(User);
                if (assignedStates.Count == 0)
                    return query.Where(_ => false);
                return query.Where(w => w.BasicStateId.HasValue && assignedStates.Contains(w.BasicStateId.Value));
            }

            if (IsStateRole(role))
            {
                var stateId = CurrentStateId();
                if (!stateId.HasValue || stateId.Value <= 0)
                    return query.Where(_ => false);
                return query.Where(w => w.BasicStateId == stateId.Value);
            }

            if (IsRegionRole(role))
            {
                var regionId = CurrentRegionId();
                if (!regionId.HasValue || regionId.Value <= 0)
                    return query.Where(_ => false);
                return query.Where(w => w.RegionId == regionId.Value);
            }

            if (IsCreatorRole(role))
            {
                var userId = CurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return query.Where(_ => false);
                return query.Where(w => w.CreatedBy == userId);
            }

            return query.Where(_ => false);
        }

        private async Task<IQueryable<SPIC.Core.Entities.RackPoint>> ApplyRakepointRoleFilter(
            IQueryable<SPIC.Core.Entities.RackPoint> query, string role)
        {
            if (IsAvpRole(role))
            {
                // AVP sees only Rake Points belonging to the Zone assigned to the logged-in AVP.
                var zoneId = CurrentZoneId();
                if (!zoneId.HasValue || zoneId.Value <= 0)
                    return query.Where(_ => false);

                var stateIdsInZone = await _context.States
                    .AsNoTracking()
                    .Where(s => s.ZoneId == zoneId.Value)
                    .Select(s => s.Id)
                    .ToListAsync();

                return query.Where(r => r.BasicStateId.HasValue && stateIdsInZone.Contains(r.BasicStateId.Value));
            }

            if (IsUnrestrictedRole(role))
                return query;

            // SpecialAdmin: multi-location scope. Data is restricted to every state
            // assigned to this user (from the JWT). All other roles are untouched.
            if (SpecialAdminScope.IsSpecialAdmin(User))
            {
                var assignedStates = SpecialAdminScope.AssignedStateIds(User);
                if (assignedStates.Count == 0)
                    return query.Where(_ => false);
                return query.Where(r => r.BasicStateId.HasValue && assignedStates.Contains(r.BasicStateId.Value));
            }

            if (IsStateRole(role))
            {
                var stateId = CurrentStateId();
                if (!stateId.HasValue || stateId.Value <= 0)
                    return query.Where(_ => false);
                return query.Where(r => r.BasicStateId == stateId.Value);
            }

            if (IsRegionRole(role))
            {
                var regionId = CurrentRegionId();
                if (!regionId.HasValue || regionId.Value <= 0)
                    return query.Where(_ => false);
                return query.Where(r => r.RegionId == regionId.Value);
            }

            if (IsCreatorRole(role))
            {
                var userId = CurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return query.Where(_ => false);
                return query.Where(r => r.CreatedBy == userId);
            }

            return query.Where(_ => false);
        }

        private string CurrentRole() =>
            User.FindFirst(ClaimTypes.Role)?.Value ??
            User.FindFirst("Role")?.Value ??
            string.Empty;

        private string? CurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value ??
            User.FindFirst("spic:user_id")?.Value;

        private int? CurrentStateId() => ReadIntClaim("spic:state_id", "StateId");
        private int? CurrentRegionId() => ReadIntClaim("spic:region_id", "RegionId");
        private int? CurrentZoneId() => ReadIntClaim("spic:zone_id", "ZoneId");

        private int? ReadIntClaim(params string[] names)
        {
            foreach (var name in names)
            {
                var value = User.FindFirst(name)?.Value;
                if (int.TryParse(value, out var id) && id > 0)
                    return id;
            }
            return null;
        }

        private static bool IsCreatorRole(string role) =>
            role.Equals("MO", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("MDO", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("JMDO", StringComparison.OrdinalIgnoreCase);

        private static bool IsRegionRole(string role) =>
            role.Equals("RM", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("RMD", StringComparison.OrdinalIgnoreCase);

        private static bool IsStateRole(string role) =>
            role.Equals("SMM", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("SMD", StringComparison.OrdinalIgnoreCase);

        private static bool IsAvpRole(string role) =>
            role.Equals("AVP", StringComparison.OrdinalIgnoreCase);

        private static bool IsUnrestrictedRole(string role) =>
            role.Equals("AVP", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("CorporateAdmin", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Director", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class LogisticsReportRowDto
    {
        public int StateId { get; set; }
        public string State { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int AsPerSap { get; set; }
        public int PendingWithMo { get; set; }
        public int PendingRm { get; set; }
        public int PendingRmActive { get; set; }
        public int PendingRmInactive { get; set; }
        public int PendingRmTotal { get; set; }
        public int PendingSmm { get; set; }
        public int PendingSmmActive { get; set; }
        public int PendingSmmInactive { get; set; }
        public int PendingSmmTotal { get; set; }
        public int PendingWithAvp { get; set; }
        public int PendingWithAvpActive { get; set; }
        public int PendingWithAvpInactive { get; set; }
        public int PendingWithAvpTotal { get; set; }
        public int Completed { get; set; }
        public int CompletedActive { get; set; }
        public int CompletedInactive { get; set; }
        public int CompletedTotal { get; set; }
    }

    public sealed class LogisticsReportTotalDto
    {
        public int TotalCount { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int AsPerSap { get; set; }
        public int PendingWithMo { get; set; }
        public int PendingRm { get; set; }
        public int PendingRmActive { get; set; }
        public int PendingRmInactive { get; set; }
        public int PendingRmTotal { get; set; }
        public int PendingSmm { get; set; }
        public int PendingSmmActive { get; set; }
        public int PendingSmmInactive { get; set; }
        public int PendingSmmTotal { get; set; }
        public int PendingWithAvp { get; set; }
        public int PendingWithAvpActive { get; set; }
        public int PendingWithAvpInactive { get; set; }
        public int PendingWithAvpTotal { get; set; }
        public int Completed { get; set; }
        public int CompletedActive { get; set; }
        public int CompletedInactive { get; set; }
        public int CompletedTotal { get; set; }
    }

    public sealed class LogisticsReportResponseDto
    {
        public List<LogisticsReportRowDto> Rows { get; set; } = new();
        public LogisticsReportTotalDto Total { get; set; } = new();
    }
}
