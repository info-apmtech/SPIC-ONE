using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    [Route("api/[controller]")]
    public class CropController(IGenericRepository<Crop> repo) : GenericCrudController<Crop>(repo);

    [Route("api/[controller]")]
    public class CompetitorController(IGenericRepository<Competitor> repo) : GenericCrudController<Competitor>(repo);

    [Route("api/[controller]")]
    public class SectorController(IGenericRepository<Sector> repo) : GenericCrudController<Sector>(repo);

    [Route("api/[controller]")]
    public class UnitController(IGenericRepository<Unit> repo) : GenericCrudController<Unit>(repo);

    [Route("api/[controller]")]
    public class CategoryController(IGenericRepository<Category> repo) : GenericCrudController<Category>(repo);

    [Route("api/[controller]")]
    public class ProductGroupController(IGenericRepository<ProductGroup> repo) : GenericCrudController<ProductGroup>(repo);

    [Route("api/[controller]")]
    public class ProductController(IGenericRepository<Product> repo) : GenericCrudController<Product>(repo);

    [Route("api/[controller]")]
    public class WarehouseController(IGenericRepository<Warehouse> repo) : GenericCrudController<Warehouse>(repo);

    [Route("api/[controller]")]
    public class RackPointController(IGenericRepository<RackPoint> repo) : GenericCrudController<RackPoint>(repo);

    [Route("api/[controller]")]
    public class PortController(IGenericRepository<Port> repo) : GenericCrudController<Port>(repo);

    [Route("api/[controller]")]
    public class BankController(IGenericRepository<Bank> repo) : GenericCrudController<Bank>(repo);

    [Route("api/[controller]")]
    public class FinancialYearController(IGenericRepository<FinancialYear> repo) : GenericCrudController<FinancialYear>(repo);

    [Route("api/[controller]")]
    public class RelationshipController(IGenericRepository<Relationship> repo) : GenericCrudController<Relationship>(repo);

    [Route("api/[controller]")]
    public class LyingWithMasterController(IGenericRepository<LyingWithMaster> repo) : GenericCrudController<LyingWithMaster>(repo);
}
