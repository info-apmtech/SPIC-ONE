using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{

    [Route("api/[controller]")]
    public class DealerRegistrationController(IGenericRepository<DealerRegistration> repo) : GenericCrudController<DealerRegistration>(repo);
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
