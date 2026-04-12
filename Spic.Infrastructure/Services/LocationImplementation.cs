using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spic.Infrastructure.Services
{
	public class LocationImplementation //: ILocationService
	{
	//	private static readonly List<Zone> _zones = new()
	//{
	//	new Zone
	//	{
	//		Id = 1,
	//		ZoneName = "North Zone",
	//		ZoneCode = "NZ001",
	//		Status = true,
	//		CreatedAt = DateTime.Now
	//	},
	//	new Zone
	//	{
	//		Id = 2,
	//		ZoneName = "South Zone",
	//		ZoneCode = "SZ001",
	//		Status = true,
	//		CreatedAt = DateTime.Now
	//	}
	//};

		//public Task<List<Zone>> GetAllAsync()
		//{
		//	return Task.FromResult(_zones.ToList());
		//}

		//public Task<Zone?> GetByIdAsync(int id)
		//{
		//	var zone = _zones.FirstOrDefault(x => x.Id == id);
		//	return Task.FromResult(zone);
		//}

		//public Task<Zone> CreateAsync(Zone zone)
		//{
		//	zone.Id = _zones.Any() ? _zones.Max(x => x.Id) + 1 : 1;
		//	zone.CreatedAt = DateTime.Now;
		//	_zones.Add(zone);
		//	return Task.FromResult(zone);
		//}

		//public Task<Zone?> UpdateAsync(int id, Zone zone)
		//{
		//	var existing = _zones.FirstOrDefault(x => x.Id == id);
		//	if (existing == null)
		//		return Task.FromResult<Zone?>(null);

		//	existing.ZoneName = zone.ZoneName;
		//	existing.ZoneCode = zone.ZoneCode;
		//	existing.Status = zone.Status;

		//	return Task.FromResult<Zone?>(existing);
		//}

		//public Task<bool> DeleteAsync(int id)
		//{
		//	var zone = _zones.FirstOrDefault(x => x.Id == id);
		//	if (zone == null)
		//		return Task.FromResult(false);

		//	_zones.Remove(zone);
		//	return Task.FromResult(true);
		//}
	}
}
