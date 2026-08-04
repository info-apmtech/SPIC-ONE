using System;
using System.Collections.Generic;
using System.Globalization;
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
	/// <summary>
	/// Production-safe state-wise stock ledger.
	///
	/// Stock sources (snapshot data - historical dates are not added):
	/// - WholesalerStockAsOnToday.Stock
	/// - DptReport.ClosingBalance
	/// - WarehouseDistrictGlobalStockReconciliation.ClosingStock
	///
	/// Sales sources (movement data inside the selected date range):
	/// - SalesWholesaler.QuantityMT
	/// - SalesCompanySale.QuantityMT
	/// - DptReport.SoldQuantity
	///
	/// Existing DTO, controller and Razor contracts are preserved.
	/// </summary>
	public sealed class StockDetailsService : IStockDetailsService
	{
		private const int DefaultPageSize = 16;
		private const int MaximumDashboardPageSize = 500;

		private static readonly CultureInfo LabelCulture =
			CultureInfo.GetCultureInfo("en-IN");

		private readonly AppDbContext _db;

		public StockDetailsService(AppDbContext db)
		{
			_db = db;
		}

		public async Task<StockDetailsDto> GetDashboardAsync(
			StockDetailsFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new StockDetailsFilter();
			NormalizeFilter(filter);

			var period = ResolvePeriod(filter);

			// Opening stock is the latest available snapshot on or before DateFrom.
			var openingStockByState = await LoadCombinedStockByStateAsync(
				filter.StateIds,
				period.PeriodStart,
				cancellationToken);

			// Closing stock is the latest available snapshot on or before DateTo.
			var closingStockByState = await LoadCombinedStockByStateAsync(
				filter.StateIds,
				period.AsOnDate,
				cancellationToken);

			// Company and wholesaler sales are transactions. DPT SoldQuantity is
			// treated as the movement reported for that DPT report date.
			var salesByState = await LoadSalesByStateAsync(
				filter.StateIds,
				period.PeriodStart,
				period.AsOnDate,
				period.AsOnNextDay,
				cancellationToken);

			var involvedStateIds = openingStockByState.Keys
				.Union(closingStockByState.Keys)
				.Union(salesByState.Keys)
				.Distinct()
				.ToList();

			var stateNames = await LoadStateNamesAsync(
				involvedStateIds,
				cancellationToken);

			var rows = BuildRows(
				involvedStateIds,
				openingStockByState,
				closingStockByState,
				salesByState,
				stateNames);

			// Preserve the current page behaviour: search also affects cards and totals.
			if (!string.IsNullOrWhiteSpace(filter.Search))
			{
				var search = filter.Search.Trim();

				rows = rows
					.Where(x => x.StateName.Contains(
						search,
						StringComparison.OrdinalIgnoreCase))
					.ToList();
			}

			var grandTotal = BuildGrandTotal(rows);
			var summary = new StockDetailsSummaryDto
			{
				TotalStock = grandTotal.TotalStock,
				TotalSales = grandTotal.TotalSales,
				ClosingStock = grandTotal.ClosingStock,
				SalesPct = grandTotal.SalesPct
			};

			rows = ApplySorting(rows, filter);

			var totalCount = rows.Count;
			var exportAll = filter.PageSize == int.MaxValue;
			var pageSize = exportAll
				? int.MaxValue
				: Math.Min(filter.PageSize, MaximumDashboardPageSize);

			var pageItems = exportAll
				? rows
				: rows
					.Skip((filter.Page - 1) * pageSize)
					.Take(pageSize)
					.ToList();

			return new StockDetailsDto
			{
				Summary = summary,
				Labels = BuildLabels(period.PeriodStart, period.AsOnDate),
				GrandTotal = grandTotal,
				Grid = new PagedResult<StockDetailsRowDto>
				{
					Items = pageItems,
					TotalCount = totalCount,
					Page = filter.Page,
					PageSize = pageSize
				}
			};
		}

		// =====================================================================
		// Combined current-stock snapshot loading
		// =====================================================================

		private async Task<Dictionary<int, decimal>> LoadCombinedStockByStateAsync(
			IReadOnlyCollection<int> stateIds,
			DateTime asOnDate,
			CancellationToken cancellationToken)
		{
			var result = new Dictionary<int, decimal>();

			// Run sequentially because the same scoped DbContext cannot execute
			// multiple database operations concurrently.
			var wholesaler = await LoadLatestWholesalerStockByStateAsync(
				stateIds,
				asOnDate,
				cancellationToken);

			MergeQuantities(result, wholesaler);

			var retailer = await LoadLatestDptClosingStockByStateAsync(
				stateIds,
				asOnDate,
				cancellationToken);

			MergeQuantities(result, retailer);

			var warehouse = await LoadLatestWarehouseStockByStateAsync(
				stateIds,
				asOnDate,
				cancellationToken);

			MergeQuantities(result, warehouse);

			return result;
		}

		/// <summary>
		/// Uses one latest row per wholesaler/dealer + product business key on or
		/// before the requested date. Old snapshot dates are never added together.
		/// </summary>
		private async Task<Dictionary<int, decimal>> LoadLatestWholesalerStockByStateAsync(
			IReadOnlyCollection<int> stateIds,
			DateTime asOnDate,
			CancellationToken cancellationToken)
		{
			var asOnExclusive = asOnDate.Date.AddDays(1);

			var baseQuery = _db.WholesalerStockAsOnTodays
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.StockDate < asOnExclusive);

			if (stateIds.Count > 0)
			{
				baseQuery = baseQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));
			}

			// NOT EXISTS selects the newest snapshot for each business key.
			var latestQuery = baseQuery.Where(current =>
				!baseQuery.Any(candidate =>
					(candidate.StateId ?? 0) == (current.StateId ?? 0) &&
					(candidate.DistrictId ?? 0) == (current.DistrictId ?? 0) &&
					(candidate.DealerRegistrationId ?? 0) ==
						(current.DealerRegistrationId ?? 0) &&
					(candidate.DealerRegistrationId.HasValue
						? 0
						: candidate.IfmsDealerId ?? 0) ==
					(current.DealerRegistrationId.HasValue
						? 0
						: current.IfmsDealerId ?? 0) &&
					(candidate.DealerRegistrationId.HasValue || candidate.IfmsDealerId.HasValue
						? string.Empty
						: candidate.AgencyName ?? string.Empty) ==
					(current.DealerRegistrationId.HasValue || current.IfmsDealerId.HasValue
						? string.Empty
						: current.AgencyName ?? string.Empty) &&
					(candidate.CompanyId ?? 0) == (current.CompanyId ?? 0) &&
					(candidate.PlantId ?? 0) == (current.PlantId ?? 0) &&
					(candidate.ProductId ?? 0) == (current.ProductId ?? 0) &&
					(
						candidate.StockDate > current.StockDate ||
						(candidate.StockDate == current.StockDate &&
						 candidate.UpdatedAt > current.UpdatedAt) ||
						(candidate.StockDate == current.StockDate &&
						 candidate.UpdatedAt == current.UpdatedAt &&
						 candidate.Id > current.Id)
					)));

			var rows = await latestQuery
				.GroupBy(x => x.StateId!.Value)
				.Select(group => new StateQuantity
				{
					StateId = group.Key,
					Quantity = group.Sum(x => x.Stock)
				})
				.ToListAsync(cancellationToken);

			return rows.ToDictionary(x => x.StateId, x => x.Quantity);
		}

		/// <summary>
		/// Uses one latest DPT row per retailer + product business key on or before
		/// the requested date. ClosingBalance is the current retailer stock.
		/// </summary>
		private async Task<Dictionary<int, decimal>> LoadLatestDptClosingStockByStateAsync(
			IReadOnlyCollection<int> stateIds,
			DateTime asOnDate,
			CancellationToken cancellationToken)
		{
			var asOnExclusive = asOnDate.Date.AddDays(1);

			var baseQuery = _db.DptReports
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.CreatedAt < asOnExclusive);

			if (stateIds.Count > 0)
			{
				baseQuery = baseQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));
			}

			var latestQuery = baseQuery.Where(current =>
				!baseQuery.Any(candidate =>
					(candidate.StateId ?? 0) == (current.StateId ?? 0) &&
					(candidate.DistrictId ?? 0) == (current.DistrictId ?? 0) &&
					(candidate.SubDistrictId ?? 0) == (current.SubDistrictId ?? 0) &&
					(candidate.DealerRegistrationId ?? 0) ==
						(current.DealerRegistrationId ?? 0) &&
					(candidate.DealerRegistrationId.HasValue
						? 0
						: candidate.IfmsDealerId ?? 0) ==
					(current.DealerRegistrationId.HasValue
						? 0
						: current.IfmsDealerId ?? 0) &&
					(candidate.DealerRegistrationId.HasValue || candidate.IfmsDealerId.HasValue
						? string.Empty
						: candidate.RetailerName ?? string.Empty) ==
					(current.DealerRegistrationId.HasValue || current.IfmsDealerId.HasValue
						? string.Empty
						: current.RetailerName ?? string.Empty) &&
					(candidate.CompanyId ?? 0) == (current.CompanyId ?? 0) &&
					(candidate.PlantId ?? 0) == (current.PlantId ?? 0) &&
					(candidate.ProductId ?? 0) == (current.ProductId ?? 0) &&
					(
						candidate.CreatedAt > current.CreatedAt ||
						(candidate.CreatedAt == current.CreatedAt &&
						 candidate.UpdatedAt > current.UpdatedAt) ||
						(candidate.CreatedAt == current.CreatedAt &&
						 candidate.UpdatedAt == current.UpdatedAt &&
						 candidate.Id > current.Id)
					)));

			var rows = await latestQuery
				.GroupBy(x => x.StateId!.Value)
				.Select(group => new StateQuantity
				{
					StateId = group.Key,
					Quantity = group.Sum(x => x.ClosingBalance)
				})
				.ToListAsync(cancellationToken);

			return rows.ToDictionary(x => x.StateId, x => x.Quantity);
		}

		/// <summary>
		/// Uses one latest warehouse row per warehouse/location/product business key
		/// on or before the requested date.
		/// </summary>
		private async Task<Dictionary<int, decimal>> LoadLatestWarehouseStockByStateAsync(
			IReadOnlyCollection<int> stateIds,
			DateTime asOnDate,
			CancellationToken cancellationToken)
		{
			var asOnExclusive = asOnDate.Date.AddDays(1);

			var baseQuery = _db.WarehouseDistrictGlobalStockReconciliations
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.CreatedAt < asOnExclusive);

			if (stateIds.Count > 0)
			{
				baseQuery = baseQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));
			}

			var latestQuery = baseQuery.Where(current =>
				!baseQuery.Any(candidate =>
					(candidate.StateId ?? 0) == (current.StateId ?? 0) &&
					(candidate.DistrictId ?? 0) == (current.DistrictId ?? 0) &&
					(candidate.WarehouseId ?? 0) == (current.WarehouseId ?? 0) &&
					(candidate.PlantId ?? 0) == (current.PlantId ?? 0) &&
					(candidate.ProductId ?? 0) == (current.ProductId ?? 0) &&
					(
						candidate.CreatedAt > current.CreatedAt ||
						(candidate.CreatedAt == current.CreatedAt &&
						 candidate.UpdatedAt > current.UpdatedAt) ||
						(candidate.CreatedAt == current.CreatedAt &&
						 candidate.UpdatedAt == current.UpdatedAt &&
						 candidate.Id > current.Id)
					)));

			var rows = await latestQuery
				.GroupBy(x => x.StateId!.Value)
				.Select(group => new StateQuantity
				{
					StateId = group.Key,
					Quantity = group.Sum(x => x.ClosingStock)
				})
				.ToListAsync(cancellationToken);

			return rows.ToDictionary(x => x.StateId, x => x.Quantity);
		}

		// =====================================================================
		// Combined sales loading
		// =====================================================================

		/// <summary>
		/// Adds sales movements from:
		/// - SalesWholesaler.QuantityMT
		/// - SalesCompanySale.QuantityMT
		/// - DptReport.SoldQuantity
		///
		/// Company/wholesaler sales use InvoiceDate. DPT sales use CreatedAt,
		/// which is the selected report date saved by the upload service.
		/// </summary>
		private async Task<Dictionary<int, SalesAggregate>> LoadSalesByStateAsync(
			IReadOnlyCollection<int> stateIds,
			DateTime periodStart,
			DateTime asOnDate,
			DateTime asOnNextDay,
			CancellationToken cancellationToken)
		{
			var wholesalerQuery = _db.SalesWholesalers
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.InvoiceDate.HasValue &&
					x.InvoiceDate.Value >= periodStart &&
					x.InvoiceDate.Value < asOnNextDay);

			var companyQuery = _db.SalesCompanySales
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.InvoiceDate.HasValue &&
					x.InvoiceDate.Value >= periodStart &&
					x.InvoiceDate.Value < asOnNextDay);

			var dptQuery = _db.DptReports
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.CreatedAt >= periodStart &&
					x.CreatedAt < asOnNextDay &&
					x.SoldQuantity != 0m);

			if (stateIds.Count > 0)
			{
				wholesalerQuery = wholesalerQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));

				companyQuery = companyQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));

				dptQuery = dptQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));
			}

			// Protect DPT sales from accidental duplicate rows for the same report
			// date and business key. Historical report dates remain additive sales.
			var deduplicatedDptQuery = dptQuery.Where(current =>
				!dptQuery.Any(candidate =>
					candidate.CreatedAt == current.CreatedAt &&
					(candidate.StateId ?? 0) == (current.StateId ?? 0) &&
					(candidate.DistrictId ?? 0) == (current.DistrictId ?? 0) &&
					(candidate.SubDistrictId ?? 0) == (current.SubDistrictId ?? 0) &&
					(candidate.DealerRegistrationId ?? 0) ==
						(current.DealerRegistrationId ?? 0) &&
					(candidate.DealerRegistrationId.HasValue
						? 0
						: candidate.IfmsDealerId ?? 0) ==
					(current.DealerRegistrationId.HasValue
						? 0
						: current.IfmsDealerId ?? 0) &&
					(candidate.DealerRegistrationId.HasValue || candidate.IfmsDealerId.HasValue
						? string.Empty
						: candidate.RetailerName ?? string.Empty) ==
					(current.DealerRegistrationId.HasValue || current.IfmsDealerId.HasValue
						? string.Empty
						: current.RetailerName ?? string.Empty) &&
					(candidate.CompanyId ?? 0) == (current.CompanyId ?? 0) &&
					(candidate.PlantId ?? 0) == (current.PlantId ?? 0) &&
					(candidate.ProductId ?? 0) == (current.ProductId ?? 0) &&
					(
						candidate.UpdatedAt > current.UpdatedAt ||
						(candidate.UpdatedAt == current.UpdatedAt &&
						 candidate.Id > current.Id)
					)));

			var combinedQuery = wholesalerQuery
				.Select(x => new SalesSourceRow
				{
					StateId = x.StateId!.Value,
					ActivityDate = x.InvoiceDate!.Value,
					Quantity = x.QuantityMT
				})
				.Concat(
					companyQuery.Select(x => new SalesSourceRow
					{
						StateId = x.StateId!.Value,
						ActivityDate = x.InvoiceDate!.Value,
						Quantity = x.QuantityMT
					}))
				.Concat(
					deduplicatedDptQuery.Select(x => new SalesSourceRow
					{
						StateId = x.StateId!.Value,
						ActivityDate = x.CreatedAt,
						Quantity = x.SoldQuantity
					}));

			var aggregates = await combinedQuery
				.GroupBy(x => x.StateId)
				.Select(group => new SalesAggregate
				{
					StateId = group.Key,
					SalesBefore = group.Sum(x =>
						x.ActivityDate < asOnDate ? x.Quantity : 0m),
					SalesOnDay = group.Sum(x =>
						x.ActivityDate >= asOnDate ? x.Quantity : 0m)
				})
				.ToListAsync(cancellationToken);

			return aggregates.ToDictionary(x => x.StateId);
		}

		// =====================================================================
		// Quantity merge helper
		// =====================================================================

		private static void MergeQuantities(
			IDictionary<int, decimal> destination,
			IReadOnlyDictionary<int, decimal> source)
		{
			foreach (var item in source)
			{
				destination[item.Key] = destination.TryGetValue(
					item.Key,
					out var existing)
					? existing + item.Value
					: item.Value;
			}
		}

		// =====================================================================
		// State names and row calculations
		// =====================================================================

		private async Task<Dictionary<int, string>> LoadStateNamesAsync(
			IReadOnlyCollection<int> stateIds,
			CancellationToken cancellationToken)
		{
			if (stateIds.Count == 0)
			{
				return new Dictionary<int, string>();
			}

			return await _db.Set<State>()
				.AsNoTracking()
				.Where(x => stateIds.Contains(x.Id))
				.Select(x => new
				{
					x.Id,
					x.StateName
				})
				.ToDictionaryAsync(
					x => x.Id,
					x => x.StateName ?? string.Empty,
					cancellationToken);
		}

		private static List<StockDetailsRowDto> BuildRows(
			IReadOnlyCollection<int> stateIds,
			IReadOnlyDictionary<int, decimal> openingStockByState,
			IReadOnlyDictionary<int, decimal> closingStockByState,
			IReadOnlyDictionary<int, SalesAggregate> salesByState,
			IReadOnlyDictionary<int, string> stateNames)
		{
			var rows = new List<StockDetailsRowDto>(stateIds.Count);

			foreach (var stateId in stateIds)
			{
				openingStockByState.TryGetValue(stateId, out var openingStock);
				closingStockByState.TryGetValue(stateId, out var closingStock);
				salesByState.TryGetValue(stateId, out var sales);

				rows.Add(MergeRow(
					stateId,
					stateNames.TryGetValue(stateId, out var stateName)
						? stateName
						: "-",
					openingStock,
					closingStock,
					sales?.SalesBefore ?? 0m,
					sales?.SalesOnDay ?? 0m));
			}

			return rows;
		}

		/// <summary>
		/// The three stock tables already store closing/current stock snapshots.
		/// Therefore current stock must not be reduced by sales a second time.
		///
		/// To preserve the existing UI columns, the ledger is reconciled as:
		/// TotalSales = SalesBefore + SalesOnDay
		/// ClosingStock = latest combined stock snapshot as on DateTo
		/// TotalStock = ClosingStock + TotalSales
		/// Supplies = TotalStock - OpeningStock
		///
		/// Supplies is therefore the net inward/adjustment required to reconcile
		/// opening stock, sales and closing stock for the selected period.
		/// </summary>
		private static StockDetailsRowDto MergeRow(
			int stateId,
			string stateName,
			decimal openingStock,
			decimal closingStock,
			decimal salesBefore,
			decimal salesOnDay)
		{
			var totalSales = salesBefore + salesOnDay;
			var totalStock = closingStock + totalSales;
			var supplies = totalStock - openingStock;

			return new StockDetailsRowDto
			{
				StateId = stateId,
				StateName = stateName,
				OpeningStock = openingStock,
				Supplies = supplies,
				TotalStock = totalStock,
				SalesBefore = salesBefore,
				SalesOnDay = salesOnDay,
				TotalSales = totalSales,
				ClosingStock = closingStock,
				SalesPct = Percentage(totalSales, totalStock)
			};
		}

		private static StockDetailsRowDto BuildGrandTotal(
			IReadOnlyCollection<StockDetailsRowDto> rows)
		{
			var grandTotal = new StockDetailsRowDto
			{
				StateId = 0,
				StateName = "Grand Total",
				OpeningStock = rows.Sum(x => x.OpeningStock),
				Supplies = rows.Sum(x => x.Supplies),
				TotalStock = rows.Sum(x => x.TotalStock),
				SalesBefore = rows.Sum(x => x.SalesBefore),
				SalesOnDay = rows.Sum(x => x.SalesOnDay),
				TotalSales = rows.Sum(x => x.TotalSales),
				ClosingStock = rows.Sum(x => x.ClosingStock)
			};

			grandTotal.SalesPct = Percentage(
				grandTotal.TotalSales,
				grandTotal.TotalStock);

			return grandTotal;
		}

		private static List<StockDetailsRowDto> ApplySorting(
			IEnumerable<StockDetailsRowDto> rows,
			StockDetailsFilter filter)
		{
			var sortColumn = filter.SortColumn?.Trim().ToLowerInvariant();
			var descending = string.Equals(
				filter.SortDir?.Trim(),
				"desc",
				StringComparison.OrdinalIgnoreCase);

			return sortColumn switch
			{
				"totalstock" => descending
					? rows.OrderByDescending(x => x.TotalStock)
						.ThenBy(x => x.StateName)
						.ToList()
					: rows.OrderBy(x => x.TotalStock)
						.ThenBy(x => x.StateName)
						.ToList(),

				"totalsales" => descending
					? rows.OrderByDescending(x => x.TotalSales)
						.ThenBy(x => x.StateName)
						.ToList()
					: rows.OrderBy(x => x.TotalSales)
						.ThenBy(x => x.StateName)
						.ToList(),

				"closing" => descending
					? rows.OrderByDescending(x => x.ClosingStock)
						.ThenBy(x => x.StateName)
						.ToList()
					: rows.OrderBy(x => x.ClosingStock)
						.ThenBy(x => x.StateName)
						.ToList(),

				"salespct" => descending
					? rows.OrderByDescending(x => x.SalesPct)
						.ThenBy(x => x.StateName)
						.ToList()
					: rows.OrderBy(x => x.SalesPct)
						.ThenBy(x => x.StateName)
						.ToList(),

				"state" => descending
					? rows.OrderByDescending(x => x.StateName).ToList()
					: rows.OrderBy(x => x.StateName).ToList(),

				_ => rows.OrderBy(x => x.StateName).ToList()
			};
		}

		private static ReportingPeriod ResolvePeriod(StockDetailsFilter filter)
		{
			var today = DateTime.UtcNow.Date;

			var rangeStart = filter.DateFrom?.Date ??
				new DateTime(today.Year, today.Month, 1);

			var rangeEnd = filter.DateTo?.Date ?? today;

			if (rangeEnd < rangeStart)
			{
				rangeEnd = rangeStart;
			}

			var periodStart = DateTime.SpecifyKind(
				rangeStart,
				DateTimeKind.Utc);

			var asOnDate = DateTime.SpecifyKind(
				rangeEnd,
				DateTimeKind.Utc);

			return new ReportingPeriod
			{
				PeriodStart = periodStart,
				AsOnDate = asOnDate,
				AsOnNextDay = asOnDate.AddDays(1)
			};
		}

		private static StockDetailsLabelsDto BuildLabels(
			DateTime periodStart,
			DateTime asOnDate)
		{
			var sameMonth =
				periodStart.Year == asOnDate.Year &&
				periodStart.Month == asOnDate.Month;

			var beforeEnd = asOnDate.AddDays(-1);

			var suppliesLabel = sameMonth
				? periodStart.ToString("MMMM", LabelCulture)
				: $"{periodStart.ToString("d MMM", LabelCulture)} - " +
				  $"{asOnDate.ToString("d MMM", LabelCulture)}";

			string salesBeforeRange;

			if (beforeEnd < periodStart)
			{
				salesBeforeRange = "-";
			}
			else if (
				periodStart.Year == beforeEnd.Year &&
				periodStart.Month == beforeEnd.Month)
			{
				salesBeforeRange =
					$"{periodStart.Day}-{beforeEnd.Day} " +
					periodStart.ToString("MMM", LabelCulture);
			}
			else
			{
				salesBeforeRange =
					$"{periodStart.ToString("d MMM", LabelCulture)} - " +
					$"{beforeEnd.ToString("d MMM", LabelCulture)}";
			}

			return new StockDetailsLabelsDto
			{
				OpeningAsOn = periodStart.ToString("d MMM", LabelCulture),
				SuppliesMonth = suppliesLabel,
				SalesBeforeRange = salesBeforeRange,
				SalesOnDay = asOnDate.ToString("d MMM", LabelCulture),
				ClosingAsOn = asOnDate.ToString("d MMM", LabelCulture)
			};
		}

		private static double Percentage(decimal numerator, decimal denominator)
		{
			return denominator == 0m
				? 0d
				: (double)(numerator / denominator) * 100d;
		}

		private static void NormalizeFilter(StockDetailsFilter filter)
		{
			filter.StateIds ??= new List<int>();
			filter.FinancialYearIds ??= new List<int>();

			filter.StateIds = filter.StateIds
				.Where(x => x > 0)
				.Distinct()
				.ToList();

			filter.FinancialYearIds = filter.FinancialYearIds
				.Where(x => x > 0)
				.Distinct()
				.ToList();

			filter.Page = Math.Max(1, filter.Page);

			if (filter.PageSize != int.MaxValue)
			{
				filter.PageSize = filter.PageSize <= 0
					? DefaultPageSize
					: filter.PageSize;
			}

			filter.SortDir = string.Equals(
				filter.SortDir,
				"desc",
				StringComparison.OrdinalIgnoreCase)
				? "desc"
				: "asc";
		}

		private sealed class ReportingPeriod
		{
			public DateTime PeriodStart { get; set; }
			public DateTime AsOnDate { get; set; }
			public DateTime AsOnNextDay { get; set; }
		}

		private sealed class StateQuantity
		{
			public int StateId { get; set; }
			public decimal Quantity { get; set; }
		}

		private sealed class SalesSourceRow
		{
			public int StateId { get; set; }
			public DateTime ActivityDate { get; set; }
			public decimal Quantity { get; set; }
		}

		private sealed class SalesAggregate
		{
			public int StateId { get; set; }
			public decimal SalesBefore { get; set; }
			public decimal SalesOnDay { get; set; }
		}
	}
}