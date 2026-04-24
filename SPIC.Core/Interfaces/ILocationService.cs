using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Interfaces
{
	public interface ILocationService
	{
		Task<List<Zone>> GetAllAsync();
		Task<Zone?> GetByIdAsync(int id);
		Task<Zone> CreateAsync(Zone zone);
		Task<Zone?> UpdateAsync(int id, Zone zone);
		Task<bool> DeleteAsync(int id);
	}
}
