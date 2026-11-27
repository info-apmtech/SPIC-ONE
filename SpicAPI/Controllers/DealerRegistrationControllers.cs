using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Collections.Generic;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Identity;
using SpicAPI.Services;
namespace SpicAPI.Controllers
{

    [Route("api/[controller]")]
    public class DealerRegistrationController : GenericCrudController<DealerRegistration>
    {
        private readonly IGenericRepository<DealerApprovalHistory>? _historyRepo;
        private readonly IGenericRepository<DealerExperience>? _expRepo;
        private readonly IGenericRepository<AnnualSaleDataLastFYofDealerRegistration>? _annualRepo;
        private readonly IGenericRepository<DealerWarehouseFacilities>? _whRepo;
        private readonly IGenericRepository<DealerRailFacilities>? _railRepo;
        private readonly IGenericRepository<DealerPortFacilities>? _portRepo;
        private readonly IGenericRepository<DealerMarketDetail>? _marketRepo;
        private readonly IGenericRepository<DealerCompaniesOperatingInArea>? _compRepo;
        private readonly IGenericRepository<DealerOwnershipInfo>? _ownerRepo;
        private readonly IGenericRepository<SalesPlanningInDealerRegistration>? _salesPlanRepo;
        private readonly IGenericRepository<DealerAssetBank>? _bankRepo;
        private readonly IGenericRepository<DealerAssetLand>? _landRepo;
        private readonly IGenericRepository<DealerAssetBuilding>? _buildingRepo;
        private readonly IGenericRepository<DealerCreditLimitProposal>? _creditRepo;
        private readonly IGenericRepository<DealerRegistrationDocuments>? _docsRepo;
        private readonly IGenericRepository<Designation>? _designationRepo;
        private readonly IGenericRepository<PartnerFamilyDetails>? _partnerRepo;
        private readonly IGenericRepository<PartnerOccupation>? _occRepo;
        private readonly IGenericRepository<DealerLoanLiabilities>? _loanRepo;
        private readonly IGenericRepository<UserInfo> _userInfoRepo;
        private readonly UserManager<UserInfo> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _db;
        private readonly IDealerInsightService _dealerInsightService;
        // Single constructor: optional repositories are injected when registered. Defaults to null to avoid breaking DI.
        public DealerRegistrationController(
            IGenericRepository<DealerRegistration> repo,
            IGenericRepository<UserInfo> userInfoRepo,
            UserManager<UserInfo> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext db,
            IDealerInsightService dealerInsightService,
            IGenericRepository<DealerApprovalHistory>? historyRepo = null,
            IGenericRepository<DealerExperience>? expRepo = null,
            IGenericRepository<AnnualSaleDataLastFYofDealerRegistration>? annualRepo = null,
            IGenericRepository<DealerWarehouseFacilities>? whRepo = null,
            IGenericRepository<DealerRailFacilities>? railRepo = null,
            IGenericRepository<DealerPortFacilities>? portRepo = null,
            IGenericRepository<DealerMarketDetail>? marketRepo = null,
            IGenericRepository<DealerCompaniesOperatingInArea>? compRepo = null,
            IGenericRepository<DealerOwnershipInfo>? ownerRepo = null,
            IGenericRepository<SalesPlanningInDealerRegistration>? salesPlanRepo = null,
            IGenericRepository<DealerAssetBank>? bankRepo = null,
            IGenericRepository<DealerAssetLand>? landRepo = null,
            IGenericRepository<DealerAssetBuilding>? buildingRepo = null,
            IGenericRepository<DealerCreditLimitProposal>? creditRepo = null,
            IGenericRepository<DealerRegistrationDocuments>? docsRepo = null,
            IGenericRepository<Designation>? designationRepo = null,
            IGenericRepository<PartnerFamilyDetails>? partnerRepo = null,
            IGenericRepository<PartnerOccupation>? occRepo = null,
            IGenericRepository<DealerLoanLiabilities>? loanRepo = null
            ) : base(repo)
        {
            _historyRepo = historyRepo;
            _userInfoRepo = userInfoRepo;
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _dealerInsightService = dealerInsightService;
            _expRepo = expRepo;
            _annualRepo = annualRepo;
            _whRepo = whRepo;
            _railRepo = railRepo;
            _portRepo = portRepo;
            _marketRepo = marketRepo;
            _compRepo = compRepo;
            _ownerRepo = ownerRepo;
            _salesPlanRepo = salesPlanRepo;
            _bankRepo = bankRepo;
            _landRepo = landRepo;
            _buildingRepo = buildingRepo;
            _creditRepo = creditRepo;
            _docsRepo = docsRepo;
            _designationRepo = designationRepo;
            _partnerRepo = partnerRepo;
            _occRepo = occRepo;
            _loanRepo = loanRepo;
        }
        private static readonly HashSet<string> _writeRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "CorporateAdmin", "Director", "AVP", "SMD", "SMM", "RM", "RMD", "MDO", "JMDO", "MO"
        };

        private static readonly HashSet<string> _adminRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "CorporateAdmin"
        };

        private static readonly HashSet<string> _hqCreatorRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "MDO", "JMDO", "MO"
        };

        private bool IsWriteRole(string role) => _writeRoles.Contains(role);
        private bool IsAdminRole(string role) => _adminRoles.Contains(role);
        private bool IsHqCreatorRole(string role) => _hqCreatorRoles.Contains(role);

        // Backward-compatible New Dealer identity recovery.
        // Some already-approved New Dealers may have had IsNewDealerRegistration
        // accidentally cleared by an older maintenance UI. The dedicated New Dealer
        // application-fee / trade-deposit fields are not used by the normal Existing
        // Dealer creation flow, so they provide a safe recovery signal without using
        // DealerCode as the dealer-type discriminator.
        private static bool HasNewDealerRegistrationEvidence(DealerRegistration dealer)
        {
            if (dealer.IsNewDealerRegistration)
                return true;

            return
                dealer.DealershipApplicationFeeBankId.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeDDNumber) ||
                dealer.DealershipApplicationFeeDDDate.HasValue ||
                (dealer.DealershipApplicationFeeAmount ?? 0m) > 0m ||
                !string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeFilePath) ||
                dealer.SpicTradeDepositDDBankId.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.SpicTradeDepositDDNumber) ||
                dealer.SpicTradeDepositDDDate.HasValue ||
                (dealer.SpicTradeDepositDDAmount ?? 0m) > 0m ||
                !string.IsNullOrWhiteSpace(dealer.SpicTradeDepositFilePath) ||
                dealer.GflTradeDepositDDBankId.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.GflTradeDepositDDNumber) ||
                dealer.GflTradeDepositDDDate.HasValue ||
                (dealer.GflTradeDepositDDAmount ?? 0m) > 0m ||
                !string.IsNullOrWhiteSpace(dealer.GflTradeDepositFilePath);
        }

        private static void ClearTerminatedContactNumbers(DealerRegistration dealer)
        {
            if (dealer.Status != DealerStatus.Terminated)
                return;

            dealer.OfficialContactNumber = string.Empty;
            dealer.WhatsAppNumber = string.Empty;
            dealer.AlternativeNumber = null;
        }

        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] DealerRegistration entity)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!IsWriteRole(role))
                return Forbid();

            ClearTerminatedContactNumbers(entity);
            return await base.Create(entity);
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] DealerRegistration entity)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!IsWriteRole(role))
                return Forbid();

            var persisted = await _db.DealerRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (persisted == null)
                return NotFound(new { message = "Dealer registration was not found." });

            if (!IsOfficialContactNumberOptional(entity) && string.IsNullOrWhiteSpace(entity.OfficialContactNumber))
                return BadRequest("Dealer login cannot be created because the Official Contact Number is missing.");

            if (string.IsNullOrEmpty(entity.UserTableId) && !string.IsNullOrEmpty(persisted.UserTableId))
                entity.UserTableId = persisted.UserTableId;

            ClearTerminatedContactNumbers(entity);
            return await base.Update(id, entity);
        }

        [HttpDelete("{id}")]
        public override async Task<IActionResult> Delete(int id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!IsAdminRole(role))
                return Forbid();

            return await base.Delete(id);
        }

        [HttpGet("all")]
        public override async Task<IActionResult> GetAllWithInactive()
        {
            var query = _repo.GetAllWithInactive();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var regionClaim = User.FindFirst("spic:region_id")?.Value;
            var stateClaim = User.FindFirst("spic:state_id")?.Value;
            var hqClaim = User.FindFirst("spic:hq_id")?.Value;

            // Admin / CorporateAdmin → full data
            if (role == "Admin" || role == "CorporateAdmin")
                return Ok(await query.ToListAsync());
            if (SpecialAdminScope.IsSpecialAdmin(User))
                query = query.Where(x => SpecialAdminScope.AssignedStateIds(User).Contains(x.StateId));
            else if (role == "RM" || role == "RMD")
            {
                var regionIds = ClaimIntList("spic:region_ids");
                if (regionIds != null)
                    query = regionIds.Count > 0 ? query.Where(x => regionIds.Contains(x.Region)) : query.Where(x => false);
                else if (int.TryParse(regionClaim, out var regionId) && regionId > 0)
                    query = query.Where(x => x.Region == regionId);
                else
                    query = query.Where(x => false);
            }
            else if ((role == "SM" || role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId))
                query = query.Where(x => x.StateId == stateId);
            else if ((role == "MDO" || role == "JMDO" || role == "MO"))
            {
                var hqIds = ClaimIntList("spic:hq_ids");
                if (hqIds != null)
                    query = hqIds.Count > 0 ? query.Where(x => hqIds.Contains(x.HQ)) : query.Where(x => false);
                else if (int.TryParse(hqClaim, out var hqId) && hqId > 0)
                    query = query.Where(x => x.HQ == hqId);
                else if (int.TryParse(stateClaim, out var moStateId) && moStateId > 0)
                    query = query.Where(x => x.StateId == moStateId);
                else
                    query = query.Where(x => false);
            }
            else
                query = query.Where(x => x.CreatedBy == userId);
            return Ok(await query.ToListAsync());
        }
        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var regionClaim = User.FindFirst("spic:region_id")?.Value;
            var stateClaim = User.FindFirst("spic:state_id")?.Value;
            var hqClaim = User.FindFirst("spic:hq_id")?.Value;

            var query = _repo.GetAllWithInactive();

            if (role != "Admin" && role != "CorporateAdmin")
            {
                if (SpecialAdminScope.IsSpecialAdmin(User))
                    query = query.Where(x => SpecialAdminScope.AssignedStateIds(User).Contains(x.StateId));
                else if (role == "RM" || role == "RMD")
                {
                    var regionIds = ClaimIntList("spic:region_ids");
                    if (regionIds != null)
                        query = regionIds.Count > 0 ? query.Where(x => regionIds.Contains(x.Region)) : query.Where(x => false);
                    else if (int.TryParse(regionClaim, out var regionId) && regionId > 0)
                        query = query.Where(x => x.Region == regionId);
                    else
                        query = query.Where(x => false);
                }
                else if ((role == "SM" || role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId))
                    query = query.Where(x => x.StateId == stateId);
                else if (role == "MDO" || role == "JMDO" || role == "MO")
                {
                    var hqIds = ClaimIntList("spic:hq_ids");
                    var hasMoState = int.TryParse(stateClaim, out var moStateId) && moStateId > 0;

                    // Existing/imported dealer rows can predate HQ assignment and carry HQ 0.
                    // They are not owned by any other HQ, so keep them visible to an MO only
                    // inside the MO's own State. A dealer explicitly assigned to a different
                    // (non-zero) HQ is still excluded.
                    if (hqIds != null)
                    {
                        if (hqIds.Count == 0)
                            query = query.Where(x => false);
                        else
                            query = hasMoState
                                ? query.Where(x => hqIds.Contains(x.HQ) || (x.HQ == 0 && x.StateId == moStateId))
                                : query.Where(x => hqIds.Contains(x.HQ));
                    }
                    else if (int.TryParse(hqClaim, out var claimHqId) && claimHqId > 0)
                    {
                        query = hasMoState
                            ? query.Where(x => x.HQ == claimHqId || (x.HQ == 0 && x.StateId == moStateId))
                            : query.Where(x => x.HQ == claimHqId);
                    }
                    else if (hasMoState)
                        query = query.Where(x => x.StateId == moStateId);
                    else
                        query = query.Where(x => false);
                }
                else
                    query = query.Where(x => x.CreatedBy == userId);
            }

            var lookup = await query
                .Select(x => new { x.Id, x.DealerCode, x.FirmName, x.StateId })
                .ToListAsync();

            return Ok(lookup);
        }

        /// <summary>
        /// Territory-scoped, filterable flat list of active dealers for a picker (e.g. the JMDO
        /// Tasks Allocation wizard) - none of this controller's other GET endpoints return this
        /// shape: "all"/"lookup"/"submitted" have no district/taluk/coordinate fields or district
        /// filter, and "dashboard/page" is the approval-workflow grid (paged by RegionId only, no
        /// StateId/DistrictId filter, and includes Draft/Pending rows a picker shouldn't offer).
        /// Same hqId/regionId/stateId query-param convention as SubDealerRegistrationController's
        /// own "list" endpoint, so TasksAllocation.razor's existing LoginState-based call needs no
        /// other change than the URL.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetDealerList(
            [FromQuery] int? stateId,
            [FromQuery] int? regionId,
            [FromQuery] int? hqId,
            [FromQuery] int? districtId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 100 : Math.Min(pageSize, 1000);

            var query = _db.DealerRegistrations.AsNoTracking()
                .Where(x => x.Status == DealerStatus.Active);

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var regionClaim = User.FindFirst("spic:region_id")?.Value;
            var stateClaim = User.FindFirst("spic:state_id")?.Value;
            var hqClaim = User.FindFirst("spic:hq_id")?.Value;

            if (role != "Admin" && role != "CorporateAdmin")
            {
                if (SpecialAdminScope.IsSpecialAdmin(User))
                    query = query.Where(x => SpecialAdminScope.AssignedStateIds(User).Contains(x.StateId));
                else if (role == "RM" || role == "RMD")
                {
                    var regionIds = ClaimIntList("spic:region_ids");
                    if (regionIds != null)
                        query = regionIds.Count > 0 ? query.Where(x => regionIds.Contains(x.Region)) : query.Where(x => false);
                    else if (int.TryParse(regionClaim, out var claimRegionId) && claimRegionId > 0)
                        query = query.Where(x => x.Region == claimRegionId);
                    else
                        query = query.Where(x => false);
                }
                else if ((role == "SM" || role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var claimStateId))
                    query = query.Where(x => x.StateId == claimStateId);
                else if (role == "MDO" || role == "JMDO" || role == "MO")
                {
                    var hqIds = ClaimIntList("spic:hq_ids");
                    if (hqIds != null)
                        query = hqIds.Count > 0 ? query.Where(x => hqIds.Contains(x.HQ)) : query.Where(x => false);
                    else if (int.TryParse(hqClaim, out var claimHqId) && claimHqId > 0)
                        query = query.Where(x => x.HQ == claimHqId);
                    else if (int.TryParse(stateClaim, out var moStateId) && moStateId > 0)
                        query = query.Where(x => x.StateId == moStateId);
                    else
                        query = query.Where(x => false);
                }
                else if (!string.IsNullOrWhiteSpace(role))
                    query = query.Where(x => x.CreatedBy == userId);
            }

            // UI filters can only narrow the already role-scoped query, same convention as
            // SubDealerRegistrationController's own "list" endpoint.
            if (stateId is > 0) query = query.Where(x => x.StateId == stateId.Value);
            if (regionId is > 0) query = query.Where(x => x.Region == regionId.Value);
            if (hqId is > 0) query = query.Where(x => x.HQ == hqId.Value);
            if (districtId is > 0) query = query.Where(x => x.DistrictId == districtId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    x.FirmName.ToLower().Contains(term) ||
                    (x.DealerCode != null && x.DealerCode.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderBy(x => x.FirmName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.FirmName,
                    x.DealerCode,
                    x.DistrictId,
                    x.Taluk,
                    x.Latitude,
                    x.Longitude,
                    x.WholeSaleFertilizerLicenseNumber,
                    x.RetailFertilizerLicenseNumber
                })
                .ToListAsync(cancellationToken);

            var items = rows.Select(x => new DealerListItemDto
            {
                Id = x.Id,
                FirmName = x.FirmName,
                DealerCode = x.DealerCode,
                DistrictId = x.DistrictId,
                Taluk = x.Taluk,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                TypeLabel = !string.IsNullOrWhiteSpace(x.WholeSaleFertilizerLicenseNumber) ? "Wholesaler"
                    : !string.IsNullOrWhiteSpace(x.RetailFertilizerLicenseNumber) ? "Retailer"
                    : "Dealer"
            }).ToList();

            // One batched query set for the whole page (not one call per dealer) - see
            // DealerInsightService's own doc comment for why this is a separate, direct-query
            // service rather than calling into LiquidationCycleService/PendingAckService per dealer.
            var insights = await _dealerInsightService.GetInsightsAsync(items.Select(i => i.Id), cancellationToken);
            foreach (var item in items)
            {
                if (!insights.TryGetValue(item.Id, out var insight)) continue;
                item.IfmsId = insight.IfmsId;
                item.PosLiquidationQty = insight.PosLiquidationQty;
                item.PendingAckQty = insight.PendingAckQty;
            }

            return Ok(new DealerPagedListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        /// <summary>Minimal single-dealer lookup for TaskDetail.razor's location fallback chain -
        /// no plain GET {id} exists on this controller (GenericCrudController's own GetById
        /// returns the full DealerRegistration shape, heavier than this page needs).</summary>
        [HttpGet("{id:int}/brief")]
        public async Task<IActionResult> GetBrief(int id)
        {
            var dealer = await _db.DealerRegistrations.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new DealerBriefDto { FirmName = x.FirmName, Village = x.Village, Taluk = x.Taluk, DistrictId = x.DistrictId })
                .FirstOrDefaultAsync();

            if (dealer == null)
                return NotFound();

            return Ok(dealer);
        }

        /// <summary>
        /// Returns the dealer record linked to the given application user
        /// (DealerRegistration.UserTableId == UserInfo.Id). Small lookup used by
        /// the Guest Details page to auto-fill dealer contact details, mirroring
        /// the existing server-side user-to-dealer resolution used elsewhere.
        /// </summary>
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { Success = false, Message = "User id is required." });

            var dealer = await _repo.GetAllWithInactive()
                .AsNoTracking()
                .Where(d => d.UserTableId == userId)
                .Select(d => new { d.Id, d.DealerCode })
                .FirstOrDefaultAsync();

            if (dealer == null)
                return NotFound(new { Success = false, Message = "Dealer record not found." });

            return Ok(dealer);
        }

        private static bool IsMaintenanceEnclosuresComplete(
            DealerRegistration dealer,
            DealerRegistrationDocuments? docs,
            List<DealerOwnershipInfo> partners,
            List<DealerAssetLand> lands,
            List<DealerAssetBuilding> buildings)
        {
            static bool Has(string? value) => !string.IsNullOrEmpty(value);

            var required = new List<string?>
            {
                dealer.GSTFilePath,
                dealer.PANFilePath,
                dealer.AadhaarFilePath,
                docs?.FY1ITReturnFilePath,
                docs?.FY2ITReturnFilePath,
            };

            if (dealer.InSpic)
                required.Add(docs?.SpecimanFilePath);
            if (dealer.InGreenStar)
                required.Add(docs?.GreenstarSpecimanFilePath);

            var hasWholesaleLicense = !string.IsNullOrEmpty(dealer.WholeSaleFertilizerLicenseNumber);
            var hasRetailLicense = !string.IsNullOrEmpty(dealer.RetailFertilizerLicenseNumber);

            if (hasWholesaleLicense)
                required.Add(dealer.WholesaleLicenseFilePath);
            if (!hasRetailLicense || hasWholesaleLicense)
                required.Add(docs?.RetailerListFilePath);
            if (hasRetailLicense)
                required.Add(dealer.RetailLicenseFilePath);

            if (dealer.EntityType == EntityType.Partnership)
                required.Add(docs?.PartnershipDeadFilePath);
            if (dealer.EntityType == EntityType.LLP)
                required.Add(docs?.LlpAgreementFilePath);
            if (dealer.EntityType == EntityType.PvtLtd || dealer.EntityType == EntityType.PubLtd)
            {
                required.Add(docs?.BoardReasolutionFilePath);
                required.Add(docs?.ArticlesOfAssociationFilePath);
                required.Add(docs?.MemorandumOfAssociationFilePath);
            }
            if (dealer.EntityType == EntityType.Society)
                required.Add(docs?.ByLaw);
            if (dealer.EntityType != 0 && dealer.EntityType != EntityType.soleProprietor)
                required.Add(docs?.AuthorizationLetterFilePath);

            var isSoleProprietor = dealer.EntityType == EntityType.soleProprietor;
            foreach (var p in partners)
            {
                required.Add(p.ProprietorImagePath);
                if (!isSoleProprietor)
                {
                    required.Add(p.AadhaarFilePath);
                    required.Add(p.PANFilePath);
                }
            }

            foreach (var l in lands)
            {
                required.Add(l.UploadedECDocumentPath);
                required.Add(l.UploadedLandDocumentPath);
                required.Add(l.UploadedValuationCertificatePath);
            }

            foreach (var b in buildings)
            {
                required.Add(b.UploadedECDocumentPath);
                required.Add(b.UploadedBuildingDocumentPath);
                required.Add(b.UploadedValuationCertificatePath);
            }

            return required.All(Has);
        }

        /// <summary>
        /// Returns a per-step completion summary for the given dealer.
        /// This consolidates multiple client calls into a single API.
        /// The response is an array of objects with StepNo (1-based) and IsComplete boolean.
        /// For a verified approved New Dealer opened in existing-maintenance flow, steps 10/11/12
        /// use existing maintenance validation rules (company/conditional applicability and required data).
        /// </summary>
        [HttpGet("{dealerId}/step-completion-summary")]
        public async Task<IActionResult> GetStepCompletionSummary(int dealerId, [FromQuery] string? flow = null)
        {
            var result = new List<object>();

            var dealer = await _repo.GetByIdAsync(dealerId);
            var maintenanceRequested = string.Equals(flow, "existing-maintenance", StringComparison.OrdinalIgnoreCase);
            var isApprovedNewDealerMaintenance = maintenanceRequested
                && dealer != null
                && HasNewDealerRegistrationEvidence(dealer)
                && dealer.AVPApproved == true
                && !string.IsNullOrWhiteSpace(dealer.DealerCode);

            DealerCreditLimitProposal? maintenanceProposal = null;
            DealerRegistrationDocuments? maintenanceDocs = null;
            var maintenancePartners = new List<DealerOwnershipInfo>();
            var maintenanceLands = new List<DealerAssetLand>();
            var maintenanceBuildings = new List<DealerAssetBuilding>();
            if (isApprovedNewDealerMaintenance)
            {
                maintenanceProposal = _creditRepo != null
                    ? await _creditRepo.GetAll().FirstOrDefaultAsync(x => EF.Property<int>(x, "DealerId") == dealerId)
                    : null;
                maintenanceDocs = _docsRepo != null
                    ? await _docsRepo.GetAll().FirstOrDefaultAsync(x => EF.Property<int>(x, "DealerId") == dealerId)
                    : null;
                maintenancePartners = _ownerRepo != null
                    ? await _ownerRepo.GetAll().Where(x => EF.Property<int>(x, "DealerId") == dealerId).ToListAsync()
                    : new List<DealerOwnershipInfo>();
                maintenanceLands = _landRepo != null
                    ? await _landRepo.GetAll().Where(x => EF.Property<int>(x, "DealerId") == dealerId).ToListAsync()
                    : new List<DealerAssetLand>();
                maintenanceBuildings = _buildingRepo != null
                    ? await _buildingRepo.GetAll().Where(x => EF.Property<int>(x, "DealerId") == dealerId).ToListAsync()
                    : new List<DealerAssetBuilding>();
            }

            // Step 1: Dealer basic info (DealerRegistration exists).
            // PinCode is only collected on the full registration flow; Terminated /
            // Inactive-Terminated (restricted) flows skip Primary Location entirely,
            // so also treat those statuses as complete for Step 1.
            bool step1 = dealer != null &&
                (!string.IsNullOrWhiteSpace(dealer.PinCode) ||
                 dealer.Status == DealerStatus.Terminated ||
                 (dealer.Status == DealerStatus.InActive && dealer.InactiveProposal == FutureBusinessProposal.Terminated));
            result.Add(new { StepNo = 1, IsComplete = step1 });

            // Step 2: DealerExperience (existing maintenance validates years + at least one record)
            bool step2;
            if (isApprovedNewDealerMaintenance)
                step2 = dealer!.YearsofExperiance > 0 && _expRepo != null && await _expRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            else
                step2 = _expRepo != null && await _expRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 2, IsComplete = step2 });

            // Step 3: AnnualSaleDataLastFYofDealerRegistration (requires at least one non-zero row)
            bool step3;
            if (isApprovedNewDealerMaintenance)
            {
                var annualRows = _annualRepo != null
                    ? await _annualRepo.GetAll().Where(x => EF.Property<int>(x, "DealerId") == dealerId).ToListAsync()
                    : new List<AnnualSaleDataLastFYofDealerRegistration>();
                step3 = annualRows.Any(r => r.OwnRetailsSaleQty != 0 || r.OwnRetailsSaleAmount != 0 || r.SaleToDealerQty != 0 || r.SaleToDealerAmount != 0);
            }
            else
                step3 = _annualRepo != null && await _annualRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 3, IsComplete = step3 });

            // Step 4: Warehouse facilities (maintenance requires at least 2 of each type)
            bool wh = _whRepo != null && await _whRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool rail = _railRepo != null && await _railRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool port = _portRepo != null && await _portRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool step4;
            if (isApprovedNewDealerMaintenance)
            {
                var whCount = _whRepo != null ? await _whRepo.GetAll().CountAsync(x => EF.Property<int>(x, "DealerId") == dealerId) : 0;
                var railCount = _railRepo != null ? await _railRepo.GetAll().CountAsync(x => EF.Property<int>(x, "DealerId") == dealerId) : 0;
                var portCount = _portRepo != null ? await _portRepo.GetAll().CountAsync(x => EF.Property<int>(x, "DealerId") == dealerId) : 0;
                step4 = whCount >= 2 && railCount >= 2 && portCount >= 2;
            }
            else
                step4 = wh || rail || port;
            result.Add(new { StepNo = 4, IsComplete = step4 });

            // Step 5: Market detail
            bool step5 = _marketRepo != null && await _marketRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 5, IsComplete = step5 });

            // Step 6: Companies operating in area
            bool step6 = _compRepo != null && await _compRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 6, IsComplete = step6 });

            // Step 7: Ownership info
            bool step7 = _ownerRepo != null && await _ownerRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 7, IsComplete = step7 });

            // Step 8: Sales planning (requires at least one non-zero row)
            bool step8;
            if (isApprovedNewDealerMaintenance)
            {
                var planRows = _salesPlanRepo != null
                    ? await _salesPlanRepo.GetAll().Where(x => EF.Property<int>(x, "DealerId") == dealerId).ToListAsync()
                    : new List<SalesPlanningInDealerRegistration>();
                step8 = planRows.Any(r =>
                    (r.AprilQty + r.MayQty + r.JuneQty + r.JulyQty + r.AugustQty + r.SeptemberQty + r.OctoberQty + r.NovemberQty + r.DecemberQty + r.JanuaryQty + r.FebruaryQty + r.MarchQty) != 0 ||
                    (r.AprilAmount + r.MayAmount + r.JuneAmount + r.JulyAmount + r.AugustAmount + r.SeptemberAmount + r.OctoberAmount + r.NovemberAmount + r.DecemberAmount + r.JanuaryAmount + r.FebruaryAmount + r.MarchAmount) != 0);
            }
            else
                step8 = _salesPlanRepo != null && await _salesPlanRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 8, IsComplete = step8 });

            // Step 9: Assets (bank/land/building)
            bool bank = _bankRepo != null && await _bankRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool land = _landRepo != null && await _landRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool building = _buildingRepo != null && await _buildingRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool step9 = bank || land || building || dealer?.IsCreditLimitConsentGiven == true;
            result.Add(new { StepNo = 9, IsComplete = step9 });

            if (isApprovedNewDealerMaintenance && dealer != null)
            {
                var consent = dealer.IsCreditLimitConsentGiven == true;
                var step10 = !dealer.InSpic
                    || consent
                    || (maintenanceProposal != null && maintenanceProposal.SpicMonthlyAvgNetOverdues.HasValue && maintenanceProposal.SpicMonthlyAvgNetOverdues.Value >= 0);
                result.Add(new { StepNo = 10, IsComplete = step10 });

                var step11 = !dealer.InGreenStar
                    || consent
                    || (maintenanceProposal != null && maintenanceProposal.GreenstarMonthlyAvgNetOverdues.HasValue && maintenanceProposal.GreenstarMonthlyAvgNetOverdues.Value >= 0);
                result.Add(new { StepNo = 11, IsComplete = step11 });

                var step12 = IsMaintenanceEnclosuresComplete(dealer, maintenanceDocs, maintenancePartners, maintenanceLands, maintenanceBuildings);
                result.Add(new { StepNo = 12, IsComplete = step12 });
            }
            else
            {
                // Step 10: Credit limit proposal (SPIC). New-dealer flow instead records
                // the SPIC Trade Deposit Details (Dealership Application Fee + Trade Deposit DD).
                // Consent dealers are marked complete here - they skip the CreditLimit pages.
                bool credit = _creditRepo != null && await _creditRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
                bool step10 = credit;
                if (dealer?.IsNewDealerRegistration == true)
                {
                    step10 = dealer.InSpic
                        && (!string.IsNullOrWhiteSpace(dealer.SpicTradeDepositDDNumber)
                            || dealer.SpicTradeDepositDDBankId > 0
                            || (dealer.SpicTradeDepositDDAmount ?? 0) > 0
                            || !string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeDDNumber)
                            || dealer.DealershipApplicationFeeBankId > 0
                            || (dealer.DealershipApplicationFeeAmount ?? 0) > 0);
                }
                // Consent path: skip credit limit pages - mark as complete.
                if (dealer?.IsCreditLimitConsentGiven == true)
                    step10 = true;
                result.Add(new { StepNo = 10, IsComplete = step10 });

                // Step 11: Credit limit for GreenStar (same check as step 10).
                // New-dealer flow instead records the GFL Trade Deposit Details.
                // Consent dealers are marked complete here - they skip the CreditLimitForGreenStar page.
                bool step11 = credit;
                if (dealer?.IsNewDealerRegistration == true)
                {
                    step11 = dealer.InGreenStar
                        && (!string.IsNullOrWhiteSpace(dealer.GflTradeDepositDDNumber)
                            || dealer.GflTradeDepositDDBankId > 0
                            || (dealer.GflTradeDepositDDAmount ?? 0) > 0);
                }
                // Consent path: skip credit limit pages - mark as complete.
                if (dealer?.IsCreditLimitConsentGiven == true)
                    step11 = true;
                result.Add(new { StepNo = 11, IsComplete = step11 });

                // Step 12: Documents
                bool step12 = _docsRepo != null && await _docsRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
                if (dealer?.IsNewDealerRegistration == true && _docsRepo != null)
                {
                    var docsRow = await _docsRepo.GetAll().FirstOrDefaultAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
                    step12 = docsRow != null && !string.IsNullOrWhiteSpace(docsRow.RequestLetterFilePath);
                }
                result.Add(new { StepNo = 12, IsComplete = step12 });
            }

            // Step 13: Final submission (dealer record exists)
            bool step13 = dealer != null;
            result.Add(new { StepNo = 13, IsComplete = step13 });

            return Ok(result);
        }

        private async Task<int?> ResolveDealerDesignationIdAsync()
        {
            if (_designationRepo == null) return null;

            var designation = await _designationRepo.GetAll()
                .FirstOrDefaultAsync(d => d.Name != null && d.Name.ToUpper() == "DEALER");

            return designation?.Id;
        }
        private async Task EnsureRoleAndAssignAsync(UserInfo user, AppRole role)
        {
            var roleName = role.ToString();

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));

                if (!roleResult.Succeeded)
                    throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);

                if (!addRoleResult.Succeeded)
                    throw new Exception(string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
            }
        }
        [HttpPut("update-with-user/{id}")]
        public async Task<IActionResult> UpdateDealerWithUser(int id, [FromBody] DealerRegistration dealer)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!IsWriteRole(role))
                return Forbid();

            if (id != dealer.Id) return BadRequest("ID mismatch");

            // A dealer's State/HQ/Region are assigned once at registration from the
            // original registering MO and must remain permanently fixed. Editors of any
            // role (MO/RM/SM/AVP/Admin/other MOs) must never be able to overwrite them
            // with their own State/HQ/Region while updating an existing dealer.
            var persisted = await _db.DealerRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (persisted == null)
                return NotFound(new { message = "Dealer registration was not found." });

            // A record that never had a location (older dealers, imports) may be filled in once;
            // a stored value is never replaced.
            if (persisted.StateId > 0) dealer.StateId = persisted.StateId;
            if (persisted.Region > 0) dealer.Region = persisted.Region;
            if (persisted.HQ > 0) dealer.HQ = persisted.HQ;

            // A Terminated registration must never provision or activate a login. It has no
            // usable contact-number credential (the Terminated form does not require one), and
            // dealer accounts are otherwise created only at the approved Dealer Code stages.
            // Similarly, Inactive dealers with proposals "Not Traceable" or "To Be Terminated"
            // do not require contact numbers and must not provision logins with blank credentials.
            // Official Contact Number is mandatory for Active and Inactive -> Future Business,
            // regardless of whether UserTableId already exists.
            if (!IsOfficialContactNumberOptional(dealer) && string.IsNullOrWhiteSpace(dealer.OfficialContactNumber))
                return BadRequest("Dealer login cannot be created because the Official Contact Number is missing.");

            if (string.IsNullOrEmpty(dealer.UserTableId) && !string.IsNullOrEmpty(persisted.UserTableId))
                dealer.UserTableId = persisted.UserTableId;

            if (string.IsNullOrEmpty(dealer.UserTableId)
                && !string.IsNullOrEmpty(dealer.DealerCode))
            {
                var userError = await EnsureDealerUserAsync(dealer);
                if (!string.IsNullOrEmpty(userError))
                    return BadRequest(userError);
            }

            ClearTerminatedContactNumbers(dealer);

            // Save the DealerRegistration changes
            await _repo.UpdateAsync(dealer);

            return Ok(dealer);
        }

        private static bool IsOfficialContactNumberOptional(DealerRegistration dealer)
        {
            return dealer.Status == DealerStatus.Terminated
                || (dealer.Status == DealerStatus.InActive && dealer.InactiveProposal == FutureBusinessProposal.NotTraceable)
                || (dealer.Status == DealerStatus.InActive && dealer.InactiveProposal == FutureBusinessProposal.Terminated);
        }

        /// <summary>
        /// Creates (or links) the login account for a dealer identified by its DealerCode.
        /// Returns an error message on failure, or null on success.
        /// </summary>
        private async Task<string?> EnsureDealerUserAsync(DealerRegistration dealer)
        {
            if (dealer.Status == DealerStatus.Terminated)
            {
                var existingTermUser = await _userInfoRepo.GetAll()
                    .FirstOrDefaultAsync(u => u.NormalizedUserName == dealer.DealerCode.ToUpper());
                if (existingTermUser != null)
                {
                    dealer.UserTableId = existingTermUser.Id;
                }
                return null;
            }

            var phonePass = dealer.OfficialContactNumber?.Trim();

            // Official Contact Number is mandatory for Active and Inactive -> Future Business,
            // regardless of whether the user already exists in the database.
            if (string.IsNullOrWhiteSpace(phonePass))
            {
                if (IsOfficialContactNumberOptional(dealer))
                    return null;

                return "Dealer login cannot be created because the Official Contact Number is missing.";
            }

            // 1. Check if the user ALREADY EXISTS in the database by their DealerCode
            var existingAppUser = await _userInfoRepo.GetAll()
                .FirstOrDefaultAsync(u => u.NormalizedUserName == dealer.DealerCode.ToUpper());

            if (existingAppUser != null)
            {
                // 2. The account already exists! Just link the existing ID to the dealer.
                // If it has no designation yet, assign the default Dealer designation.
                if (existingAppUser.DesignationId == null)
                {
                    var existingUserDesignationId = await ResolveDealerDesignationIdAsync();
                    if (existingUserDesignationId != null)
                    {
                        existingAppUser.DesignationId = existingUserDesignationId;
                        existingAppUser.UpdatedAt = DateTime.UtcNow;
                        existingAppUser.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                        await _userManager.UpdateAsync(existingAppUser);
                    }
                }

                dealer.UserTableId = existingAppUser.Id;
                return null;
            }

            // 3. The account truly doesn't exist. Create it safely.
            var newUserId = Guid.NewGuid().ToString();

            var dealerDesignationId = await ResolveDealerDesignationIdAsync();

            var newUser = new UserInfo
            {
                Id = newUserId,
                UserName = dealer.DealerCode,
                NormalizedUserName = dealer.DealerCode.ToUpper(),
                PhoneNumber = dealer.OfficialContactNumber,
                Password = phonePass,
                Name = dealer.FirmName,
                Role = (SPIC.Core.Entities.AppRole)11,
                DesignationId = dealerDesignationId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System"
            };

            var createUserResult = await _userManager.CreateAsync(newUser, phonePass);

            if (!createUserResult.Succeeded)
            {
                return string.Join(", ", createUserResult.Errors.Select(e => e.Description));
            }

            await EnsureRoleAndAssignAsync(newUser, newUser.Role);
            dealer.UserTableId = newUserId;
            return null;
        }

        /// <summary>
        /// Submits an already-saved registration for approval without binding the
        /// complete DealerRegistration entity from the request body. This prevents
        /// hidden/unavailable fields such as UserTableId and DealerCode from causing
        /// model-binding validation errors in the New Dealer flow.
        /// </summary>
        [HttpPost("{id:int}/submit-for-review")]
        public async Task<IActionResult> SubmitForReview(
            int id,
            [FromQuery] string? flow = null)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            if (!IsWriteRole(role))
                return Forbid();

            var dealer = await _repo.GetByIdAsync(id);
            if (dealer == null)
                return NotFound(new { message = "Dealer registration was not found." });

            var maintenanceRequested = string.Equals(
                flow,
                "existing-maintenance",
                StringComparison.OrdinalIgnoreCase);

            // existing-maintenance is valid only for a New Dealer that already completed
            // its first AVP approval and therefore already owns a real DealerCode. The
            // permanent IsNewDealerRegistration flag is deliberately NOT changed.
            var isExistingMaintenance =
                maintenanceRequested &&
                HasNewDealerRegistrationEvidence(dealer) &&
                !string.IsNullOrWhiteSpace(dealer.DealerCode);

            if (maintenanceRequested && !isExistingMaintenance)
            {
                return BadRequest(new
                {
                    message = "Existing Dealer maintenance is available only after the New Dealer has received a Dealer Code."
                });
            }

            // If an older maintenance build cleared the permanent flag, restore it only
            // when this request is an authenticated approved-New-Dealer maintenance cycle.
            // This makes the correction persistent without changing normal Existing Dealers.
            if (isExistingMaintenance && !dealer.IsNewDealerRegistration)
                dealer.IsNewDealerRegistration = true;

            var isOriginalNewDealerSubmission =
                dealer.IsNewDealerRegistration && !isExistingMaintenance;

            // Initial New Dealer Creation reuses the two Credit Limit routes for the
            // SPIC/GFL trade-deposit forms. Validate those saved fields only for the
            // initial New Dealer submission, never for approved-dealer maintenance.
            if (isOriginalNewDealerSubmission)
            {
                var errors = new Dictionary<string, string[]>();

                if (!dealer.InSpic && !dealer.InGreenStar)
                {
                    errors["CompanySelection"] = new[]
                    {
                        "Select SPIC, Greenstar, or both before final submission."
                    };
                }

                if (dealer.InSpic && dealer.IsCreditLimitConsentGiven != true)
                {
                    if (dealer.DealershipApplicationFeeBankId is null or <= 0)
                        errors[nameof(dealer.DealershipApplicationFeeBankId)] = new[] { "Application Fee Bank is required." };
                    if (string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeDDNumber))
                        errors[nameof(dealer.DealershipApplicationFeeDDNumber)] = new[] { "Application Fee DD Number is required." };
                    if (!dealer.DealershipApplicationFeeDDDate.HasValue)
                        errors[nameof(dealer.DealershipApplicationFeeDDDate)] = new[] { "Application Fee DD Date is required." };
                    if ((dealer.DealershipApplicationFeeAmount ?? 0) <= 0)
                        errors[nameof(dealer.DealershipApplicationFeeAmount)] = new[] { "Application Fee Amount must be greater than zero." };
                    if (string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeePayableAt))
                        errors[nameof(dealer.DealershipApplicationFeePayableAt)] = new[] { "Application Fee Payable At is required." };

                    if (dealer.SpicTradeDepositDDBankId is null or <= 0)
                        errors[nameof(dealer.SpicTradeDepositDDBankId)] = new[] { "SPIC Trade Deposit Bank is required." };
                    if (string.IsNullOrWhiteSpace(dealer.SpicTradeDepositDDNumber))
                        errors[nameof(dealer.SpicTradeDepositDDNumber)] = new[] { "SPIC Trade Deposit DD Number is required." };
                    if (!dealer.SpicTradeDepositDDDate.HasValue)
                        errors[nameof(dealer.SpicTradeDepositDDDate)] = new[] { "SPIC Trade Deposit DD Date is required." };
                    if ((dealer.SpicTradeDepositDDAmount ?? 0) <= 0)
                        errors[nameof(dealer.SpicTradeDepositDDAmount)] = new[] { "SPIC Trade Deposit Amount must be greater than zero." };
                }

                if (dealer.InGreenStar && dealer.IsCreditLimitConsentGiven != true)
                {
                    if (dealer.GflTradeDepositDDBankId is null or <= 0)
                        errors[nameof(dealer.GflTradeDepositDDBankId)] = new[] { "GFL Trade Deposit Bank is required." };
                    if (string.IsNullOrWhiteSpace(dealer.GflTradeDepositDDNumber))
                        errors[nameof(dealer.GflTradeDepositDDNumber)] = new[] { "GFL Trade Deposit DD Number is required." };
                    if (!dealer.GflTradeDepositDDDate.HasValue)
                        errors[nameof(dealer.GflTradeDepositDDDate)] = new[] { "GFL Trade Deposit DD Date is required." };
                    if ((dealer.GflTradeDepositDDAmount ?? 0) <= 0)
                        errors[nameof(dealer.GflTradeDepositDDAmount)] = new[] { "GFL Trade Deposit Amount must be greater than zero." };
                }

                // Only the New Dealer flow requires this additional enclosure.
                if (_docsRepo != null)
                {
                    var documents = await _docsRepo.GetAll()
                        .FirstOrDefaultAsync(x => EF.Property<int>(x, "DealerId") == id);

                    if (documents == null || string.IsNullOrWhiteSpace(documents.RequestLetterFilePath))
                    {
                        errors[nameof(DealerRegistrationDocuments.RequestLetterFilePath)] = new[]
                        {
                            "Request Letter for Dealership is required. Upload it on the firm's letterhead with seal and signature."
                        };
                    }
                }

                if (errors.Count > 0)
                {
                    return BadRequest(new
                    {
                        title = "New Dealer final-submission validation failed.",
                        status = 400,
                        errors
                    });
                }
            }

            if (isExistingMaintenance)
            {
                // This is a NEW approval cycle for an already-approved New Dealer that is
                // now being maintained as an Existing Dealer. Reset all approval stages so
                // Dashboard immediately becomes In RM, then follows RM -> SMM -> AVP.
                dealer.RMApproved = null;
                dealer.SMApproved = null;
                dealer.AVPApproved = null;
            }
            else
            {
                // Preserve the existing rejection/resubmission workflow exactly. The
                // record returns only to the approval level that rejected it.
                if (dealer.AVPApproved == false)
                {
                    dealer.AVPApproved = null;
                }
                else if (dealer.SMApproved == false)
                {
                    dealer.SMApproved = null;
                    dealer.AVPApproved = null;
                }
                else if (dealer.RMApproved == false)
                {
                    dealer.RMApproved = null;
                    dealer.SMApproved = null;
                    dealer.AVPApproved = null;
                }
            }

            // Existing Dealer behavior remains unchanged. Maintenance records are already
            // active approved dealers, so keep them active while their credit-limit update
            // passes through a fresh approval cycle. Initial New Dealers still receive
            // active status only at their first final AVP approval.
            if ((!dealer.IsNewDealerRegistration || isExistingMaintenance) &&
                dealer.Status != DealerStatus.InActive &&
                dealer.Status != DealerStatus.Terminated)
            {
                dealer.Status = DealerStatus.Active;
            }

            dealer.IsSubmittedForReview = true;
            dealer.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            dealer.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(dealer);

            return Ok(new
            {
                dealerId = dealer.Id,
                isSubmittedForReview = true,
                message = "Registration submitted successfully."
            });
        }

        /// <summary>
        /// Assigns a unique sequential numeric DealerCode to a new-flow dealer once its
        /// registration is finally approved. The code is the largest existing numeric
        /// DealerCode + 1, and is checked for uniqueness before being saved.
        ///
        /// Safety rules (New Dealer workflow):
        ///  - Only NEW_DEALER records (IsNewDealerRegistration == true) may receive a code here.
        ///  - MO (RM), SMM (SM) and AVP approvals must all be complete.
        ///  - The allocation is idempotent: a code is generated only once per dealer.
        ///  - Generation happens inside a database transaction and never replaces an
        ///    existing Dealer Code.
        /// </summary>
        [HttpPost("{id}/generate-dealer-code")]
        public async Task<IActionResult> GenerateDealerCode(int id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!IsWriteRole(role))
                return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

            var dealer = await _repo.GetByIdAsync(id);
            if (dealer == null)
                return NotFound(new { message = "Dealer registration was not found." });

            if (!dealer.IsNewDealerRegistration)
            {
                // Existing Dealer records already have a code; never regenerate/reallocate it.
                return Ok(new { dealer.Id, dealer.DealerCode, alreadyAllocated = !string.IsNullOrWhiteSpace(dealer.DealerCode) });
            }

            // Dealer Code must only be generated after the complete approval chain.
            if (dealer.RMApproved != true || dealer.SMApproved != true || dealer.AVPApproved != true)
            {
                return BadRequest(new
                {
                    message = "Dealer Code can only be generated after MO, SMM and AVP approvals are complete."
                });
            }

            // Idempotent: one New Dealer = one Dealer Code.
            if (!string.IsNullOrWhiteSpace(dealer.DealerCode))
                return Ok(new { dealer.Id, dealer.DealerCode, alreadyAllocated = true });

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var fresh = await _db.DealerRegistrations.FirstOrDefaultAsync(x => x.Id == id);
                if (fresh == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new { message = "Dealer registration was not found." });
                }

                // Re-check inside the transaction to guard against concurrent allocation.
                if (!string.IsNullOrWhiteSpace(fresh.DealerCode))
                {
                    await transaction.RollbackAsync();
                    return Ok(new { fresh.Id, fresh.DealerCode, alreadyAllocated = true });
                }

                string candidate = await GetNextDealerCodeAsync();

                bool inUse = await _db.DealerRegistrations
                    .AnyAsync(x => x.DealerCode != null && x.DealerCode == candidate);
                if (inUse)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new
                    {
                        message = "Dealer Code allocation failed: generated code is already in use. Please retry."
                    });
                }

                fresh.DealerCode = candidate;
                fresh.Status = DealerStatus.Active;
                fresh.UpdatedBy = userId;
                fresh.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (string.IsNullOrEmpty(fresh.UserTableId) && !string.IsNullOrEmpty(fresh.DealerCode))
                {
                    var userError = await EnsureDealerUserAsync(fresh);
                    if (!string.IsNullOrEmpty(userError))
                        return BadRequest(new { message = userError });

                    // Persist the dealer's login account link (UserTableId).
                    await _db.SaveChangesAsync();
                }

                return Ok(new { fresh.Id, fresh.DealerCode, alreadyAllocated = false });
            }
            catch
            {
                try { await transaction.RollbackAsync(); } catch { }
                return StatusCode(500, new
                {
                    message = "Dealer Code allocation failed. The record was not modified. Please retry."
                });
            }
        }

        /// <summary>
        /// Returns the next numeric DealerCode preview for final New Dealer AVP review.
        /// This endpoint is read-only: it does not save DealerCode or any company code.
        /// The optional legacy greenStarPrefix query is retained for older clients, but
        /// the current AVP UI supports selecting multiple N/Z/T prefixes after preview.
        /// </summary>
        [HttpGet("{id}/dealer-code-preview")]
        public async Task<IActionResult> GetDealerCodePreview(
            int id,
            [FromQuery] string? greenStarPrefix = null)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            if (role != "AVP" && role != "Director" && role != "CorporateAdmin" && role != "Admin")
                return Forbid();

            var dealer = await _repo.GetByIdAsync(id);
            if (dealer == null)
                return NotFound(new { message = "Dealer registration was not found." });

            if (!dealer.IsNewDealerRegistration)
                return BadRequest(new { message = "Dealer Code preview is only available for New Dealer registrations." });

            if (dealer.RMApproved != true || dealer.SMApproved != true || dealer.AVPApproved != null)
                return BadRequest(new { message = "Dealer Code preview is available only at pending final AVP approval." });

            if (!dealer.InSpic && !dealer.InGreenStar)
                return BadRequest(new { message = "The registration has no SPIC or GreenStar selection." });

            string? legacyPrefix = null;
            if (!string.IsNullOrWhiteSpace(greenStarPrefix))
            {
                legacyPrefix = NormalizeGreenStarPrefix(greenStarPrefix);
                if (legacyPrefix == null)
                    return BadRequest(new { message = "GreenStar prefix must be N, Z or T." });
            }

            var dealerCode = await GetNextDealerCodeAsync();

            return Ok(new
            {
                DealerCode = dealerCode,
                SPICCode = dealer.InSpic ? $"D{dealerCode}" : null,
                GreenStarCode = dealer.InGreenStar && legacyPrefix != null
                    ? $"{legacyPrefix}{dealerCode}"
                    : null,
                GreenStarPrefix = legacyPrefix,
                dealer.InSpic,
                dealer.InGreenStar
            });
        }

        /// <summary>
        /// AVP approval for a New Dealer. The initial call records the AVP decision;
        /// after AVP Office submits both Dealer IDs, a separate final call records
        /// "Final AVP" history. Existing RM/SMM approval, send-back and Existing Dealer
        /// flows are unchanged.
        /// </summary>
        [HttpPost("{id}/final-approve")]
        public async Task<IActionResult> FinalApprove(int id, [FromBody] FinalApproveRequest request)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            if (role != "AVP" && role != "Director" && role != "CorporateAdmin" && role != "Admin")
                return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

            var dealer = await _repo.GetByIdAsync(id);
            if (dealer == null)
                return NotFound(new { message = "Dealer registration was not found." });

            if (!dealer.IsNewDealerRegistration)
                return BadRequest(new { message = "This dealer is not a New Dealer registration." });

            if (dealer.RMApproved != true || dealer.SMApproved != true)
                return BadRequest(new
                {
                    message = "Dealer must be approved by MO and SMM before final AVP approval."
                });

            var remarks = string.IsNullOrWhiteSpace(request?.Remarks)
                ? string.Empty
                : request.Remarks.Trim();

            if (await HasFinalAvpApprovalAsync(id))
                return Ok(new { dealer.Id, alreadyFinalApproved = true, fullyApproved = true });

            var hasAvpOfficeSubmission = await IsAvpOfficeSubmittedCurrentlyAsync(id);
            if (!hasAvpOfficeSubmission)
            {
                return BadRequest(new { message = "AVP Office must submit the required Dealer IDs before final approval." });
            }

            if (string.IsNullOrWhiteSpace(dealer.DealerCode) ||
                (dealer.InSpic && string.IsNullOrWhiteSpace(dealer.SPICCode)) ||
                (dealer.InGreenStar && string.IsNullOrWhiteSpace(dealer.GreenStarCode)))
            {
                return BadRequest(new { message = "AVP Office must submit the required Dealer IDs before final approval." });
            }

            await using var finalTransaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var finalFresh = await _db.DealerRegistrations.FirstOrDefaultAsync(x => x.Id == id);
                if (finalFresh == null)
                {
                    await finalTransaction.RollbackAsync();
                    return NotFound(new { message = "Dealer registration was not found." });
                }

                if (await HasFinalAvpApprovalAsync(id))
                {
                    await finalTransaction.RollbackAsync();
                    return Ok(new { finalFresh.Id, alreadyFinalApproved = true, fullyApproved = true });
                }

                if (!finalFresh.IsNewDealerRegistration ||
                    finalFresh.RMApproved != true ||
                    finalFresh.SMApproved != true ||
                    !await IsAvpOfficeSubmittedCurrentlyAsync(id) ||
                    string.IsNullOrWhiteSpace(finalFresh.DealerCode) ||
                    (finalFresh.InSpic && string.IsNullOrWhiteSpace(finalFresh.SPICCode)) ||
                    (finalFresh.InGreenStar && string.IsNullOrWhiteSpace(finalFresh.GreenStarCode)))
                {
                    await finalTransaction.RollbackAsync();
                    return BadRequest(new { message = "AVP Office must submit the required Dealer IDs before final approval." });
                }

                var finalApprovedAt = DateTime.Now;

                // Preserve saved codes; final AVP does not generate or change them.
                finalFresh.AVPApproved = true;
                finalFresh.Status = DealerStatus.Active;
                finalFresh.FinalApprovalDate = finalApprovedAt;
                finalFresh.IsSubmittedForReview = true;
                finalFresh.UpdatedBy = userId;
                finalFresh.UpdatedAt = finalApprovedAt;

                if (finalFresh.InSpic && finalFresh.DateOfAppointment == default)
                    finalFresh.DateOfAppointment = finalApprovedAt;

                if (finalFresh.InGreenStar && !finalFresh.GreenstarDateOfAppointment.HasValue)
                    finalFresh.GreenstarDateOfAppointment = finalApprovedAt;

                if (_historyRepo != null)
                {
                    await _historyRepo.CreateAsync(new DealerApprovalHistory
                    {
                        DealerId = finalFresh.Id,
                        ApprovedBy = userId,
                        Role = "AVP",
                        ApprovedAt = finalApprovedAt,
                        Remarks = remarks,
                        IsApproved = true
                    });
                }

                await _db.SaveChangesAsync();
                await finalTransaction.CommitAsync();

                if (string.IsNullOrEmpty(finalFresh.UserTableId) && !string.IsNullOrWhiteSpace(finalFresh.DealerCode))
                {
                    var userError = await EnsureDealerUserAsync(finalFresh);
                    if (!string.IsNullOrEmpty(userError))
                        return BadRequest(new { message = userError });

                    await _db.SaveChangesAsync();
                }

                return Ok(new
                {
                    finalFresh.Id,
                    finalFresh.DealerCode,
                    finalFresh.SPICCode,
                    finalFresh.GreenStarCode,
                    alreadyFinalApproved = false,
                    fullyApproved = true
                });
            }
            catch
            {
                try { await finalTransaction.RollbackAsync(); } catch { }
                return StatusCode(500, new
                {
                    message = "Final AVP approval failed and was rolled back. Please retry."
                });
            }
        }

        /// <summary>
        /// AVP Office step for New Dealer Registration (MO → RM → SM → AVP Office → AVP).
        /// AVP Office manually enters Customer/Dealer Code and selected company codes,
        /// which are saved for final AVP review/approval. Existing Dealer flows unchanged.
        /// </summary>
        [HttpPost("{id:int}/avp-office-submit")]
        public async Task<IActionResult> AvpOfficeSubmit(int id, [FromBody] AvpOfficeSubmitRequest request)
        {
            if (!await IsAvpOfficeAsync() || !await HasSubmitDealerIdsPermissionAsync())
                return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

            var dealer = await _repo.GetByIdAsync(id);
            if (dealer == null)
                return NotFound(new { message = "Dealer registration was not found." });

            if (!HasNewDealerRegistrationEvidence(dealer))
                return BadRequest(new { message = "This dealer is not a New Dealer registration." });

            if (dealer.RMApproved != true || dealer.SMApproved != true)
                return BadRequest(new { message = "AVP Office can act only after MO and SMM approvals are complete." });

            if (await HasFinalAvpApprovalAsync(id))
                return BadRequest(new { message = "Final AVP approval is already completed for this dealer." });

            var zoneStateIds = await GetAvpOfficeZoneStateIdsAsync();
            if (zoneStateIds.Count == 0 || !zoneStateIds.Contains(dealer.StateId))
                return Forbid();

            var alreadySubmitted = await IsAvpOfficeSubmittedCurrentlyAsync(id);
            if (alreadySubmitted)
                return Ok(new
                {
                    dealer.Id,
                    dealer.DealerCode,
                    dealer.SPICCode,
                    dealer.GreenStarCode,
                    dealer.InSpic,
                    dealer.InGreenStar,
                    alreadySubmitted = true
                });

            var dealerCode = NormalizeDealerId(request?.DealerCode);
            var inSpic = request?.InSpic ?? dealer.InSpic;
            var inGreenStar = request?.InGreenStar ?? dealer.InGreenStar;
            var spicDealerId = NormalizeDealerId(request?.SpicDealerId);
            var gflDealerId = NormalizeDealerId(request?.GflDealerId);

            if (dealerCode == null)
                return BadRequest(new { message = "Customer/Dealer Code is required." });

            if (!inSpic && !inGreenStar)
                return BadRequest(new { message = "Select at least one company (SPIC or GFL)." });

            if (inSpic && spicDealerId == null)
                return BadRequest(new { message = "SPIC Code is required when SPIC is selected." });

            if (inGreenStar && gflDealerId == null)
                return BadRequest(new { message = "GFL Code is required when GFL is selected." });

            var remarks = string.IsNullOrWhiteSpace(request?.Remarks)
                ? string.Empty
                : request.Remarks.Trim();

            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var fresh = await _db.DealerRegistrations.FirstOrDefaultAsync(x => x.Id == id);
                if (fresh == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new { message = "Dealer registration was not found." });
                }

                if (!HasNewDealerRegistrationEvidence(fresh) ||
                    fresh.RMApproved != true || fresh.SMApproved != true)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "AVP Office can act only after MO and SMM approvals are complete." });
                }

                if (await HasFinalAvpApprovalAsync(id))
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Final AVP approval is already completed for this dealer." });
                }

                var alreadySubmittedInTransaction = await IsAvpOfficeSubmittedCurrentlyAsync(id);
                if (alreadySubmittedInTransaction)
                {
                    await transaction.RollbackAsync();
                    return Ok(new
                    {
                        fresh.Id,
                        fresh.DealerCode,
                        fresh.SPICCode,
                        fresh.GreenStarCode,
                        fresh.InSpic,
                        fresh.InGreenStar,
                        alreadySubmitted = true
                    });
                }

                if (inSpic && spicDealerId == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "SPIC Code is required when SPIC is selected." });
                }

                if (inGreenStar && gflDealerId == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "GFL Code is required when GFL is selected." });
                }

                var dealerCodeInUse = await _db.DealerRegistrations.AnyAsync(x =>
                    x.Id != fresh.Id &&
                    x.DealerCode != null &&
                    x.DealerCode.ToUpper() == dealerCode.ToUpper());

                if (dealerCodeInUse)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new { message = $"Customer/Dealer Code {dealerCode} is already in use." });
                }

                if (inSpic && spicDealerId != null)
                {
                    var spicInUse = await _db.DealerRegistrations.AnyAsync(x =>
                        x.Id != fresh.Id &&
                        x.SPICCode != null &&
                        x.SPICCode.ToUpper() == spicDealerId.ToUpper());

                    if (spicInUse)
                    {
                        await transaction.RollbackAsync();
                        return Conflict(new { message = $"SPIC Code {spicDealerId} is already in use." });
                    }
                }

                if (inGreenStar && gflDealerId != null)
                {
                    var gflInUse = await _db.DealerRegistrations.AnyAsync(x =>
                        x.Id != fresh.Id &&
                        ((x.GreenStarCode != null && x.GreenStarCode.ToUpper() == gflDealerId.ToUpper()) ||
                         (x.NCode != null && x.NCode.ToUpper() == gflDealerId.ToUpper()) ||
                         (x.TnCode != null && x.TnCode.ToUpper() == gflDealerId.ToUpper())));

                    if (gflInUse)
                    {
                        await transaction.RollbackAsync();
                        return Conflict(new { message = $"GFL Code {gflDealerId} is already in use." });
                    }
                }

                var completedAt = DateTime.Now;

                fresh.DealerCode = dealerCode;
                fresh.InSpic = inSpic;
                fresh.InGreenStar = inGreenStar;
                fresh.SPICCode = inSpic ? spicDealerId : null;
                fresh.GreenStarCode = inGreenStar ? gflDealerId : null;
                fresh.IsSubmittedForReview = true;
                fresh.UpdatedBy = userId;
                fresh.UpdatedAt = completedAt;

                if (inSpic && fresh.DateOfAppointment == default)
                    fresh.DateOfAppointment = completedAt;

                if (inGreenStar && !fresh.GreenstarDateOfAppointment.HasValue)
                    fresh.GreenstarDateOfAppointment = completedAt;

                if (_historyRepo != null)
                {
                    await _historyRepo.CreateAsync(new DealerApprovalHistory
                    {
                        DealerId = fresh.Id,
                        ApprovedBy = userId,
                        Role = "AVP Office",
                        ApprovedAt = completedAt,
                        Remarks = remarks,
                        IsApproved = true
                    });
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    fresh.Id,
                    fresh.DealerCode,
                    fresh.SPICCode,
                    fresh.GreenStarCode,
                    fresh.InSpic,
                    fresh.InGreenStar,
                    alreadySubmitted = false
                });
            }
            catch
            {
                try { await transaction.RollbackAsync(); } catch { }
                return StatusCode(500, new
                {
                    message = "AVP Office submission failed and was rolled back. Dealer IDs were not saved. Please retry."
                });
            }
        }

        private static string? NormalizeDealerId(string? value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0 || trimmed.Length > 50)
                return null;

            // Company Dealer IDs are identifiers, not free text: no whitespace or control characters.
            return trimmed.Any(ch => char.IsWhiteSpace(ch) || char.IsControl(ch))
                ? null
                : trimmed;
        }

        private static string? NormalizeGreenStarPrefix(string? prefix)
        {
            var value = (prefix ?? string.Empty).Trim().ToUpperInvariant();
            return value == "N" || value == "Z" || value == "T"
                ? value
                : null;
        }

        private static bool TryNormalizeGreenStarPrefixes(
            IEnumerable<string>? prefixes,
            string? legacyPrefix,
            out List<string> normalizedPrefixes)
        {
            var raw = prefixes?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList() ?? new List<string>();

            // Backward compatibility for the previous single-prefix client contract.
            if (raw.Count == 0 && !string.IsNullOrWhiteSpace(legacyPrefix))
                raw.Add(legacyPrefix.Trim());

            normalizedPrefixes = new List<string>();
            foreach (var item in raw)
            {
                var normalized = NormalizeGreenStarPrefix(item);
                if (normalized == null)
                    return false;

                if (!normalizedPrefixes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    normalizedPrefixes.Add(normalized);
            }

            return true;
        }

        /// <summary>
        /// Returns the next available numeric DealerCode (largest existing numeric code + 1).
        /// This intentionally preserves the existing DealerCode sequence and storage format.
        /// </summary>
        private async Task<string> GetNextDealerCodeAsync()
        {
            var existingCodes = await _db.DealerRegistrations
                .Where(x => x.DealerCode != null)
                .Select(x => x.DealerCode!)
                .ToListAsync();

            long max = 0;
            foreach (var code in existingCodes)
            {
                if (long.TryParse(code, out var numeric) && numeric > max)
                    max = numeric;
            }

            string candidate;
            do
            {
                max++;
                candidate = max.ToString();
            }
            while (existingCodes.Any(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase)));

            return candidate;
        }

        public class FinalApproveRequest
        {
            public string? Remarks { get; set; }
            public bool FinalAfterAvpOffice { get; set; }
            public string? DealerCode { get; set; }

            // Current UI: supports selecting any combination of N/Z/T.
            public List<string> GreenStarPrefixes { get; set; } = new();

            // Kept so an older deployed UI using the single-prefix contract does not break.
            public string? GreenStarPrefix { get; set; }
        }

        public class AvpOfficeSubmitRequest
        {
            /// <summary>Manually entered internal/Customer Dealer Code.</summary>
            public string? DealerCode { get; set; }

            /// <summary>Selected company flags for this New Dealer submission.</summary>
            public bool? InSpic { get; set; }
            public bool? InGreenStar { get; set; }

            /// <summary>Manually entered SPIC company/dealer code (required when SPIC is selected).</summary>
            public string? SpicDealerId { get; set; }

            /// <summary>Manually entered GFL/GreenStar company/dealer code (required when GFL/GreenStar is selected).</summary>
            public string? GflDealerId { get; set; }

            public string? Remarks { get; set; }
        }

        [HttpPost("{id}/send-back")]
        public async Task<IActionResult> SendBack(int id, [FromBody] DealerSendBackRequest request)
        {
            var dealer = await _repo.GetByIdAsync(id);
            if (dealer == null) return NotFound();

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var isAvpOffice = await IsAvpOfficeAsync();
            var callerRole = isAvpOffice ? "AVP Office" : role;

            var isInitialNewDealer = dealer.IsNewDealerRegistration && !await HasFinalAvpApprovalAsync(id);

            // Record history entry
            var remarks = new System.Text.StringBuilder();
            remarks.AppendLine($"Reason: {request.Reason}");
            remarks.AppendLine($"Priority: {request.Priority}");
            if (request.Sections != null && request.Sections.Any())
                remarks.AppendLine($"Sections: {string.Join(", ", request.Sections)}");

            var targetRole = request.TargetRole;
            if (string.IsNullOrWhiteSpace(targetRole))
            {
                if (callerRole == "RM" || callerRole == "RMD") targetRole = "MO";
                else if (callerRole == "SMD" || callerRole == "SMM") targetRole = "RM";
                else if (callerRole == "AVP Office") targetRole = "SM";
                else if (callerRole == "AVP" || callerRole == "CorporateAdmin" || callerRole == "Admin" || callerRole == "Director")
                    targetRole = isInitialNewDealer ? "AVP Office" : "SM";
            }
            remarks.AppendLine($"TargetRole: {targetRole}");

            var history = new DealerApprovalHistory
            {
                DealerId = id,
                ApprovedBy = userId,
                Role = callerRole,
                ApprovedAt = DateTime.Now,
                Remarks = remarks.ToString(),
                IsApproved = false
            };

            if (_historyRepo != null)
            {
                await _historyRepo.CreateAsync(history);
            }

            // Update dealer approval flags depending on the caller role and workflow
            if (callerRole == "RM" || callerRole == "RMD")
            {
                dealer.RMApproved = false;
            }
            else if (callerRole == "SMD" || callerRole == "SMM")
            {
                dealer.SMApproved = false;
            }
            else if (callerRole == "AVP Office")
            {
                // AVP Office Send Back returns to SMD/SMM
                dealer.SMApproved = false;
            }
            else if (callerRole == "AVP" || callerRole == "CorporateAdmin" || callerRole == "Admin" || callerRole == "Director")
            {
                if (isInitialNewDealer)
                {
                    // AVP Send Back returns to AVP Office.
                    // Retain existing Dealer IDs and allow correction and resubmission.
                    dealer.AVPApproved = null;
                }
                else
                {
                    dealer.AVPApproved = false;
                }
            }

            dealer.UpdatedAt = DateTime.Now;
            dealer.UpdatedBy = userId;

            await _repo.PatchAsync(id, dealer);

            return Ok(new { message = "Send back recorded", dealer });
        }
        [HttpPut("{id}/entity-type/{entityType:int}")]
        public async Task<IActionResult> SetEntityType(int id, int entityType)
        {
            var updated = await _repo.UpdatePropertyAsync(
                id,
                nameof(DealerRegistration.EntityType),
                (SPIC.Core.Entities.EntityType)entityType);

            if (updated == null) return NotFound();
            return Ok(new { updated.Id, EntityType = (int)entityType });
        }
        public class DealerSendBackRequest
        {
            public int DealerId { get; set; }
            public string Reason { get; set; } = "";
            public string Priority { get; set; } = "High";
            public List<string> Sections { get; set; } = new List<string>();
            public string? TargetRole { get; set; }
        }

        [HttpGet("dashboard-sap-counts")]
        public async Task<IActionResult> GetDashboardSapCounts()
        {
            var query = _repo.GetAllWithInactive();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var regionClaim = User.FindFirst("spic:region_id")?.Value;
            var stateClaim = User.FindFirst("spic:state_id")?.Value;
            var hqClaim = User.FindFirst("spic:hq_id")?.Value;

            var isAvpOffice = await IsAvpOfficeAsync();
            if (isAvpOffice)
            {
                // AVP Office works on New Dealers in its zone after SM approval and before
                // AVP Office submission. Existing Dealer flow is unchanged.
                var zoneStateIds = await GetAvpOfficeZoneStateIdsAsync();
                var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
                query = query.Where(x => x.IsNewDealerRegistration
                    && x.RMApproved == true
                    && x.SMApproved == true
                    && x.AVPApproved == null
                    && !avpOfficeSubmittedDealerIds.Contains(x.Id)
                    && zoneStateIds.Contains(x.StateId));
            }
            else if (role == "AVP")
            {
                var zoneStateIds = await GetZoneStateIdsAsync();
                query = query.Where(x => zoneStateIds.Contains(x.StateId));
            }
            else if (SpecialAdminScope.IsSpecialAdmin(User))
            {
                var assignedStates = SpecialAdminScope.AssignedStateIds(User);
                query = query.Where(x => assignedStates.Contains(x.StateId));
            }
            else if (role != "Admin" && role != "CorporateAdmin" && role != "Director")
            {
                if ((role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId) && stateId > 0)
                    query = query.Where(x => x.StateId == stateId);
                else if (role == "RM" || role == "RMD")
                {
                    var regionIds = ClaimIntList("spic:region_ids");
                    if (regionIds != null)
                        query = regionIds.Count > 0 ? query.Where(x => regionIds.Contains(x.Region)) : query.Where(x => false);
                    else if (int.TryParse(regionClaim, out var regionId) && regionId > 0)
                        query = query.Where(x => x.Region == regionId);
                    else
                        query = query.Where(x => false);
                }
                else if (role == "MDO" || role == "JMDO" || role == "MO")
                {
                    var hqIds = ClaimIntList("spic:hq_ids");
                    if (hqIds != null)
                        query = hqIds.Count > 0 ? query.Where(x => hqIds.Contains(x.HQ)) : query.Where(x => false);
                    else if (int.TryParse(hqClaim, out var hqId) && hqId > 0)
                        query = query.Where(x => x.HQ == hqId);
                    else
                        query = query.Where(x => false);
                }
                else
                    query = query.Where(x => x.CreatedBy == userId);
            }

            var sapCounts = await query.Select(d => new { d.Id, d.StateId, d.InSpic, d.InGreenStar }).ToListAsync();

            var sapSpicByState = sapCounts.Where(d => d.InSpic).GroupBy(d => d.StateId).ToDictionary(g => g.Key, g => g.Count());
            var sapGflByState = sapCounts.Where(d => d.InGreenStar).GroupBy(d => d.StateId).ToDictionary(g => g.Key, g => g.Count());
            var allStateIds = sapCounts.Select(d => d.StateId).Distinct().ToList();

            return Ok(new
            {
                TotalDealers = sapCounts.Count,
                SpicByState = sapSpicByState,
                GflByState = sapGflByState,
                AllStateIds = allStateIds
            });
        }

        [HttpGet("dashboard-completion-counts")]
        public async Task<IActionResult> GetDashboardCompletionCounts()
        {
            var counts = new Dictionary<int, int>();

            var query = _repo.GetAllWithInactive()
                .Where(x => (x.PinCode != null && x.PinCode != "")
                    || x.Status == DealerStatus.Terminated
                    || (x.Status == DealerStatus.InActive && x.InactiveProposal == FutureBusinessProposal.Terminated)
                    || x.DealerType == RegistrationDealerType.Department);

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var regionClaim = User.FindFirst("spic:region_id")?.Value;
            var stateClaim = User.FindFirst("spic:state_id")?.Value;
            var hqClaim = User.FindFirst("spic:hq_id")?.Value;

            var isAvpOffice = await IsAvpOfficeAsync();
            if (isAvpOffice)
            {
                // AVP Office works on New Dealers in its zone after SM approval and before
                // AVP Office submission. Existing Dealer flow is unchanged.
                var zoneStateIds = await GetAvpOfficeZoneStateIdsAsync();
                var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
                query = query.Where(x => x.IsNewDealerRegistration
                    && x.RMApproved == true
                    && x.SMApproved == true
                    && x.AVPApproved == null
                    && !avpOfficeSubmittedDealerIds.Contains(x.Id)
                    && zoneStateIds.Contains(x.StateId));
            }
            else if (role == "AVP")
            {
                var zoneStateIds = await GetZoneStateIdsAsync();
                query = query.Where(x => zoneStateIds.Contains(x.StateId));
            }
            else if (SpecialAdminScope.IsSpecialAdmin(User))
            {
                var assignedStates = SpecialAdminScope.AssignedStateIds(User);
                query = query.Where(x => assignedStates.Contains(x.StateId));
            }
            else if (role != "Admin" && role != "CorporateAdmin" && role != "Director")
            {
                if ((role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId) && stateId > 0)
                    query = query.Where(x => x.StateId == stateId);
                else if (role == "RM" || role == "RMD")
                {
                    var regionIds = ClaimIntList("spic:region_ids");
                    if (regionIds != null)
                        query = regionIds.Count > 0 ? query.Where(x => regionIds.Contains(x.Region)) : query.Where(x => false);
                    else if (int.TryParse(regionClaim, out var regionId) && regionId > 0)
                        query = query.Where(x => x.Region == regionId);
                    else
                        query = query.Where(x => false);
                }
                else if (role == "MDO" || role == "JMDO" || role == "MO")
                {
                    var hqIds = ClaimIntList("spic:hq_ids");
                    if (hqIds != null)
                        query = hqIds.Count > 0 ? query.Where(x => hqIds.Contains(x.HQ)) : query.Where(x => false);
                    else if (int.TryParse(hqClaim, out var hqId) && hqId > 0)
                        query = query.Where(x => x.HQ == hqId);
                    else
                        query = query.Where(x => false);
                }
                else
                    query = query.Where(x => x.CreatedBy == userId);
            }

            var dealers = await query.Select(x => new { x.Id, x.PinCode }).ToListAsync();

            foreach (var d in dealers)
            {
                counts[d.Id] = 0;
                if (!string.IsNullOrWhiteSpace(d.PinCode))
                {
                    counts[d.Id] = 1; // Step 1
                }
            }

            if (_expRepo != null) foreach (var id in await _expRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;
            if (_annualRepo != null) foreach (var id in await _annualRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

            var whIds = _whRepo != null ? await _whRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
            var railIds = _railRepo != null ? await _railRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
            var portIds = _portRepo != null ? await _portRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
            foreach (var id in whIds.Concat(railIds).Concat(portIds).Distinct()) if (counts.ContainsKey(id)) counts[id]++;

            if (_marketRepo != null) foreach (var id in await _marketRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;
            if (_compRepo != null) foreach (var id in await _compRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;
            if (_ownerRepo != null) foreach (var id in await _ownerRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

            if (_ownerRepo != null)
            {
                var ownerPartners = _partnerRepo != null ? await _partnerRepo.GetAll().Select(x => EF.Property<int>(x, "OwnershipPartnerId")).Distinct().ToListAsync() : new List<int>();
                var ownerOccs = _occRepo != null ? await _occRepo.GetAll().Select(x => EF.Property<int>(x, "OwnershipPartnerId")).Distinct().ToListAsync() : new List<int>();
                var step8Owners = ownerPartners.Concat(ownerOccs).Distinct().ToList();
                if (step8Owners.Any())
                {
                    var step8Dealers = await _ownerRepo.GetAll().Where(o => step8Owners.Contains(o.Id)).Select(o => EF.Property<int>(o, "DealerId")).Distinct().ToListAsync();
                    foreach (var id in step8Dealers) if (counts.ContainsKey(id)) counts[id]++;
                }
            }

            if (_salesPlanRepo != null) foreach (var id in await _salesPlanRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

            var bankIds = _bankRepo != null ? await _bankRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
            var landIds = _landRepo != null ? await _landRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
            var buildIds = _buildingRepo != null ? await _buildingRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
            foreach (var id in bankIds.Concat(landIds).Concat(buildIds).Distinct()) if (counts.ContainsKey(id)) counts[id]++;

            if (_loanRepo != null) foreach (var id in await _loanRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

            if (_creditRepo != null)
            {
                var creditIds = await _creditRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync();
                foreach (var id in creditIds) if (counts.ContainsKey(id)) counts[id]++;
            }

            if (_docsRepo != null) foreach (var id in await _docsRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

            return Ok(counts);
        }

        /// <summary>
        /// Returns only dealers that have submitted their basic info (PinCode is present).
        /// Used by the Dashboard — drafts without a PinCode are excluded.
        /// </summary>
        [HttpGet("submitted")]
        public async Task<IActionResult> GetSubmitted()
        {
            // A dealer counts as "submitted" if either:
            //  - it went through the full registration flow (PinCode collected), OR
            //  - it's a Terminated / Inactive-Terminated (restricted) flow, which
            //    intentionally skips Primary Location / PinCode collection, OR
            //  - it's a Department (entity type), which skips the full registration flow.
            // The predicate and the role scope below are the single definition shared with
            // the paged dashboard endpoints (see the "Dashboard server-side paging" region).
            var query = _repo.GetAllWithInactive().Where(SubmittedDashboardPredicate);

            query = await ApplySubmittedRoleScopeAsync(query);

            // Dashboard needs to distinguish the FIRST New Dealer final approval from an
            // approved New Dealer that later completed the Existing Dealer maintenance cycle.
            // Use existing data only; no new DB column is introduced.
            var creditLimitDealerIds = _creditRepo == null
                ? new List<int>()
                : await _creditRepo.GetAll()
                    .Select(x => x.DealerId)
                    .Distinct()
                    .ToListAsync();

            var twiceFinalApprovedDealerIds = _historyRepo == null
                ? new List<int>()
                : await _historyRepo.GetAll()
                    .Where(h =>
                        h.IsApproved == true &&
                        (h.Role == "AVP" ||
                         h.Role == "Director" ||
                         h.Role == "CorporateAdmin" ||
                         h.Role == "Admin"))
                    .GroupBy(h => h.DealerId)
                    .Where(g => g.Count() >= 2)
                    .Select(g => g.Key)
                    .ToListAsync();

            var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
            var finalAvpApprovedDealerIds = await GetFinalAvpApprovedDealerIdsAsync();

            // Return only the fields used by Dashboard. This prevents newly added
            // registration columns from breaking the existing-dealer list when a
            // database migration is pending. A new dealer has no DealerCode until
            // final approval, so the card type can be derived without reading the
            // new IsNewDealerRegistration database column.
            var dashboardDealers = await query
                .Select(x => new
                {
                    x.Id,
                    x.IsDealer,
                    x.InSpic,
                    x.InGreenStar,
                    // Permanent DB flag is the primary source. The remaining conditions are a
                    // backward-compatible recovery only for historical New Dealer records whose
                    // flag was accidentally cleared by an older maintenance page.
                    IsNewDealerRegistration =
                        x.IsNewDealerRegistration ||
                        x.DealershipApplicationFeeBankId != null ||
                        x.DealershipApplicationFeeDDNumber != null ||
                        x.DealershipApplicationFeeDDDate != null ||
                        x.DealershipApplicationFeeAmount != null ||
                        x.DealershipApplicationFeeFilePath != null ||
                        x.SpicTradeDepositDDBankId != null ||
                        x.SpicTradeDepositDDNumber != null ||
                        x.SpicTradeDepositDDDate != null ||
                        x.SpicTradeDepositDDAmount != null ||
                        x.SpicTradeDepositFilePath != null ||
                        x.GflTradeDepositDDBankId != null ||
                        x.GflTradeDepositDDNumber != null ||
                        x.GflTradeDepositDDDate != null ||
                        x.GflTradeDepositDDAmount != null ||
                        x.GflTradeDepositFilePath != null,

                    IsAvpOfficeSubmitted = avpOfficeSubmittedDealerIds.Contains(x.Id),
                    IsFinalAvpApproved = finalAvpApprovedDealerIds.Contains(x.Id),

                    // First New Dealer approval has no completed Existing Dealer maintenance cycle.
                    // Maintenance is considered complete only after Credit Limit data exists AND
                    // a second successful final-level approval has been recorded.
                    HasCompletedExistingMaintenance =
                        creditLimitDealerIds.Contains(x.Id) &&
                        twiceFinalApprovedDealerIds.Contains(x.Id),

                    x.DealerCode,
                    x.CreatedBy,
                    x.SPICCode,
                    GreenStarCode = x.GreenStarCode ?? x.NCode ?? x.TnCode,
                    x.NCode,
                    x.TnCode,
                    x.StateId,
                    x.Region,
                    x.HQ,
                    Status = (int)x.Status,
                    x.InactiveProposal,
                    x.FirmName,
                    x.BusinessEntityType,
                    x.EntityType,
                    x.WholeSaleFertilizerLicenseNumber,
                    x.RetailFertilizerLicenseNumber,
                    x.PinCode,
                    x.Latitude,
                    x.Longitude,
                    x.RMApproved,
                    x.SMApproved,
                    x.AVPApproved,
                    x.IsSubmittedForReview,
                    x.IsFinalAmountSettled,
                    x.UpdatedAt
                })
                .ToListAsync();

            return Ok(dashboardDealers);
        }

        private async Task<List<int>> GetZoneStateIdsAsync()
        {
            var zoneId = int.TryParse(User.FindFirst("spic:zone_id")?.Value, out var z) ? z : 0;
            if (zoneId <= 0)
                return new List<int>();

            return await _db.States
                .AsNoTracking()
                .Where(s => s.ZoneId == zoneId)
                .Select(s => s.Id)
                .ToListAsync();
        }

        /// <summary>
        /// AVP Office fail-closed zone scope: the assigned zone must still exist and
        /// be active, and the result is the set of states it contains. An invalid,
        /// inactive, missing, or empty zone returns an empty list (no access).
        /// </summary>
        private async Task<List<int>> GetAvpOfficeZoneStateIdsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new List<int>();

            var login = await _db.Employeelogins
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == userId && l.Role == AppRole.CommonRole && l.IsActive);

            if (login == null || login.ZoneId <= 0)
                return new List<int>();

            var zoneIsActive = await _db.Zones
                .AsNoTracking()
                .AnyAsync(z => z.Id == login.ZoneId && z.IsActive);
            if (!zoneIsActive)
                return new List<int>();

            return await _db.States
                .AsNoTracking()
                .Where(s => s.ZoneId == login.ZoneId)
                .Select(s => s.Id)
                .ToListAsync();
        }

        /// <summary>
        /// CommonRole users only act as AVP in Dealer Review when their currently
        /// assigned designation is the active "AVP Office" designation. The JWT
        /// carries the role but not the designation, so this is resolved from the DB.
        /// </summary>
        private async Task<bool> IsAvpOfficeAsync()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(role, nameof(AppRole.CommonRole), StringComparison.OrdinalIgnoreCase))
                return false;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || user.DesignationId is null or <= 0)
                return false;

            var designation = _designationRepo != null
                ? await _designationRepo
                    .GetWhere(d => d.Id == user.DesignationId.Value)
                    .FirstOrDefaultAsync()
                : await _db.Designations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == user.DesignationId.Value);

            return designation is { IsActive: true }
                && string.Equals(designation.Name?.Trim(), "AVP Office", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// AVP Office Dealer ID submission permission. Must be an active CommonRole user
        /// with the active "AVP Office" designation; zone and dealer-stage checks are
        /// enforced separately by the endpoint.
        /// </summary>
        private async Task<bool> HasSubmitDealerIdsPermissionAsync()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(role, nameof(AppRole.CommonRole), StringComparison.OrdinalIgnoreCase))
                return false;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || user.DesignationId is null or <= 0)
                return false;

            var designation = _designationRepo != null
                ? await _designationRepo
                    .GetWhere(d => d.Id == user.DesignationId.Value)
                    .FirstOrDefaultAsync()
                : await _db.Designations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == user.DesignationId.Value);

            return designation is { IsActive: true }
                && string.Equals(designation.Name?.Trim(), "AVP Office", StringComparison.OrdinalIgnoreCase);
        }

        #region Dashboard server-side paging

        // ─────────────────────────────────────────────────────────────────────────────
        //  GET api/DealerRegistration/dashboard/page
        //  GET api/DealerRegistration/dashboard/summary
        //
        //  Shared/Pages/Dashboard.razor used to download EVERY submitted registration
        //  (≈1,200 rows for an admin) and then filter, sort, count and page in the
        //  browser. These two endpoints do the same work in SQL and return one page of
        //  rows plus the numbers behind the KPI tiles.
        //
        //  Both reuse the SAME "submitted" definition and the SAME role scope as
        //  GET submitted (see SubmittedDashboardPredicate / ApplySubmittedRoleScopeAsync
        //  below — GetSubmitted now calls them too), and then additionally apply the
        //  location scope the page applied client-side in ApplyRoleFilter.
        //
        //  Nothing here changes GET submitted or GET dashboard-completion-counts: the
        //  Dealer Review List, the Dealer State Summary page and the Dashboard's Excel
        //  export still use the full list endpoint unchanged.
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The Dashboard's "submitted" definition — the single copy, shared by
        /// GET submitted and the two paged dashboard endpoints. See GetSubmitted's
        /// comment for what each branch means.
        /// </summary>
        private static readonly Expression<Func<DealerRegistration, bool>> SubmittedDashboardPredicate =
            x => (x.PinCode != null && x.PinCode != "")
                || x.Status == DealerStatus.Terminated
                || (x.Status == DealerStatus.InActive && x.InactiveProposal == FutureBusinessProposal.Terminated)
                || (x.Status == DealerStatus.InActive && x.InactiveProposal == FutureBusinessProposal.NotTraceable)
                || x.DealerType == RegistrationDealerType.Department;

        /// <summary>
        /// "New Dealer" as the Dashboard sees it. The permanent DB flag is the primary
        /// source; the remaining conditions are the same backward-compatible recovery the
        /// GET submitted projection performs for historical records whose flag was cleared.
        /// </summary>
        private static readonly Expression<Func<DealerRegistration, bool>> IsNewDealerRegistrationExpr =
            x => x.IsNewDealerRegistration
                || x.DealershipApplicationFeeBankId != null
                || x.DealershipApplicationFeeDDNumber != null
                || x.DealershipApplicationFeeDDDate != null
                || x.DealershipApplicationFeeAmount != null
                || x.DealershipApplicationFeeFilePath != null
                || x.SpicTradeDepositDDBankId != null
                || x.SpicTradeDepositDDNumber != null
                || x.SpicTradeDepositDDDate != null
                || x.SpicTradeDepositDDAmount != null
                || x.SpicTradeDepositFilePath != null
                || x.GflTradeDepositDDBankId != null
                || x.GflTradeDepositDDNumber != null
                || x.GflTradeDepositDDDate != null
                || x.GflTradeDepositDDAmount != null
                || x.GflTradeDepositFilePath != null;

        /// <summary>Same rule, evaluated in memory for the rows of one page.</summary>
        private static readonly Func<DealerRegistration, bool> IsNewDealerRegistrationFunc =
            IsNewDealerRegistrationExpr.Compile();

        /// <summary>
        /// IDs of New Dealers that have completed the AVP Office submission step.
        /// Chronologically evaluates history so that AVP Send Back returns the dealer
        /// to AVP Office, allowing resubmission without deleting historical records.
        /// </summary>
        private async Task<List<int>> GetAvpOfficeSubmittedDealerIdsAsync()
        {
            if (_historyRepo == null) return new List<int>();

            var relevantHistory = await _historyRepo.GetAll()
                .AsNoTracking()
                .Where(h => h.Role == "AVP Office"
                         || h.Role == "AVP" || h.Role == "Director" || h.Role == "Admin" || h.Role == "CorporateAdmin" || h.Role == "Final AVP"
                         || h.Role == "SMD" || h.Role == "SMM" || h.Role == "SM")
                .Select(h => new { h.DealerId, h.Role, h.IsApproved, h.ApprovedAt, h.Id })
                .ToListAsync();

            var submittedIds = relevantHistory
                .GroupBy(h => h.DealerId)
                .Where(g =>
                {
                    var lastSmApproval = g
                        .Where(h => (h.Role == "SMD" || h.Role == "SMM" || h.Role == "SM") && h.IsApproved)
                        .OrderByDescending(h => h.ApprovedAt)
                        .ThenByDescending(h => h.Id)
                        .FirstOrDefault();

                    var latestAvpStage = g
                        .Where(h => (lastSmApproval == null || h.ApprovedAt >= lastSmApproval.ApprovedAt)
                                 && (h.Role == "AVP Office"
                                     || ((h.Role == "AVP" || h.Role == "Director" || h.Role == "Admin" || h.Role == "CorporateAdmin" || h.Role == "Final AVP") && !h.IsApproved)))
                        .OrderByDescending(h => h.ApprovedAt)
                        .ThenByDescending(h => h.Id)
                        .FirstOrDefault();

                    return latestAvpStage != null && latestAvpStage.Role == "AVP Office" && latestAvpStage.IsApproved;
                })
                .Select(g => g.Key)
                .ToList();

            // Completed legacy records
            var completedIds = await _db.DealerRegistrations
                .AsNoTracking()
                .Where(x => x.IsNewDealerRegistration
                    && x.AVPApproved == true
                    && x.DealerCode != null && x.DealerCode != "")
                .Select(x => x.Id)
                .ToListAsync();

            return submittedIds
                .Concat(completedIds)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Returns the IDs of initial New Dealer applications that were returned by AVP Office
        /// back to SMD/SMM. These records have SMApproved == false, but must route to SMD/SMM
        /// (not RM/RMD).
        /// </summary>
        private async Task<List<int>> GetAvpOfficeReturnedDealerIdsAsync()
        {
            if (_historyRepo == null) return new List<int>();

            var finalAvpApprovedDealerIds = await GetFinalAvpApprovedDealerIdsAsync();

            var candidateDealerIds = await _db.DealerRegistrations
                .AsNoTracking()
                .Where(x => x.IsNewDealerRegistration
                    && x.RMApproved == true
                    && x.SMApproved == false
                    && !finalAvpApprovedDealerIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (candidateDealerIds.Count == 0) return new List<int>();

            var relevantHistory = await _historyRepo.GetAll()
                .AsNoTracking()
                .Where(h => candidateDealerIds.Contains(h.DealerId)
                         && (h.Role == "AVP Office"
                             || h.Role == "SMD" || h.Role == "SMM" || h.Role == "SM"))
                .Select(h => new { h.DealerId, h.Role, h.IsApproved, h.ApprovedAt, h.Id })
                .ToListAsync();

            var returnedIds = relevantHistory
                .GroupBy(h => h.DealerId)
                .Where(g =>
                {
                    var latestAction = g
                        .OrderByDescending(h => h.ApprovedAt)
                        .ThenByDescending(h => h.Id)
                        .FirstOrDefault();

                    return latestAction != null
                        && string.Equals(latestAction.Role, "AVP Office", StringComparison.OrdinalIgnoreCase)
                        && !latestAction.IsApproved;
                })
                .Select(g => g.Key)
                .ToList();

            return returnedIds;
        }

        private async Task<bool> IsAvpOfficeSubmittedCurrentlyAsync(int dealerId)
        {
            if (_historyRepo == null) return false;

            var relevant = await _historyRepo.GetAll()
                .AsNoTracking()
                .Where(h => h.DealerId == dealerId
                         && (h.Role == "AVP Office"
                             || h.Role == "AVP" || h.Role == "Director" || h.Role == "Admin" || h.Role == "CorporateAdmin" || h.Role == "Final AVP"
                             || h.Role == "SMD" || h.Role == "SMM" || h.Role == "SM"))
                .Select(h => new { h.Role, h.IsApproved, h.ApprovedAt, h.Id })
                .ToListAsync();

            var lastSmApproval = relevant
                .Where(h => (h.Role == "SMD" || h.Role == "SMM" || h.Role == "SM") && h.IsApproved)
                .OrderByDescending(h => h.ApprovedAt)
                .ThenByDescending(h => h.Id)
                .FirstOrDefault();

            var latestAvpStage = relevant
                .Where(h => (lastSmApproval == null || h.ApprovedAt >= lastSmApproval.ApprovedAt)
                         && (h.Role == "AVP Office"
                             || ((h.Role == "AVP" || h.Role == "Director" || h.Role == "Admin" || h.Role == "CorporateAdmin" || h.Role == "Final AVP") && !h.IsApproved)))
                .OrderByDescending(h => h.ApprovedAt)
                .ThenByDescending(h => h.Id)
                .FirstOrDefault();

            if (latestAvpStage != null)
            {
                return latestAvpStage.Role == "AVP Office" && latestAvpStage.IsApproved;
            }

            return await _db.DealerRegistrations
                .AsNoTracking()
                .AnyAsync(x => x.Id == dealerId
                    && x.IsNewDealerRegistration
                    && x.AVPApproved == true
                    && x.DealerCode != null && x.DealerCode != "");
        }

        private async Task<bool> HasApprovedHistoryRoleAsync(int dealerId, string role) =>
            _historyRepo != null && await _historyRepo.GetAll()
                .AnyAsync(h => h.DealerId == dealerId && h.IsApproved == true && h.Role == role);

        private async Task<bool> HasFinalAvpApprovalAsync(int dealerId)
        {
            if (await HasApprovedHistoryRoleAsync(dealerId, "Final AVP"))
                return true;

            return await _db.DealerRegistrations
                .AsNoTracking()
                .AnyAsync(x => x.Id == dealerId
                    && x.IsNewDealerRegistration
                    && x.AVPApproved == true
                    && x.DealerCode != null && x.DealerCode != "");
        }

        private async Task<List<int>> GetFinalAvpApprovedDealerIdsAsync()
        {
            var finalRoleIds = _historyRepo == null
                ? new List<int>()
                : await _historyRepo.GetAll()
                    .Where(h => h.IsApproved == true && h.Role == "Final AVP")
                    .Select(h => h.DealerId)
                    .Distinct()
                    .ToListAsync();

            var avpOfficeRoleIds = _historyRepo == null
                ? new List<int>()
                : await _historyRepo.GetAll()
                    .Where(h => h.IsApproved == true && h.Role == "AVP Office")
                    .Select(h => h.DealerId)
                    .Distinct()
                    .ToListAsync();

            var legacyFinalIds = await _db.DealerRegistrations
                .AsNoTracking()
                .Where(x => x.IsNewDealerRegistration
                    && x.AVPApproved == true
                    && x.DealerCode != null && x.DealerCode != ""
                    && !avpOfficeRoleIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            return finalRoleIds
                .Concat(legacyFinalIds)
                .Distinct()
                .ToList();
        }

        private static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> expression) =>
            Expression.Lambda<Func<T, bool>>(Expression.Not(expression.Body), expression.Parameters);

        /// <summary>
        /// The role scope GET submitted applies. Extracted verbatim so the paged
        /// endpoints can never drift from it.
        /// </summary>
        private async Task<IQueryable<DealerRegistration>> ApplySubmittedRoleScopeAsync(IQueryable<DealerRegistration> query)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var regionClaim = User.FindFirst("spic:region_id")?.Value;
            var stateClaim = User.FindFirst("spic:state_id")?.Value;
            var hqClaim = User.FindFirst("spic:hq_id")?.Value;

            var isUnrestrictedRole =
                role == "Admin" || role == "CorporateAdmin" ||
                role == "Director";

            var isAvpOffice = await IsAvpOfficeAsync();

            if (isAvpOffice)
            {
                // AVP Office works on New Dealers in its zone after SM approval and before
                // AVP Office submission. Existing Dealer flow is unchanged.
                var zoneStateIds = await GetAvpOfficeZoneStateIdsAsync();
                var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
                query = query.Where(x => x.IsNewDealerRegistration
                    && x.RMApproved == true
                    && x.SMApproved == true
                    && x.AVPApproved == null
                    && !avpOfficeSubmittedDealerIds.Contains(x.Id)
                    && zoneStateIds.Contains(x.StateId));
            }
            else if (role == "AVP")
            {
                var zoneStateIds = await GetZoneStateIdsAsync();
                query = query.Where(x => zoneStateIds.Contains(x.StateId));
            }
            else if (SpecialAdminScope.IsSpecialAdmin(User))
            {
                var assignedStates = SpecialAdminScope.AssignedStateIds(User);
                query = query.Where(x => assignedStates.Contains(x.StateId));
            }
            else if (!isUnrestrictedRole)
            {
                if ((role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId) && stateId > 0)
                    query = query.Where(x => x.StateId == stateId);
                else if (role == "RM" || role == "RMD")
                {
                    var regionIds = ClaimIntList("spic:region_ids");
                    if (regionIds != null)
                        query = regionIds.Count > 0 ? query.Where(x => regionIds.Contains(x.Region)) : query.Where(x => false);
                    else if (int.TryParse(regionClaim, out var regionId) && regionId > 0)
                        query = query.Where(x => x.Region == regionId);
                    else
                        query = query.Where(x => false);
                }
                else if (role == "MDO" || role == "JMDO" || role == "MO")
                {
                    var hqIds = ClaimIntList("spic:hq_ids");
                    if (hqIds != null)
                        query = hqIds.Count > 0 ? query.Where(x => hqIds.Contains(x.HQ)) : query.Where(x => false);
                    else if (int.TryParse(hqClaim, out var hqId) && hqId > 0)
                        query = query.Where(x => x.HQ == hqId);
                    else
                        query = query.Where(x => false);
                }
                else
                    query = query.Where(x => x.CreatedBy == userId);
            }

            return query;
        }

        /// <summary>
        /// The extra location narrowing the Dashboard page used to do in the browser
        /// (ApplyRoleFilter). LoginState reads the very same JWT claims this reads:
        ///   IsSpecialAdminWithAssignments → spic:assigned_state_ids
        ///   IsStateRole  (SMD / SMM)      → spic:state_id   → DealerRegistration.StateId
        ///   IsRegionRole (RM / RMD)       → spic:region_id  → DealerRegistration.Region
        ///   IsHQRole     (MO/MDO/JMDO)    → spic:hq_id      → DealerRegistration.HQ
        /// Every other role (Admin, CorporateAdmin, Director, AVP, …) is left unfiltered
        /// here, exactly as the page left it. A missing claim reads as 0 on both sides.
        /// </summary>
        private IQueryable<DealerRegistration> ApplyDashboardUserScope(IQueryable<DealerRegistration> query)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (SpecialAdminScope.IsSpecialAdmin(User))
            {
                var assignedStates = SpecialAdminScope.AssignedStateIds(User);
                return assignedStates.Count > 0
                    ? query.Where(x => assignedStates.Contains(x.StateId))
                    : query;
            }

            var stateId = ClaimInt("spic:state_id");
            var regionId = ClaimInt("spic:region_id");
            var hqId = ClaimInt("spic:hq_id");

            if (role == "SMD" || role == "SMM")
                return query.Where(x => x.StateId == stateId);
            if (role == "RM" || role == "RMD")
            {
                var regionIds = ClaimIntList("spic:region_ids");
                if (regionIds != null)
                    return regionIds.Count > 0 ? query.Where(x => regionIds.Contains(x.Region)) : query.Where(x => false);
                return regionId > 0 ? query.Where(x => x.Region == regionId) : query.Where(x => false);
            }
            if (role == "MO" || role == "MDO" || role == "JMDO")
            {
                var hqIds = ClaimIntList("spic:hq_ids");
                if (hqIds != null)
                    return hqIds.Count > 0 ? query.Where(x => hqIds.Contains(x.HQ)) : query.Where(x => false);
                return hqId > 0 ? query.Where(x => x.HQ == hqId) : query.Where(x => false);
            }

            return query;
        }

        private List<int>? ClaimIntList(string claimType)
        {
            var claim = User.FindFirst(claimType);
            if (claim == null) return null;
            if (string.IsNullOrWhiteSpace(claim.Value)) return new List<int>();
            return claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        private int ClaimInt(string claimType) =>
            int.TryParse(User.FindFirst(claimType)?.Value, out var value) ? value : 0;

        /// <summary>
        /// The page's search / company / dealer-type / status / region / approval-status
        /// filters, translated to SQL. Mirrors Dashboard.razor's ApplyFilters exactly,
        /// including the approval-status chain of GetApprovalStatus:
        ///   not submitted        → "Draft"
        ///   RM false             → "Rejected by RM"
        ///   SM false             → "Rejected by SMM"
        ///   AVP false            → "Rejected by AVP"
        ///   AVP true             → "Approved"
        ///   RM null              → "In RM"
        ///   RM true, SM null     → "In SMM"
        ///   RM true, SM true,
        ///   AVP null             → "In AVP"
        /// </summary>
        private static IQueryable<DealerRegistration> ApplyDashboardFilters(
            IQueryable<DealerRegistration> query, DealerDashboardQuery request,
            List<int> avpOfficeSubmittedDealerIds,
            List<int> finalAvpApprovedDealerIds,
            List<int>? avpOfficeReturnedDealerIds = null)
        {
            var search = request.Search?.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x =>
                    (x.DealerCode != null && x.DealerCode.ToLower().Contains(search)) ||
                    (x.FirmName != null && x.FirmName.ToLower().Contains(search)));

            var company = request.Company?.Trim();
            if (company == "SPIC") query = query.Where(x => x.InSpic);
            else if (company == "GreenStar") query = query.Where(x => x.InGreenStar);
            else if (company == "Both") query = query.Where(x => x.InSpic && x.InGreenStar);

            var registrationType = request.RegistrationType?.Trim();
            if (registrationType == "new") query = query.Where(IsNewDealerRegistrationExpr);
            else if (registrationType == "existing") query = query.Where(Not(IsNewDealerRegistrationExpr));

            if (request.Status >= 0)
            {
                var status = (DealerStatus)request.Status;
                query = query.Where(x => x.Status == status);
            }

            if (request.RegionId > 0)
            {
                var regionId = request.RegionId;
                query = query.Where(x => x.Region == regionId);
            }

            var workflowStatus = request.WorkflowStatus;
            if (!string.IsNullOrWhiteSpace(workflowStatus) && workflowStatus != "All")
            {
                query = workflowStatus switch
                {
                    "Pending Draft" => query.Where(x => x.IsSubmittedForReview != true),
                    "Pending RM" => query.Where(x => x.IsSubmittedForReview == true
                        && ((x.RMApproved == null && x.SMApproved != false && x.AVPApproved == null)
                            || (x.RMApproved != false && x.SMApproved == false && (avpOfficeReturnedDealerIds == null || !avpOfficeReturnedDealerIds.Contains(x.Id))))),
                    "Pending SMM" => query.Where(x => x.IsSubmittedForReview == true
                        && ((x.RMApproved == true && x.SMApproved == null && x.AVPApproved == null)
                            || (x.RMApproved != false && x.SMApproved != false && x.AVPApproved == false)
                            || (x.RMApproved != false && x.SMApproved == false && avpOfficeReturnedDealerIds != null && avpOfficeReturnedDealerIds.Contains(x.Id)))),
                    "Pending AVP" => query.Where(x => x.IsSubmittedForReview == true
                        && x.RMApproved == true && x.SMApproved == true && x.AVPApproved == null
                        && (!x.IsNewDealerRegistration || avpOfficeSubmittedDealerIds.Contains(x.Id))),
                    "Pending AVP Office" => query.Where(x => x.IsSubmittedForReview == true
                        && x.RMApproved == true && x.SMApproved == true && x.AVPApproved == null
                        && x.IsNewDealerRegistration
                        && !avpOfficeSubmittedDealerIds.Contains(x.Id)),
                    "Pending Final AVP" => query.Where(x => x.IsSubmittedForReview == true
                        && x.RMApproved == true && x.SMApproved == true && x.AVPApproved == null
                        && x.IsNewDealerRegistration
                        && avpOfficeSubmittedDealerIds.Contains(x.Id)),
                    "Approved" => query.Where(x => x.IsSubmittedForReview == true
                        && x.RMApproved != false && x.SMApproved != false && x.AVPApproved == true),
                    "Rejected/Returned" => query.Where(x => x.IsSubmittedForReview == true
                        && (x.RMApproved == false || x.SMApproved == false || x.AVPApproved == false)),
                    _ => query
                };
            }

            return query;
        }

        /// <summary>
        /// "Pending for me", as Dashboard.razor's IsPendingForMe computes it: the record's
        /// current owner level (GetCurrentOwnerLevel) matched against the signed-in user's
        /// role. The role tests are constants for the request, so this translates to a
        /// plain CASE expression and can drive ORDER BY.
        ///   Draft / Rejected by RM → MO  → the creator, or an MO / MDO / JMDO user
        ///   In RM  / Rejected SMM  → RM  → RM / RMD
        ///   In SMM / Rejected AVP  → SMM → SMD / SMM
        ///   In AVP                 → AVP → Admin / CorporateAdmin / Director / AVP
        /// </summary>
        private Expression<Func<DealerRegistration, bool>> BuildPendingForMeExpression(
            bool isAvpOffice,
            List<int> avpOfficeSubmittedDealerIds,
            List<int> finalAvpApprovedDealerIds,
            List<int>? avpOfficeReturnedDealerIds = null)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var isCreatorRole = role == "MO" || role == "MDO" || role == "JMDO";
            var isRegionRole = role == "RM" || role == "RMD";
            var isStateRole = role == "SMD" || role == "SMM";
            var isAdminRole = role == "Admin" || role == "CorporateAdmin" || role == "Director" || role == "AVP";

            var hasUserId = !string.IsNullOrEmpty(userId);
            var currentUserId = userId ?? string.Empty;

            // AVP Office's actionable rows are New Dealers in zone after SM approval
            // and before AVP Office submission (existing dealer flow unchanged).
            if (isAvpOffice)
            {
                return x => x.IsSubmittedForReview == true
                    && x.IsNewDealerRegistration
                    && x.RMApproved == true && x.SMApproved == true
                    && x.AVPApproved == null
                    && !avpOfficeSubmittedDealerIds.Contains(x.Id);
            }

            return x =>
                x.IsSubmittedForReview != true
                    ? isCreatorRole || (hasUserId && x.CreatedBy == currentUserId)
                    : x.RMApproved == false
                        ? isCreatorRole || (hasUserId && x.CreatedBy == currentUserId)
                        : (avpOfficeReturnedDealerIds != null && avpOfficeReturnedDealerIds.Contains(x.Id))
                            ? isStateRole
                            : x.SMApproved == false
                                ? isRegionRole
                                : x.AVPApproved == false
                                    ? isStateRole
                                    : x.RMApproved == null
                                        ? isRegionRole
                                        : x.RMApproved == true && x.SMApproved == null
                                            ? isStateRole
                                            : x.RMApproved == true && x.SMApproved == true && x.AVPApproved == null
                                                ? (x.IsNewDealerRegistration
                                                    ? (avpOfficeSubmittedDealerIds.Contains(x.Id) && isAdminRole)
                                                    : isAdminRole)
                                                : false;
        }

        /// <summary>The submitted, role-scoped, filtered set both endpoints work on.</summary>
        private async Task<IQueryable<DealerRegistration>> BuildDashboardQueryAsync(DealerDashboardQuery request)
        {
            var query = _repo.GetAllWithInactive().Where(SubmittedDashboardPredicate);
            query = await ApplySubmittedRoleScopeAsync(query);
            query = ApplyDashboardUserScope(query);
            var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
            var finalAvpApprovedDealerIds = await GetFinalAvpApprovedDealerIdsAsync();
            var avpOfficeReturnedDealerIds = await GetAvpOfficeReturnedDealerIdsAsync();
            return ApplyDashboardFilters(query, request, avpOfficeSubmittedDealerIds, finalAvpApprovedDealerIds, avpOfficeReturnedDealerIds);
        }

        /// <summary>
        /// One page of dealer cards. Ordering is "pending for me" first, then the
        /// requested sort — the same two-key ordering the page did client-side. Id is
        /// added as a final tie-break so paging can never repeat or drop a row.
        /// </summary>
        [HttpGet("dashboard/page")]
        public async Task<IActionResult> GetDashboardPage([FromQuery] DealerDashboardQuery request)
        {
            request ??= new DealerDashboardQuery();

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 30 : Math.Min(request.PageSize, 200);

            var query = await BuildDashboardQueryAsync(request);

            var total = await query.CountAsync();

            var isAvpOffice = await IsAvpOfficeAsync();
            var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
            var finalAvpApprovedDealerIds = await GetFinalAvpApprovedDealerIdsAsync();
            var avpOfficeReturnedDealerIds = await GetAvpOfficeReturnedDealerIdsAsync();
            var pendingForMe = BuildPendingForMeExpression(isAvpOffice, avpOfficeSubmittedDealerIds, finalAvpApprovedDealerIds, avpOfficeReturnedDealerIds);
            var sort = (request.Sort ?? "newest").Trim().ToLowerInvariant();

            IOrderedQueryable<DealerRegistration> ordered = sort switch
            {
                "oldest" => query.OrderByDescending(pendingForMe).ThenBy(x => x.UpdatedAt),
                "name" => query.OrderByDescending(pendingForMe).ThenBy(x => x.FirmName),
                _ => query.OrderByDescending(pendingForMe).ThenByDescending(x => x.UpdatedAt)
            };

            var rows = await ordered
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var items = await MapDashboardItemsAsync(rows);

            return Ok(new DealerDashboardPageDto
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

        /// <summary>
        /// Every number the Dashboard's KPI tiles and status counters show, for the same
        /// filter and scope as dashboard/page. One aggregate query; no rows travel.
        /// </summary>
        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] DealerDashboardQuery request)
        {
            request ??= new DealerDashboardQuery();

            var query = await BuildDashboardQueryAsync(request);

            // The tiles read their numbers off the filtered list, so these counters
            // reproduce Dashboard.razor's CountDraft / CountInRM / CountInSMM /
            // CountInAVP / CountApproved / CountRejected* one for one — including the
            // fact that Approved and the Rejected counters look at the raw approval
            // flags and ignore IsSubmittedForReview.
            var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
            var finalAvpApprovedDealerIds = await GetFinalAvpApprovedDealerIdsAsync();
            var avpOfficeReturnedDealerIds = await GetAvpOfficeReturnedDealerIdsAsync();

            var aggregate = await query
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Draft = g.Count(x => x.Status != DealerStatus.Terminated && x.IsSubmittedForReview != true),
                    InRM = g.Count(x => x.IsSubmittedForReview == true
                        && ((x.RMApproved == null && x.SMApproved != false && x.AVPApproved == null)
                            || (x.RMApproved != false && x.SMApproved == false && !avpOfficeReturnedDealerIds.Contains(x.Id)))),
                    InSMM = g.Count(x => x.IsSubmittedForReview == true
                        && ((x.RMApproved == true && x.SMApproved == null && x.AVPApproved == null)
                            || (x.RMApproved != false && x.SMApproved != false && x.AVPApproved == false)
                            || (x.RMApproved != false && x.SMApproved == false && avpOfficeReturnedDealerIds.Contains(x.Id)))),
                    InAVP = g.Count(x => x.IsSubmittedForReview == true
                        && x.RMApproved == true && x.SMApproved == true && x.AVPApproved == null
                        && (!x.IsNewDealerRegistration || avpOfficeSubmittedDealerIds.Contains(x.Id))),
                    InAvpOffice = g.Count(x => x.IsSubmittedForReview == true
                        && x.RMApproved == true && x.SMApproved == true && x.AVPApproved == null
                        && x.IsNewDealerRegistration
                        && !avpOfficeSubmittedDealerIds.Contains(x.Id)),
                    InFinalAvp = 0,
                    Approved = g.Count(x => x.AVPApproved == true),
                    Rejected = g.Count(x => x.RMApproved == false || x.SMApproved == false || x.AVPApproved == false),
                    RejectedByRM = g.Count(x => x.RMApproved == false),
                    RejectedBySMM = g.Count(x => x.SMApproved == false && !avpOfficeReturnedDealerIds.Contains(x.Id)),
                    RejectedByAVP = g.Count(x => x.AVPApproved == false),
                    StatusActive = g.Count(x => x.Status == DealerStatus.Active),
                    StatusInactive = g.Count(x => x.Status == DealerStatus.InActive),
                    StatusTerminated = g.Count(x => x.Status == DealerStatus.Terminated)
                })
                .FirstOrDefaultAsync();

            var summary = new DealerDashboardSummaryDto();

            if (aggregate != null)
            {
                summary.TotalRegistrations = aggregate.Total;
                summary.Draft = aggregate.Draft;
                summary.InRM = aggregate.InRM;
                summary.InSMM = aggregate.InSMM;
                summary.InAVP = aggregate.InAVP;
                summary.InAvpOffice = aggregate.InAvpOffice;
                summary.InFinalAvp = aggregate.InFinalAvp;
                summary.Approved = aggregate.Approved;
                summary.Rejected = aggregate.Rejected;
                summary.RejectedByRM = aggregate.RejectedByRM;
                summary.RejectedBySMM = aggregate.RejectedBySMM;
                summary.RejectedByAVP = aggregate.RejectedByAVP;
                summary.StatusActive = aggregate.StatusActive;
                summary.StatusInactive = aggregate.StatusInactive;
                summary.StatusTerminated = aggregate.StatusTerminated;
            }

            summary.ByStatus = new Dictionary<int, int>
            {
                [(int)DealerStatus.Active] = summary.StatusActive,
                [(int)DealerStatus.InActive] = summary.StatusInactive,
                [(int)DealerStatus.Terminated] = summary.StatusTerminated
            };

            return Ok(summary);
        }

        /// <summary>
        /// Turns one page of entities into cards. The projection matches GET submitted
        /// field for field; HasCompletedExistingMaintenance and StepCount are resolved
        /// for the page's ids only, instead of for the whole table.
        /// </summary>
        private async Task<List<DealerDashboardItemDto>> MapDashboardItemsAsync(List<DealerRegistration> rows)
        {
            var ids = rows.Select(x => x.Id).ToList();

            // Same evidence GET submitted uses, narrowed to this page.
            var creditLimitDealerIds = _creditRepo == null || ids.Count == 0
                ? new List<int>()
                : await _creditRepo.GetAll()
                    .Where(x => ids.Contains(x.DealerId))
                    .Select(x => x.DealerId)
                    .Distinct()
                    .ToListAsync();

            var twiceFinalApprovedDealerIds = _historyRepo == null || ids.Count == 0
                ? new List<int>()
                : await _historyRepo.GetAll()
                    .Where(h => ids.Contains(h.DealerId))
                    .Where(h =>
                        h.IsApproved == true &&
                        (h.Role == "AVP" ||
                         h.Role == "Director" ||
                         h.Role == "CorporateAdmin" ||
                         h.Role == "Admin"))
                    .GroupBy(h => h.DealerId)
                    .Where(g => g.Count() >= 2)
                    .Select(g => g.Key)
                    .ToListAsync();

            var avpOfficeSubmittedDealerIds = await GetAvpOfficeSubmittedDealerIdsAsync();
            var finalAvpApprovedDealerIds = await GetFinalAvpApprovedDealerIdsAsync();
            var avpOfficeReturnedDealerIds = await GetAvpOfficeReturnedDealerIdsAsync();

            var stepCounts = await GetDashboardStepCountsAsync(ids);

            return rows.Select(x => new DealerDashboardItemDto
            {
                Id = x.Id,
                IsDealer = x.IsDealer,
                InSpic = x.InSpic,
                InGreenStar = x.InGreenStar,
                IsNewDealerRegistration = IsNewDealerRegistrationFunc(x),
                IsAvpOfficeSubmitted = avpOfficeSubmittedDealerIds.Contains(x.Id),
                IsAvpOfficeReturned = avpOfficeReturnedDealerIds.Contains(x.Id),
                IsFinalAvpApproved = finalAvpApprovedDealerIds.Contains(x.Id),
                HasCompletedExistingMaintenance =
                    creditLimitDealerIds.Contains(x.Id) && twiceFinalApprovedDealerIds.Contains(x.Id),
                DealerCode = x.DealerCode,
                CreatedBy = x.CreatedBy,
                SPICCode = x.SPICCode,
                GreenStarCode = x.GreenStarCode ?? x.NCode ?? x.TnCode,
                NCode = x.NCode,
                TnCode = x.TnCode,
                StateId = x.StateId,
                Region = x.Region,
                HQ = x.HQ,
                Status = (int)x.Status,
                InactiveProposal = x.InactiveProposal,
                FirmName = x.FirmName,
                BusinessEntityType = x.BusinessEntityType,
                EntityType = (int?)x.EntityType,
                WholeSaleFertilizerLicenseNumber = x.WholeSaleFertilizerLicenseNumber,
                RetailFertilizerLicenseNumber = x.RetailFertilizerLicenseNumber,
                PinCode = x.PinCode,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                RMApproved = x.RMApproved,
                SMApproved = x.SMApproved,
                AVPApproved = x.AVPApproved,
                IsSubmittedForReview = x.IsSubmittedForReview,
                IsFinalAmountSettled = x.IsFinalAmountSettled,
                UpdatedAt = x.UpdatedAt,
                StepCount = (string.IsNullOrWhiteSpace(x.PinCode) ? 0 : 1)
                    + (stepCounts.TryGetValue(x.Id, out var extra) ? extra : 0)
            }).ToList();
        }

        /// <summary>
        /// Registration step-completion counts for a handful of dealers. Same step
        /// definition and same order as GET dashboard-completion-counts, minus step 1
        /// (PinCode), which the caller adds from the row it already has. Restricting the
        /// sub-table queries to the page's ids is what makes this cheap enough to fold
        /// into the page response, so the Dashboard no longer needs the whole-table call.
        /// </summary>
        private async Task<Dictionary<int, int>> GetDashboardStepCountsAsync(List<int> dealerIds)
        {
            var counts = dealerIds.Distinct().ToDictionary(id => id, _ => 0);
            if (counts.Count == 0) return counts;

            void Bump(IEnumerable<int> ids)
            {
                foreach (var id in ids.Distinct())
                    if (counts.ContainsKey(id)) counts[id]++;
            }

            async Task<List<int>> DealerIdsOf<T>(IGenericRepository<T>? repo) where T : class =>
                repo == null
                    ? new List<int>()
                    : await repo.GetAll()
                        .Where(x => dealerIds.Contains(EF.Property<int>(x, "DealerId")))
                        .Select(x => EF.Property<int>(x, "DealerId"))
                        .Distinct()
                        .ToListAsync();

            Bump(await DealerIdsOf(_expRepo));                                          // Step 2
            Bump(await DealerIdsOf(_annualRepo));                                       // Step 3

            var warehouseIds = await DealerIdsOf(_whRepo);                              // Step 4
            var railIds = await DealerIdsOf(_railRepo);
            var portIds = await DealerIdsOf(_portRepo);
            Bump(warehouseIds.Concat(railIds).Concat(portIds));

            Bump(await DealerIdsOf(_marketRepo));                                       // Step 5
            Bump(await DealerIdsOf(_compRepo));                                         // Step 6
            Bump(await DealerIdsOf(_ownerRepo));                                        // Step 7

            if (_ownerRepo != null)                                                     // Step 8
            {
                var ownershipIds = await _ownerRepo.GetAll()
                    .Where(o => dealerIds.Contains(EF.Property<int>(o, "DealerId")))
                    .Select(o => o.Id)
                    .ToListAsync();

                if (ownershipIds.Count > 0)
                {
                    var ownerPartners = _partnerRepo != null
                        ? await _partnerRepo.GetAll()
                            .Where(x => ownershipIds.Contains(EF.Property<int>(x, "OwnershipPartnerId")))
                            .Select(x => EF.Property<int>(x, "OwnershipPartnerId"))
                            .Distinct().ToListAsync()
                        : new List<int>();

                    var ownerOccupations = _occRepo != null
                        ? await _occRepo.GetAll()
                            .Where(x => ownershipIds.Contains(EF.Property<int>(x, "OwnershipPartnerId")))
                            .Select(x => EF.Property<int>(x, "OwnershipPartnerId"))
                            .Distinct().ToListAsync()
                        : new List<int>();

                    var step8Owners = ownerPartners.Concat(ownerOccupations).Distinct().ToList();
                    if (step8Owners.Count > 0)
                    {
                        var step8Dealers = await _ownerRepo.GetAll()
                            .Where(o => step8Owners.Contains(o.Id))
                            .Select(o => EF.Property<int>(o, "DealerId"))
                            .Distinct().ToListAsync();
                        Bump(step8Dealers);
                    }
                }
            }

            Bump(await DealerIdsOf(_salesPlanRepo));                                    // Step 9

            var bankIds = await DealerIdsOf(_bankRepo);                                 // Step 10
            var landIds = await DealerIdsOf(_landRepo);
            var buildingIds = await DealerIdsOf(_buildingRepo);
            Bump(bankIds.Concat(landIds).Concat(buildingIds));

            Bump(await DealerIdsOf(_loanRepo));                                         // Step 11

            if (_creditRepo != null)                                                    // Step 12
            {
                var creditIds = await _creditRepo.GetAll()
                    .Where(x => dealerIds.Contains(x.DealerId))
                    .Select(x => x.DealerId)
                    .Distinct().ToListAsync();
                Bump(creditIds);
            }

            Bump(await DealerIdsOf(_docsRepo));                                         // Step 13

            return counts;
        }

        #endregion
    }
    [Route("api/[controller]")]
    public class DealerExperienceController(IGenericRepository<DealerExperience> repo) : GenericCrudController<DealerExperience>(repo);

    [Route("api/[controller]")]
    public class AnnualSaleDataLastFYController(IGenericRepository<AnnualSaleDataLastFYofDealerRegistration> repo) : GenericCrudController<AnnualSaleDataLastFYofDealerRegistration>(repo);

    [Route("api/[controller]")]
    public class DealerWarehouseFacilitiesController(IGenericRepository<DealerWarehouseFacilities> repo) : GenericCrudController<DealerWarehouseFacilities>(repo);

    [Route("api/[controller]")]
    public class DealerRailFacilitiesController(IGenericRepository<DealerRailFacilities> repo) : GenericCrudController<DealerRailFacilities>(repo);

    [Route("api/[controller]")]
    public class DealerPortFacilitiesController(IGenericRepository<DealerPortFacilities> repo) : GenericCrudController<DealerPortFacilities>(repo);

    [Route("api/[controller]")]
    public class DealerMarketDetailController(IGenericRepository<DealerMarketDetail> repo) : GenericCrudController<DealerMarketDetail>(repo);

    [Route("api/[controller]")]
    public class DealerCompaniesOperatingInAreaController
        : GenericCrudController<DealerCompaniesOperatingInArea>
    {
        private readonly IGenericRepository<DealerCompaniesOperatingInArea> _repo;

        public DealerCompaniesOperatingInAreaController(
            IGenericRepository<DealerCompaniesOperatingInArea> repo) : base(repo)
        {
            _repo = repo;
        }

        [HttpGet("dealer/{dealerId}/has-greenstar")]
        public async Task<IActionResult> HasGreenStar(int dealerId)
        {
            if (dealerId <= 0)
                return BadRequest(false);

            var hasGreenStar = await _repo.ExistsAsync(x =>
                EF.Property<int>(x, "DealerId") == dealerId &&
                EF.Property<string>(x, "CompaniesOperating") != null &&
                EF.Property<string>(x, "CompaniesOperating").ToUpper() == "GREEN STAR");

            return Ok(hasGreenStar);
        }

    }

    [Route("api/[controller]")]
    public class DealerOwnershipInfoController(IGenericRepository<DealerOwnershipInfo> repo) : GenericCrudController<DealerOwnershipInfo>(repo);

    [Route("api/[controller]")]
    public class PartnerFamilyDetailsController(IGenericRepository<PartnerFamilyDetails> repo) : GenericCrudController<PartnerFamilyDetails>(repo);

    [Route("api/[controller]")]
    public class PartnerOccupationController(IGenericRepository<PartnerOccupation> repo) : GenericCrudController<PartnerOccupation>(repo);

    [Route("api/[controller]")]
    public class SalesPlanningController(IGenericRepository<SalesPlanningInDealerRegistration> repo) : GenericCrudController<SalesPlanningInDealerRegistration>(repo);

    [Route("api/[controller]")]
    public class DealerAssetBankController(IGenericRepository<DealerAssetBank> repo) : GenericCrudController<DealerAssetBank>(repo);

    [Route("api/[controller]")]
    public class DealerAssetLandController(IGenericRepository<DealerAssetLand> repo) : GenericCrudController<DealerAssetLand>(repo);

    [Route("api/[controller]")]
    public class DealerAssetBuildingController(IGenericRepository<DealerAssetBuilding> repo) : GenericCrudController<DealerAssetBuilding>(repo);

    [Route("api/[controller]")]
    public class DealerLoanLiabilitiesController(IGenericRepository<DealerLoanLiabilities> repo) : GenericCrudController<DealerLoanLiabilities>(repo);

    [Route("api/[controller]")]
    public class DealerCreditLimitProposalController(IGenericRepository<DealerCreditLimitProposal> repo) : GenericCrudController<DealerCreditLimitProposal>(repo);

    [Route("api/[controller]")]
    public class DealerCreditLimitSalesPerformanceController(IGenericRepository<DealerCreditLimitSalesPerformance> repo) : GenericCrudController<DealerCreditLimitSalesPerformance>(repo);

    [Route("api/[controller]")]
    public class DealerRegistrationDocumentsController(IGenericRepository<DealerRegistrationDocuments> repo) : GenericCrudController<DealerRegistrationDocuments>(repo);

    [Route("api/[controller]")]
    public class DealerApprovalHistoryController(IGenericRepository<DealerApprovalHistory> repo) : GenericCrudController<DealerApprovalHistory>(repo);

    [Route("api/[controller]")]
    public class CreditLimitHistoryController(IGenericRepository<CreditLimitHistory> repo) : GenericCrudController<CreditLimitHistory>(repo);


}