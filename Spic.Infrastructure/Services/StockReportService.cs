// ============================================================================
//  StockReportService (implementation)
//  Location: Spic.Infrastructure/Services/  (create a Services folder if needed)
//
//  Implements IStockReportService. Reads WholesalerStockAsOnToday and builds
//  every dashboard widget from ONE filtered IQueryable.
//
//  BEFORE IT COMPILES, change two things:
//   1. `AppDbContext` -> your actual DbContext type (in Spic.Infrastructure).
//   2. The `using` lines below to match YOUR namespaces:
//        - SPIC.Core.DTOs        (where StockReportDtos.cs lives)
//        - SPIC.Core.Interfaces  (where IStockReportService.cs lives)
//        - SPIC.Core.Entities    (WholesalerStockAsOnToday, State, Product, DealershipNature,
//                                 DealerRegistration, IfmsDealer)
//        - the namespace of your DbContext
//
//  PostgreSQL / Npgsql notes:
//   * No DateDiff - ageing is expressed as StockDate vs date thresholds, which
//     Npgsql translates cleanly. The exact day number is computed in memory,
//     only AFTER paging (grid) or over a single lightweight date column.
//   * EF.Functions.ILike = case-insensitive search on Postgres.
//   * `today` is Kind=Utc, matching Npgsql's default DateTime -> timestamptz.
//     If StockDate is `timestamp WITHOUT time zone`, change Today() to
//     DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified).
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
// using Spic.Infrastructure.Data;   // <-- your DbContext namespace

namespace Spic.Infrastructure.Services
{
	public class StockReportService : IStockReportService
	{
		private readonly AppDbContext _db;   // <-- rename to your DbContext type

		public StockReportService(AppDbContext db) => _db = db;

		// ---- Status / ageing thresholds (move to a config table if you want them editable) ----
		private const int FreshMax = 7;        // 0-7    => Fresh
		private const int MediumMax = 30;       // 8-30   => Medium
		private const int SlowMax = 60;         // 31-60  => Slow Moving
												// > 60   => Dead Stock
		private const int HighAgeingDays = 180; // "High Ageing Stock" KPI = older than 6 months

		private static DateTime Today() => DateTime.UtcNow.Date;

		public async Task<StockDashboardDto> GetDashboardAsync(StockReportFilter f)
		{
			var today = Today();
			var baseQuery = ApplyFilters(_db.Set<WholesalerStockAsOnToday>().AsNoTracking(), f, today);

			// ---- TOTAL STOCK = sum of stock lying across all four sources ----
			// 1. Wholesaler stock (WholesalerStockAsOnToday.Stock)
			var wholesalerStock = await baseQuery.SumAsync(s => (decimal?)s.Stock) ?? 0m;

			// 2. Retailer stock (DptReport "Retailer Weekly Stock" -> ClosingBalance)
			var retailerStock = await ApplyDptFilters(_db.Set<DptReport>().AsNoTracking(), f)
				.SumAsync(x => (decimal?)x.ClosingBalance) ?? 0m;

			// 3. State-level global stock reconciliation
			//    ASSUMPTION: the stock column is "ClosingStock". If your entity uses a
			//    different name (e.g. ClosingBalance / AvailableStock / Quantity), change
			//    it here - it will show as a compile error, not a runtime 500.
			var stateReconStock = await _db.Set<StateGlobalStockReconciliation>().AsNoTracking()
				.SumAsync(x => (decimal?)x.ClosingStock) ?? 0m;

			// 4. Warehouse/district-level global stock reconciliation (same assumption)
			var warehouseReconStock = await _db.Set<WarehouseDistrictGlobalStockReconciliation>().AsNoTracking()
				.SumAsync(x => (decimal?)x.ClosingStock) ?? 0m;

			var totalStock = wholesalerStock + retailerStock + stateReconStock + warehouseReconStock;

			// ---- Total-stock comparison: current date vs SAME date last year ----
			// Computed on the wholesaler snapshot (the only source with a per-row
			// StockDate). Needs last-year rows to exist in the table, otherwise 0%.
			var currentDateStock = await baseQuery
				.Where(s => s.StockDate == today)
				.SumAsync(s => (decimal?)s.Stock) ?? 0m;

			var lastYearStock = await baseQuery
				.Where(s => s.StockDate == today.AddYears(-1))
				.SumAsync(s => (decimal?)s.Stock) ?? 0m;

			decimal totalStockChangePct = lastYearStock == 0
				? 0m
				: Math.Round(((currentDateStock - lastYearStock) / lastYearStock) * 100m, 1);

			// Sequential: a single DbContext is not thread-safe, so we don't parallelise.
			var summary = await GetSummaryAsync(baseQuery, today, totalStock, totalStockChangePct);
			var stateWise = await GetStateWiseAsync(baseQuery, today);
			var productWise = await GetProductWiseAsync(baseQuery);
			var grid = await GetGridAsync(baseQuery, f, today);

			return new StockDashboardDto
			{
				Summary = summary,
				StateWise = stateWise,
				ProductWise = productWise,
				Grid = grid
			};
		}

		// ---- Filters for the retailer (DptReport) table. No ageing/date filter. ----
		private IQueryable<DptReport> ApplyDptFilters(IQueryable<DptReport> q, StockReportFilter f)
		{
			q = q.Where(x => x.ClosingBalance > 0);

			if (f.StateIds.Count > 0)
				q = q.Where(x => x.StateId.HasValue && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Count > 0)
				q = q.Where(x => x.DistrictId.HasValue && f.DistrictIds.Contains(x.DistrictId.Value));
			if (f.SubDistrictIds.Count > 0)
				q = q.Where(x => x.SubDistrictId.HasValue && f.SubDistrictIds.Contains(x.SubDistrictId.Value));
			if (f.ProductIds.Count > 0)
				q = q.Where(x => x.ProductId.HasValue && f.ProductIds.Contains(x.ProductId.Value));
			if (f.LyingWithIds.Count > 0)
				q = q.Where(x => x.DealershipNatureId.HasValue && f.LyingWithIds.Contains(x.DealershipNatureId.Value));

			return q;
		}

		// ---- Shared filter, applied to every widget ----
		private IQueryable<WholesalerStockAsOnToday> ApplyFilters(
			IQueryable<WholesalerStockAsOnToday> q, StockReportFilter f, DateTime today)
		{
			q = q.Where(s => s.Stock > 0);

			if (f.StateIds.Count > 0)
				q = q.Where(s => s.StateId.HasValue && f.StateIds.Contains(s.StateId.Value));
			if (f.DistrictIds.Count > 0)
				q = q.Where(s => s.DistrictId.HasValue && f.DistrictIds.Contains(s.DistrictId.Value));
			if (f.ProductIds.Count > 0)
				q = q.Where(s => s.ProductId.HasValue && f.ProductIds.Contains(s.ProductId.Value));
			// "Lying With" -> DealershipNature (Wholesaler / Retailer / etc). Confirm this mapping.
			if (f.LyingWithIds.Count > 0)
				q = q.Where(s => s.DealershipNatureId.HasValue && f.LyingWithIds.Contains(s.DealershipNatureId.Value));

			// Ageing ranges -> date windows (bools captured client-side so EF sees constants)
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

		// ---- Summary cards ----
		private async Task<SummaryDto> GetSummaryAsync(
			IQueryable<WholesalerStockAsOnToday> q, DateTime today,
			decimal totalStock, decimal totalStockChangePct)
		{
			// Distinct dealers overall + for today / yesterday, in one grouped round trip.
			var dealerBuckets = await q
				.Where(s => s.DealerRegistrationId != null)
				.GroupBy(s => s.DealerRegistrationId)
				.Select(g => new
				{
					DealerId = g.Key,
					IsToday = g.Any(x => x.StockDate >= today),
					IsYesterday = g.Any(x => x.StockDate >= today.AddDays(-1) && x.StockDate < today)
				})
				.ToListAsync();

			var dealerCount = dealerBuckets.Count;
			var todayDealers = dealerBuckets.Count(x => x.IsToday);
			var yesterdayDealers = dealerBuckets.Count(x => x.IsYesterday);
			var dealerDiff = todayDealers - yesterdayDealers;

			var highAgeingCount = await q
				.Where(s => s.StockDate <= today.AddDays(-HighAgeingDays))
				.CountAsync();

			// ---- Average Ageing ----
			// Ageing is measured from the ACK date (when the stock's status moved
			// new -> acknowledged). Right now we use StockDate as that anchor.
			// >>> If your ack date is a different column (e.g. AckDate / AckThroughDate
			//     on a sales/acknowledgement record), change `s.StockDate` below to it
			//     and add the join. Grouped so we transfer only (date, count) pairs. <<<
			var dateGroups = await q
				.GroupBy(s => s.StockDate)
				.Select(g => new { Date = g.Key, Count = g.Count() })
				.ToListAsync();

			double avgAgeing = 0;
			long totalRecords = dateGroups.Sum(x => (long)x.Count);
			if (totalRecords > 0)
			{
				double totalDays = dateGroups.Sum(x => (today - x.Date.Date).TotalDays * x.Count);
				avgAgeing = totalDays / totalRecords;
			}

			return new SummaryDto
			{
				// 1. Total Stock across all four sources (computed in GetDashboardAsync)
				TotalStock = totalStock,
				TotalStockChangePct = totalStockChangePct,   // current date vs same date last year

				// 2. Dealer count with today-vs-yesterday difference
				DealerCount = dealerCount,
				TodayDealerCount = todayDealers,
				YesterdayDealerCount = yesterdayDealers,

				// 3. Average ageing (from ack/stock date)
				AverageAgeing = Math.Round(avgAgeing, 0),
				AverageAgeingChange = 0,   // needs history to compute a delta

				// 4. High ageing = stock older than 6 months
				HighAgeingCount = highAgeingCount,
				HighAgeingChange = 0        // needs history to compute a delta
			};
		}

		// ---- State-wise bar chart: current year vs previous year (by StockDate.Year) ----
		private async Task<List<StateStockDto>> GetStateWiseAsync(IQueryable<WholesalerStockAsOnToday> q, DateTime today)
		{
			int cy = today.Year;

			var rows = await (
				from s in q
				join st in _db.Set<State>() on s.StateId equals st.Id
				group new { s.Stock, s.StockDate } by st.StateName into g
				select new StateStockDto
				{
					StateName = g.Key,
					CurrentYear = g.Where(x => x.StockDate.Year == cy).Sum(x => (decimal?)x.Stock) ?? 0m,
					PreviousYear = g.Where(x => x.StockDate.Year == cy - 1).Sum(x => (decimal?)x.Stock) ?? 0m
				}).ToListAsync();

			return rows.OrderByDescending(r => r.CurrentYear).ToList();
		}

		// ---- Product-wise donut ----
		private async Task<List<ProductStockDto>> GetProductWiseAsync(IQueryable<WholesalerStockAsOnToday> q)
		{
			var raw = await (
				from s in q
				join p in _db.Set<Product>() on s.ProductId equals p.Id
				group s by p.Name into g
				select new { Name = g.Key, Qty = g.Sum(x => (decimal?)x.Stock) ?? 0m }).ToListAsync();

			var total = raw.Sum(r => r.Qty);
			var palette = new[] { "#059669", "#f59e0b", "#6366f1", "#ef4444", "#0ea5e9", "#ec4899", "#14b8a6", "#f97316" };

			return raw.OrderByDescending(r => r.Qty).Select((r, i) => new ProductStockDto
			{
				ProductName = r.Name,
				Quantity = r.Qty,
				Percentage = total == 0 ? 0 : (double)(r.Qty / total) * 100,
				Color = palette[i % palette.Length]
			}).ToList();
		}

		// ---- Grid: server-side paging + sorting + search ----
		private async Task<PagedResult<StockRowDto>> GetGridAsync(
			IQueryable<WholesalerStockAsOnToday> q, StockReportFilter f, DateTime today)
		{
			// Phone numbers are joined in here:
			//   * DealerRegistration (via DealerRegistrationId) carries three numbers.
			//   * IfmsDealer (via IfmsDealerId) is the fallback for dealers that were
			//     never formally registered.
			// Both joins are LEFT joins, so a missing side just yields null.
			var projected =
				from s in q
				join st in _db.Set<State>() on s.StateId equals st.Id into stj
				from st in stj.DefaultIfEmpty()
				join p in _db.Set<Product>() on s.ProductId equals p.Id into pj
				from p in pj.DefaultIfEmpty()
				join dn in _db.Set<DealershipNature>() on s.DealershipNatureId equals dn.Id into dnj
				from dn in dnj.DefaultIfEmpty()
				join reg in _db.Set<DealerRegistration>() on s.DealerRegistrationId equals reg.Id into regj
				from reg in regj.DefaultIfEmpty()
				join ifd in _db.Set<IfmsDealer>() on s.IfmsDealerId equals ifd.Id into ifdj
				from ifd in ifdj.DefaultIfEmpty()
				select new GridRaw
				{
					DealerRegistrationId = s.DealerRegistrationId,
					StateName = st != null ? st.StateName : "",
					DealerName = s.AgencyName ?? "",
					ProductName = p != null ? p.Name : "",
					Quantity = s.Stock,
					LyingWith = dn != null ? dn.Name : "",
					StockDate = s.StockDate,

					// Registered dealer's three numbers.
					WhatsAppNumber = reg != null ? reg.WhatsAppNumber : null,
					OfficialContactNumber = reg != null ? reg.OfficialContactNumber : null,
					AlternativeNumber = reg != null ? reg.AlternativeNumber : null,

					// >>> ASSUMPTION: IfmsDealer's phone column is "MobileNo". If it is
					//     named differently (e.g. Mobile / PhoneNo / ContactNo), change it
					//     here - compile error, not a runtime 500. Remove this line if the
					//     entity has no phone at all. <<<
					//IfmsMobileNo = ifd != null ? ifd.MobileNo : null
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

			// For ageing: older StockDate = higher ageing, so sort direction is inverted vs the date.
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

			var items = pageRows.Select(x =>
			{
				int ageing = (int)Math.Floor((today - x.StockDate.Date).TotalDays);
				if (ageing < 0) ageing = 0;

				return new StockRowDto
				{
					DealerRegistrationId = x.DealerRegistrationId,
					StateName = x.StateName,
					DealerName = x.DealerName,
					ProductName = x.ProductName,
					Quantity = x.Quantity,
					LyingWith = x.LyingWith,
					AgeingDays = ageing,
					Status = MapStatus(ageing),

					// Registered dealer numbers (trimmed, null if blank).
					WhatsAppNumber = Blank(x.WhatsAppNumber),
					OfficialContactNumber = Blank(x.OfficialContactNumber),
					AlternativeNumber = Blank(x.AlternativeNumber),

					// Primary number used by the grid's single WhatsApp button:
					// first registered number, else the IFMS fallback.
					MobileNo = FirstNonBlank(
						x.WhatsAppNumber, x.OfficialContactNumber, x.AlternativeNumber, x.IfmsMobileNo)
				};
			}).ToList();

			return new PagedResult<StockRowDto>
			{
				Items = items,
				TotalCount = total,
				Page = f.Page,
				PageSize = f.PageSize
			};
		}

		// ---- All rows (no paging) - used by Excel / PDF export ----
		public async Task<List<StockRowDto>> GetAllRowsAsync(StockReportFilter f)
		{
			var today = Today();
			var q = ApplyFilters(_db.Set<WholesalerStockAsOnToday>().AsNoTracking(), f, today);

			var exportFilter = new StockReportFilter
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

		// Returns the trimmed value, or null if the string is blank.
		private static string? Blank(string? value)
			=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		// Returns the first non-blank (trimmed) value from the list, else null.
		private static string? FirstNonBlank(params string?[] values)
			=> values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

		private class GridRaw
		{
			public int? DealerRegistrationId { get; set; }
			public string StateName { get; set; } = "";
			public string DealerName { get; set; } = "";
			public string ProductName { get; set; } = "";
			public decimal Quantity { get; set; }
			public string LyingWith { get; set; } = "";
			public DateTime StockDate { get; set; }

			public string? WhatsAppNumber { get; set; }
			public string? OfficialContactNumber { get; set; }
			public string? AlternativeNumber { get; set; }
			public string? IfmsMobileNo { get; set; }
		}
	}
}
