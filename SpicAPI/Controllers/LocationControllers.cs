using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    [Route("api/[controller]")]
    public class ZoneController(IGenericRepository<Zone> repo) : GenericCrudController<Zone>(repo);

    [Route("api/[controller]")]
    public class StateController(IGenericRepository<State> repo) : GenericCrudController<State>(repo);

    [Route("api/[controller]")]
    public class DistrictController(IGenericRepository<District> repo) : GenericCrudController<District>(repo);

    [Route("api/[controller]")]
    public class SubDistrictController(IGenericRepository<SubDistrict> repo) : GenericCrudController<SubDistrict>(repo);
}
