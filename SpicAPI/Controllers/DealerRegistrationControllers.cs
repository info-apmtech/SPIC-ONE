using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;
using static System.Net.WebRequestMethods;
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
        private readonly IGenericRepository<UserInfo> _userInfoRepo;
        // Single constructor: optional repositories are injected when registered. Defaults to null to avoid breaking DI.
        public DealerRegistrationController(
            IGenericRepository<DealerRegistration> repo,
            IGenericRepository<UserInfo> userInfoRepo,
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
            IGenericRepository<DealerRegistrationDocuments>? docsRepo = null
            ) : base(repo)
        {
            _historyRepo = historyRepo;
            _userInfoRepo = userInfoRepo;
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
            if (role == "RM" && int.TryParse(regionClaim, out var regionId))
                query = query.Where(x => x.Region == regionId);
            else if ((role == "SM") && int.TryParse(stateClaim, out var stateId))
                query = query.Where(x => x.StateId == stateId);
            else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(stateClaim, out var moStateId))
                query = query.Where(x => x.StateId == moStateId);
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
                if (role == "RM" && int.TryParse(regionClaim, out var regionId))
                    query = query.Where(x => x.Region == regionId);
                else if (role == "SM" && int.TryParse(stateClaim, out var stateId))
                    query = query.Where(x => x.StateId == stateId);
                else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(stateClaim, out var moStateId))
                    query = query.Where(x => x.StateId == moStateId);
                else
                    query = query.Where(x => x.CreatedBy == userId);
            }

            var lookup = await query
                .Select(x => new { x.Id, x.DealerCode, x.FirmName, x.StateId })
                .ToListAsync();

            return Ok(lookup);
        }

        /// <summary>
        /// Returns a per-step completion summary for the given dealer.
        /// This consolidates multiple client calls into a single API.
        /// The response is an array of objects with StepNo (1-based) and IsComplete boolean.
        /// </summary>
        [HttpGet("{dealerId}/step-completion-summary")]
        public async Task<IActionResult> GetStepCompletionSummary(int dealerId)
        {
            var result = new List<object>();

            var dealer = await _repo.GetByIdAsync(dealerId);

            // Step 1: Dealer basic info (DealerRegistration exists and has PinCode)
            bool step1 = dealer != null && !string.IsNullOrWhiteSpace(dealer.PinCode);
            result.Add(new { StepNo = 1, IsComplete = step1 });

            // Step 2: DealerExperience
            bool step2 = _expRepo != null && await _expRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 2, IsComplete = step2 });

            // Step 3: AnnualSaleDataLastFYofDealerRegistration
            bool step3 = _annualRepo != null && await _annualRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 3, IsComplete = step3 });

            // Step 4: Warehouse facilities (warehouse + rail + port combined)
            bool wh = _whRepo != null && await _whRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool rail = _railRepo != null && await _railRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool port = _portRepo != null && await _portRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool step4 = wh || rail || port;
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

            // Step 8: Sales planning
            bool step8 = _salesPlanRepo != null && await _salesPlanRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 8, IsComplete = step8 });

            // Step 9: Assets (bank/land/building)
            bool bank = _bankRepo != null && await _bankRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool land = _landRepo != null && await _landRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool building = _buildingRepo != null && await _buildingRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool step9 = bank || land || building;
            result.Add(new { StepNo = 9, IsComplete = step9 });

            // Step 10: Credit limit proposal
            bool credit = _creditRepo != null && await _creditRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            bool step10 = credit;
            result.Add(new { StepNo = 10, IsComplete = step10 });

            // Step 11: Credit limit for GreenStar (same check as step 10)
            bool step11 = credit;
            result.Add(new { StepNo = 11, IsComplete = step11 });

            // Step 12: Documents
            bool step12 = _docsRepo != null && await _docsRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
            result.Add(new { StepNo = 12, IsComplete = step12 });

            // Step 13: Final submission (dealer record exists)
            bool step13 = dealer != null;
            result.Add(new { StepNo = 13, IsComplete = step13 });

            return Ok(result);
        }
        [HttpPut("update-with-user/{id}")]
        public async Task<IActionResult> UpdateDealerWithUser(int id, [FromBody] DealerRegistration dealer)
        {
            if (id != dealer.Id) return BadRequest("ID mismatch");

            if (string.IsNullOrEmpty(dealer.UserTableId) && !string.IsNullOrEmpty(dealer.DealerCode))
            {
                // 1. Check if the user ALREADY EXISTS in the database by their DealerCode
                var existingAppUser = await _userInfoRepo.GetAll()
                    .FirstOrDefaultAsync(u => u.NormalizedUserName == dealer.DealerCode.ToUpper());

                if (existingAppUser != null)
                {
                    // 2. The account already exists! Just link the existing ID to the dealer.
                    dealer.UserTableId = existingAppUser.Id;
                }
                else
                {
                    // 3. The account truly doesn't exist. Create it safely.
                    var newUserId = Guid.NewGuid().ToString();
                    var phonePass = dealer.OfficialContactNumber ?? "1234567890";

                    var newUser = new UserInfo
                    {
                        Id = newUserId,
                        UserName = dealer.DealerCode,
                        NormalizedUserName = dealer.DealerCode.ToUpper(),
                        PhoneNumber = dealer.OfficialContactNumber,
                        Password = phonePass,
                        Name = dealer.FirmName,
                        Role = (SPIC.Core.Entities.AppRole)11,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System",
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System"
                    };

                    await _userInfoRepo.CreateAsync(newUser);
                    dealer.UserTableId = newUserId;
                }
            }

            // Save the DealerRegistration changes
            await _repo.UpdateAsync(dealer);

            return Ok(dealer);
        }
        [HttpPost("{id}/send-back")]
        public async Task<IActionResult> SendBack(int id, [FromBody] DealerSendBackRequest request)
        {
            var dealer = await _repo.GetByIdAsync(id);
            if (dealer == null) return NotFound();

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            // Record history entry
            var remarks = new System.Text.StringBuilder();
            remarks.AppendLine($"Reason: {request.Reason}");
            remarks.AppendLine($"Priority: {request.Priority}");
            if (request.Sections != null && request.Sections.Any())
                remarks.AppendLine($"Sections: {string.Join(", ", request.Sections)}");
            if (!string.IsNullOrWhiteSpace(request.TargetRole))
                remarks.AppendLine($"TargetRole: {request.TargetRole}");

            var history = new DealerApprovalHistory
            {
                DealerId = id,
                ApprovedBy = userId,
                Role = role,
                ApprovedAt = DateTime.Now,
                Remarks = remarks.ToString()
            };

            if (_historyRepo != null)
            {
                await _historyRepo.CreateAsync(history);
            }

            // Update dealer approval flags depending on the caller role
            if (role == "RM" || role == "RMD")
                dealer.RMApproved = false;
            else if (role == "SMD" || role == "SMM")
                dealer.SMApproved = false;
            else if (role == "AVP" || role == "CorporateAdmin" || role == "Admin")
                dealer.AVPApproved = false;

            await _repo.PatchAsync(id, dealer);

            return Ok(new { message = "Send back recorded" });
        }

        public class DealerSendBackRequest
        {
            public int DealerId { get; set; }
            public string Reason { get; set; } = "";
            public string Priority { get; set; } = "High";
            public List<string> Sections { get; set; } = new List<string>();
            public string? TargetRole { get; set; }
        }

        /// <summary>
        /// Returns only dealers that have submitted their basic info (PinCode is present).
        /// Used by the Dashboard — drafts without a PinCode are excluded.
        /// </summary>
        [HttpGet("submitted")]
        public async Task<IActionResult> GetSubmitted()
        {
            var query = _repo.GetAllWithInactive()
                .Where(x => x.PinCode != null && x.PinCode != "");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var regionClaim = User.FindFirst("spic:region_id")?.Value;
            var stateClaim = User.FindFirst("spic:state_id")?.Value;
            var hqClaim = User.FindFirst("spic:hq_id")?.Value;

            if (role == "Admin" || role == "CorporateAdmin" || role == "Director" || role == "AVP")
                return Ok(await query.ToListAsync());
            if ((role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId) && stateId > 0)
                query = query.Where(x => x.StateId == stateId);
            else if ((role == "RM" || role == "RMD") && int.TryParse(regionClaim, out var regionId) && regionId > 0)
                query = query.Where(x => x.Region == regionId);
            else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(hqClaim, out var hqId) && hqId > 0)
                query = query.Where(x => x.HQ == hqId);
            else
                query = query.Where(x => x.CreatedBy == userId);

            return Ok(await query.ToListAsync());
        }
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


}
