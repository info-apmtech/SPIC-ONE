// ============================================================================
//  StockDetailsService (implements IStockDetailsService)
//  Location: Spic.Infrastructure/Services/
//
//  Builds a state-wise ledger:
//    Opening + Supplies = Total Stock,  Total Stock - Total Sales = Closing.
//
//  BEFORE IT COMPILES: rename `AppDbContext` and match the `using` namespaces,
//  exactly as in StockReportService.
//
//  ---- DECISIONS TO CONFIRM (each isolated to one spot below) ----
//   [S1] STOCK SOURCE: Opening/Supplies come from StateGlobalStockReconciliation,
//        filtered to the selected month by CreatedAt. If the month should be
//        picked by a different column (or the table isn't monthly), change the
//        CreatedAt window in BuildStockAsync.
//   [S2] SUPPLIES = Receipt + ProductionImports. Drop ProductionImports (or add
//        other movement columns) in BuildStockAsync if your definition differs.
//   [S3] CLOSING is computed (Total Stock - Total Sales), not the native
//        ClosingStock column. Swap in MergeRow if you want the stored value.
//   [S4] SALES = QuantityMT from SalesWholesaler + SalesCompanySale, by InvoiceDate.
//        Change the Sum column (e.g. ReceivedQuantity) in BuildSales* if needed.
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;   // <-- your DbContext namespace
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Globalization;

namespace Spic.Infrastructure.Services
{
	public class StockDetailsService : IStockDetailsService
	{
		private readonly AppDbContext _db;   // <-- rename to your DbContext type

		public StockDetailsService(AppDbContext db) => _db = db;

		public async Task<StockDetailsDto> GetDashboardAsync(StockDetailsFilter f)
		{
			var today = DateTime.UtcNow.Date;

			// ---- Resolve the reporting window from From/To dates ----
			var rangeStart = f.DateFrom?.Date ?? new DateTime(today.Year, today.Month, 1);
			var rangeEnd = f.DateTo?.Date ?? today;
			if (rangeEnd < rangeStart) rangeEnd = rangeStart;

			var fromUtc = DateTime.SpecifyKind(rangeStart, DateTimeKind.Utc);   // window start / opening anchor
			var asOnStart = DateTime.SpecifyKind(rangeEnd, DateTimeKind.Utc);    // 00:00 of the "as on" (last) day
			var asOnNextDay = asOnStart.AddDays(1);                              // exclusive upper bound

			var stateIds = f.StateIds;

			// ---- [S1][S2] Stock: Opening / Supplies per state (whole window) ----
			var stock = await BuildStockAsync(stateIds, fromUtc, asOnNextDay);

			// ---- [S4] Sales per state, bucketed by InvoiceDate ----
			var salesW = await BuildSalesWholesalerAsync(stateIds, fromUtc, asOnStart, asOnNextDay);
			var salesC = await BuildSalesCompanyAsync(stateIds, fromUtc, asOnStart, asOnNextDay);

			// Merge sales buckets
			var sales = new Dictionary<int, (decimal Before, decimal OnDay)>();
			foreach (var s in salesW.Concat(salesC))
			{
				sales.TryGetValue(s.StateId, out var cur);
				sales[s.StateId] = (cur.Before + s.Before, cur.OnDay + s.OnDay);
			}

			// ---- State name lookup for the union of all involved states ----
			var stateNames = await _db.Set<State>().AsNoTracking()
				.Select(s => new { s.Id, s.StateName })
				.ToDictionaryAsync(s => s.Id, s => s.StateName);

			var allStateIds = new HashSet<int>(stock.Keys);
			allStateIds.UnionWith(sales.Keys);

			// ---- Build one row per state ----
			var rows = allStateIds.Select(id =>
			{
				stock.TryGetValue(id, out var st);
				sales.TryGetValue(id, out var sl);
				return MergeRow(id, stateNames.TryGetValue(id, out var nm) ? nm : "-", st.Opening, st.Supplies, sl.Before, sl.OnDay);
			}).ToList();

			// ---- Search (state name) ----
			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var term = f.Search.Trim();
				rows = rows.Where(r => r.StateName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
			}

			// ---- Grand total across all filtered states ----
			var grand = new StockDetailsRowDto
			{
				StateId = 0,
				StateName = "Grand Total",
				OpeningStock = rows.Sum(r => r.OpeningStock),
				Supplies = rows.Sum(r => r.Supplies),
				TotalStock = rows.Sum(r => r.TotalStock),
				SalesBefore = rows.Sum(r => r.SalesBefore),
				SalesOnDay = rows.Sum(r => r.SalesOnDay),
				TotalSales = rows.Sum(r => r.TotalSales),
				ClosingStock = rows.Sum(r => r.ClosingStock)
			};
			grand.SalesPct = grand.TotalStock == 0 ? 0 : (double)(grand.TotalSales / grand.TotalStock) * 100;

			// ---- KPI cards ----
			var summary = new StockDetailsSummaryDto
			{
				TotalStock = grand.TotalStock,
				TotalSales = grand.TotalSales,
				ClosingStock = grand.ClosingStock,
				SalesPct = grand.SalesPct
			};

			// ---- Sort ----
			rows = (f.SortColumn?.ToLowerInvariant(), (f.SortDir ?? "asc").ToLowerInvariant()) switch
			{
				("totalstock", "desc") => rows.OrderByDescending(r => r.TotalStock).ToList(),
				("totalstock", _) => rows.OrderBy(r => r.TotalStock).ToList(),
				("totalsales", "desc") => rows.OrderByDescending(r => r.TotalSales).ToList(),
				("totalsales", _) => rows.OrderBy(r => r.TotalSales).ToList(),
				("closing", "desc") => rows.OrderByDescending(r => r.ClosingStock).ToList(),
				("closing", _) => rows.OrderBy(r => r.ClosingStock).ToList(),
				("salespct", "desc") => rows.OrderByDescending(r => r.SalesPct).ToList(),
				("salespct", _) => rows.OrderBy(r => r.SalesPct).ToList(),
				("state", "desc") => rows.OrderByDescending(r => r.StateName).ToList(),
				_ => rows.OrderBy(r => r.StateName).ToList()
			};

			// ---- Page ----
			var totalCount = rows.Count;
			var pageSize = f.PageSize <= 0 ? 16 : f.PageSize;
			var pageRows = rows.Skip((Math.Max(1, f.Page) - 1) * pageSize).Take(pageSize).ToList();

			return new StockDetailsDto
			{
				Summary = summary,
				Labels = BuildLabels(fromUtc, asOnStart),
				GrandTotal = grand,
				Grid = new PagedResult<StockDetailsRowDto>
				{
					Items = pageRows,
					TotalCount = totalCount,
					Page = f.Page,
					PageSize = pageSize
				}
			};
		}

		// [S3] Row assembly + derived columns.
		private static StockDetailsRowDto MergeRow(int stateId, string name,
			decimal opening, decimal supplies, decimal salesBefore, decimal salesOnDay)
		{
			var totalStock = opening + supplies;
			var totalSales = salesBefore + salesOnDay;
			var closing = totalStock - totalSales;   // [S3] swap for stored ClosingStock if desired
			return new StockDetailsRowDto
			{
				StateId = stateId,
				StateName = name,
				OpeningStock = opening,
				Supplies = supplies,
				TotalStock = totalStock,
				SalesBefore = salesBefore,
				SalesOnDay = salesOnDay,
				TotalSales = totalSales,
				ClosingStock = closing,
				SalesPct = totalStock == 0 ? 0 : (double)(totalSales / totalStock) * 100
			};
		}

		// [S1][S2] Opening + Supplies from the state reconciliation, for the selected month.
		private async Task<Dictionary<int, (decimal Opening, decimal Supplies)>> BuildStockAsync(
			List<int> stateIds, DateTime monthStart, DateTime monthEndNext)
		{
			var q = _db.Set<StateGlobalStockReconciliation>().AsNoTracking()
				.Where(x => x.StateId != null
						 && x.CreatedAt >= monthStart && x.CreatedAt < monthEndNext);   // [S1] month window

			if (stateIds.Count > 0)
				q = q.Where(x => stateIds.Contains(x.StateId!.Value));

			var agg = await q
				.GroupBy(x => x.StateId!.Value)
				.Select(g => new
				{
					StateId = g.Key,
					Opening = g.Sum(x => x.OpeningStock),
					Supplies = g.Sum(x => x.Receipt + x.ProductionImports)   // [S2]
				})
				.ToListAsync();

			return agg.ToDictionary(a => a.StateId, a => (a.Opening, a.Supplies));
		}

		private async Task<List<(int StateId, decimal Before, decimal OnDay)>> BuildSalesWholesalerAsync(
			List<int> stateIds, DateTime monthStart, DateTime asOnStart, DateTime asOnNextDay)
		{
			var q = _db.Set<SalesWholesaler>().AsNoTracking()
				.Where(x => x.StateId != null && x.InvoiceDate != null
						 && x.InvoiceDate >= monthStart && x.InvoiceDate < asOnNextDay);

			if (stateIds.Count > 0)
				q = q.Where(x => stateIds.Contains(x.StateId!.Value));

			var agg = await q
				.GroupBy(x => x.StateId!.Value)
				.Select(g => new
				{
					StateId = g.Key,
					Before = g.Where(x => x.InvoiceDate < asOnStart).Sum(x => (decimal?)x.QuantityMT) ?? 0m,
					OnDay = g.Where(x => x.InvoiceDate >= asOnStart).Sum(x => (decimal?)x.QuantityMT) ?? 0m   // [S4]
				})
				.ToListAsync();

			return agg.Select(a => (a.StateId, a.Before, a.OnDay)).ToList();
		}

		private async Task<List<(int StateId, decimal Before, decimal OnDay)>> BuildSalesCompanyAsync(
			List<int> stateIds, DateTime monthStart, DateTime asOnStart, DateTime asOnNextDay)
		{
			var q = _db.Set<SalesCompanySale>().AsNoTracking()
				.Where(x => x.StateId != null && x.InvoiceDate != null
						 && x.InvoiceDate >= monthStart && x.InvoiceDate < asOnNextDay);

			if (stateIds.Count > 0)
				q = q.Where(x => stateIds.Contains(x.StateId!.Value));

			var agg = await q
				.GroupBy(x => x.StateId!.Value)
				.Select(g => new
				{
					StateId = g.Key,
					Before = g.Where(x => x.InvoiceDate < asOnStart).Sum(x => (decimal?)x.QuantityMT) ?? 0m,
					OnDay = g.Where(x => x.InvoiceDate >= asOnStart).Sum(x => (decimal?)x.QuantityMT) ?? 0m   // [S4]
				})
				.ToListAsync();

			return agg.Select(a => (a.StateId, a.Before, a.OnDay)).ToList();
		}

		private static StockDetailsLabelsDto BuildLabels(DateTime from, DateTime asOn)
		{
			var ci = CultureInfo.InvariantCulture;
			bool sameMonth = from.Year == asOn.Year && from.Month == asOn.Month;
			var beforeEnd = asOn.AddDays(-1);

			string suppliesLabel = sameMonth
				? from.ToString("MMMM", ci)
				: $"{from.ToString("d MMM", ci)} - {asOn.ToString("d MMM", ci)}";

			string beforeRange;
			if (beforeEnd < from)
				beforeRange = "—";
			else if (from.Year == beforeEnd.Year && from.Month == beforeEnd.Month)
				beforeRange = $"{from.Day}-{beforeEnd.Day} {from.ToString("MMM", ci)}";
			else
				beforeRange = $"{from.ToString("d MMM", ci)} - {beforeEnd.ToString("d MMM", ci)}";

			return new StockDetailsLabelsDto
			{
				OpeningAsOn = from.ToString("d MMM", ci),
				SuppliesMonth = suppliesLabel,
				SalesBeforeRange = beforeRange,
				SalesOnDay = asOn.ToString("d MMM", ci),
				ClosingAsOn = asOn.ToString("d MMM", ci)
			};
		}
	}
}