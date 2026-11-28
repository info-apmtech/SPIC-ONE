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
    /// Farmer portal Task 2 (docs/farmer-portal.md): dealers near the farmer, the farmer's SPIC team
    /// (JMDO / MDO / RM / RMD) and call-back requests to them. Farmer role only; everything is scoped to
    /// the caller's own profile and requests.
    ///
    ///   GET  api/Farmer/dealers                       -> FarmerDealerListDto (Nearby -> District -> State)
    ///   GET  api/Farmer/contacts                      -> FarmerTeamDto
    ///   POST api/Farmer/contacts/{contactId}/request  -> FarmerContactRequestDto (max 3 Open per staff member)
    ///   GET  api/Farmer/contacts/requests             -> the farmer's own requests, newest first
    ///
    /// Dealers: only <see cref="FarmerDealerRules.ListableForFarmers"/> rows and only public business
    /// fields (firm, address, official / WhatsApp number, map link, GPS); never PAN, bank, GST or owner
    /// data. Team: <see cref="FarmerTeamRules.Resolve"/>. Staff phone numbers only when
    /// FarmerPortal:ShowStaffPhoneNumbers is true (default false). GPS is never logged.
    /// </summary>
    [FarmerAccess]
    [Authorize(Roles = nameof(AppRole.Farmer))]
    [ApiController]
    [Route("api/Farmer")]
    public class FarmerConnectController : ControllerBase
    {
        private const string NoProfileMessage = "Please finish your profile first.";
        private const int MaxRequestsListed = 100;

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public FarmerConnectController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool ShowStaffPhones => bool.TryParse(_config["FarmerPortal:ShowStaffPhoneNumbers"], out var on) && on;

        // ------------------------------------------------------------------ dealers

        [HttpGet("dealers")]
        public async Task<IActionResult> Dealers(
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] int? radiusKm,
            [FromQuery] int? districtId,
            [FromQuery] string? q,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profile = await _db.FarmerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null) return NotFound(new FarmerAuthError { Message = NoProfileMessage });

            var radius = FarmerDealerRules.ClampRadius(radiusKm);
            var (pageNo, size) = FarmerDealerRules.ClampPaging(page, pageSize);
            var search = FarmerDealerRules.CleanSearch(q);
            var gps = FarmerGeo.IsValid(lat, lng);

            // The district defaults to the farmer's own; another district must exist (its state is used
            // for the state fallback).
            var district = districtId is > 0 ? districtId.Value : profile.DistrictId;
            var districtRow = await _db.Districts.AsNoTracking()
                .Where(d => d.Id == district)
                .Select(d => new { d.Id, d.DistrictName, d.StateId })
                .FirstOrDefaultAsync(ct);
            if (districtRow is null && districtId is > 0)
                return BadRequest(new FarmerAuthError { Message = "Please choose a district from the list." });
            var stateId = districtRow?.StateId ?? profile.StateId;

            var listable = _db.DealerRegistrations.AsNoTracking().Where(FarmerDealerRules.ListableForFarmers);
            if (search is not null)
            {
                var pattern = "%" + EscapeLike(search) + "%";
                listable = listable.Where(d =>
                    EF.Functions.ILike(d.FirmName, pattern)
                    || EF.Functions.ILike(d.Village, pattern)
                    || (d.SubVillage != null && EF.Functions.ILike(d.SubVillage, pattern)));
            }

            // 1. Within the radius of the GPS point (box pre-filter in SQL, exact haversine here).
            var hits = new List<FarmerDealerHit>();
            if (gps)
            {
                var box = FarmerGeo.BoundingBox(lat!.Value, lng!.Value, radius);
                var inBox = await Points(listable.Where(d =>
                    d.Latitude != null && d.Longitude != null
                    && d.Latitude >= box.MinLat && d.Latitude <= box.MaxLat
                    && d.Longitude >= box.MinLng && d.Longitude <= box.MaxLng)).ToListAsync(ct);
                hits = FarmerDealerRules.Nearby(inBox, lat.Value, lng.Value, radius);
            }
            var nearbyCount = hits.Count;

            // 2. The district, then 3. the state (address state, else the business state); the same
            // chain as FarmerDealerRules.Select, run against the database one step at a time.
            int districtCount = 0, stateCount = 0;
            if (nearbyCount == 0)
            {
                var inDistrict = await Points(listable.Where(d => d.DistrictId == district)).ToListAsync(ct);
                districtCount = inDistrict.Count;
                if (districtCount > 0)
                {
                    hits = FarmerDealerRules.Ordered(inDistrict, lat, lng);
                }
                else if (stateId > 0)
                {
                    var inState = await Points(listable.Where(d => (d.DealerStateId ?? d.StateId) == stateId)).ToListAsync(ct);
                    stateCount = inState.Count;
                    hits = FarmerDealerRules.Ordered(inState, lat, lng);
                }
            }
            var scope = FarmerDealerRules.ChooseScope(gps, nearbyCount, districtCount, stateCount);

            var pageHits = hits.Skip((pageNo - 1) * size).Take(size).ToList();
            var items = await DealerCardsAsync(pageHits, ct);

            var stateName = stateId > 0
                ? await _db.States.AsNoTracking().Where(s => s.Id == stateId).Select(s => s.StateName).FirstOrDefaultAsync(ct)
                : null;

            return Ok(new FarmerDealerListDto
            {
                Scope = scope,
                RadiusKm = radius,
                UsedLocation = gps,
                DistrictId = districtRow?.Id,
                DistrictName = districtRow?.DistrictName,
                StateName = stateName,
                Search = search,
                Page = pageNo,
                PageSize = size,
                Total = hits.Count,
                Items = items
            });
        }

        private static IQueryable<FarmerDealerPoint> Points(IQueryable<DealerRegistration> q) =>
            q.Select(d => new FarmerDealerPoint(d.Id, d.FirmName, d.DistrictId, d.DealerStateId ?? d.StateId, d.Latitude, d.Longitude));

        /// <summary>Public card fields for one page of dealers, in the ranked order.</summary>
        private async Task<List<FarmerDealerDto>> DealerCardsAsync(List<FarmerDealerHit> pageHits, CancellationToken ct)
        {
            if (pageHits.Count == 0) return new List<FarmerDealerDto>();
            var ids = pageHits.Select(h => h.Dealer.Id).ToList();

            // Explicit projection: only these public columns ever leave the database.
            var rows = await _db.DealerRegistrations.AsNoTracking()
                .Where(d => ids.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    d.FirmName,
                    d.ShopNoORRoomNoOrBlockNo,
                    d.Street,
                    d.SubVillage,
                    d.Village,
                    d.Taluk,
                    d.DistrictId,
                    d.PinCode,
                    d.OfficialContactNumber,
                    d.WhatsAppNumber,
                    d.GoogleMapURL,
                    d.Latitude,
                    d.Longitude
                })
                .ToListAsync(ct);

            var districtIds = rows.Where(r => r.DistrictId is > 0).Select(r => r.DistrictId!.Value).Distinct().ToList();
            var districtNames = await _db.Districts.AsNoTracking()
                .Where(d => districtIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.DistrictName, ct);

            var byId = rows.ToDictionary(r => r.Id);
            var cards = new List<FarmerDealerDto>(pageHits.Count);
            foreach (var hit in pageHits)
            {
                if (!byId.TryGetValue(hit.Dealer.Id, out var r)) continue;
                var districtName = r.DistrictId is { } did && districtNames.TryGetValue(did, out var dn) ? dn : null;
                var gps = FarmerGeo.IsValid(r.Latitude, r.Longitude);
                cards.Add(new FarmerDealerDto
                {
                    Id = r.Id,
                    FirmName = r.FirmName.Trim(),
                    AddressLine1 = NullIfEmpty(FarmerDealerRules.JoinParts(r.ShopNoORRoomNoOrBlockNo, r.Street)),
                    AddressLine2 = NullIfEmpty(FarmerDealerRules.JoinDistinct(r.SubVillage, r.Village, r.Taluk)),
                    Village = NullIfEmpty(r.Village?.Trim()),
                    DistrictName = districtName,
                    PinCode = NullIfEmpty(r.PinCode?.Trim()),
                    Phone = FarmerContactLinks.DialNumber(r.OfficialContactNumber),
                    WhatsApp = FarmerContactLinks.WhatsAppNumber(r.WhatsAppNumber),
                    GoogleMapUrl = FarmerContactLinks.SafeMapUrl(r.GoogleMapURL),
                    DirectionsUrl = FarmerContactLinks.DirectionsUrl(r.Latitude, r.Longitude, r.GoogleMapURL,
                        FarmerDealerRules.JoinParts(r.FirmName, r.Village, districtName)),
                    Latitude = gps ? r.Latitude : null,
                    Longitude = gps ? r.Longitude : null,
                    DistanceKm = hit.DistanceKm is { } km ? Math.Round(km, 1) : null
                });
            }
            return cards;
        }

        // ------------------------------------------------------------------ team

        [HttpGet("contacts")]
        public async Task<IActionResult> Contacts(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profile = await _db.FarmerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null) return NotFound(new FarmerAuthError { Message = NoProfileMessage });

            var team = await ResolveTeamAsync(profile, ct);
            var memberIds = team.Members.Select(m => m.Staff.UserId).ToList();
            var openCounts = await _db.FarmerContactRequests.AsNoTracking()
                .Where(r => r.FarmerUserId == userId && r.Status == FarmerContactRequestStatus.Open && memberIds.Contains(r.StaffUserId))
                .GroupBy(r => r.StaffUserId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            var hqIds = team.Members.Where(m => m.AreaHqId is > 0).Select(m => m.AreaHqId!.Value).Distinct().ToList();
            var regionIds = team.Members.Where(m => m.AreaRegionId is > 0).Select(m => m.AreaRegionId!.Value).Distinct().ToList();
            var hqNames = await _db.Headquarters.AsNoTracking().Where(h => hqIds.Contains(h.Id)).ToDictionaryAsync(h => h.Id, h => h.HeadquarterName, ct);
            var regionNames = await _db.Regions.AsNoTracking().Where(r => regionIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.RegionName, ct);

            var showPhones = ShowStaffPhones;
            var dto = new FarmerTeamDto
            {
                Basis = team.Basis,
                NearestDealerKm = team.NearestDealerKm,
                PhonesShown = showPhones,
                DistrictName = await _db.Districts.AsNoTracking().Where(d => d.Id == profile.DistrictId).Select(d => d.DistrictName).FirstOrDefaultAsync(ct),
                StateName = await _db.States.AsNoTracking().Where(s => s.Id == profile.StateId).Select(s => s.StateName).FirstOrDefaultAsync(ct),
                Members = team.Members.Select(m => new FarmerContactDto
                {
                    ContactId = m.Staff.UserId,
                    Name = m.Staff.Name,
                    Role = m.Staff.Role,
                    RoleLabel = FarmerTeamRules.RoleLabel(m.Staff.Role),
                    AreaName = m.AreaHqId is { } h && hqNames.TryGetValue(h, out var hn) ? hn
                        : m.AreaRegionId is { } rg && regionNames.TryGetValue(rg, out var rn) ? rn : null,
                    AreaKind = m.AreaHqId is > 0 ? "Headquarter" : m.AreaRegionId is > 0 ? "Region" : null,
                    Phone = showPhones ? FarmerContactLinks.DialNumber(m.Staff.OfficialPhone) : null,
                    WhatsApp = showPhones ? FarmerContactLinks.WhatsAppNumber(m.Staff.OfficialPhone) : null,
                    OpenRequests = openCounts.TryGetValue(m.Staff.UserId, out var n) ? n : 0
                }).ToList()
            };
            return Ok(dto);
        }

        [HttpPost("contacts/{contactId}/request")]
        public async Task<IActionResult> CreateRequest(string contactId, [FromBody] FarmerContactRequestCreateDto? dto, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            if (dto is null) return BadRequest(new FarmerProfileErrorDto { Message = "Please choose what you need help with." });

            var errors = FarmerContactRequestRules.Validate(dto.Topic, dto.Message, dto.PreferredTime);
            if (errors.Count > 0)
                return BadRequest(new FarmerProfileErrorDto { Message = "Please check the highlighted fields.", Errors = errors });

            var profile = await _db.FarmerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile is null) return NotFound(new FarmerProfileErrorDto { Message = NoProfileMessage });

            // Only someone on this farmer's own resolved team can be asked to call back.
            var team = await ResolveTeamAsync(profile, ct);
            var member = team.Members.FirstOrDefault(m => string.Equals(m.Staff.UserId, contactId, StringComparison.Ordinal));
            if (member is null)
                return NotFound(new FarmerProfileErrorDto { Message = "This officer is not on your SPIC team any more. Please refresh the page." });

            var now = DateTime.UtcNow;
            var request = new FarmerContactRequest
            {
                FarmerUserId = userId,
                StaffUserId = member.Staff.UserId,
                StaffRole = member.Staff.Role,
                Topic = dto.Topic!.Value,
                Message = FarmerContactRequestRules.CleanText(dto.Message),
                PreferredTime = string.IsNullOrWhiteSpace(dto.PreferredTime) ? null : dto.PreferredTime.Trim(),
                Status = FarmerContactRequestStatus.Open,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            // Count and insert under one advisory lock per farmer + staff member, so two taps at once (or
            // both API replicas) cannot pass the cap together.
            var npgsql = _db.Database.IsNpgsql();
            await using var tx = npgsql ? await _db.Database.BeginTransactionAsync(ct) : null;
            if (npgsql)
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({"fcr:" + userId + ":" + member.Staff.UserId}, 0))", ct);

            var open = await _db.FarmerContactRequests.CountAsync(r =>
                r.FarmerUserId == userId && r.StaffUserId == member.Staff.UserId && r.Status == FarmerContactRequestStatus.Open, ct);
            if (!FarmerContactRequestRules.CanOpenAnother(open))
            {
                return Conflict(new FarmerProfileErrorDto
                {
                    Message = $"You already have {FarmerContactRequestRules.MaxOpenPerStaff} open requests with {member.Staff.Name}. They will call you soon."
                });
            }

            _db.FarmerContactRequests.Add(request);
            await _db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);

            return Ok(ToFarmerDto(request, member.Staff.Name));
        }

        [HttpGet("contacts/requests")]
        public async Task<IActionResult> MyRequests(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var rows = await _db.FarmerContactRequests.AsNoTracking()
                .Where(r => r.FarmerUserId == userId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.Id)
                .Take(MaxRequestsListed)
                .ToListAsync(ct);

            var names = await StaffNamesAsync(rows.Select(r => r.StaffUserId).Distinct().ToList(), ct);
            return Ok(rows.Select(r => ToFarmerDto(r, names.TryGetValue(r.StaffUserId, out var n) ? n : "SPIC officer")).ToList());
        }

        private static FarmerContactRequestDto ToFarmerDto(FarmerContactRequest r, string staffName) => new()
        {
            Id = r.Id,
            ContactId = r.StaffUserId,
            StaffName = staffName,
            StaffRole = r.StaffRole,
            StaffRoleLabel = FarmerTeamRules.RoleLabel(r.StaffRole),
            Topic = r.Topic,
            Message = r.Message,
            PreferredTime = r.PreferredTime,
            Status = r.Status,
            CreatedAtUtc = DateTime.SpecifyKind(r.CreatedAtUtc, DateTimeKind.Utc),
            UpdatedAtUtc = DateTime.SpecifyKind(r.UpdatedAtUtc, DateTimeKind.Utc)
        };

        // ------------------------------------------------------------------ team resolution

        /// <summary>
        /// Feeds <see cref="FarmerTeamRules.Resolve"/>: the listable dealers of the farmer's district (or,
        /// when there are none, those with GPS within 100 km of the farmer) and every active JMDO / MDO /
        /// RM / RMD login whose user account is active and has the same role.
        /// </summary>
        private async Task<FarmerTeamResult> ResolveTeamAsync(FarmerProfile profile, CancellationToken ct)
        {
            var listable = _db.DealerRegistrations.AsNoTracking().Where(FarmerDealerRules.ListableForFarmers);
            var dealers = await TeamDealers(listable.Where(d => d.DistrictId == profile.DistrictId)).ToListAsync(ct);

            if (dealers.Count == 0 && FarmerGeo.IsValid(profile.Latitude, profile.Longitude))
            {
                var box = FarmerGeo.BoundingBox(profile.Latitude!.Value, profile.Longitude!.Value, FarmerTeamRules.NearestDealerMaxKm);
                dealers = await TeamDealers(listable.Where(d =>
                    d.Latitude != null && d.Longitude != null
                    && d.Latitude >= box.MinLat && d.Latitude <= box.MaxLat
                    && d.Longitude >= box.MinLng && d.Longitude <= box.MaxLng)).ToListAsync(ct);
            }

            // A dealer without a Region takes its Headquarter's region.
            var hqIds = dealers.Where(d => d.HqId > 0 && d.RegionId <= 0).Select(d => d.HqId).Distinct().ToList();
            if (hqIds.Count > 0)
            {
                var hqRegion = await _db.Headquarters.AsNoTracking().Where(h => hqIds.Contains(h.Id)).ToDictionaryAsync(h => h.Id, h => h.RegionId, ct);
                dealers = dealers.Select(d => d.RegionId <= 0 && hqRegion.TryGetValue(d.HqId, out var rid) ? d with { RegionId = rid } : d).ToList();
            }

            var roles = FarmerTeamRules.TeamRoles.ToList();
            var staffRows = await (
                from login in _db.Employeelogins.AsNoTracking()
                join user in _db.Users.AsNoTracking() on login.UserId equals user.Id
                join info in _db.EmployeeInformation.AsNoTracking() on login.EmployeeInformationID equals info.Id into infoJoin
                from info in infoJoin.DefaultIfEmpty()
                // The login role must be the account role (FarmerTeamRules.IsTeamLogin): the token, and so
                // the staff call-back page, carry the account role.
                where login.IsActive && user.IsActive && roles.Contains(login.Role) && user.Role == login.Role
                select new
                {
                    login.UserId,
                    login.Role,
                    AccountRole = user.Role,
                    LoginActive = login.IsActive,
                    AccountActive = user.IsActive,
                    login.StateId,
                    login.RegionId,
                    login.HeadquartersId,
                    login.RegionIds,
                    login.HeadquartersIds,
                    InfoName = info != null ? info.Name : null,
                    UserName = user.Name,
                    Official = info != null ? info.OfficialPhoneNumber : null
                }).ToListAsync(ct);

            var staff = staffRows
                .Where(s => FarmerTeamRules.IsTeamLogin(s.Role, s.AccountRole, s.LoginActive, s.AccountActive))
                .Select(s => new FarmerTeamStaff(
                s.UserId, s.Role, s.StateId, s.RegionId, s.HeadquartersId, s.RegionIds, s.HeadquartersIds,
                FirstNonBlank(s.InfoName, s.UserName) ?? "SPIC officer",
                s.Official)).ToList();

            return FarmerTeamRules.Resolve(profile.DistrictId, profile.StateId, profile.Latitude, profile.Longitude, dealers, staff);
        }

        private static IQueryable<FarmerTeamDealer> TeamDealers(IQueryable<DealerRegistration> q) =>
            q.Select(d => new FarmerTeamDealer(d.Id, d.DistrictId, d.DealerStateId ?? d.StateId, d.Region, d.HQ, d.Latitude, d.Longitude));

        /// <summary>Display names of staff users (employee record name, else the account name).</summary>
        private async Task<Dictionary<string, string>> StaffNamesAsync(List<string> userIds, CancellationToken ct)
        {
            if (userIds.Count == 0) return new Dictionary<string, string>();
            var rows = await (
                from user in _db.Users.AsNoTracking()
                where userIds.Contains(user.Id)
                join login in _db.Employeelogins.AsNoTracking() on user.Id equals login.UserId into loginJoin
                from login in loginJoin.DefaultIfEmpty()
                join info in _db.EmployeeInformation.AsNoTracking() on (login != null ? login.EmployeeInformationID : -1) equals info.Id into infoJoin
                from info in infoJoin.DefaultIfEmpty()
                select new { user.Id, UserName = user.Name, InfoName = info != null ? info.Name : null }).ToListAsync(ct);

            return rows.GroupBy(r => r.Id)
                .ToDictionary(g => g.Key, g => FirstNonBlank(g.Select(r => r.InfoName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)), g.First().UserName) ?? "SPIC officer");
        }

        private static string? FirstNonBlank(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

        private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

        private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
