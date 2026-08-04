// ============================================================================
//  Spic.Infrastructure / Services / ProductStockAvailabilityService.cs
//
//  Current stock is built from the latest snapshot independently for:
//    * WholesalerStockAsOnToday.Stock       using StockDate
//    * DptReport.ClosingBalance             using CreatedAt report date
//    * WarehouseDistrict...ClosingStock     using CreatedAt report date
//
//  Sales are built from:
//    * SalesCompanySale.QuantityMT          summed by InvoiceDate
//    * SalesWholesaler.QuantityMT           summed by InvoiceDate
//    * DptReport.SoldQuantity               latest DPT snapshot only
//
//  Historical stock snapshots are never added together.
//
//  Product identity:
//    * ProductId keeps its existing positive pivot key.
//    * IfmsProductId uses a negative internal pivot key to prevent ID collisions.
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
		private const string IfmsColumnGroup = "IFMS Products";

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

			// Keep the operations sequential because they share the same scoped DbContext.
			var wholesalerStock = await LoadLatestWholesalerStockAsync(
				filter,
				dateFrom,
				dateToExclusive,
				cancellationToken);

			var dptStockAndSales = await LoadLatestDptStockAndSalesAsync(
				filter,
				dateFrom,
				dateToExclusive,
				cancellationToken);

			var warehouseStock = await LoadLatestWarehouseStockAsync(
				filter,
				dateFrom,
				dateToExclusive,
				cancellationToken);

			var companySales = await LoadCompanySalesAsync(
				filter,
				dateFrom,
				dateToExclusive,
				cancellationToken);

			var wholesalerSales = await LoadWholesalerSalesAsync(
				filter,
				dateFrom,
				dateToExclusive,
				cancellationToken);

			var combined = new Dictionary<StateProductKey, CombinedValue>();

			Merge(combined, wholesalerStock);
			Merge(combined, dptStockAndSales);
			Merge(combined, warehouseStock);
			Merge(combined, companySales);
			Merge(combined, wholesalerSales);

			if (combined.Count == 0)
			{
				return EmptyDashboard(page, pageSize);
			}

			var stateIds = combined.Keys
				.Select(key => key.StateId)
				.Distinct()
				.ToList();

			// Product table IDs stay positive. IFMS product IDs use a negative pivot
			// key internally so Product.Id 10 and IfmsProduct.Id 10 never collide.
			var productKeys = combined.Keys
				.Select(key => key.ProductKey)
				.Distinct()
				.ToList();

			var approvedProductIds = productKeys
				.Where(key => key > 0)
				.Distinct()
				.ToList();

			var ifmsProductIds = productKeys
				.Where(key => key < 0)
				.Select(key => -key)
				.Distinct()
				.ToList();

			var stateNames = await _db.Set<State>()
				.AsNoTracking()
				.Where(state => stateIds.Contains(state.Id))
				.ToDictionaryAsync(
					state => state.Id,
					state => state.StateName ?? string.Empty,
					cancellationToken);

			var productNames = approvedProductIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<Product>()
					.AsNoTracking()
					.Where(product => approvedProductIds.Contains(product.Id))
					.ToDictionaryAsync(
						product => product.Id,
						product => product.Name ?? string.Empty,
						cancellationToken);

			var ifmsProductNames = ifmsProductIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<IfmsProduct>()
					.AsNoTracking()
					.Where(product => ifmsProductIds.Contains(product.Id))
					.ToDictionaryAsync(
						product => product.Id,
						product => product.Name ?? string.Empty,
						cancellationToken);

			var columns = productKeys
				.Select(productKey =>
				{
					var isIfmsProduct = productKey < 0;
					var actualProductId = isIfmsProduct ? -productKey : productKey;
					var name = isIfmsProduct
						? ifmsProductNames.TryGetValue(actualProductId, out var ifmsName)
							? ifmsName
							: null
						: productNames.TryGetValue(actualProductId, out var productName)
							? productName
							: null;

					return new ProdStockColumnDto
					{
						// Existing ProductId remains the dictionary/pivot key.
						// Approved products are positive; IFMS products are negative.
						ProductId = productKey,
						ApprovedProductId = isIfmsProduct ? null : actualProductId,
						IfmsProductId = isIfmsProduct ? actualProductId : null,
						ProductName = NormalizeName(name),
						Group = isIfmsProduct ? IfmsColumnGroup : DefaultColumnGroup
					};
				})
				.OrderBy(column => column.IsIfmsProduct)
				.ThenBy(column => column.ProductName, StringComparer.OrdinalIgnoreCase)
				.ThenBy(column => column.ProductId)
				.ToList();

			var rows = BuildRows(combined, stateNames);

			// Preserve the existing behavior: search filters state rows before cards,
			// grand totals, sorting and paging.
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
			var summary = BuildSummary(
				rows,
				columns.Count,
				grandTotal.Total,
				grandTotal.TotalSales);

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

		// --------------------------------------------------------------------
		// Latest current-stock snapshots
		// --------------------------------------------------------------------

		private async Task<List<SourceAggregate>> LoadLatestWholesalerStockAsync(
			ProductStockAvailabilityFilter filter,
			DateTime? dateFrom,
			DateTime? dateToExclusive,
			CancellationToken cancellationToken)
		{
			var query = _db.WholesalerStockAsOnTodays
				.AsNoTracking()
				.Where(row =>
					row.StateId.HasValue &&
					(row.ProductId.HasValue || row.IfmsProductId.HasValue));

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(row => filter.StateIds.Contains(row.StateId!.Value));
			}

			if (dateFrom.HasValue)
			{
				query = query.Where(row => row.StockDate >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				query = query.Where(row => row.StockDate < dateToExclusive.Value);
			}

			var latestDate = await query
				.OrderByDescending(row => row.StockDate)
				.Select(row => (DateTime?)row.StockDate)
				.FirstOrDefaultAsync(cancellationToken);

			if (!latestDate.HasValue)
			{
				return new List<SourceAggregate>();
			}

			var start = ToUtcDate(latestDate.Value);
			var end = start.AddDays(1);

			return await query
				.Where(row => row.StockDate >= start && row.StockDate < end)
				.GroupBy(row => new
				{
					StateId = row.StateId!.Value,
					row.ProductId,
					row.IfmsProductId
				})
				.Select(group => new SourceAggregate
				{
					StateId = group.Key.StateId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					Stock = group.Sum(row => row.Stock),
					Sales = 0m
				})
				.ToListAsync(cancellationToken);
		}

		private async Task<List<SourceAggregate>> LoadLatestDptStockAndSalesAsync(
			ProductStockAvailabilityFilter filter,
			DateTime? dateFrom,
			DateTime? dateToExclusive,
			CancellationToken cancellationToken)
		{
			var query = _db.DptReports
				.AsNoTracking()
				.Where(row =>
					row.StateId.HasValue &&
					(row.ProductId.HasValue || row.IfmsProductId.HasValue));

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(row => filter.StateIds.Contains(row.StateId!.Value));
			}

			if (dateFrom.HasValue)
			{
				query = query.Where(row => row.CreatedAt >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				query = query.Where(row => row.CreatedAt < dateToExclusive.Value);
			}

			var latestDate = await query
				.OrderByDescending(row => row.CreatedAt)
				.Select(row => (DateTime?)row.CreatedAt)
				.FirstOrDefaultAsync(cancellationToken);

			if (!latestDate.HasValue)
			{
				return new List<SourceAggregate>();
			}

			var start = ToUtcDate(latestDate.Value);
			var end = start.AddDays(1);

			return await query
				.Where(row => row.CreatedAt >= start && row.CreatedAt < end)
				.GroupBy(row => new
				{
					StateId = row.StateId!.Value,
					row.ProductId,
					row.IfmsProductId
				})
				.Select(group => new SourceAggregate
				{
					StateId = group.Key.StateId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					Stock = group.Sum(row => row.ClosingBalance),
					Sales = group.Sum(row => row.SoldQuantity)
				})
				.ToListAsync(cancellationToken);
		}

		private async Task<List<SourceAggregate>> LoadLatestWarehouseStockAsync(
			ProductStockAvailabilityFilter filter,
			DateTime? dateFrom,
			DateTime? dateToExclusive,
			CancellationToken cancellationToken)
		{
			var query = _db.WarehouseDistrictGlobalStockReconciliations
				.AsNoTracking()
				.Where(row =>
					row.StateId.HasValue &&
					(row.ProductId.HasValue || row.IfmsProductId.HasValue));

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(row => filter.StateIds.Contains(row.StateId!.Value));
			}

			if (dateFrom.HasValue)
			{
				query = query.Where(row => row.CreatedAt >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				query = query.Where(row => row.CreatedAt < dateToExclusive.Value);
			}

			var latestDate = await query
				.OrderByDescending(row => row.CreatedAt)
				.Select(row => (DateTime?)row.CreatedAt)
				.FirstOrDefaultAsync(cancellationToken);

			if (!latestDate.HasValue)
			{
				return new List<SourceAggregate>();
			}

			var start = ToUtcDate(latestDate.Value);
			var end = start.AddDays(1);

			return await query
				.Where(row => row.CreatedAt >= start && row.CreatedAt < end)
				.GroupBy(row => new
				{
					StateId = row.StateId!.Value,
					row.ProductId,
					row.IfmsProductId
				})
				.Select(group => new SourceAggregate
				{
					StateId = group.Key.StateId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					Stock = group.Sum(row => row.ClosingStock),
					Sales = 0m
				})
				.ToListAsync(cancellationToken);
		}

		// --------------------------------------------------------------------
		// Transaction sales
		// --------------------------------------------------------------------

		private async Task<List<SourceAggregate>> LoadCompanySalesAsync(
			ProductStockAvailabilityFilter filter,
			DateTime? dateFrom,
			DateTime? dateToExclusive,
			CancellationToken cancellationToken)
		{
			var query = _db.SalesCompanySales
				.AsNoTracking()
				.Where(row =>
					row.StateId.HasValue &&
					(row.ProductId.HasValue || row.IfmsProductId.HasValue) &&
					row.InvoiceDate.HasValue);

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(row => filter.StateIds.Contains(row.StateId!.Value));
			}

			if (dateFrom.HasValue)
			{
				query = query.Where(row => row.InvoiceDate!.Value >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				query = query.Where(row => row.InvoiceDate!.Value < dateToExclusive.Value);
			}

			return await query
				.GroupBy(row => new
				{
					StateId = row.StateId!.Value,
					row.ProductId,
					row.IfmsProductId
				})
				.Select(group => new SourceAggregate
				{
					StateId = group.Key.StateId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					Stock = 0m,
					Sales = group.Sum(row => row.QuantityMT)
				})
				.ToListAsync(cancellationToken);
		}

		private async Task<List<SourceAggregate>> LoadWholesalerSalesAsync(
			ProductStockAvailabilityFilter filter,
			DateTime? dateFrom,
			DateTime? dateToExclusive,
			CancellationToken cancellationToken)
		{
			var query = _db.SalesWholesalers
				.AsNoTracking()
				.Where(row =>
					row.StateId.HasValue &&
					(row.ProductId.HasValue || row.IfmsProductId.HasValue) &&
					row.InvoiceDate.HasValue);

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(row => filter.StateIds.Contains(row.StateId!.Value));
			}

			if (dateFrom.HasValue)
			{
				query = query.Where(row => row.InvoiceDate!.Value >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				query = query.Where(row => row.InvoiceDate!.Value < dateToExclusive.Value);
			}

			return await query
				.GroupBy(row => new
				{
					StateId = row.StateId!.Value,
					row.ProductId,
					row.IfmsProductId
				})
				.Select(group => new SourceAggregate
				{
					StateId = group.Key.StateId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					Stock = 0m,
					Sales = group.Sum(row => row.QuantityMT)
				})
				.ToListAsync(cancellationToken);
		}

		// --------------------------------------------------------------------
		// Pivot construction
		// --------------------------------------------------------------------

		private static void Merge(
			IDictionary<StateProductKey, CombinedValue> target,
			IEnumerable<SourceAggregate> source)
		{
			foreach (var item in source)
			{
				var productKey = BuildProductKey(item.ProductId, item.IfmsProductId);
				if (productKey == 0)
				{
					continue;
				}

				var key = new StateProductKey(item.StateId, productKey);

				if (!target.TryGetValue(key, out var value))
				{
					value = new CombinedValue();
					target[key] = value;
				}

				value.Stock += item.Stock;
				value.Sales += item.Sales;
			}
		}

		private static List<ProdStockStateRowDto> BuildRows(
			IReadOnlyDictionary<StateProductKey, CombinedValue> combined,
			IReadOnlyDictionary<int, string> stateNames)
		{
			var stateBuilders = new Dictionary<int, StateRowBuilder>();

			foreach (var item in combined)
			{
				var key = item.Key;
				var value = item.Value;

				if (!stateBuilders.TryGetValue(key.StateId, out var state))
				{
					state = new StateRowBuilder
					{
						StateId = key.StateId,
						StateName = stateNames.TryGetValue(key.StateId, out var name)
							? NormalizeName(name)
							: "-"
					};

					stateBuilders[key.StateId] = state;
				}

				state.StockQuantities[key.ProductKey] = value.Stock;
				state.SalesQuantities[key.ProductKey] = value.Sales;
				state.TotalStock += value.Stock;
				state.TotalSales += value.Sales;
			}

			return stateBuilders.Values
				.Select(state => new ProdStockStateRowDto
				{
					StateId = state.StateId,
					StateName = state.StateName,
					Quantities = state.StockQuantities,
					SalesQuantities = state.SalesQuantities,
					Total = state.TotalStock,
					TotalSales = state.TotalSales
				})
				.ToList();
		}

		private static ProdStockStateRowDto BuildGrandTotal(
			IReadOnlyCollection<ProdStockStateRowDto> rows,
			IReadOnlyCollection<ProdStockColumnDto> columns)
		{
			var stockQuantities = columns.ToDictionary(
				column => column.ProductId,
				_ => 0m);

			var salesQuantities = columns.ToDictionary(
				column => column.ProductId,
				_ => 0m);

			decimal totalStock = 0m;
			decimal totalSales = 0m;

			foreach (var row in rows)
			{
				totalStock += row.Total;
				totalSales += row.TotalSales;

				foreach (var item in row.Quantities)
				{
					stockQuantities[item.Key] =
						stockQuantities.TryGetValue(item.Key, out var current)
							? current + item.Value
							: item.Value;
				}

				foreach (var item in row.SalesQuantities)
				{
					salesQuantities[item.Key] =
						salesQuantities.TryGetValue(item.Key, out var current)
							? current + item.Value
							: item.Value;
				}
			}

			return new ProdStockStateRowDto
			{
				StateId = 0,
				StateName = "Grand Total",
				Quantities = stockQuantities,
				SalesQuantities = salesQuantities,
				Total = totalStock,
				TotalSales = totalSales
			};
		}

		private static ProdStockSummaryDto BuildSummary(
			IReadOnlyCollection<ProdStockStateRowDto> rows,
			int productCount,
			decimal totalStock,
			decimal totalSales)
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
				TotalQuantity = totalStock,
				TotalSales = totalSales,
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

				"sales" when descending => rows
					.OrderByDescending(row => row.TotalSales)
					.ThenBy(row => row.StateName, StringComparer.OrdinalIgnoreCase),

				"sales" => rows
					.OrderBy(row => row.TotalSales)
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
					SalesQuantities = new Dictionary<int, decimal>(),
					Total = 0m,
					TotalSales = 0m
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
			return value.HasValue ? ToUtcDate(value.Value) : null;
		}

		private static DateTime? ToUtcExclusiveEnd(DateTime? value)
		{
			return value.HasValue ? ToUtcDate(value.Value).AddDays(1) : null;
		}

		private static DateTime ToUtcDate(DateTime value)
		{
			return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
		}

		private static int BuildProductKey(int? productId, int? ifmsProductId)
		{
			// Preserve the previous Product table identity exactly.
			if (productId.HasValue && productId.Value > 0)
			{
				return productId.Value;
			}

			// Negative values are internal pivot keys only. The real IFMS ID remains
			// available on ProdStockColumnDto.IfmsProductId.
			if (ifmsProductId.HasValue && ifmsProductId.Value > 0)
			{
				return -ifmsProductId.Value;
			}

			return 0;
		}

		private static string NormalizeName(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
		}

		private readonly record struct StateProductKey(int StateId, int ProductKey);

		private sealed class SourceAggregate
		{
			public int StateId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
			public decimal Stock { get; set; }
			public decimal Sales { get; set; }
		}

		private sealed class CombinedValue
		{
			public decimal Stock { get; set; }
			public decimal Sales { get; set; }
		}

		private sealed class StateRowBuilder
		{
			public int StateId { get; set; }
			public string StateName { get; set; } = "";
			public Dictionary<int, decimal> StockQuantities { get; } = new();
			public Dictionary<int, decimal> SalesQuantities { get; } = new();
			public decimal TotalStock { get; set; }
			public decimal TotalSales { get; set; }
		}
	}
}