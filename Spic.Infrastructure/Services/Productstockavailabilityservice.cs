// ============================================================================
//  Spic.Infrastructure / Services / ProductStockAvailabilityService.cs
//
//  Performance-focused implementation that preserves the existing flow:
//    * Source table      : WholesalerStockAsOnToday only.
//    * Snapshot rule     : latest StockDate day inside the selected range.
//    * Cell calculation  : SUM(Stock) grouped by State + Product.
//    * Search semantics  : filters State rows before cards/grand total/paging.
//    * Region/HQ filters : resolved into StateIds by the Razor page.
//
//  Main improvements:
//    * Uses an index-friendly ORDER BY ... LIMIT 1 for latest snapshot lookup.
//    * Aggregates in SQL and transfers only State x Product totals.
//    * Builds columns, pivot rows and totals with single-pass dictionaries.
//    * Supports request cancellation all the way into EF Core.
//    * Avoids repeated GroupBy/Sum enumeration over the same result set.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;

namespace Spic.Infrastructure.Services
{
	public class ProductStockAvailabilityService : IProductStockAvailabilityService
	{
		private const decimal LowStockThresholdMt = 1000m;
		private const int DefaultPageSize = 16;
		private const int MaxInteractivePageSize = 500;
		private const string DefaultColumnGroup = "Products";

		private readonly AppDbContext _db;

		public ProductStockAvailabilityService(AppDbContext db)
		{
			_db = db;
		}

		public async Task<ProductStockAvailabilityDto> GetDashboardAsync(
			ProductStockAvailabilityFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new ProductStockAvailabilityFilter();
			NormalizeFilter(filter);

			var page = filter.Page;
			var pageSize = ResolvePageSize(filter.PageSize);
			var dateFrom = ToUtcStart(filter.DateFrom);
			var dateToExclusive = ToUtcExclusiveEnd(filter.DateTo);

			var stockQuery = _db.WholesalerStockAsOnTodays
				.AsNoTracking()
				.Where(stock =>
					stock.StateId.HasValue &&
					stock.ProductId.HasValue);

			if (filter.StateIds.Count > 0)
			{
				stockQuery = stockQuery.Where(stock =>
					stock.StateId.HasValue &&
					filter.StateIds.Contains(stock.StateId.Value));
			}

			if (dateFrom.HasValue)
			{
				stockQuery = stockQuery.Where(stock =>
					stock.StockDate >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				stockQuery = stockQuery.Where(stock =>
					stock.StockDate < dateToExclusive.Value);
			}

			// ORDER BY + FirstOrDefault is index-friendly when StockDate is indexed.
			// We then retain the existing rule: every row from that latest calendar day.
			var latestStockDate = await stockQuery
				.OrderByDescending(stock => stock.StockDate)
				.Select(stock => (DateTime?)stock.StockDate)
				.FirstOrDefaultAsync(cancellationToken);

			if (!latestStockDate.HasValue)
			{
				return EmptyDashboard(page, pageSize);
			}

			var snapshotStart = ToUtcDate(latestStockDate.Value);
			var snapshotEnd = snapshotStart.AddDays(1);

			var aggregateRows = await (
				from stock in stockQuery
				where stock.StockDate >= snapshotStart &&
					  stock.StockDate < snapshotEnd
				join state in _db.Set<State>().AsNoTracking()
					on stock.StateId equals (int?)state.Id
				join product in _db.Set<Product>().AsNoTracking()
					on stock.ProductId equals (int?)product.Id
				group stock by new
				{
					StateId = state.Id,
					state.StateName,
					ProductId = product.Id,
					ProductName = product.Name
				}
				into grouped
				select new AggregateRow
				{
					StateId = grouped.Key.StateId,
					StateName = grouped.Key.StateName ?? "",
					ProductId = grouped.Key.ProductId,
					ProductName = grouped.Key.ProductName ?? "",
					Quantity = grouped.Sum(item => item.Stock)
				})
				.ToListAsync(cancellationToken);

			if (aggregateRows.Count == 0)
			{
				return EmptyDashboard(page, pageSize);
			}

			var pivot = BuildPivot(aggregateRows);
			var columns = pivot.Columns;
			var rows = pivot.Rows;

			// Preserve the previous behavior: search changes cards and grand total,
			// while the product-column list remains based on the complete snapshot.
			if (!string.IsNullOrWhiteSpace(filter.Search))
			{
				var search = filter.Search.Trim();
				rows = rows
					.Where(row => row.StateName.Contains(
						search,
						StringComparison.OrdinalIgnoreCase))
					.ToList();
			}

			var grandTotal = BuildGrandTotal(rows, columns);
			var summary = BuildSummary(rows, columns.Count, grandTotal.Total);
			var sortedRows = ApplySorting(rows, filter);
			var totalCount = sortedRows.Count;

			List<ProdStockStateRowDto> pageRows;

			if (pageSize == int.MaxValue)
			{
				pageRows = sortedRows;
			}
			else
			{
				var skipLong = (long)(page - 1) * pageSize;
				var skip = skipLong > int.MaxValue ? int.MaxValue : (int)skipLong;

				pageRows = sortedRows
					.Skip(skip)
					.Take(pageSize)
					.ToList();
			}

			return new ProductStockAvailabilityDto
			{
				Summary = summary,
				Columns = columns,
				GrandTotal = grandTotal,
				Grid = new PagedResult<ProdStockStateRowDto>
				{
					Items = pageRows,
					TotalCount = totalCount,
					Page = page,
					PageSize = pageSize
				}
			};
		}

		private static PivotResult BuildPivot(
			IReadOnlyCollection<AggregateRow> aggregateRows)
		{
			var productNames = new Dictionary<int, string>();
			var stateBuilders = new Dictionary<int, StateRowBuilder>();

			foreach (var item in aggregateRows)
			{
				if (!productNames.ContainsKey(item.ProductId))
				{
					productNames[item.ProductId] = NormalizeName(item.ProductName);
				}

				if (!stateBuilders.TryGetValue(item.StateId, out var state))
				{
					state = new StateRowBuilder
					{
						StateId = item.StateId,
						StateName = NormalizeName(item.StateName)
					};

					stateBuilders[item.StateId] = state;
				}

				// SQL grouping already produces one row per State + Product.
				// += keeps this safe if a provider/query change ever emits duplicates.
				if (state.Quantities.TryGetValue(item.ProductId, out var existing))
				{
					state.Quantities[item.ProductId] = existing + item.Quantity;
				}
				else
				{
					state.Quantities[item.ProductId] = item.Quantity;
				}

				state.Total += item.Quantity;
			}

			var columns = productNames
				.Select(item => new ProdStockColumnDto
				{
					ProductId = item.Key,
					ProductName = item.Value,
					Group = DefaultColumnGroup
				})
				.OrderBy(column => column.Group, StringComparer.OrdinalIgnoreCase)
				.ThenBy(column => column.ProductName, StringComparer.OrdinalIgnoreCase)
				.ToList();

			var rows = stateBuilders.Values
				.Select(state => new ProdStockStateRowDto
				{
					StateId = state.StateId,
					StateName = state.StateName,
					Quantities = state.Quantities,
					Total = state.Total
				})
				.ToList();

			return new PivotResult(columns, rows);
		}

		private static ProdStockStateRowDto BuildGrandTotal(
			IReadOnlyCollection<ProdStockStateRowDto> rows,
			IReadOnlyCollection<ProdStockColumnDto> columns)
		{
			var quantities = new Dictionary<int, decimal>(columns.Count);

			foreach (var column in columns)
			{
				quantities[column.ProductId] = 0m;
			}

			decimal total = 0m;

			foreach (var row in rows)
			{
				total += row.Total;

				foreach (var item in row.Quantities)
				{
					quantities[item.Key] = quantities.TryGetValue(item.Key, out var current)
						? current + item.Value
						: item.Value;
				}
			}

			return new ProdStockStateRowDto
			{
				StateId = 0,
				StateName = "Grand Total",
				Quantities = quantities,
				Total = total
			};
		}

		private static ProdStockSummaryDto BuildSummary(
			IReadOnlyCollection<ProdStockStateRowDto> rows,
			int productCount,
			decimal totalQuantity)
		{
			ProdStockStateRowDto? highest = null;
			var lowStockAlerts = 0;

			foreach (var row in rows)
			{
				if (row.Total < LowStockThresholdMt)
				{
					lowStockAlerts++;
				}

				if (highest is null ||
					row.Total > highest.Total ||
					(row.Total == highest.Total &&
					 string.Compare(
						 row.StateName,
						 highest.StateName,
						 StringComparison.OrdinalIgnoreCase) < 0))
				{
					highest = row;
				}
			}

			return new ProdStockSummaryDto
			{
				TotalStates = rows.Count,
				TotalProducts = productCount,
				TotalQuantity = totalQuantity,
				HighestStockState = highest?.StateName ?? "-",
				HighestStockQuantity = highest?.Total ?? 0m,
				LowStockAlerts = lowStockAlerts
			};
		}

		private static List<ProdStockStateRowDto> ApplySorting(
			IEnumerable<ProdStockStateRowDto> rows,
			ProductStockAvailabilityFilter filter)
		{
			var sortColumn = filter.SortColumn?.Trim().ToLowerInvariant();
			var descending = string.Equals(
				filter.SortDir?.Trim(),
				"desc",
				StringComparison.OrdinalIgnoreCase);

			IOrderedEnumerable<ProdStockStateRowDto> sorted = sortColumn switch
			{
				"total" when descending => rows
					.OrderByDescending(row => row.Total)
					.ThenBy(row => row.StateName, StringComparer.OrdinalIgnoreCase),

				"total" => rows
					.OrderBy(row => row.Total)
					.ThenBy(row => row.StateName, StringComparer.OrdinalIgnoreCase),

				"state" when descending => rows
					.OrderByDescending(row => row.StateName, StringComparer.OrdinalIgnoreCase),

				_ => rows
					.OrderBy(row => row.StateName, StringComparer.OrdinalIgnoreCase)
			};

			return sorted.ToList();
		}

		private static ProductStockAvailabilityDto EmptyDashboard(
			int page,
			int pageSize)
		{
			return new ProductStockAvailabilityDto
			{
				Summary = new ProdStockSummaryDto(),
				Columns = new List<ProdStockColumnDto>(),
				GrandTotal = new ProdStockStateRowDto
				{
					StateId = 0,
					StateName = "Grand Total",
					Quantities = new Dictionary<int, decimal>(),
					Total = 0m
				},
				Grid = new PagedResult<ProdStockStateRowDto>
				{
					Items = new List<ProdStockStateRowDto>(),
					TotalCount = 0,
					Page = page,
					PageSize = pageSize
				}
			};
		}

		private static void NormalizeFilter(ProductStockAvailabilityFilter filter)
		{
			filter.StateIds ??= new List<int>();
			filter.RegionIds ??= new List<int>();
			filter.HeadQuarterIds ??= new List<int>();

			filter.StateIds = filter.StateIds
				.Where(id => id > 0)
				.Distinct()
				.ToList();

			filter.RegionIds = filter.RegionIds
				.Where(id => id > 0)
				.Distinct()
				.ToList();

			filter.HeadQuarterIds = filter.HeadQuarterIds
				.Where(id => id > 0)
				.Distinct()
				.ToList();

			filter.Page = Math.Max(1, filter.Page);
		}

		private static int ResolvePageSize(int requestedPageSize)
		{
			// The controller uses int.MaxValue only for an unpaged Excel export.
			if (requestedPageSize == int.MaxValue)
			{
				return int.MaxValue;
			}

			if (requestedPageSize <= 0)
			{
				return DefaultPageSize;
			}

			return Math.Min(requestedPageSize, MaxInteractivePageSize);
		}

		private static DateTime? ToUtcStart(DateTime? value)
		{
			return value.HasValue
				? ToUtcDate(value.Value)
				: null;
		}

		private static DateTime? ToUtcExclusiveEnd(DateTime? value)
		{
			return value.HasValue
				? ToUtcDate(value.Value).AddDays(1)
				: null;
		}

		private static DateTime ToUtcDate(DateTime value)
		{
			return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
		}

		private static string NormalizeName(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? "-"
				: value.Trim();
		}

		private sealed class AggregateRow
		{
			public int StateId { get; set; }
			public string StateName { get; set; } = "";
			public int ProductId { get; set; }
			public string ProductName { get; set; } = "";
			public decimal Quantity { get; set; }
		}

		private sealed class StateRowBuilder
		{
			public int StateId { get; set; }
			public string StateName { get; set; } = "";
			public Dictionary<int, decimal> Quantities { get; } = new();
			public decimal Total { get; set; }
		}

		private sealed record PivotResult(
			List<ProdStockColumnDto> Columns,
			List<ProdStockStateRowDto> Rows);
	}
}