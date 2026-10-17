using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    // Existing route names and endpoints are intentionally unchanged.
    [Route("api/[controller]")]
    public class ZoneController(IGenericRepository<Zone> repo, AppDbContext db)
        : LocationCrudController<Zone>(repo, db, "ZoneId");

    [Route("api/[controller]")]
    public class StateController(IGenericRepository<State> repo, AppDbContext db)
        : LocationCrudController<State>(repo, db, "StateId");

    [Route("api/[controller]")]
    public class DistrictController(IGenericRepository<District> repo, AppDbContext db)
        : LocationCrudController<District>(repo, db, "DistrictId")
    {
        [HttpGet("byState/{stateId}")]
        public async Task<IActionResult> GetByState(int stateId)
        {
            var items = await _repo.GetAll().Where(x => x.StateId == stateId).ToListAsync();
            return Ok(items);
        }
    }

    [Route("api/[controller]")]
    public class SubDistrictController(IGenericRepository<SubDistrict> repo, AppDbContext db)
        : LocationCrudController<SubDistrict>(repo, db, "SubDistrictId")
    {
        [HttpGet("byDistrict/{districtId}")]
        public async Task<IActionResult> GetByDistrict(int districtId)
        {
            var items = await _repo.GetAll().Where(x => x.DistrictId == districtId).ToListAsync();
            return Ok(items);
        }
    }

    [Route("api/[controller]")]
    public class RegionController(IGenericRepository<Region> repo, AppDbContext db)
        : LocationCrudController<Region>(repo, db, "RegionId", "Region")
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
    public class HeadquarterController(IGenericRepository<Headquarter> repo, AppDbContext db)
        : LocationCrudController<Headquarter>(repo, db,
            "HeadquarterId", "HeadquartersId", "HeadQuartersId", "HQ")
    {
        [HttpGet("byRegion/{regionId}")]
        public async Task<IActionResult> GetByRegion(int regionId)
        {
            var items = await _repo.GetAll().Where(x => x.RegionId == regionId).ToListAsync();
            return Ok(items);
        }

        // For SpecialAdmin multi-region: returns all HQs belonging to any of
        // the supplied regions. regionIds is a comma-separated list.
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
