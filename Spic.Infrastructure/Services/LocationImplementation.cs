using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
    public class LocationImplementation : ILocationService
    {
        private readonly IGenericRepository<Zone> _zoneRepo;

        public LocationImplementation(IGenericRepository<Zone> zoneRepo)
        {
            _zoneRepo = zoneRepo;
        }

        public async Task<List<Zone>> GetAllAsync()
        {
            // IQueryable — SQL filters, not in-memory
            return await _zoneRepo.GetAll()
                .OrderBy(z => z.ZoneName)
                .ToListAsync();
        }

        public async Task<List<Zone>> GetActiveAsync()
        {
            return await _zoneRepo.GetWhere(z => z.IsActive)
                .OrderBy(z => z.ZoneName)
                .ToListAsync();
        }

        public async Task<Zone?> GetByIdAsync(int id)
        {
            return await _zoneRepo.GetByIdAsync(id);
        }

        public async Task<Zone> CreateAsync(Zone zone)
        {
            return await _zoneRepo.CreateAsync(zone);
        }

        // No manual property mapping — PatchAsync handles it all
        public async Task<Zone?> UpdateAsync(int id, Zone zone)
        {
            return await _zoneRepo.PatchAsync(id, zone);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _zoneRepo.DeleteAsync(id);
        }
    }
}
