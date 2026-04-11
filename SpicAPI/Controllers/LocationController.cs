using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LocationController : ControllerBase
	{
		private readonly ILocationService _locationService;

		public LocationController(ILocationService locationService)
		{
			_locationService = locationService;
		}

		//[HttpGet]
		//public async Task<IActionResult> GetAll()
		//{
		//	var zones = await _zoneService.GetAllAsync();
		//	return Ok(zones);
		//}

		//[HttpGet("{id}")]
		//public async Task<IActionResult> GetById(int id)
		//{
		//	var zone = await _zoneService.GetByIdAsync(id);
		//	if (zone == null)
		//		return NotFound();

		//	return Ok(zone);
		//}

		//[HttpPost]
		//public async Task<IActionResult> Create([FromBody] Zone zone)
		//{
		//	var created = await _zoneService.CreateAsync(zone);
		//	return Ok(created);
		//}

		//[HttpPut("{id}")]
		//public async Task<IActionResult> Update(int id, [FromBody] Zone zone)
		//{
		//	var updated = await _zoneService.UpdateAsync(id, zone);
		//	if (updated == null)
		//		return NotFound();

		//	return Ok(updated);
		//}

		//[HttpDelete("{id}")]
		//public async Task<IActionResult> Delete(int id)
		//{
		//	var deleted = await _zoneService.DeleteAsync(id);
		//	if (!deleted)
		//		return NotFound();

		//	return Ok(new { message = "Deleted successfully" });
		//}
	}
}
