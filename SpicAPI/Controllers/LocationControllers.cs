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

        // For SpecialAdmin multi-state: returns all regions belonging to any of
        // the supplied states. stateIds is a comma-separated list.
        [HttpGet("byStates")]
        public async Task<IActionResult> GetByStates([FromQuery] string stateIds)
        {
            var ids = (stateIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var v) ? v : 0)
                .Where(v => v > 0)
                .ToHashSet();

            if (ids.Count == 0)
                return Ok(new List<Region>());

            var items = await _repo.GetAll().Where(x => ids.Contains(x.StateId)).ToListAsync();
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

        // For SpecialAdmin multi-region: returns all HQs belonging to any of the
        // supplied regions. regionIds is a comma-separated list.
        [HttpGet("byRegions")]
        public async Task<IActionResult> GetByRegions([FromQuery] string regionIds)
        {
            var ids = (regionIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var v) ? v : 0)
                .Where(v => v > 0)
                .ToHashSet();

            if (ids.Count == 0)
                return Ok(new List<Headquarter>());

            var items = await _repo.GetAll().Where(x => ids.Contains(x.RegionId)).ToListAsync();
            return Ok(items);
        }
    }
}
