using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;

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

            var warehouses = await ApplyWarehouseRoleFilter(
                    _context.Warehouses.Where(w => w.IsActive), role)
                .Select(w => new { w.Id, w.StateId,
                    w.IsSubmittedForReview, w.RMApproved, w.SMApproved, w.AVPApproved })
                .ToListAsync();

            var sapTotal = await _context.PVTMasters.CountAsync(p => p.IsActive);

            var rows = warehouses
                .GroupBy(w => w.StateId)
                .Select(g => new LogisticsReportRowDto
                {
                    StateId = g.Key,
                    TotalCount = g.Count(),
                    AsPerSap = 0,
                    PendingWithMo = 0,
                    PendingRm = g.Count(w =>
                        w.IsSubmittedForReview &&
                        w.RMApproved == null &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),
                    PendingSmm = g.Count(w =>
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == null &&
                        w.AVPApproved == null),
                    PendingWithAvp = g.Count(w =>
                        w.IsSubmittedForReview &&
                        w.RMApproved == true &&
                        w.SMApproved == true &&
                        w.AVPApproved == null),
                    Completed = g.Count(w =>
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

            var rakepoints = await ApplyRakepointRoleFilter(
                    _context.RackPoints.Where(r => r.IsActive), role)
                .Select(r => new { r.Id, r.StateId,
                    r.IsSubmittedForReview, r.RMApproved, r.SMApproved, r.AVPApproved })
                .ToListAsync();

            var sapTotal = await _context.RakePointMasters.CountAsync(rp => rp.IsActive);

            var rows = rakepoints
                .GroupBy(r => r.StateId)
                .Select(g => new LogisticsReportRowDto
                {
                    StateId = g.Key,
                    TotalCount = g.Count(),
                    AsPerSap = 0,
                    PendingWithMo = 0,
                    PendingRm = g.Count(r =>
                        r.IsSubmittedForReview &&
                        r.RMApproved == null &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),
                    PendingSmm = g.Count(r =>
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == null &&
                        r.AVPApproved == null),
                    PendingWithAvp = g.Count(r =>
                        r.IsSubmittedForReview &&
                        r.RMApproved == true &&
                        r.SMApproved == true &&
                        r.AVPApproved == null),
                    Completed = g.Count(r =>
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
                AsPerSap = sapTotal,
                PendingWithMo = Math.Max(0, sapTotal - (totalRm + totalSmm + totalAvp + totalCompleted)),
                PendingRm = totalRm,
                PendingSmm = totalSmm,
                PendingWithAvp = totalAvp,
                Completed = totalCompleted
            };
        }

        private IQueryable<SPIC.Core.Entities.Warehouse> ApplyWarehouseRoleFilter(
            IQueryable<SPIC.Core.Entities.Warehouse> query, string role)
        {
            if (IsUnrestrictedRole(role))
                return query;

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

        private IQueryable<SPIC.Core.Entities.RackPoint> ApplyRakepointRoleFilter(
            IQueryable<SPIC.Core.Entities.RackPoint> query, string role)
        {
            if (IsUnrestrictedRole(role))
                return query;

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
        public int AsPerSap { get; set; }
        public int PendingWithMo { get; set; }
        public int PendingRm { get; set; }
        public int PendingSmm { get; set; }
        public int PendingWithAvp { get; set; }
        public int Completed { get; set; }
    }

    public sealed class LogisticsReportTotalDto
    {
        public int TotalCount { get; set; }
        public int AsPerSap { get; set; }
        public int PendingWithMo { get; set; }
        public int PendingRm { get; set; }
        public int PendingSmm { get; set; }
        public int PendingWithAvp { get; set; }
        public int Completed { get; set; }
    }

    public sealed class LogisticsReportResponseDto
    {
        public List<LogisticsReportRowDto> Rows { get; set; } = new();
        public LogisticsReportTotalDto Total { get; set; } = new();
    }
}
