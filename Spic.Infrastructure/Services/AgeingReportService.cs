// ============================================================================
//  AgeingReportService  — Spic.Infrastructure/Services/ (beside StockReportService)
//
//  Reads WholesalerStockAsOnToday (same table StockReport uses) and builds the
//  ageing dashboard: KPI cards, "Ageing by State" bar, day-bucket donut, grid.
//
//  Ageing = (today - StockDate). Status thresholds match StockReport.
//  Property names (State.StateName, Product.Name, DealershipNature.Name,
//  WholesalerStockAsOnToday.Stock/StockDate/...) are taken from your
//  StockReportService, so this should compile against the same entities.
// ============================================================================

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
	public class AgeingReportService : IAgeingReportService
	{
		private readonly AppDbContext _db;
		public AgeingReportService(AppDbContext db) => _db = db;

		// Status thresholds (same as StockReport)
		private const int FreshMax = 7;    // 0-7   => Fresh
		private const int MediumMax = 30;  // 8-30  => Medium
		private const int SlowMax = 60;    // 31-60 => Slow Moving
										   // > 60  => Dead Stock

		private static DateTime Today() => DateTime.UtcNow.Date;

		public async Task<AgeingDashboardDto> GetDashboardAsync(AgeingReportFilter f)
		{
			var today = Today();
			var baseQuery = ApplyFilters(_db.Set<WholesalerStockAsOnToday>().AsNoTracking(), f, today);

			var summary = await GetSummaryAsync(baseQuery, today);
			var buckets = await GetDayBucketsAsync(baseQuery, today);
			var grid = await GetGridAsync(baseQuery, f, today);

			return new AgeingDashboardDto
			{
				Summary    = summary,
				StateWise  = new(),   // no longer used — both charts are ageing-by-days now
				DayBuckets = buckets,
				Grid       = grid
			};
		}

		// ---- Shared filter (mirrors StockReportService.ApplyFilters) ----
		private IQueryable<WholesalerStockAsOnToday> ApplyFilters(
			IQueryable<WholesalerStockAsOnToday> q, AgeingReportFilter f, DateTime today)
		{
			q = q.Where(s => s.Stock > 0);

			if (f.StateIds.Count > 0)
				q = q.Where(s => s.StateId.HasValue && f.StateIds.Contains(s.StateId.Value));
			if (f.DistrictIds.Count > 0)
				q = q.Where(s => s.DistrictId.HasValue && f.DistrictIds.Contains(s.DistrictId.Value));
			if (f.ProductIds.Count > 0)
				q = q.Where(s => s.ProductId.HasValue && f.ProductIds.Contains(s.ProductId.Value));
			if (f.LyingWithIds.Count > 0)
				q = q.Where(s => s.DealershipNatureId.HasValue && f.LyingWithIds.Contains(s.DealershipNatureId.Value));

			if (f.AgeingRanges.Count > 0)
			{
				bool r030 = f.AgeingRanges.Contains("0-30");
				bool r3160 = f.AgeingRanges.Contains("31-60");
				bool r6190 = f.AgeingRanges.Contains("61-90");
				bool r91120 = f.AgeingRanges.Contains("91-120");
				bool rAbove = f.AgeingRanges.Contains("Above 120");

				var d30 = today.AddDays(-30);
				var d60 = today.AddDays(-60);
				var d90 = today.AddDays(-90);
				var d120 = today.AddDays(-120);

				q = q.Where(s =>
					(r030 && s.StockDate > d30) ||
					(r3160 && s.StockDate <= d30 && s.StockDate > d60) ||
					(r6190 && s.StockDate <= d60 && s.StockDate > d90) ||
					(r91120 && s.StockDate <= d90 && s.StockDate > d120) ||
					(rAbove && s.StockDate <= d120));
			}

			return q;
		}

		// ---- KPI cards ----
		private async Task<AgeingSummaryDto> GetSummaryAsync(
			IQueryable<WholesalerStockAsOnToday> q, DateTime today)
		{
			var totalStock = await q.SumAsync(s => (decimal?)s.Stock) ?? 0m;

			// One grouped round trip: (date, stock, count). Ageing computed in memory.
			var dateGroups = await q
				.GroupBy(s => s.StockDate)
				.Select(g => new { Date = g.Key, Stock = g.Sum(x => x.Stock), Count = g.Count() })
				.ToListAsync();

			double weightedDays = 0;
			long totalRows = 0;
			decimal stock30to60 = 0, stock60plus = 0;

			foreach (var d in dateGroups)
			{
				var ageing = (int)Math.Floor((today - d.Date.Date).TotalDays);
				if (ageing < 0) ageing = 0;

				weightedDays += (double)ageing * d.Count;
				totalRows += d.Count;

				if (ageing > 30 && ageing <= 60) stock30to60 += d.Stock;
				if (ageing > 60) stock60plus += d.Stock;
			}

			return new AgeingSummaryDto
			{
				TotalStock = totalStock,
				TotalStockChangePct = 0m,                        // needs history to compute
				AverageAgeing = totalRows > 0 ? Math.Round(weightedDays / totalRows, 1) : 0,
				AverageAgeingChange = 0,
				Stock30To60 = stock30to60,
				Stock30To60ChangePct = 0m,
				Stock60Plus = stock60plus,
				Stock60PlusChangePct = 0m
			};
		}

		// ---- Donut + table: stock by ageing-day bucket ----
		private async Task<List<AgeingBucketDto>> GetDayBucketsAsync(
			IQueryable<WholesalerStockAsOnToday> q, DateTime today)
		{
			var dateGroups = await q
				.GroupBy(s => s.StockDate)
				.Select(g => new { Date = g.Key, Stock = g.Sum(x => x.Stock) })
				.ToListAsync();

			// buckets: 0-30, 30-90, 90-180, 180-365, 365+
			decimal b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0;
			foreach (var d in dateGroups)
			{
				var age = (int)Math.Floor((today - d.Date.Date).TotalDays);
				if (age < 0) age = 0;
				if (age <= 30) b0 += d.Stock;
				else if (age <= 90) b1 += d.Stock;
				else if (age <= 180) b2 += d.Stock;
				else if (age <= 365) b3 += d.Stock;
				else b4 += d.Stock;
			}

			var total = b0 + b1 + b2 + b3 + b4;
			double Pct(decimal v) => total == 0 ? 0 : Math.Round((double)(v / total) * 100, 1);

			return new List<AgeingBucketDto>
			{
				new() { Label = "0 - 30 Days",    Category = "Fresh",       Stock = b0, Percentage = Pct(b0), Color = "#059669" },
				new() { Label = "30 - 90 Days",   Category = "Medium",      Stock = b1, Percentage = Pct(b1), Color = "#34d399" },
				new() { Label = "90 - 180 Days",  Category = "Slow Moving", Stock = b2, Percentage = Pct(b2), Color = "#f59e0b" },
				new() { Label = "180 - 365 Days", Category = "Long Aged",   Stock = b3, Percentage = Pct(b3), Color = "#ef4444" },
				new() { Label = "365+ Days",      Category = "Critical",    Stock = b4, Percentage = Pct(b4), Color = "#b91c1c" }
			};
		}

		// ---- Grid: paging + sorting + search ----
		private async Task<PagedResult<AgeingRowDto>> GetGridAsync(
			IQueryable<WholesalerStockAsOnToday> q, AgeingReportFilter f, DateTime today)
		{
			var projected =
				from s in q
				join st in _db.Set<State>() on s.StateId equals st.Id into stj
				from st in stj.DefaultIfEmpty()
				join p in _db.Set<Product>() on s.ProductId equals p.Id into pj
				from p in pj.DefaultIfEmpty()
				join d in _db.Set<District>() on s.DistrictId equals d.Id into dj
				from d in dj.DefaultIfEmpty()
				join dr in _db.Set<DealerRegistration>() on s.DealerRegistrationId equals dr.Id into drj
				from dr in drj.DefaultIfEmpty()
				select new GridRaw
				{
					DealerRegistrationId = s.DealerRegistrationId,
					StateName = st != null ? st.StateName : "",
					DistrictName = d != null ? d.DistrictName : "",
					DealerName = !string.IsNullOrWhiteSpace(s.AgencyName)
						? s.AgencyName
						: (dr != null ? dr.FirmName : ""),
					DealerCode = dr != null ? dr.DealerCode : null,
					MobileNo = null,            // TODO: confirm DealerRegistration.MobileNo
					HeadquarterId = null,  // TODO: confirm DealerRegistration.HeadquarterId
										   //SubDistrictId = dr != null ? dr.SubDistrictId : null,  // TODO: confirm DealerRegistration.SubDistrictId
					ProductName = p != null ? p.Name : "",
					Quantity = s.Stock,
					StockDate = s.StockDate
				};

			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var term = $"%{f.Search.Trim()}%";
				projected = projected.Where(x =>
					EF.Functions.ILike(x.DealerName, term) ||
					EF.Functions.ILike(x.StateName, term) ||
					EF.Functions.ILike(x.ProductName, term));
			}

			var total = await projected.CountAsync();

			// Older StockDate = higher ageing, so "ageing" sort inverts the date.
			projected = (f.SortColumn?.ToLowerInvariant(), f.SortDir?.ToLowerInvariant()) switch
			{
				("dealer", "desc") => projected.OrderByDescending(x => x.DealerName),
				("dealer", _) => projected.OrderBy(x => x.DealerName),
				("product", "desc") => projected.OrderByDescending(x => x.ProductName),
				("product", _) => projected.OrderBy(x => x.ProductName),
				("quantity", "desc") => projected.OrderByDescending(x => x.Quantity),
				("quantity", _) => projected.OrderBy(x => x.Quantity),
				("ageing", "desc") => projected.OrderBy(x => x.StockDate),
				("ageing", _) => projected.OrderByDescending(x => x.StockDate),
				("state", "desc") => projected.OrderByDescending(x => x.StateName),
				_ => projected.OrderBy(x => x.StateName)
			};

			var pageRows = await projected
				.Skip((f.Page - 1) * f.PageSize)
				.Take(f.PageSize)
				.ToListAsync();

			// Resolve Head Quarter / Sub-District names for this page (masters are small).
			// TODO: confirm entity types Headquarter / SubDistrict and their name properties.
			var hqIds = pageRows.Where(x => x.HeadquarterId.HasValue).Select(x => x.HeadquarterId!.Value).Distinct().ToList();
			var sdIds = pageRows.Where(x => x.SubDistrictId.HasValue).Select(x => x.SubDistrictId!.Value).Distinct().ToList();

			var hqNames = hqIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<Headquarter>().Where(h => hqIds.Contains(h.Id))
					.ToDictionaryAsync(h => h.Id, h => h.HeadquarterName);
			var sdNames = sdIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<SubDistrict>().Where(x => sdIds.Contains(x.Id))
					.ToDictionaryAsync(x => x.Id, x => x.SubDistrictName);

			var items = pageRows.Select(x =>
			{
				int ageing = (int)Math.Floor((today - x.StockDate.Date).TotalDays);
				if (ageing < 0) ageing = 0;
				return new AgeingRowDto
				{
					DealerRegistrationId = x.DealerRegistrationId,
					StateName = x.StateName,
					DistrictName = x.DistrictName,
					HeadQuarterName = x.HeadquarterId.HasValue && hqNames.TryGetValue(x.HeadquarterId.Value, out var hqn) ? hqn : null,
					SubDistrictName = x.SubDistrictId.HasValue && sdNames.TryGetValue(x.SubDistrictId.Value, out var sdn) ? sdn : null,
					DealerName = x.DealerName,
					DealerCode = string.IsNullOrWhiteSpace(x.DealerCode) ? x.DealerRegistrationId?.ToString() : x.DealerCode,
					MobileNo = x.MobileNo,
					ProductName = x.ProductName,
					Quantity = x.Quantity,
					EntryDate = x.StockDate,
					AgeingDays = ageing,
					Status = MapStatus(ageing)
				};
			}).ToList();

			return new PagedResult<AgeingRowDto>
			{
				Items = items,
				TotalCount = total,
				Page = f.Page,
				PageSize = f.PageSize
			};
		}

		// ---- All rows (no paging) for export ----
		public async Task<List<AgeingRowDto>> GetAllRowsAsync(AgeingReportFilter f)
		{
			var today = Today();
			var q = ApplyFilters(_db.Set<WholesalerStockAsOnToday>().AsNoTracking(), f, today);

			var exportFilter = new AgeingReportFilter
			{
				StateIds = f.StateIds,
				DistrictIds = f.DistrictIds,
				ProductIds = f.ProductIds,
				LyingWithIds = f.LyingWithIds,
				AgeingRanges = f.AgeingRanges,
				Search = f.Search,
				SortColumn = f.SortColumn,
				SortDir = f.SortDir,
				Page = 1,
				PageSize = int.MaxValue
			};

			var grid = await GetGridAsync(q, exportFilter, today);
			return grid.Items;
		}

		private static string MapStatus(int days) => days switch
		{
			<= FreshMax => "Fresh",
			<= MediumMax => "Medium",
			<= SlowMax => "Slow Moving",
			_ => "Dead Stock"
		};

		private class GridRaw
		{
			public int? DealerRegistrationId { get; set; }
			public string StateName { get; set; } = "";
			public string DistrictName { get; set; } = "";
			public string DealerName { get; set; } = "";
			public string? DealerCode { get; set; }
			public string? MobileNo { get; set; }
			public int? HeadquarterId { get; set; }
			public int? SubDistrictId { get; set; }
			public string ProductName { get; set; } = "";
			public decimal Quantity { get; set; }
			public DateTime StockDate { get; set; }
		}
	}
}