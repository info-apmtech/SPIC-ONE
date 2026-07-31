// ============================================================================
//  ProductStockAvailabilityService (implementation of IProductStockAvailabilityService)
//  Location: Spic.Infrastructure/Services/
//
//  Builds a State x Product availability pivot from WholesalerStockAsOnToday.
//
//  BEFORE IT COMPILES, confirm two things (same as StockReportService):
//   1. `AppDbContext`  -> your actual DbContext type.
//   2. The `using` lines match YOUR namespaces (DTOs / Interfaces / Entities).
//
//  Entity assumptions (identical to StockReportService, which already compiles):
//   * WholesalerStockAsOnToday: StateId, ProductId, Stock, StockDate
//   * State.StateName / State.Id
//   * Product.Name / Product.Id
//
//  ---- The one deliberate design choice ----
//  Cell value = SUM(WholesalerStockAsOnToday.Stock) for (State, Product).
//  To source cells from a different table (or a combination), change ONLY the
//  `agg` query below - everything downstream (columns, rows, KPIs) is generic.
//  NB: the reconciliation tables (State/Warehouse) hold running balances, so
//  summing their ClosingStock across dates would double-count - don't just add
//  them here without a "latest snapshot per state/product" filter.
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;   // <-- your DbContext namespace
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
	public class ProductStockAvailabilityService : IProductStockAvailabilityService
	{
		private readonly AppDbContext _db;   // <-- rename to your DbContext type

		public ProductStockAvailabilityService(AppDbContext db) => _db = db;

		// A state is a "low stock alert" when its total availability falls below this (MT).
		// Move to a config table if you want it editable.
		private const decimal LowStockThresholdMt = 1000m;

		public async Task<ProductStockAvailabilityDto> GetDashboardAsync(ProductStockAvailabilityFilter f)
		{
			// ---- Base filter on the snapshot ----
			var q = _db.Set<WholesalerStockAsOnToday>().AsNoTracking()
				.Where(x => x.Stock > 0 && x.StateId != null && x.ProductId != null);

			if (f.StateIds.Count > 0)
				q = q.Where(x => f.StateIds.Contains(x.StateId!.Value));

			if (f.DateFrom.HasValue)
			{
				var df = DateTime.SpecifyKind(f.DateFrom.Value.Date, DateTimeKind.Utc);
				q = q.Where(x => x.StockDate >= df);
			}
			if (f.DateTo.HasValue)
			{
				var dt = DateTime.SpecifyKind(f.DateTo.Value.Date, DateTimeKind.Utc);
				q = q.Where(x => x.StockDate <= dt);
			}

			// ---- One grouped round trip: (State, Product) -> summed Stock, with names ----
			var aggRaw = await (
				from s in q
				join st in _db.Set<State>() on s.StateId equals st.Id
				join p in _db.Set<Product>() on s.ProductId equals p.Id
				group s by new { s.StateId, st.StateName, s.ProductId, ProductName = p.Name } into g
				select new
				{
					g.Key.StateId,
					g.Key.StateName,
					g.Key.ProductId,
					g.Key.ProductName,
					Qty = g.Sum(x => x.Stock)
				}).ToListAsync();

			var agg = aggRaw
				.Where(a => a.StateId.HasValue && a.ProductId.HasValue)
				.Select(a => new AggRow
				{
					StateId = a.StateId!.Value,
					StateName = a.StateName ?? "",
					ProductId = a.ProductId!.Value,
					ProductName = a.ProductName ?? "",
					Qty = a.Qty
				})
				.ToList();

			// ---- Pivot columns (products present), ordered by group band then name ----
			var columns = agg
				.GroupBy(a => a.ProductId)
				.Select(g => new ProdStockColumnDto
				{
					ProductId = g.Key,
					ProductName = g.First().ProductName,
					// TODO: set from Product.Category (e.g. "Normal Products"/"Imported Products"/"Others")
					// when that column exists; leaving it constant keeps a single header band.
					Group = "Products"
				})
				.OrderBy(c => c.Group)
				.ThenBy(c => c.ProductName)
				.ToList();

			// ---- One row per state ----
			var allRows = agg
				.GroupBy(a => a.StateId)
				.Select(g => new ProdStockStateRowDto
				{
					StateId = g.Key,
					StateName = g.First().StateName,
					Quantities = g.GroupBy(x => x.ProductId)
								  .ToDictionary(x => x.Key, x => x.Sum(y => y.Qty)),
					Total = g.Sum(x => x.Qty)
				})
				.ToList();

			// ---- Search (state name) ----
			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var term = f.Search.Trim();
				allRows = allRows
					.Where(r => r.StateName.Contains(term, StringComparison.OrdinalIgnoreCase))
					.ToList();
			}

			// ---- Grand total across ALL filtered states (independent of paging) ----
			var grand = new ProdStockStateRowDto { StateId = 0, StateName = "Grand Total" };
			foreach (var col in columns)
				grand.Quantities[col.ProductId] =
					allRows.Sum(r => r.Quantities.TryGetValue(col.ProductId, out var v) ? v : 0m);
			grand.Total = allRows.Sum(r => r.Total);

			// ---- KPI cards ----
			var top = allRows.OrderByDescending(r => r.Total).FirstOrDefault();
			var summary = new ProdStockSummaryDto
			{
				TotalStates = allRows.Count,
				TotalProducts = columns.Count,
				TotalQuantity = grand.Total,
				HighestStockState = top?.StateName ?? "-",
				HighestStockQuantity = top?.Total ?? 0m,
				LowStockAlerts = allRows.Count(r => r.Total < LowStockThresholdMt)
			};

			// ---- Sort ----
			allRows = (f.SortColumn?.ToLowerInvariant(), (f.SortDir ?? "asc").ToLowerInvariant()) switch
			{
				("total", "desc") => allRows.OrderByDescending(r => r.Total).ToList(),
				("total", _) => allRows.OrderBy(r => r.Total).ToList(),
				("state", "desc") => allRows.OrderByDescending(r => r.StateName).ToList(),
				_ => allRows.OrderBy(r => r.StateName).ToList()
			};

			// ---- Page ----
			var totalCount = allRows.Count;
			var pageSize = f.PageSize <= 0 ? 16 : f.PageSize;
			var pageRows = allRows
				.Skip((Math.Max(1, f.Page) - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			return new ProductStockAvailabilityDto
			{
				Summary = summary,
				Columns = columns,
				GrandTotal = grand,
				Grid = new PagedResult<ProdStockStateRowDto>
				{
					Items = pageRows,
					TotalCount = totalCount,
					Page = f.Page,
					PageSize = pageSize
				}
			};
		}

		private class AggRow
		{
			public int StateId { get; set; }
			public string StateName { get; set; } = "";
			public int ProductId { get; set; }
			public string ProductName { get; set; } = "";
			public decimal Qty { get; set; }
		}
	}
}