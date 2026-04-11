using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Web.Services
{
	//public class LocationAPiService : ILocationApiService
	//{
	//	private readonly HttpClient _httpClient;

	//	public LocationAPiService(HttpClient httpClient)
	//	{
	//		_httpClient = httpClient;
	//	}

	//	public async Task<List<Zone>> GetZonesAsync()
	//	{
	//		var result = await _httpClient.GetFromJsonAsync<List<Zone>>("api/zone");
	//		return result ?? new List<Zone>();
	//	}

	//	public async Task<Zone?> GetZoneByIdAsync(int id)
	//	{
	//		return await _httpClient.GetFromJsonAsync<Zone>($"api/zone/{id}");
	//	}

	//	public async Task<Zone?> CreateZoneAsync(Zone zone)
	//	{
	//		var response = await _httpClient.PostAsJsonAsync("api/zone", zone);
	//		if (!response.IsSuccessStatusCode)
	//			return null;

	//		return await response.Content.ReadFromJsonAsync<Zone>();
	//	}

	//	public async Task<Zone?> UpdateZoneAsync(int id, Zone zone)
	//	{
	//		var response = await _httpClient.PutAsJsonAsync($"api/zone/{id}", zone);
	//		if (!response.IsSuccessStatusCode)
	//			return null;

	//		return await response.Content.ReadFromJsonAsync<Zone>();
	//	}

	//	public async Task<bool> DeleteZoneAsync(int id)
	//	{
	//		var response = await _httpClient.DeleteAsync($"api/zone/{id}");
	//		return response.IsSuccessStatusCode;
	//	}
	//}
}
