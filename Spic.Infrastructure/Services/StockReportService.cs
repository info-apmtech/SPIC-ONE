using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
	/// <summary>
	/// Production-safe Stock Report implementation.
	///
	/// IMPORTANT SNAPSHOT RULE:
	/// WholesalerStockAsOnToday, DptReport and
	/// WarehouseDistrictGlobalStockReconciliation are stock snapshots.
	/// Historical snapshot dates are never added together.
	///
	/// For each source, the service selects the latest available report day on or
	/// before the required as-of date, removes duplicate business rows from that
	/// snapshot day, and then totals the selected snapshot.
	///
	/// Example for the same dealer/product:
	/// 01-Aug = 10, 02-Aug = 15, 03-Aug = 12 -> current stock is 12, not 37.
	///
	/// AGEING SOURCES:
	/// 1. Company Sales: Status.Name = Ack + RetailerReceiptDate.
	/// 2. Wholesaler Sales: Status.Name = Ack + RetailerReceiptDate.
	/// 3. Retailer Sales: DptReport.SoldQuantity > 0, using DPT CreatedAt report date
	///    only as an ACK-equivalent fallback for DPT/retailer stock because the
	///    supplied DPT model has no StatusId or RetailerReceiptDate.
	/// </summary>
	public sealed class StockReportService : IStockReportService
	{
		private readonly AppDbContext _db;

		// ACK-based stock-ageing buckets. Boundaries are non-overlapping:
		// Fresh 0-30, Medium 31-90, Slow Moving 91-180,
		// Long Aged 181-364 and Critical 365+.
		private const int FreshMax = 30;
		private const int MediumMax = 90;
		private const int SlowMax = 180;
		private const int CriticalMin = 365;

		private const string AckPendingStatus = "ACK Pending";
		private const string AckUnavailableStatus = "ACK Not Available";

		private const string WholesalerStockSource = "Wholesaler Stock";
		private const string RetailerStockSource = "Retailer Stock";
		private const string WarehouseStockSource = "Warehouse Stock";

		private static readonly string[] ProductPalette =
		{
			"#059669",
			"#f59e0b",
			"#6366f1",
			"#ef4444",
			"#0ea5e9",
			"#ec4899",
			"#14b8a6",
			"#f97316"
		};

		public StockReportService(AppDbContext db)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
		}

		public async Task<List<ProductFilterOptionDto>> GetProductOptionsAsync()
		{
			// Keep the approved Product master unchanged and add IFMS products as a
			// second typed source. Prefixes prevent Product.Id and IfmsProduct.Id
			// having the same number from being treated as the same product.
			var approvedProducts = await _db.Set<Product>()
				.AsNoTracking()
				.Select(x => new
				{
					x.Id,
					x.Name
				})
				.ToListAsync();

			var ifmsProducts = await _db.Set<IfmsProduct>()
				.AsNoTracking()
				.Select(x => new
				{
					x.Id,
					x.Name
				})
				.ToListAsync();

			return approvedProducts
				.Where(x => !string.IsNullOrWhiteSpace(x.Name))
				.Select(x => new ProductFilterOptionDto
				{
					Value = $"P:{x.Id}",
					Name = x.Name!.Trim(),
					Source = "Product"
				})
				.Concat(ifmsProducts
					.Where(x => !string.IsNullOrWhiteSpace(x.Name))
					.Select(x => new ProductFilterOptionDto
					{
						Value = $"I:{x.Id}",
						Name = $"{x.Name!.Trim()} (IFMS)",
						Source = "IFMS"
					}))
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.Value, StringComparer.Ordinal)
				.ToList();
		}

		public async Task<StockDashboardDto> GetDashboardAsync(
			StockReportFilter filter)
		{
			filter ??= new StockReportFilter();
			NormalizeFilter(filter);

			var today = DateTime.UtcNow.Date;

			// As-of boundaries are exclusive and include the complete target day.
			var currentAsOfExclusive = today.AddDays(1);
			var yesterdayAsOfExclusive = today;
			var lastMonthAsOfExclusive = today.AddMonths(-1).AddDays(1);
			var previousYearAsOfExclusive = today.AddYears(-1).AddDays(1);

			// -----------------------------------------------------------------
			// 1. Locate the available snapshot days once for each source.
			// -----------------------------------------------------------------
			var wholesalerAvailableDates = await GetWholesalerAvailableDatesAsync(
				currentAsOfExclusive);

			var dptAvailableDates = await GetDptAvailableDatesAsync(
				currentAsOfExclusive);

			var warehouseAvailableDates = await GetWarehouseAvailableDatesAsync(
				currentAsOfExclusive);

			var currentWholesalerDate = LatestDateBefore(
				wholesalerAvailableDates,
				currentAsOfExclusive);

			var yesterdayWholesalerDate = LatestDateBefore(
				wholesalerAvailableDates,
				yesterdayAsOfExclusive);

			var lastMonthWholesalerDate = LatestDateBefore(
				wholesalerAvailableDates,
				lastMonthAsOfExclusive);

			var previousYearWholesalerDate = LatestDateBefore(
				wholesalerAvailableDates,
				previousYearAsOfExclusive);

			var currentDptDate = LatestDateBefore(
				dptAvailableDates,
				currentAsOfExclusive);

			var yesterdayDptDate = LatestDateBefore(
				dptAvailableDates,
				yesterdayAsOfExclusive);

			var lastMonthDptDate = LatestDateBefore(
				dptAvailableDates,
				lastMonthAsOfExclusive);

			var previousYearDptDate = LatestDateBefore(
				dptAvailableDates,
				previousYearAsOfExclusive);

			var currentWarehouseDate = LatestDateBefore(
				warehouseAvailableDates,
				currentAsOfExclusive);

			var yesterdayWarehouseDate = LatestDateBefore(
				warehouseAvailableDates,
				yesterdayAsOfExclusive);

			var lastMonthWarehouseDate = LatestDateBefore(
				warehouseAvailableDates,
				lastMonthAsOfExclusive);

			var previousYearWarehouseDate = LatestDateBefore(
				warehouseAvailableDates,
				previousYearAsOfExclusive);

			// -----------------------------------------------------------------
			// 2. Load only the snapshot days required by this dashboard request.
			// -----------------------------------------------------------------
			var wholesalerSnapshotMap = await LoadWholesalerSnapshotsAsync(
				filter,
				DistinctDates(
					currentWholesalerDate,
					yesterdayWholesalerDate,
					lastMonthWholesalerDate,
					previousYearWholesalerDate));

			var dptSnapshotMap = await LoadDptSnapshotsAsync(
				filter,
				DistinctDates(
					currentDptDate,
					yesterdayDptDate,
					lastMonthDptDate,
					previousYearDptDate));

			var warehouseSnapshotMap = await LoadWarehouseSnapshotsAsync(
				filter,
				DistinctDates(
					currentWarehouseDate,
					yesterdayWarehouseDate,
					lastMonthWarehouseDate,
					previousYearWarehouseDate));

			var currentWholesalerRows = GetSnapshotRows(
				wholesalerSnapshotMap,
				currentWholesalerDate);

			var yesterdayWholesalerRows = GetSnapshotRows(
				wholesalerSnapshotMap,
				yesterdayWholesalerDate);

			var lastMonthWholesalerRows = GetSnapshotRows(
				wholesalerSnapshotMap,
				lastMonthWholesalerDate);

			var previousYearWholesalerRows = GetSnapshotRows(
				wholesalerSnapshotMap,
				previousYearWholesalerDate);

			var currentDptRows = GetSnapshotRows(
				dptSnapshotMap,
				currentDptDate);

			var yesterdayDptRows = GetSnapshotRows(
				dptSnapshotMap,
				yesterdayDptDate);

			var lastMonthDptRows = GetSnapshotRows(
				dptSnapshotMap,
				lastMonthDptDate);

			var previousYearDptRows = GetSnapshotRows(
				dptSnapshotMap,
				previousYearDptDate);

			var currentWarehouseRows = GetSnapshotRows(
				warehouseSnapshotMap,
				currentWarehouseDate);

			var yesterdayWarehouseRows = GetSnapshotRows(
				warehouseSnapshotMap,
				yesterdayWarehouseDate);

			var lastMonthWarehouseRows = GetSnapshotRows(
				warehouseSnapshotMap,
				lastMonthWarehouseDate);

			var previousYearWarehouseRows = GetSnapshotRows(
				warehouseSnapshotMap,
				previousYearWarehouseDate);

			// -----------------------------------------------------------------
			// 3. Ageing lookup for dealer-held stock. Strict sources require
			// Status.Name = "Ack" and RetailerReceiptDate. DPT SoldQuantity is
			// included as the retailer-sales fallback described in BuildAckLookupAsync.
			// -----------------------------------------------------------------
			var acknowledgementLookup = await BuildAckLookupAsync(
				currentWholesalerRows
					.Concat(yesterdayWholesalerRows)
					.Concat(lastMonthWholesalerRows)
					.Concat(previousYearWholesalerRows),
				currentDptRows
					.Concat(yesterdayDptRows)
					.Concat(lastMonthDptRows)
					.Concat(previousYearDptRows));

			// Ageing filters apply only to rows having a valid Status=Ack receipt
			// date. Warehouse rows have no confirmed dealer ACK relationship and
			// are therefore excluded only when an ageing filter is selected.
			currentWholesalerRows = ApplyWholesalerAgeingFilter(
				currentWholesalerRows, filter, today, acknowledgementLookup);
			yesterdayWholesalerRows = ApplyWholesalerAgeingFilter(
				yesterdayWholesalerRows, filter, today, acknowledgementLookup);
			lastMonthWholesalerRows = ApplyWholesalerAgeingFilter(
				lastMonthWholesalerRows, filter, today, acknowledgementLookup);
			previousYearWholesalerRows = ApplyWholesalerAgeingFilter(
				previousYearWholesalerRows, filter, today, acknowledgementLookup);

			currentDptRows = ApplyDptAgeingFilter(
				currentDptRows, filter, today, acknowledgementLookup);
			yesterdayDptRows = ApplyDptAgeingFilter(
				yesterdayDptRows, filter, today, acknowledgementLookup);
			lastMonthDptRows = ApplyDptAgeingFilter(
				lastMonthDptRows, filter, today, acknowledgementLookup);
			previousYearDptRows = ApplyDptAgeingFilter(
				previousYearDptRows, filter, today, acknowledgementLookup);

			if (filter.AgeingRanges.Count > 0)
			{
				currentWarehouseRows.Clear();
				yesterdayWarehouseRows.Clear();
				lastMonthWarehouseRows.Clear();
				previousYearWarehouseRows.Clear();
			}

			// -----------------------------------------------------------------
			// 4. Summary cards - current snapshots only, never historical sums.
			// -----------------------------------------------------------------
			var currentWholesalerTotal = currentWholesalerRows.Sum(x => x.Stock);
			var currentDptTotal = currentDptRows.Sum(x => x.ClosingBalance);
			var currentWarehouseTotal = currentWarehouseRows.Sum(x => x.ClosingStock);

			var totalStock =
				currentWholesalerTotal +
				currentDptTotal +
				currentWarehouseTotal;

			var lastMonthStock =
				lastMonthWholesalerRows.Sum(x => x.Stock) +
				lastMonthDptRows.Sum(x => x.ClosingBalance) +
				lastMonthWarehouseRows.Sum(x => x.ClosingStock);

			var totalStockChangePct = lastMonthStock == 0m
				? 0m
				: Math.Round(
					((totalStock - lastMonthStock) / lastMonthStock) * 100m,
					1);

			// Count every unique stock holder from the three current snapshot sources.
			// The same DealerRegistrationId / IfmsDealerId appearing in wholesaler stock
			// and DPT is counted once. Warehouse reconciliation has no dealer column,
			// so each distinct WarehouseId is counted as one additional stock holder.
			var dealerCount = CountUniqueDealers(
				currentWholesalerRows,
				currentDptRows,
				currentWarehouseRows);

			var todayDealerCount = dealerCount;

			var yesterdayDealerCount = CountUniqueDealers(
				yesterdayWholesalerRows,
				yesterdayDptRows,
				yesterdayWarehouseRows);

			double ageingTotal = 0d;
			var ageingRowCount = 0;
			var highAgeingCount = 0;
			decimal highAgeingStock = 0m;

			foreach (var row in currentWholesalerRows)
			{
				var ageingDays = AckAgeForWholesalerRow(
					acknowledgementLookup,
					today,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId,
					row.IfmsProductId);

				AddAgeingSummary(
					ageingDays,
					row.Stock,
					ref ageingTotal,
					ref ageingRowCount,
					ref highAgeingCount,
					ref highAgeingStock);
			}

			foreach (var row in currentDptRows)
			{
				var ageingDays = AckAgeForDptRow(
					acknowledgementLookup,
					today,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId,
					row.IfmsProductId);

				AddAgeingSummary(
					ageingDays,
					row.ClosingBalance,
					ref ageingTotal,
					ref ageingRowCount,
					ref highAgeingCount,
					ref highAgeingStock);
			}

			var averageAgeing = ageingRowCount == 0
				? 0d
				: Math.Round(ageingTotal / ageingRowCount, 0);

			var summary = new SummaryDto
			{
				TotalStock = totalStock,
				TotalStockChangePct = totalStockChangePct,
				DealerCount = dealerCount,
				TodayDealerCount = todayDealerCount,
				YesterdayDealerCount = yesterdayDealerCount,
				AverageAgeing = averageAgeing,
				AverageAgeingChange = 0d,
				HighAgeingCount = highAgeingCount,
				HighAgeingChange = 0,
				HighAgeingStock = highAgeingStock
			};

			// -----------------------------------------------------------------
			// 5. State-wise chart: current snapshot vs previous-year as-of snapshot.
			// -----------------------------------------------------------------
			var stateAccumulator = new Dictionary<int, StateAccumulator>();

			AddStateRows(
				stateAccumulator,
				currentWholesalerRows,
				x => x.StateId,
				x => x.Stock,
				isCurrent: true);

			AddStateRows(
				stateAccumulator,
				currentDptRows,
				x => x.StateId,
				x => x.ClosingBalance,
				isCurrent: true);

			AddStateRows(
				stateAccumulator,
				currentWarehouseRows,
				x => x.StateId,
				x => x.ClosingStock,
				isCurrent: true);

			AddStateRows(
				stateAccumulator,
				previousYearWholesalerRows,
				x => x.StateId,
				x => x.Stock,
				isCurrent: false);

			AddStateRows(
				stateAccumulator,
				previousYearDptRows,
				x => x.StateId,
				x => x.ClosingBalance,
				isCurrent: false);

			AddStateRows(
				stateAccumulator,
				previousYearWarehouseRows,
				x => x.StateId,
				x => x.ClosingStock,
				isCurrent: false);

			var stateIds = stateAccumulator.Keys.ToList();
			var stateNames = stateIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<State>()
					.AsNoTracking()
					.Where(x => stateIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.StateName ?? string.Empty);

			var stateWise = stateAccumulator
				.Select(item => new StateStockDto
				{
					StateName = stateNames.TryGetValue(item.Key, out var stateName)
						? stateName
						: $"State {item.Key}",
					CurrentYear = item.Value.Current,
					PreviousYear = item.Value.Previous
				})
				.OrderByDescending(x => x.CurrentYear)
				.ThenBy(x => x.StateName)
				.ToList();

			// -----------------------------------------------------------------
			// 6. Product-wise chart: current snapshots only.
			// Approved products and IFMS products share the chart, but their IDs
			// remain isolated through P:<id> and I:<id> identity keys.
			// -----------------------------------------------------------------
			var productAccumulator =
				new Dictionary<string, decimal>(StringComparer.Ordinal);

			AddProductRows(
				productAccumulator,
				currentWholesalerRows,
				x => x.ProductId,
				x => x.IfmsProductId,
				x => x.Stock);

			AddProductRows(
				productAccumulator,
				currentDptRows,
				x => x.ProductId,
				x => x.IfmsProductId,
				x => x.ClosingBalance);

			AddProductRows(
				productAccumulator,
				currentWarehouseRows,
				x => x.ProductId,
				x => x.IfmsProductId,
				x => x.ClosingStock);

			var productNames = await LoadProductNameLookupAsync(
				productAccumulator.Keys);

			var productTotal = productAccumulator.Values.Sum();

			var productWise = productAccumulator
				.OrderByDescending(item => item.Value)
				.ThenBy(item => item.Key, StringComparer.Ordinal)
				.Select((item, index) => new ProductStockDto
				{
					ProductName = GetProductName(productNames, item.Key),
					Quantity = item.Value,
					Percentage = productTotal == 0m
						? 0d
						: Math.Round((double)(item.Value / productTotal) * 100d, 2),
					Color = ProductPalette[index % ProductPalette.Length]
				})
				.ToList();

			// -----------------------------------------------------------------
			// 7. Grid: all three latest current stock snapshots.
			// -----------------------------------------------------------------
			var grid = await BuildGridAsync(
				currentWholesalerRows,
				currentDptRows,
				currentWarehouseRows,
				filter,
				today,
				acknowledgementLookup,
				paged: true);

			return new StockDashboardDto
			{
				Summary = summary,
				StateWise = stateWise,
				ProductWise = productWise,
				Grid = grid
			};
		}

		public async Task<List<StockRowDto>> GetAllRowsAsync(
			StockReportFilter filter)
		{
			filter ??= new StockReportFilter();
			NormalizeFilter(filter);

			var today = DateTime.UtcNow.Date;
			var currentAsOfExclusive = today.AddDays(1);

			var wholesalerAvailableDates = await GetWholesalerAvailableDatesAsync(
				currentAsOfExclusive);

			var dptAvailableDates = await GetDptAvailableDatesAsync(
				currentAsOfExclusive);

			var warehouseAvailableDates = await GetWarehouseAvailableDatesAsync(
				currentAsOfExclusive);

			var currentWholesalerDate = LatestDateBefore(
				wholesalerAvailableDates,
				currentAsOfExclusive);

			var currentDptDate = LatestDateBefore(
				dptAvailableDates,
				currentAsOfExclusive);

			var currentWarehouseDate = LatestDateBefore(
				warehouseAvailableDates,
				currentAsOfExclusive);

			var wholesalerSnapshotMap = await LoadWholesalerSnapshotsAsync(
				filter,
				DistinctDates(currentWholesalerDate));

			var dptSnapshotMap = await LoadDptSnapshotsAsync(
				filter,
				DistinctDates(currentDptDate));

			var warehouseSnapshotMap = await LoadWarehouseSnapshotsAsync(
				filter,
				DistinctDates(currentWarehouseDate));

			var currentWholesalerRows = GetSnapshotRows(
				wholesalerSnapshotMap,
				currentWholesalerDate);

			var currentDptRows = GetSnapshotRows(
				dptSnapshotMap,
				currentDptDate);

			var currentWarehouseRows = GetSnapshotRows(
				warehouseSnapshotMap,
				currentWarehouseDate);

			var acknowledgementLookup = await BuildAckLookupAsync(
				currentWholesalerRows,
				currentDptRows);

			currentWholesalerRows = ApplyWholesalerAgeingFilter(
				currentWholesalerRows, filter, today, acknowledgementLookup);
			currentDptRows = ApplyDptAgeingFilter(
				currentDptRows, filter, today, acknowledgementLookup);

			if (filter.AgeingRanges.Count > 0)
			{
				currentWarehouseRows.Clear();
			}

			var result = await BuildGridAsync(
				currentWholesalerRows,
				currentDptRows,
				currentWarehouseRows,
				filter,
				today,
				acknowledgementLookup,
				paged: false);

			return result.Items;
		}

		// =====================================================================
		// Snapshot date discovery
		// =====================================================================

		private async Task<List<DateTime>> GetWholesalerAvailableDatesAsync(
			DateTime asOfExclusive)
		{
			return await _db.Set<WholesalerStockAsOnToday>()
				.AsNoTracking()
				.Where(x => x.StockDate < asOfExclusive)
				.Select(x => x.StockDate.Date)
				.Distinct()
				.ToListAsync();
		}

		private async Task<List<DateTime>> GetDptAvailableDatesAsync(
			DateTime asOfExclusive)
		{
			return await _db.Set<DptReport>()
				.AsNoTracking()
				.Where(x => x.CreatedAt < asOfExclusive)
				.Select(x => x.CreatedAt.Date)
				.Distinct()
				.ToListAsync();
		}

		private async Task<List<DateTime>> GetWarehouseAvailableDatesAsync(
			DateTime asOfExclusive)
		{
			return await _db.Set<WarehouseDistrictGlobalStockReconciliation>()
				.AsNoTracking()
				.Where(x => x.CreatedAt < asOfExclusive)
				.Select(x => x.CreatedAt.Date)
				.Distinct()
				.ToListAsync();
		}

		private static DateTime? LatestDateBefore(
			IEnumerable<DateTime> availableDates,
			DateTime asOfExclusive)
		{
			return availableDates
				.Where(x => x.Date < asOfExclusive)
				.Select(x => x.Date)
				.OrderByDescending(x => x)
				.Cast<DateTime?>()
				.FirstOrDefault();
		}

		private static List<DateTime> DistinctDates(
			params DateTime?[] dates)
		{
			return dates
				.Where(x => x.HasValue)
				.Select(x => x!.Value.Date)
				.Distinct()
				.ToList();
		}

		// =====================================================================
		// Snapshot loading and same-day duplicate protection
		// =====================================================================

		private async Task<Dictionary<DateTime, List<WholesalerStockAsOnToday>>>
			LoadWholesalerSnapshotsAsync(
				StockReportFilter filter,
				List<DateTime> snapshotDates)
		{
			var result = new Dictionary<DateTime, List<WholesalerStockAsOnToday>>();

			if (snapshotDates.Count == 0)
			{
				return result;
			}

			var query = ApplyWholesalerDimensionFilters(
				_db.Set<WholesalerStockAsOnToday>().AsNoTracking(),
				filter);

			var rows = await query
				.Where(x => snapshotDates.Contains(x.StockDate.Date))
				.ToListAsync();

			var latestRows = rows
				.GroupBy(x => new
				{
					SnapshotDate = x.StockDate.Date,
					BusinessKey = WholesalerBusinessKey(x)
				})
				.Select(group => group
					.OrderByDescending(x => x.UpdatedAt)
					.ThenByDescending(x => x.Id)
					.First())
				.Where(x => x.Stock > 0m)
				.ToList();

			foreach (var group in latestRows.GroupBy(x => x.StockDate.Date))
			{
				result[group.Key] = group.ToList();
			}

			return result;
		}

		private async Task<Dictionary<DateTime, List<DptReport>>>
			LoadDptSnapshotsAsync(
				StockReportFilter filter,
				List<DateTime> snapshotDates)
		{
			var result = new Dictionary<DateTime, List<DptReport>>();

			if (snapshotDates.Count == 0)
			{
				return result;
			}

			var query = ApplyDptDimensionFilters(
				_db.Set<DptReport>().AsNoTracking(),
				filter);

			var rows = await query
				.Where(x => snapshotDates.Contains(x.CreatedAt.Date))
				.ToListAsync();

			var latestRows = rows
				.GroupBy(x => new
				{
					SnapshotDate = x.CreatedAt.Date,
					BusinessKey = DptBusinessKey(x)
				})
				.Select(group => group
					.OrderByDescending(x => x.UpdatedAt)
					.ThenByDescending(x => x.Id)
					.First())
				.Where(x => x.ClosingBalance > 0m)
				.ToList();

			foreach (var group in latestRows.GroupBy(x => x.CreatedAt.Date))
			{
				result[group.Key] = group.ToList();
			}

			return result;
		}

		private async Task<Dictionary<DateTime, List<WarehouseDistrictGlobalStockReconciliation>>>
			LoadWarehouseSnapshotsAsync(
				StockReportFilter filter,
				List<DateTime> snapshotDates)
		{
			var result = new Dictionary<DateTime, List<WarehouseDistrictGlobalStockReconciliation>>();

			if (snapshotDates.Count == 0)
			{
				return result;
			}

			var query = ApplyWarehouseDimensionFilters(
				_db.Set<WarehouseDistrictGlobalStockReconciliation>().AsNoTracking(),
				filter);

			var rows = await query
				.Where(x => snapshotDates.Contains(x.CreatedAt.Date))
				.ToListAsync();

			var latestRows = rows
				.GroupBy(x => new
				{
					SnapshotDate = x.CreatedAt.Date,
					BusinessKey = WarehouseBusinessKey(x)
				})
				.Select(group => group
					.OrderByDescending(x => x.UpdatedAt)
					.ThenByDescending(x => x.Id)
					.First())
				.Where(x => x.ClosingStock > 0m)
				.ToList();

			foreach (var group in latestRows.GroupBy(x => x.CreatedAt.Date))
			{
				result[group.Key] = group.ToList();
			}

			return result;
		}

		private static List<T> GetSnapshotRows<T>(
			Dictionary<DateTime, List<T>> snapshotMap,
			DateTime? snapshotDate)
		{
			if (!snapshotDate.HasValue)
			{
				return new List<T>();
			}

			return snapshotMap.TryGetValue(snapshotDate.Value.Date, out var rows)
				? rows
				: new List<T>();
		}

		// =====================================================================
		// Dimension filters
		// =====================================================================

		private static IQueryable<WholesalerStockAsOnToday>
			ApplyWholesalerDimensionFilters(
				IQueryable<WholesalerStockAsOnToday> query,
				StockReportFilter filter)
		{
			if (filter.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.ProductIds.Count > 0 && filter.IfmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					(x.ProductId.HasValue &&
					 filter.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue &&
					 filter.IfmsProductIds.Contains(x.IfmsProductId.Value)));
			}
			else if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
			}
			else if (filter.IfmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.IfmsProductId.HasValue &&
					filter.IfmsProductIds.Contains(x.IfmsProductId.Value));
			}

			if (filter.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					filter.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

			return query;
		}

		private static IQueryable<DptReport> ApplyDptDimensionFilters(
			IQueryable<DptReport> query,
			StockReportFilter filter)
		{
			if (filter.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.SubDistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.SubDistrictId.HasValue &&
					filter.SubDistrictIds.Contains(x.SubDistrictId.Value));
			}

			if (filter.ProductIds.Count > 0 && filter.IfmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					(x.ProductId.HasValue &&
					 filter.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue &&
					 filter.IfmsProductIds.Contains(x.IfmsProductId.Value)));
			}
			else if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
			}
			else if (filter.IfmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.IfmsProductId.HasValue &&
					filter.IfmsProductIds.Contains(x.IfmsProductId.Value));
			}

			if (filter.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					filter.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

			return query;
		}

		private static IQueryable<WarehouseDistrictGlobalStockReconciliation>
			ApplyWarehouseDimensionFilters(
				IQueryable<WarehouseDistrictGlobalStockReconciliation> query,
				StockReportFilter filter)
		{
			if (filter.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.ProductIds.Count > 0 && filter.IfmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					(x.ProductId.HasValue &&
					 filter.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue &&
					 filter.IfmsProductIds.Contains(x.IfmsProductId.Value)));
			}
			else if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
			}
			else if (filter.IfmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.IfmsProductId.HasValue &&
					filter.IfmsProductIds.Contains(x.IfmsProductId.Value));
			}

			return query;
		}

		// =====================================================================
		// Business keys used to remove old same-day duplicate rows safely
		// =====================================================================

		private static string WholesalerBusinessKey(
			WholesalerStockAsOnToday row)
		{
			return string.Join(
				"|",
				DealerBusinessKey(
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.AgencyName),
				ProductIdentity(row.ProductId, row.IfmsProductId),
				IdPart(row.StateId),
				IdPart(row.DistrictId),
				IdPart(row.CompanyId),
				IdPart(row.PlantId));
		}

		private static string DptBusinessKey(DptReport row)
		{
			return string.Join(
				"|",
				DealerBusinessKey(
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.RetailerName),
				ProductIdentity(row.ProductId, row.IfmsProductId),
				IdPart(row.StateId),
				IdPart(row.DistrictId),
				IdPart(row.CompanyId),
				IdPart(row.PlantId));
		}

		private static string WarehouseBusinessKey(
			WarehouseDistrictGlobalStockReconciliation row)
		{
			return string.Join(
				"|",
				IdPart(row.WarehouseId),
				IdPart(row.StateId),
				IdPart(row.DistrictId),
				IdPart(row.PlantId),
				ProductIdentity(row.ProductId, row.IfmsProductId));
		}

		private static string DealerBusinessKey(
			int? dealerRegistrationId,
			int? ifmsDealerId,
			string? dealerName)
		{
			if (dealerRegistrationId.HasValue)
			{
				return "R:" + dealerRegistrationId.Value;
			}

			if (ifmsDealerId.HasValue)
			{
				return "I:" + ifmsDealerId.Value;
			}

			return "N:" + NormalizeText(dealerName);
		}

		private static string IdPart(int? value)
		{
			return value.HasValue ? value.Value.ToString() : "0";
		}

		private static string NormalizeText(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: string.Join(
					" ",
					value.Trim()
						.ToLowerInvariant()
						.Split(
							new[] { ' ', '\t', '\r', '\n' },
							StringSplitOptions.RemoveEmptyEntries));
		}

		// =====================================================================
		// Acknowledgement and ageing
		// =====================================================================

		private async Task<StockAgeingLookup> BuildAckLookupAsync(
			IEnumerable<WholesalerStockAsOnToday> wholesalerRows,
			IEnumerable<DptReport> dptRows)
		{
			var keyRows = wholesalerRows
				.Select(x => new AckStockKey
				{
					DealerRegistrationId = x.DealerRegistrationId,
					IfmsDealerId = x.IfmsDealerId,
					ProductId = x.ProductId,
					IfmsProductId = x.IfmsProductId
				})
				.Concat(dptRows.Select(x => new AckStockKey
				{
					DealerRegistrationId = x.DealerRegistrationId,
					IfmsDealerId = x.IfmsDealerId,
					ProductId = x.ProductId,
					IfmsProductId = x.IfmsProductId
				}))
				.Where(x => x.ProductId.HasValue || x.IfmsProductId.HasValue)
				.ToList();

			var registrationIds = keyRows
				.Where(x => x.DealerRegistrationId.HasValue)
				.Select(x => x.DealerRegistrationId!.Value)
				.Distinct()
				.ToList();

			var ifmsIds = keyRows
				.Where(x => x.IfmsDealerId.HasValue)
				.Select(x => x.IfmsDealerId!.Value)
				.Distinct()
				.ToList();

			var productIds = keyRows
				.Where(x => x.ProductId.HasValue)
				.Select(x => x.ProductId!.Value)
				.Distinct()
				.ToList();

			var ifmsProductIds = keyRows
				.Where(x => x.IfmsProductId.HasValue)
				.Select(x => x.IfmsProductId!.Value)
				.Distinct()
				.ToList();

			var result = new StockAgeingLookup();

			if ((productIds.Count == 0 && ifmsProductIds.Count == 0) ||
				(registrationIds.Count == 0 && ifmsIds.Count == 0))
			{
				return result;
			}

			var hasApprovedProducts = productIds.Count > 0;
			var hasIfmsProducts = ifmsProductIds.Count > 0;

			// -------------------------------------------------------------
			// 1. Strict workflow ACK sources
			// -------------------------------------------------------------
			// A receipt date alone does not start strict ACK ageing. Company and
			// wholesaler transactions must have Status.Name = "Ack".
			var statusRows = await _db.Set<Status>()
				.AsNoTracking()
				.Where(x => x.Name != null)
				.Select(x => new { x.Id, x.Name })
				.ToListAsync();

			var ackStatusIds = statusRows
				.Where(x => string.Equals(
					x.Name?.Trim(),
					"Ack",
					StringComparison.OrdinalIgnoreCase))
				.Select(x => x.Id)
				.Distinct()
				.ToList();

			if (ackStatusIds.Count > 0)
			{
				var companyQuery = _db.Set<SalesCompanySale>()
					.AsNoTracking()
					.Where(x =>
						x.StatusId.HasValue &&
						ackStatusIds.Contains(x.StatusId.Value) &&
						x.RetailerReceiptDate.HasValue &&
						((hasApprovedProducts &&
						  x.ProductId.HasValue &&
						  productIds.Contains(x.ProductId.Value)) ||
						 (hasIfmsProducts &&
						  x.IfmsProductId.HasValue &&
						  ifmsProductIds.Contains(x.IfmsProductId.Value))));

				companyQuery = ApplyCompanyDealerFilter(
					companyQuery,
					registrationIds,
					ifmsIds);

				var companyRows = await companyQuery
					.GroupBy(x => new
					{
						x.DealerRegistrationId,
						x.IfmsDealerId,
						x.ProductId,
						x.IfmsProductId
					})
					.Select(group => new AckAggregateRow
					{
						RegistrationId = group.Key.DealerRegistrationId,
						IfmsId = group.Key.IfmsDealerId,
						ProductId = group.Key.ProductId,
						IfmsProductId = group.Key.IfmsProductId,
						AckDate = group.Max(x => x.RetailerReceiptDate)
					})
					.ToListAsync();

				foreach (var row in companyRows)
				{
					AddAcknowledgement(
						result.StrictAckDates,
						"R",
						row.RegistrationId,
						row.ProductId,
						row.IfmsProductId,
						row.AckDate);
					AddAcknowledgement(
						result.StrictAckDates,
						"I",
						row.IfmsId,
						row.ProductId,
						row.IfmsProductId,
						row.AckDate);
				}

				var wholesalerSalesQuery = _db.Set<SalesWholesaler>()
					.AsNoTracking()
					.Where(x =>
						x.StatusId.HasValue &&
						ackStatusIds.Contains(x.StatusId.Value) &&
						x.RetailerReceiptDate.HasValue &&
						((hasApprovedProducts &&
						  x.ProductId.HasValue &&
						  productIds.Contains(x.ProductId.Value)) ||
						 (hasIfmsProducts &&
						  x.IfmsProductId.HasValue &&
						  ifmsProductIds.Contains(x.IfmsProductId.Value))));

				wholesalerSalesQuery = ApplyWholesalerSalesDealerFilter(
					wholesalerSalesQuery,
					registrationIds,
					ifmsIds);

				var wholesalerSalesRows = await wholesalerSalesQuery
					.GroupBy(x => new
					{
						RegistrationId = x.DealerId,
						x.IfmsDealerId,
						x.ProductId,
						x.IfmsProductId
					})
					.Select(group => new AckAggregateRow
					{
						RegistrationId = group.Key.RegistrationId,
						IfmsId = group.Key.IfmsDealerId,
						ProductId = group.Key.ProductId,
						IfmsProductId = group.Key.IfmsProductId,
						AckDate = group.Max(x => x.RetailerReceiptDate)
					})
					.ToListAsync();

				foreach (var row in wholesalerSalesRows)
				{
					AddAcknowledgement(
						result.StrictAckDates,
						"R",
						row.RegistrationId,
						row.ProductId,
						row.IfmsProductId,
						row.AckDate);
					AddAcknowledgement(
						result.StrictAckDates,
						"I",
						row.IfmsId,
						row.ProductId,
						row.IfmsProductId,
						row.AckDate);
				}
			}

			// -------------------------------------------------------------
			// 2. Retailer sales source from DPT Report.SoldQuantity
			// -------------------------------------------------------------
			// DPT has no StatusId/RetailerReceiptDate, so SoldQuantity > 0 and
			// CreatedAt remain the existing ACK-equivalent fallback.
			var dptSalesQuery = _db.Set<DptReport>()
				.AsNoTracking()
				.Where(x =>
					x.SoldQuantity > 0m &&
					((hasApprovedProducts &&
					  x.ProductId.HasValue &&
					  productIds.Contains(x.ProductId.Value)) ||
					 (hasIfmsProducts &&
					  x.IfmsProductId.HasValue &&
					  ifmsProductIds.Contains(x.IfmsProductId.Value))) &&
					x.CreatedAt < DateTime.UtcNow.Date.AddDays(1));

			dptSalesQuery = ApplyDptDealerFilter(
				dptSalesQuery,
				registrationIds,
				ifmsIds);

			var dptSalesRows = await dptSalesQuery
				.GroupBy(x => new
				{
					x.DealerRegistrationId,
					x.IfmsDealerId,
					x.ProductId,
					x.IfmsProductId
				})
				.Select(group => new AckAggregateRow
				{
					RegistrationId = group.Key.DealerRegistrationId,
					IfmsId = group.Key.IfmsDealerId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					AckDate = group.Max(x => (DateTime?)x.CreatedAt)
				})
				.ToListAsync();

			foreach (var row in dptSalesRows)
			{
				AddAcknowledgement(
					result.DptRetailerSaleDates,
					"R",
					row.RegistrationId,
					row.ProductId,
					row.IfmsProductId,
					row.AckDate);
				AddAcknowledgement(
					result.DptRetailerSaleDates,
					"I",
					row.IfmsId,
					row.ProductId,
					row.IfmsProductId,
					row.AckDate);
			}

			return result;
		}

		private static IQueryable<SalesCompanySale> ApplyCompanyDealerFilter(
			IQueryable<SalesCompanySale> query,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (registrationIds.Count > 0 && ifmsIds.Count > 0)
			{
				return query.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (registrationIds.Count > 0)
			{
				return query.Where(x =>
					x.DealerRegistrationId.HasValue &&
					registrationIds.Contains(x.DealerRegistrationId.Value));
			}

			return query.Where(x =>
				x.IfmsDealerId.HasValue &&
				ifmsIds.Contains(x.IfmsDealerId.Value));
		}

		private static IQueryable<SalesWholesaler>
			ApplyWholesalerSalesDealerFilter(
				IQueryable<SalesWholesaler> query,
				List<int> registrationIds,
				List<int> ifmsIds)
		{
			if (registrationIds.Count > 0 && ifmsIds.Count > 0)
			{
				return query.Where(x =>
					(x.DealerId.HasValue &&
					 registrationIds.Contains(x.DealerId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (registrationIds.Count > 0)
			{
				return query.Where(x =>
					x.DealerId.HasValue &&
					registrationIds.Contains(x.DealerId.Value));
			}

			return query.Where(x =>
				x.IfmsDealerId.HasValue &&
				ifmsIds.Contains(x.IfmsDealerId.Value));
		}

		private static IQueryable<DptReport> ApplyDptDealerFilter(
			IQueryable<DptReport> query,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (registrationIds.Count > 0 && ifmsIds.Count > 0)
			{
				return query.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (registrationIds.Count > 0)
			{
				return query.Where(x =>
					x.DealerRegistrationId.HasValue &&
					registrationIds.Contains(x.DealerRegistrationId.Value));
			}

			return query.Where(x =>
				x.IfmsDealerId.HasValue &&
				ifmsIds.Contains(x.IfmsDealerId.Value));
		}

		private static void AddAcknowledgement(
			IDictionary<string, DateTime> lookup,
			string dealerPrefix,
			int? dealerId,
			int? productId,
			int? ifmsProductId,
			DateTime? acknowledgementDate)
		{
			var productKey = ProductIdentity(productId, ifmsProductId);

			if (!dealerId.HasValue ||
				string.IsNullOrWhiteSpace(productKey) ||
				!acknowledgementDate.HasValue)
			{
				return;
			}

			var key = BuildAckKey(dealerPrefix, dealerId.Value, productKey);

			// Current stock is one aggregate snapshot row. Without lot/FIFO linkage,
			// the most recent valid ACK is used as the stock-ageing anchor.
			if (!lookup.TryGetValue(key, out var currentDate) ||
				acknowledgementDate.Value > currentDate)
			{
				lookup[key] = acknowledgementDate.Value;
			}
		}

		private static string BuildAckKey(
			string dealerPrefix,
			int dealerId,
			string productKey)
		{
			return $"{dealerPrefix}{dealerId}|{productKey}";
		}

		private static int? AckAgeForWholesalerRow(
			StockAgeingLookup ageingLookup,
			DateTime today,
			int? dealerRegistrationId,
			int? ifmsDealerId,
			int? productId,
			int? ifmsProductId)
		{
			var acknowledgementDate = FindAgeingAnchor(
				ageingLookup.StrictAckDates,
				dealerRegistrationId,
				ifmsDealerId,
				productId,
				ifmsProductId);

			return CalculateAgeingDays(today, acknowledgementDate);
		}

		private static int? AckAgeForDptRow(
			StockAgeingLookup ageingLookup,
			DateTime today,
			int? dealerRegistrationId,
			int? ifmsDealerId,
			int? productId,
			int? ifmsProductId)
		{
			// A genuine workflow ACK is always the first choice.
			var acknowledgementDate = FindAgeingAnchor(
				ageingLookup.StrictAckDates,
				dealerRegistrationId,
				ifmsDealerId,
				productId,
				ifmsProductId);

			// Preserve the existing DPT fallback when no strict ACK exists.
			acknowledgementDate ??= FindAgeingAnchor(
				ageingLookup.DptRetailerSaleDates,
				dealerRegistrationId,
				ifmsDealerId,
				productId,
				ifmsProductId);

			return CalculateAgeingDays(today, acknowledgementDate);
		}

		private static DateTime? FindAgeingAnchor(
			IReadOnlyDictionary<string, DateTime> lookup,
			int? dealerRegistrationId,
			int? ifmsDealerId,
			int? productId,
			int? ifmsProductId)
		{
			var productKey = ProductIdentity(productId, ifmsProductId);

			if (string.IsNullOrWhiteSpace(productKey))
			{
				return null;
			}

			if (dealerRegistrationId.HasValue &&
				lookup.TryGetValue(
					BuildAckKey("R", dealerRegistrationId.Value, productKey),
					out var registeredDate))
			{
				return registeredDate;
			}

			if (ifmsDealerId.HasValue &&
				lookup.TryGetValue(
					BuildAckKey("I", ifmsDealerId.Value, productKey),
					out var ifmsDate))
			{
				return ifmsDate;
			}

			return null;
		}

		private static int? CalculateAgeingDays(
			DateTime today,
			DateTime? acknowledgementDate)
		{
			if (!acknowledgementDate.HasValue)
			{
				return null;
			}

			var ageingDays = (today.Date - acknowledgementDate.Value.Date).Days;
			return ageingDays < 0 ? 0 : ageingDays;
		}

		private static List<WholesalerStockAsOnToday> ApplyWholesalerAgeingFilter(
			List<WholesalerStockAsOnToday> rows,
			StockReportFilter filter,
			DateTime today,
			StockAgeingLookup acknowledgementLookup)
		{
			if (filter.AgeingRanges.Count == 0)
			{
				return rows;
			}

			return rows
				.Where(row =>
				{
					var ageingDays = AckAgeForWholesalerRow(
						acknowledgementLookup,
						today,
						row.DealerRegistrationId,
						row.IfmsDealerId,
						row.ProductId,
						row.IfmsProductId);

					return ageingDays.HasValue &&
						   MatchesAgeingRange(ageingDays.Value, filter.AgeingRanges);
				})
				.ToList();
		}

		private static List<DptReport> ApplyDptAgeingFilter(
			List<DptReport> rows,
			StockReportFilter filter,
			DateTime today,
			StockAgeingLookup acknowledgementLookup)
		{
			if (filter.AgeingRanges.Count == 0)
			{
				return rows;
			}

			return rows
				.Where(row =>
				{
					var ageingDays = AckAgeForDptRow(
						acknowledgementLookup,
						today,
						row.DealerRegistrationId,
						row.IfmsDealerId,
						row.ProductId,
						row.IfmsProductId);

					return ageingDays.HasValue &&
						   MatchesAgeingRange(ageingDays.Value, filter.AgeingRanges);
				})
				.ToList();
		}

		private static bool MatchesAgeingRange(
			int ageingDays,
			IReadOnlyCollection<string> selectedRanges)
		{
			if (ageingDays < 0)
			{
				return false;
			}

			return
				(selectedRanges.Contains("0-30") && ageingDays <= 30) ||
				(selectedRanges.Contains("31-90") && ageingDays >= 31 && ageingDays <= 90) ||
				(selectedRanges.Contains("91-180") && ageingDays >= 91 && ageingDays <= 180) ||
				(selectedRanges.Contains("181-364") && ageingDays >= 181 && ageingDays <= 364) ||
				(selectedRanges.Contains("365+") && ageingDays >= 365) ||

				// Backward-compatible aliases for older clients/bookmarks.
				(selectedRanges.Contains("31-60") && ageingDays >= 31 && ageingDays <= 60) ||
				(selectedRanges.Contains("61-90") && ageingDays >= 61 && ageingDays <= 90) ||
				(selectedRanges.Contains("91-120") && ageingDays >= 91 && ageingDays <= 120) ||
				(selectedRanges.Contains("Above 120") && ageingDays > 120);
		}

		// =====================================================================
		// Grid and export
		// =====================================================================

		private async Task<PagedResult<StockRowDto>> BuildGridAsync(
			List<WholesalerStockAsOnToday> wholesalerRows,
			List<DptReport> dptRows,
			List<WarehouseDistrictGlobalStockReconciliation> warehouseRows,
			StockReportFilter filter,
			DateTime today,
			StockAgeingLookup acknowledgementLookup,
			bool paged)
		{
			var stateIds = wholesalerRows
				.Where(x => x.StateId.HasValue)
				.Select(x => x.StateId!.Value)
				.Concat(dptRows
					.Where(x => x.StateId.HasValue)
					.Select(x => x.StateId!.Value))
				.Concat(warehouseRows
					.Where(x => x.StateId.HasValue)
					.Select(x => x.StateId!.Value))
				.Distinct()
				.ToList();

			var productKeys = wholesalerRows
				.Select(x => ProductIdentity(x.ProductId, x.IfmsProductId))
				.Concat(dptRows
					.Select(x => ProductIdentity(x.ProductId, x.IfmsProductId)))
				.Concat(warehouseRows
					.Select(x => ProductIdentity(x.ProductId, x.IfmsProductId)))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.Ordinal)
				.ToList();

			var natureIds = wholesalerRows
				.Where(x => x.DealershipNatureId.HasValue)
				.Select(x => x.DealershipNatureId!.Value)
				.Concat(dptRows
					.Where(x => x.DealershipNatureId.HasValue)
					.Select(x => x.DealershipNatureId!.Value))
				.Distinct()
				.ToList();

			var registrationIds = wholesalerRows
				.Where(x => x.DealerRegistrationId.HasValue)
				.Select(x => x.DealerRegistrationId!.Value)
				.Concat(dptRows
					.Where(x => x.DealerRegistrationId.HasValue)
					.Select(x => x.DealerRegistrationId!.Value))
				.Distinct()
				.ToList();

			var ifmsIds = wholesalerRows
				.Where(x => x.IfmsDealerId.HasValue)
				.Select(x => x.IfmsDealerId!.Value)
				.Concat(dptRows
					.Where(x => x.IfmsDealerId.HasValue)
					.Select(x => x.IfmsDealerId!.Value))
				.Distinct()
				.ToList();

			var warehouseIds = warehouseRows
				.Where(x => x.WarehouseId.HasValue)
				.Select(x => x.WarehouseId!.Value)
				.Distinct()
				.ToList();

			var stateNames = stateIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<State>()
					.AsNoTracking()
					.Where(x => stateIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.StateName ?? string.Empty);

			var productNames = await LoadProductNameLookupAsync(productKeys);

			var natureNames = natureIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<DealershipNature>()
					.AsNoTracking()
					.Where(x => natureIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.Name ?? string.Empty);

			var registeredDealers = registrationIds.Count == 0
				? new Dictionary<int, RegisteredDealerContact>()
				: await _db.Set<DealerRegistration>()
					.AsNoTracking()
					.Where(x => registrationIds.Contains(x.Id))
					.Select(x => new RegisteredDealerContact
					{
						Id = x.Id,
						FirmName = x.FirmName,
						WhatsAppNumber = x.WhatsAppNumber,
						OfficialContactNumber = x.OfficialContactNumber,
						AlternativeNumber = x.AlternativeNumber
					})
					.ToDictionaryAsync(x => x.Id);

			var ifmsDealers = ifmsIds.Count == 0
				? new Dictionary<int, IfmsDealerContact>()
				: await _db.Set<IfmsDealer>()
					.AsNoTracking()
					.Where(x => ifmsIds.Contains(x.Id))
					.Select(x => new IfmsDealerContact
					{
						Id = x.Id,
						Name = x.Name,
						MobileNo = x.MobileNo
					})
					.ToDictionaryAsync(x => x.Id);

			var warehouseNames = warehouseIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<Warehouse>()
					.AsNoTracking()
					.Where(x => warehouseIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.Name ?? string.Empty);

			var projections = new List<GridProjection>(
				wholesalerRows.Count + dptRows.Count + warehouseRows.Count);

			foreach (var row in wholesalerRows)
			{
				registeredDealers.TryGetValue(
					row.DealerRegistrationId ?? 0,
					out var registeredDealer);

				ifmsDealers.TryGetValue(
					row.IfmsDealerId ?? 0,
					out var ifmsDealer);

				var ageingDays = AckAgeForWholesalerRow(
					acknowledgementLookup,
					today,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId,
					row.IfmsProductId);

				var whatsapp = Blank(registeredDealer?.WhatsAppNumber);
				var official = Blank(registeredDealer?.OfficialContactNumber);
				var alternative = Blank(registeredDealer?.AlternativeNumber);
				var ifmsMobile = Blank(ifmsDealer?.MobileNo);

				projections.Add(new GridProjection
				{
					Source = WholesalerStockSource,
					DealerRegistrationId = row.DealerRegistrationId,
					IfmsDealerId = row.IfmsDealerId,
					StateName = GetLookupName(stateNames, row.StateId),
					DealerName = FirstNonBlank(
						row.AgencyName,
						registeredDealer?.FirmName,
						ifmsDealer?.Name) ?? string.Empty,
					ProductName = GetProductName(
						productNames,
						row.ProductId,
						row.IfmsProductId),
					Quantity = row.Stock,
					LyingWith = FirstNonBlank(
						GetLookupName(natureNames, row.DealershipNatureId),
						"Wholesaler") ?? "Wholesaler",
					AgeingDays = ageingDays ?? 0,
					HasAckAgeing = ageingDays.HasValue,
					Status = ageingDays.HasValue
						? MapStatus(ageingDays.Value)
						: AckPendingStatus,
					WhatsAppNumber = whatsapp,
					OfficialContactNumber = official,
					AlternativeNumber = alternative,
					MobileNo = FirstNonBlank(
						whatsapp,
						official,
						alternative,
						ifmsMobile)
				});
			}

			foreach (var row in dptRows)
			{
				registeredDealers.TryGetValue(
					row.DealerRegistrationId ?? 0,
					out var registeredDealer);

				ifmsDealers.TryGetValue(
					row.IfmsDealerId ?? 0,
					out var ifmsDealer);

				var ageingDays = AckAgeForDptRow(
					acknowledgementLookup,
					today,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId,
					row.IfmsProductId);
				var whatsapp = Blank(registeredDealer?.WhatsAppNumber);
				var official = Blank(registeredDealer?.OfficialContactNumber);
				var alternative = Blank(registeredDealer?.AlternativeNumber);
				var ifmsMobile = Blank(ifmsDealer?.MobileNo);

				projections.Add(new GridProjection
				{
					Source = RetailerStockSource,
					DealerRegistrationId = row.DealerRegistrationId,
					IfmsDealerId = row.IfmsDealerId,
					StateName = GetLookupName(stateNames, row.StateId),
					DealerName = FirstNonBlank(
						row.RetailerName,
						registeredDealer?.FirmName,
						ifmsDealer?.Name) ?? string.Empty,
					ProductName = GetProductName(
						productNames,
						row.ProductId,
						row.IfmsProductId),
					Quantity = row.ClosingBalance,
					LyingWith = FirstNonBlank(
						GetLookupName(natureNames, row.DealershipNatureId),
						"Retailer") ?? "Retailer",
					AgeingDays = ageingDays ?? 0,
					HasAckAgeing = ageingDays.HasValue,
					Status = ageingDays.HasValue
						? MapStatus(ageingDays.Value)
						: AckPendingStatus,
					WhatsAppNumber = whatsapp,
					OfficialContactNumber = official,
					AlternativeNumber = alternative,
					MobileNo = FirstNonBlank(
						row.MobileNo,
						whatsapp,
						official,
						alternative,
						ifmsMobile)
				});
			}

			foreach (var row in warehouseRows)
			{
				// Warehouse reconciliation has no DealerRegistrationId/IfmsDealerId,
				// so it cannot be matched safely to Status=Ack dealer sales.
				var warehouseName = row.WarehouseId.HasValue &&
					warehouseNames.TryGetValue(row.WarehouseId.Value, out var name) &&
					!string.IsNullOrWhiteSpace(name)
						? name
						: row.WarehouseId.HasValue
							? $"Warehouse {row.WarehouseId.Value}"
							: "Warehouse";

				projections.Add(new GridProjection
				{
					Source = WarehouseStockSource,
					WarehouseId = row.WarehouseId,
					StateName = GetLookupName(stateNames, row.StateId),
					DealerName = warehouseName,
					ProductName = GetProductName(
						productNames,
						row.ProductId,
						row.IfmsProductId),
					Quantity = row.ClosingStock,
					LyingWith = "Warehouse",
					AgeingDays = 0,
					HasAckAgeing = false,
					Status = AckUnavailableStatus
				});
			}

			if (filter.AgeingRanges.Count > 0)
			{
				projections = projections
					.Where(x =>
						x.HasAckAgeing &&
						MatchesAgeingRange(
							x.AgeingDays,
							filter.AgeingRanges))
					.ToList();
			}

			if (!string.IsNullOrWhiteSpace(filter.Search))
			{
				var search = filter.Search.Trim();

				projections = projections
					.Where(x =>
						ContainsIgnoreCase(x.Source, search) ||
						ContainsIgnoreCase(x.StateName, search) ||
						ContainsIgnoreCase(x.DealerName, search) ||
						ContainsIgnoreCase(x.ProductName, search) ||
						ContainsIgnoreCase(x.LyingWith, search))
					.ToList();
			}

			projections = SortGrid(projections, filter);

			var totalCount = projections.Count;

			if (paged)
			{
				projections = projections
					.Skip((filter.Page - 1) * filter.PageSize)
					.Take(filter.PageSize)
					.ToList();
			}

			return new PagedResult<StockRowDto>
			{
				Items = projections
					.Select(x => new StockRowDto
					{
						DealerRegistrationId = x.DealerRegistrationId,
						StateName = x.StateName,
						DealerName = x.DealerName,
						ProductName = x.ProductName,
						Quantity = x.Quantity,
						LyingWith = x.LyingWith,
						AgeingDays = x.AgeingDays,
						HasAckAgeing = x.HasAckAgeing,
						Status = x.Status,
						MobileNo = x.MobileNo,
						WhatsAppNumber = x.WhatsAppNumber,
						OfficialContactNumber = x.OfficialContactNumber,
						AlternativeNumber = x.AlternativeNumber
					})
					.ToList(),
				TotalCount = totalCount,
				Page = paged ? filter.Page : 1,
				PageSize = paged ? filter.PageSize : totalCount
			};
		}

		private static List<GridProjection> SortGrid(
			List<GridProjection> rows,
			StockReportFilter filter)
		{
			var column = (filter.SortColumn ?? string.Empty)
				.Trim()
				.ToLowerInvariant();

			var descending = string.Equals(
				filter.SortDir,
				"desc",
				StringComparison.OrdinalIgnoreCase);

			IOrderedEnumerable<GridProjection> ordered;

			switch (column)
			{
				case "dealer":
					ordered = descending
						? rows.OrderByDescending(x => x.DealerName)
						: rows.OrderBy(x => x.DealerName);
					break;

				case "product":
					ordered = descending
						? rows.OrderByDescending(x => x.ProductName)
						: rows.OrderBy(x => x.ProductName);
					break;

				case "quantity":
					ordered = descending
						? rows.OrderByDescending(x => x.Quantity)
						: rows.OrderBy(x => x.Quantity);
					break;

				case "ageing":
					ordered = descending
						? rows.OrderBy(x => !x.HasAckAgeing)
							.ThenByDescending(x => x.AgeingDays)
						: rows.OrderBy(x => !x.HasAckAgeing)
							.ThenBy(x => x.AgeingDays);
					break;

				case "state":
				default:
					ordered = descending
						? rows.OrderByDescending(x => x.StateName)
						: rows.OrderBy(x => x.StateName);
					break;
			}

			return ordered
				.ThenBy(x => x.DealerName)
				.ThenBy(x => x.ProductName)
				.ToList();
		}

		// =====================================================================
		// Aggregation helpers
		// =====================================================================

		private static int CountUniqueDealers(
			IEnumerable<WholesalerStockAsOnToday> wholesalerRows,
			IEnumerable<DptReport> dptRows,
			IEnumerable<WarehouseDistrictGlobalStockReconciliation> warehouseRows)
		{
			var uniqueKeys = new HashSet<string>(StringComparer.Ordinal);

			foreach (var row in wholesalerRows)
			{
				AddDealerCountKey(
					uniqueKeys,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.AgencyName);
			}

			foreach (var row in dptRows)
			{
				AddDealerCountKey(
					uniqueKeys,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.RetailerName);
			}

			foreach (var row in warehouseRows)
			{
				if (row.WarehouseId.HasValue)
				{
					uniqueKeys.Add("W:" + row.WarehouseId.Value);
					continue;
				}

				// Legacy/fallback rows without WarehouseId are still de-duplicated
				// by their physical warehouse location. Product is intentionally
				// excluded so the same warehouse is not counted once per product.
				if (row.StateId.HasValue ||
					row.DistrictId.HasValue ||
					row.PlantId.HasValue)
				{
					uniqueKeys.Add(string.Join(
						"|",
						"WLOC",
						IdPart(row.StateId),
						IdPart(row.DistrictId),
						IdPart(row.PlantId)));
				}
			}

			return uniqueKeys.Count;
		}

		private static void AddDealerCountKey(
			ISet<string> uniqueKeys,
			int? dealerRegistrationId,
			int? ifmsDealerId,
			string? dealerName)
		{
			if (!dealerRegistrationId.HasValue &&
				!ifmsDealerId.HasValue &&
				string.IsNullOrWhiteSpace(dealerName))
			{
				return;
			}

			uniqueKeys.Add(DealerBusinessKey(
				dealerRegistrationId,
				ifmsDealerId,
				dealerName));
		}

		private static void AddStateRows<T>(
			IDictionary<int, StateAccumulator> accumulator,
			IEnumerable<T> rows,
			Func<T, int?> stateSelector,
			Func<T, decimal> quantitySelector,
			bool isCurrent)
		{
			foreach (var group in rows
						 .Where(x => stateSelector(x).HasValue)
						 .GroupBy(x => stateSelector(x)!.Value))
			{
				if (!accumulator.TryGetValue(group.Key, out var value))
				{
					value = new StateAccumulator();
					accumulator[group.Key] = value;
				}

				var quantity = group.Sum(quantitySelector);

				if (isCurrent)
				{
					value.Current += quantity;
				}
				else
				{
					value.Previous += quantity;
				}
			}
		}

		private static void AddProductRows<T>(
			IDictionary<string, decimal> accumulator,
			IEnumerable<T> rows,
			Func<T, int?> productSelector,
			Func<T, int?> ifmsProductSelector,
			Func<T, decimal> quantitySelector)
		{
			foreach (var row in rows)
			{
				var productKey = ProductIdentity(
					productSelector(row),
					ifmsProductSelector(row));

				if (string.IsNullOrWhiteSpace(productKey))
				{
					continue;
				}

				var quantity = quantitySelector(row);
				accumulator[productKey] =
					accumulator.TryGetValue(productKey, out var current)
						? current + quantity
						: quantity;
			}
		}

		private async Task<Dictionary<string, string>> LoadProductNameLookupAsync(
			IEnumerable<string> productKeys)
		{
			var keys = productKeys
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.Ordinal)
				.ToList();

			var approvedIds = ParseProductKeys(keys, "P:");
			var ifmsIds = ParseProductKeys(keys, "I:");

			var lookup = new Dictionary<string, string>(StringComparer.Ordinal);

			if (approvedIds.Count > 0)
			{
				var approvedProducts = await _db.Set<Product>()
					.AsNoTracking()
					.Where(x => approvedIds.Contains(x.Id))
					.Select(x => new
					{
						x.Id,
						x.Name
					})
					.ToListAsync();

				foreach (var product in approvedProducts)
				{
					lookup[$"P:{product.Id}"] =
						string.IsNullOrWhiteSpace(product.Name)
							? $"Product {product.Id}"
							: product.Name.Trim();
				}
			}

			if (ifmsIds.Count > 0)
			{
				var ifmsProducts = await _db.Set<IfmsProduct>()
					.AsNoTracking()
					.Where(x => ifmsIds.Contains(x.Id))
					.Select(x => new
					{
						x.Id,
						x.Name
					})
					.ToListAsync();

				foreach (var product in ifmsProducts)
				{
					lookup[$"I:{product.Id}"] =
						string.IsNullOrWhiteSpace(product.Name)
							? $"IFMS Product {product.Id}"
							: product.Name.Trim();
				}
			}

			return lookup;
		}

		private static List<int> ParseProductKeys(
			IEnumerable<string> productKeys,
			string prefix)
		{
			return productKeys
				.Where(x => x.StartsWith(prefix, StringComparison.Ordinal))
				.Select(x => x[prefix.Length..])
				.Where(x => int.TryParse(x, out _))
				.Select(int.Parse)
				.Distinct()
				.ToList();
		}

		private static string ProductIdentity(
			int? productId,
			int? ifmsProductId)
		{
			if (productId.HasValue)
			{
				return $"P:{productId.Value}";
			}

			if (ifmsProductId.HasValue)
			{
				return $"I:{ifmsProductId.Value}";
			}

			return string.Empty;
		}

		private static string GetProductName(
			IReadOnlyDictionary<string, string> lookup,
			int? productId,
			int? ifmsProductId)
		{
			return GetProductName(
				lookup,
				ProductIdentity(productId, ifmsProductId));
		}

		private static string GetProductName(
			IReadOnlyDictionary<string, string> lookup,
			string productKey)
		{
			if (!string.IsNullOrWhiteSpace(productKey) &&
				lookup.TryGetValue(productKey, out var name) &&
				!string.IsNullOrWhiteSpace(name))
			{
				return name.Trim();
			}

			if (productKey.StartsWith("P:", StringComparison.Ordinal))
			{
				return $"Product {productKey[2..]}";
			}

			if (productKey.StartsWith("I:", StringComparison.Ordinal))
			{
				return $"IFMS Product {productKey[2..]}";
			}

			return string.Empty;
		}

		private static void AddAgeingSummary(
			int? ageingDays,
			decimal quantity,
			ref double ageingTotal,
			ref int ageingRowCount,
			ref int highAgeingCount,
			ref decimal highAgeingStock)
		{
			if (!ageingDays.HasValue)
			{
				return;
			}

			ageingTotal += ageingDays.Value;
			ageingRowCount++;

			if (ageingDays.Value >= CriticalMin)
			{
				highAgeingCount++;
				highAgeingStock += quantity;
			}
		}

		private static string GetLookupName(
			IReadOnlyDictionary<int, string> lookup,
			int? id)
		{
			if (!id.HasValue ||
				!lookup.TryGetValue(id.Value, out var name) ||
				string.IsNullOrWhiteSpace(name))
			{
				return string.Empty;
			}

			return name.Trim();
		}

		private static string MapStatus(int ageingDays)
		{
			if (ageingDays < 0)
			{
				return AckPendingStatus;
			}

			if (ageingDays <= FreshMax)
			{
				return "Fresh";
			}

			if (ageingDays <= MediumMax)
			{
				return "Medium";
			}

			if (ageingDays <= SlowMax)
			{
				return "Slow Moving";
			}

			if (ageingDays < CriticalMin)
			{
				return "Long Aged";
			}

			return "Critical";
		}

		private static bool ContainsIgnoreCase(
			string? source,
			string value)
		{
			return !string.IsNullOrWhiteSpace(source) &&
				   source.IndexOf(
					   value,
					   StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string? Blank(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? null
				: value.Trim();
		}

		private static string? FirstNonBlank(params string?[] values)
		{
			return values
				.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
				?.Trim();
		}

		private static void NormalizeFilter(StockReportFilter filter)
		{
			filter.StateIds ??= new List<int>();
			filter.RegionIds ??= new List<int>();
			filter.HeadQuarterIds ??= new List<int>();
			filter.DistrictIds ??= new List<int>();
			filter.SubDistrictIds ??= new List<int>();
			filter.LyingWithIds ??= new List<int>();
			filter.ProductIds ??= new List<int>();
			filter.IfmsProductIds ??= new List<int>();
			filter.AgeingRanges ??= new List<string>();

			filter.Page = Math.Max(1, filter.Page);
			filter.PageSize = filter.PageSize <= 0
				? 16
				: Math.Min(filter.PageSize, 500);

			filter.SortDir = string.Equals(
				filter.SortDir,
				"desc",
				StringComparison.OrdinalIgnoreCase)
				? "desc"
				: "asc";
		}

		// =====================================================================
		// Internal DTOs
		// =====================================================================

		private sealed class StateAccumulator
		{
			public decimal Current { get; set; }
			public decimal Previous { get; set; }
		}

		private sealed class StockAgeingLookup
		{
			public Dictionary<string, DateTime> StrictAckDates { get; } =
				new(StringComparer.Ordinal);

			public Dictionary<string, DateTime> DptRetailerSaleDates { get; } =
				new(StringComparer.Ordinal);
		}

		private sealed class AckStockKey
		{
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
		}

		private sealed class AckAggregateRow
		{
			public int? RegistrationId { get; set; }
			public int? IfmsId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
			public DateTime? AckDate { get; set; }
		}

		private sealed class RegisteredDealerContact
		{
			public int Id { get; set; }
			public string? FirmName { get; set; }
			public string? WhatsAppNumber { get; set; }
			public string? OfficialContactNumber { get; set; }
			public string? AlternativeNumber { get; set; }
		}

		private sealed class IfmsDealerContact
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public string? MobileNo { get; set; }
		}

		private sealed class GridProjection
		{
			public string Source { get; set; } = string.Empty;
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public int? WarehouseId { get; set; }
			public string StateName { get; set; } = string.Empty;
			public string DealerName { get; set; } = string.Empty;
			public string ProductName { get; set; } = string.Empty;
			public decimal Quantity { get; set; }
			public string LyingWith { get; set; } = string.Empty;
			public int AgeingDays { get; set; }
			public bool HasAckAgeing { get; set; }
			public string Status { get; set; } = string.Empty;
			public string? MobileNo { get; set; }
			public string? WhatsAppNumber { get; set; }
			public string? OfficialContactNumber { get; set; }
			public string? AlternativeNumber { get; set; }
		}
	}
}