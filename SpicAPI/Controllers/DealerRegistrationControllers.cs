using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{

    [Route("api/[controller]")]
    public class DealerRegistrationController(IGenericRepository<DealerRegistration> repo) : GenericCrudController<DealerRegistration>(repo);
    [Route("api/[controller]")]
    public class ExperienceController(IGenericRepository<Experience> repo) : GenericCrudController<Experience>(repo);

    [Route("api/[controller]")]
    public class AnnualSaleDataLastFYController(IGenericRepository<AnnualSaleDataLastFYofDealerRegistration> repo) : GenericCrudController<AnnualSaleDataLastFYofDealerRegistration>(repo);

    [Route("api/[controller]")]
    public class WarehouseFacilitiesController(IGenericRepository<WarehouseFacilities> repo) : GenericCrudController<WarehouseFacilities>(repo);

    [Route("api/[controller]")]
    public class RailFacilitiesController(IGenericRepository<RailFacilities> repo) : GenericCrudController<RailFacilities>(repo);

    [Route("api/[controller]")]
    public class PortFacilitiesController(IGenericRepository<PortFacilities> repo) : GenericCrudController<PortFacilities>(repo);

    [Route("api/[controller]")]
    public class MarketDetailController(IGenericRepository<MarketDetail> repo) : GenericCrudController<MarketDetail>(repo);

    [Route("api/[controller]")]
    public class CompaniesOperatingInAreaController(IGenericRepository<CompaniesOperatingInArea> repo) : GenericCrudController<CompaniesOperatingInArea>(repo);

    [Route("api/[controller]")]
    public class OwnerShipInfoController(IGenericRepository<OwnerShipInfo> repo) : GenericCrudController<OwnerShipInfo>(repo);

    [Route("api/[controller]")]
    public class PartnerFamilyDetailsController(IGenericRepository<PartnerFamilyDetails> repo) : GenericCrudController<PartnerFamilyDetails>(repo);

    [Route("api/[controller]")]
    public class PartnerOccupationController(IGenericRepository<PartnerOccupation> repo) : GenericCrudController<PartnerOccupation>(repo);

    [Route("api/[controller]")]
    public class SalesPlanningController(IGenericRepository<SalesPlanningInDealerRegistration> repo) : GenericCrudController<SalesPlanningInDealerRegistration>(repo);

    [Route("api/[controller]")]
    public class InvestmentController(IGenericRepository<Investment> repo) : GenericCrudController<Investment>(repo);

    [Route("api/[controller]")]
    public class DealerAssetBankController(IGenericRepository<DealerAssetBank> repo) : GenericCrudController<DealerAssetBank>(repo);

    [Route("api/[controller]")]
    public class DealerAssetLandController(IGenericRepository<DealerAssetLand> repo) : GenericCrudController<DealerAssetLand>(repo);

    [Route("api/[controller]")]
    public class BuildingController(IGenericRepository<Building> repo) : GenericCrudController<Building>(repo);

    [Route("api/[controller]")]
    public class MovableController(IGenericRepository<Movable> repo) : GenericCrudController<Movable>(repo);

    [Route("api/[controller]")]
    public class DealerInfrastructureController(IGenericRepository<DealerInfrastructure> repo) : GenericCrudController<DealerInfrastructure>(repo);

    [Route("api/[controller]")]
    public class LoanLiabilitiesController(IGenericRepository<LoanLiabilities> repo) : GenericCrudController<LoanLiabilities>(repo);

    [Route("api/[controller]")]
    public class CreditLimitProposalController(IGenericRepository<CreditLimitProposal> repo) : GenericCrudController<CreditLimitProposal>(repo);

    [Route("api/[controller]")]
    public class CreditLimitSalesPerformanceController(IGenericRepository<CreditLimitSalesPerformance> repo) : GenericCrudController<CreditLimitSalesPerformance>(repo);

    [Route("api/[controller]")]
    public class DealerRegistrationDocumentsController(IGenericRepository<DealerRegistrationDocuments> repo) : GenericCrudController<DealerRegistrationDocuments>(repo);
}
