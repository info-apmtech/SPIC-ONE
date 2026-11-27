using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Staff side of the farmer call-back requests (docs/farmer-portal.md, Task 2; page /farmer-requests).
    ///
    ///   GET api/FarmerRequests?status=Open|Contacted|Closed&page&pageSize
    ///       JMDO / MDO / RM / RMD: the requests addressed to the caller. Admin / SuperAdmin: every
    ///       request, read-only. Counts per status come with every page.
    ///   PUT api/FarmerRequests/{id}  { status, note }
    ///       Only the addressed staff member (404 for anyone else, so ids are not probed).
    ///
    /// Staff see the farmer's name, phone, village and district: the farmer asked to be called.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FarmerRequestsController : ControllerBase
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly AppDbContext _db;

        public FarmerRequestsController(AppDbContext db) => _db = db;

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private AppRole? CallerRole =>
            Enum.TryParse<AppRole>(User.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] FarmerContactRequestStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var role = CallerRole;
            if (!FarmerContactRequestRules.CanUseStaffPage(role)) return Forbid();

            var admin = FarmerContactRequestRules.IsAdminViewer(role);
            var scoped = _db.FarmerContactRequests.AsNoTracking();
            if (!admin) scoped = scoped.Where(r => r.StaffUserId == userId);

            var counts = await scoped.GroupBy(r => r.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            var filtered = status is { } s ? scoped.Where(r => r.Status == s) : scoped;
            var total = await filtered.CountAsync(ct);
            var pageNo = Math.Max(1, page ?? 1);
            var size = pageSize is null or < 1 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);

            // Open first (oldest waiting first), then the rest newest first.
            var rows = await filtered
                .OrderBy(r => r.Status == FarmerContactRequestStatus.Open ? 0 : 1)
                .ThenBy(r => r.Status == FarmerContactRequestStatus.Open ? (DateTime?)r.CreatedAtUtc : null)
                .ThenByDescending(r => r.UpdatedAtUtc)
                .ThenByDescending(r => r.Id)
                .Skip((pageNo - 1) * size)
                .Take(size)
                .ToListAsync(ct);

            return Ok(new StaffFarmerRequestListDto
            {
                AdminView = admin,
                Open = counts.GetValueOrDefault(FarmerContactRequestStatus.Open),
                Contacted = counts.GetValueOrDefault(FarmerContactRequestStatus.Contacted),
                Closed = counts.GetValueOrDefault(FarmerContactRequestStatus.Closed),
                Page = pageNo,
                PageSize = size,
                Total = total,
                Items = await ToStaffDtosAsync(rows, userId, admin, ct)
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] FarmerRequestUpdateDto? dto, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var role = CallerRole;
            if (role is not { } r || !FarmerTeamRules.TeamRoles.Contains(r))
                return Forbid();

            if (dto is null) return BadRequest(new FarmerProfileErrorDto { Message = "Choose a status." });
            var errors = FarmerContactRequestRules.ValidateUpdate(dto.Status, dto.Note);
            if (errors.Count > 0)
                return BadRequest(new FarmerProfileErrorDto { Message = "Please check the highlighted fields.", Errors = errors });

            var request = await _db.FarmerContactRequests.FirstOrDefaultAsync(x => x.Id == id && x.StaffUserId == userId, ct);
            if (request is null) return NotFound(new FarmerProfileErrorDto { Message = "This request was not found." });

            var note = FarmerContactRequestRules.CleanText(dto.Note);
            request.Status = dto.Status!.Value;
            request.StaffNote = note.Length == 0 ? null : note;
            request.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok((await ToStaffDtosAsync(new List<FarmerContactRequest> { request }, userId, false, ct))[0]);
        }

        private async Task<List<StaffFarmerRequestDto>> ToStaffDtosAsync(
            List<FarmerContactRequest> rows, string callerId, bool admin, CancellationToken ct)
        {
            if (rows.Count == 0) return new List<StaffFarmerRequestDto>();

            var farmerIds = rows.Select(r => r.FarmerUserId).Distinct().ToList();
            var profiles = await _db.FarmerProfiles.AsNoTracking()
                .Where(p => farmerIds.Contains(p.UserId))
                .Select(p => new { p.UserId, p.Name, p.Phone, p.Village, p.DistrictId, p.StateId })
                .ToDictionaryAsync(p => p.UserId, ct);
            var districtIds = profiles.Values.Select(p => p.DistrictId).Distinct().ToList();
            var stateIds = profiles.Values.Select(p => p.StateId).Distinct().ToList();
            var districtNames = await _db.Districts.AsNoTracking().Where(d => districtIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.DistrictName, ct);
            var stateNames = await _db.States.AsNoTracking().Where(s => stateIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.StateName, ct);

            // Addressed staff names (employee record name, else the account name).
            var staffIds = rows.Select(r => r.StaffUserId).Distinct().ToList();
            var staffRows = await (
                from user in _db.Users.AsNoTracking()
                where staffIds.Contains(user.Id)
                join login in _db.Employeelogins.AsNoTracking() on user.Id equals login.UserId into loginJoin
                from login in loginJoin.DefaultIfEmpty()
                join info in _db.EmployeeInformation.AsNoTracking() on (login != null ? login.EmployeeInformationID : -1) equals info.Id into infoJoin
                from info in infoJoin.DefaultIfEmpty()
                select new { user.Id, UserName = user.Name, InfoName = info != null ? info.Name : null }).ToListAsync(ct);
            var staffNames = staffRows.GroupBy(s => s.Id).ToDictionary(g => g.Key,
                g => (g.Select(s => s.InfoName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.First().UserName ?? "SPIC officer").Trim());

            return rows.Select(r =>
            {
                profiles.TryGetValue(r.FarmerUserId, out var p);
                return new StaffFarmerRequestDto
                {
                    Id = r.Id,
                    FarmerName = p?.Name ?? "Farmer",
                    FarmerPhone = p?.Phone,
                    Village = p?.Village,
                    DistrictName = p is not null && districtNames.TryGetValue(p.DistrictId, out var dn) ? dn : null,
                    StateName = p is not null && stateNames.TryGetValue(p.StateId, out var sn) ? sn : null,
                    Topic = r.Topic,
                    Message = r.Message,
                    PreferredTime = r.PreferredTime,
                    Status = r.Status,
                    StaffNote = r.StaffNote,
                    StaffName = staffNames.TryGetValue(r.StaffUserId, out var name) ? name : "SPIC officer",
                    StaffRole = r.StaffRole,
                    StaffRoleLabel = FarmerTeamRules.RoleLabel(r.StaffRole),
                    CanUpdate = !admin && r.StaffUserId == callerId,
                    CreatedAtUtc = DateTime.SpecifyKind(r.CreatedAtUtc, DateTimeKind.Utc),
                    UpdatedAtUtc = DateTime.SpecifyKind(r.UpdatedAtUtc, DateTimeKind.Utc)
                };
            }).ToList();
        }
    }
}
