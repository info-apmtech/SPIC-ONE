using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace SPIC.MauiBlazorApp.Shared.Services
{
	public class LookupCacheService
	{
		private readonly HttpClient _http;
		public LookupCacheService(HttpClient http) => _http = http;

		public List<LocationItemDto> States { get; private set; } = new();
		public List<LocationItemDto> Districts { get; private set; } = new();
		public List<LocationItemDto> Regions { get; private set; } = new();
		public List<LocationItemDto> Headquarters { get; private set; } = new();
		public List<BankItemDto> Banks { get; private set; } = new();
		public List<FinancialYear> FinancialYears { get; private set; } = new();   // ← add this line

		private bool _loaded;
		private Task? _loadingTask;

		public Task EnsureLoadedAsync() =>
			_loaded ? Task.CompletedTask : (_loadingTask ??= LoadAllAsync());

		private async Task LoadAllAsync()
		{
			var statesTask = _http.GetFromJsonAsync<List<LocationItemDto>>("api/State/all");
			var districtsTask = _http.GetFromJsonAsync<List<LocationItemDto>>("api/District/all");
			var regionsTask = _http.GetFromJsonAsync<List<LocationItemDto>>("api/Region/all");
			var hqTask = _http.GetFromJsonAsync<List<LocationItemDto>>("api/Headquarter/all");
			var banksTask = _http.GetFromJsonAsync<List<BankItemDto>>("api/Bank/all");
			var fyTask = _http.GetFromJsonAsync<List<FinancialYear>>("api/FinancialYear/all");  // ← add

			await Task.WhenAll(statesTask, districtsTask, regionsTask, hqTask, banksTask, fyTask);     // ← add fyTask

			States         = statesTask.Result    ?? new();
			Districts      = districtsTask.Result ?? new();
			Regions        = regionsTask.Result   ?? new();
			Headquarters   = hqTask.Result        ?? new();
			Banks          = banksTask.Result     ?? new();
			FinancialYears = fyTask.Result        ?? new();   // ← add
			_loaded = true;
		}

		// Call this if reference data is ever edited and needs a forced reload
		public Task RefreshAsync()
		{
			_loaded = false;
			_loadingTask = null;
			return EnsureLoadedAsync();
		}
	}

	public class LocationItemDto
	{
		public int Id { get; set; }
		public string StateName { get; set; } = "";
		public string DistrictName { get; set; } = "";
		public string RegionName { get; set; } = "";
		public string HeadquarterName { get; set; } = "";
		public int StateId { get; set; }
		public int RegionId { get; set; }
	}

	public class BankItemDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public string IFSCPrefix { get; set; } = "";
	}
}
