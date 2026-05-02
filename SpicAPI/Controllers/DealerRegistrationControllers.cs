using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace SpicAPI.Controllers
{

    [Route("api/[controller]")]
    public class DealerRegistrationController : GenericCrudController<DealerRegistration>
	{
		private readonly IGenericRepository<DealerApprovalHistory>? _historyRepo;

		// Single constructor: historyRepo is optional so existing DI registrations (without historyRepo)
		// continue to work. If historyRepo is registered it will be injected automatically.
		public DealerRegistrationController(IGenericRepository<DealerRegistration> repo, IGenericRepository<DealerApprovalHistory>? historyRepo = null) : base(repo)
		{
			_historyRepo = historyRepo;
		}

		[HttpGet("all")]
		public override async Task<IActionResult> GetAllWithInactive()
		{
			var query = _repo.GetAllWithInactive();

			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var regionClaim = User.FindFirst("RegionId")?.Value;
			var stateClaim = User.FindFirst("StateId")?.Value;
			var hqClaim = User.FindFirst("HQId")?.Value;

			// Admin / CorporateAdmin → full data
			if (role == "Admin" || role == "CorporateAdmin")
				return Ok(await query.ToListAsync());
			if (role == "RM" && int.TryParse(regionClaim, out var regionId))
				query = query.Where(x => x.Region == regionId);
			else if ((role == "SM") && int.TryParse(stateClaim, out var stateId))
				query = query.Where(x => x.StateId == stateId);
			else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(hqClaim, out var hqId))
				query = query.Where(x => x.HQ == hqId);
			else
				query = query.Where(x => x.CreatedBy == userId);
			return Ok(await query.ToListAsync());
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
				Role = request.TargetRole ?? role,
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
    public class DealerCompaniesOperatingInAreaController(IGenericRepository<DealerCompaniesOperatingInArea> repo) : GenericCrudController<DealerCompaniesOperatingInArea>(repo);

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
