using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    [Route("api/[controller]")]
    public class ZoneController(IGenericRepository<Zone> repo) : GenericCrudController<Zone>(repo);

    [Route("api/[controller]")]
    public class StateController(IGenericRepository<State> repo) : GenericCrudController<State>(repo);

    [Route("api/[controller]")]
    public class DistrictController(IGenericRepository<District> repo) : GenericCrudController<District>(repo)
    {
        [HttpGet("byState/{stateId}")]
        public async Task<IActionResult> GetByState(int stateId)
        {
            var items = await _repo.GetAll().Where(x => x.StateId == stateId).ToListAsync();
            return Ok(items);
        }
    }

    [Route("api/[controller]")]
    public class SubDistrictController(IGenericRepository<SubDistrict> repo) : GenericCrudController<SubDistrict>(repo)
    {
        [HttpGet("byDistrict/{districtId}")]
        public async Task<IActionResult> GetByDistrict(int districtId)
        {
            var items = await _repo.GetAll().Where(x => x.DistrictId == districtId).ToListAsync();
            return Ok(items);
        }
    }

    [Route("api/[controller]")]
    public class RegionController(IGenericRepository<Region> repo) : GenericCrudController<Region>(repo)
    {
        [HttpGet("byState/{stateId}")]
        public async Task<IActionResult> GetByState(int stateId)
        {
            var items = await _repo.GetAll().Where(x => x.StateId == stateId).ToListAsync();
            return Ok(items);
        }
    }

    [Route("api/[controller]")]
    public class HeadquarterController(IGenericRepository<Headquarter> repo) : GenericCrudController<Headquarter>(repo)
    {
        [HttpGet("byRegion/{regionId}")]
        public async Task<IActionResult> GetByRegion(int regionId)
        {
            var items = await _repo.GetAll().Where(x => x.RegionId == regionId).ToListAsync();
            return Ok(items);
        }
    }
}
