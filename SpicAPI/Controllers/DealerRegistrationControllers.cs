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
		public DealerRegistrationController(IGenericRepository<DealerRegistration> repo): base(repo){}

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
            else if ((role == "MDO") && int.TryParse(hqClaim, out var hqId))
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
