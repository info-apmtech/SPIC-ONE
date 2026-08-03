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
	/// </summary>
	public sealed class StockReportService : IStockReportService
	{
		private readonly AppDbContext _db;

		private const int FreshMax = 7;
		private const int MediumMax = 30;
		private const int SlowMax = 60;
		private const int HighAgeingDays = 180;

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
			// 3. Acknowledgement lookup for the current wholesaler stock rows.
			// -----------------------------------------------------------------
			var acknowledgementLookup = await BuildAckLookupAsync(
				currentWholesalerRows
					.Concat(yesterdayWholesalerRows)
					.Concat(lastMonthWholesalerRows)
					.Concat(previousYearWholesalerRows));

			// Ageing filters are based on the displayed ACK-based ageing value.
			// Apply the same filter to comparison snapshots so the percentage and
			// dealer comparison use the same wholesaler population.
			currentWholesalerRows = ApplyAgeingFilter(
				currentWholesalerRows,
				filter,
				today,
				acknowledgementLookup);

			yesterdayWholesalerRows = ApplyAgeingFilter(
				yesterdayWholesalerRows,
				filter,
				today,
				acknowledgementLookup);

			lastMonthWholesalerRows = ApplyAgeingFilter(
				lastMonthWholesalerRows,
				filter,
				today,
				acknowledgementLookup);

			previousYearWholesalerRows = ApplyAgeingFilter(
				previousYearWholesalerRows,
				filter,
				today,
				acknowledgementLookup);

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
				var ageingDays = AgeForRow(
					acknowledgementLookup,
					today,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId,
					row.StockDate);

				ageingTotal += ageingDays;
				ageingRowCount++;

				if (ageingDays > HighAgeingDays)
				{
					highAgeingCount++;
					highAgeingStock += row.Stock;
				}
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
			// -----------------------------------------------------------------
			var productAccumulator = new Dictionary<int, decimal>();

			AddProductRows(
				productAccumulator,
				currentWholesalerRows,
				x => x.ProductId,
				x => x.Stock);

			AddProductRows(
				productAccumulator,
				currentDptRows,
				x => x.ProductId,
				x => x.ClosingBalance);

			AddProductRows(
				productAccumulator,
				currentWarehouseRows,
				x => x.ProductId,
				x => x.ClosingStock);

			var productIds = productAccumulator.Keys.ToList();
			var productNames = productIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<Product>()
					.AsNoTracking()
					.Where(x => productIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.Name ?? string.Empty);

			var productTotal = productAccumulator.Values.Sum();

			var productWise = productAccumulator
				.OrderByDescending(item => item.Value)
				.ThenBy(item => item.Key)
				.Select((item, index) => new ProductStockDto
				{
					ProductName = productNames.TryGetValue(item.Key, out var productName)
						? productName
						: $"Product {item.Key}",
					Quantity = item.Value,
					Percentage = productTotal == 0m
						? 0d
						: Math.Round((double)(item.Value / productTotal) * 100d, 2),
					Color = ProductPalette[index % ProductPalette.Length]
				})
				.ToList();

			// -----------------------------------------------------------------
			// 7. Grid: latest current wholesaler snapshot only.
			// -----------------------------------------------------------------
			var grid = await BuildGridAsync(
				currentWholesalerRows,
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

			var availableDates = await GetWholesalerAvailableDatesAsync(
				currentAsOfExclusive);

			var currentSnapshotDate = LatestDateBefore(
				availableDates,
				currentAsOfExclusive);

			var snapshotMap = await LoadWholesalerSnapshotsAsync(
				filter,
				DistinctDates(currentSnapshotDate));

			var currentRows = GetSnapshotRows(
				snapshotMap,
				currentSnapshotDate);

			var acknowledgementLookup = await BuildAckLookupAsync(currentRows);

			currentRows = ApplyAgeingFilter(
				currentRows,
				filter,
				today,
				acknowledgementLookup);

			var result = await BuildGridAsync(
				currentRows,
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

			if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
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

			if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
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

			if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
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
				IdPart(row.ProductId),
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
				IdPart(row.ProductId),
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
				IdPart(row.ProductId));
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

		private async Task<Dictionary<string, DateTime>> BuildAckLookupAsync(
			IEnumerable<WholesalerStockAsOnToday> stockRows)
		{
			var keys = stockRows
				.Where(x => x.ProductId.HasValue)
				.ToList();

			var registrationIds = keys
				.Where(x => x.DealerRegistrationId.HasValue)
				.Select(x => x.DealerRegistrationId!.Value)
				.Distinct()
				.ToList();

			var ifmsIds = keys
				.Where(x => x.IfmsDealerId.HasValue)
				.Select(x => x.IfmsDealerId!.Value)
				.Distinct()
				.ToList();

			var productIds = keys
				.Select(x => x.ProductId!.Value)
				.Distinct()
				.ToList();

			var result = new Dictionary<string, DateTime>(StringComparer.Ordinal);

			if (productIds.Count == 0 ||
				(registrationIds.Count == 0 && ifmsIds.Count == 0))
			{
				return result;
			}

			var companyQuery = _db.Set<SalesCompanySale>()
				.AsNoTracking()
				.Where(x =>
					x.RetailerReceiptDate.HasValue &&
					x.ProductId.HasValue &&
					productIds.Contains(x.ProductId.Value));

			companyQuery = ApplyCompanyDealerFilter(
				companyQuery,
				registrationIds,
				ifmsIds);

			var companyRows = await companyQuery
				.GroupBy(x => new
				{
					x.DealerRegistrationId,
					x.IfmsDealerId,
					x.ProductId
				})
				.Select(group => new AckAggregateRow
				{
					RegistrationId = group.Key.DealerRegistrationId,
					IfmsId = group.Key.IfmsDealerId,
					ProductId = group.Key.ProductId,
					AckDate = group.Max(x => x.RetailerReceiptDate)
				})
				.ToListAsync();

			foreach (var row in companyRows)
			{
				AddAcknowledgement(
					result,
					"R",
					row.RegistrationId,
					row.ProductId,
					row.AckDate);

				AddAcknowledgement(
					result,
					"I",
					row.IfmsId,
					row.ProductId,
					row.AckDate);
			}

			var wholesalerSalesQuery = _db.Set<SalesWholesaler>()
				.AsNoTracking()
				.Where(x =>
					x.RetailerReceiptDate.HasValue &&
					x.ProductId.HasValue &&
					productIds.Contains(x.ProductId.Value));

			wholesalerSalesQuery = ApplyWholesalerSalesDealerFilter(
				wholesalerSalesQuery,
				registrationIds,
				ifmsIds);

			var wholesalerSalesRows = await wholesalerSalesQuery
				.GroupBy(x => new
				{
					RegistrationId = x.DealerId,
					x.IfmsDealerId,
					x.ProductId
				})
				.Select(group => new AckAggregateRow
				{
					RegistrationId = group.Key.RegistrationId,
					IfmsId = group.Key.IfmsDealerId,
					ProductId = group.Key.ProductId,
					AckDate = group.Max(x => x.RetailerReceiptDate)
				})
				.ToListAsync();

			foreach (var row in wholesalerSalesRows)
			{
				AddAcknowledgement(
					result,
					"R",
					row.RegistrationId,
					row.ProductId,
					row.AckDate);

				AddAcknowledgement(
					result,
					"I",
					row.IfmsId,
					row.ProductId,
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

		private static void AddAcknowledgement(
			IDictionary<string, DateTime> lookup,
			string prefix,
			int? dealerId,
			int? productId,
			DateTime? acknowledgementDate)
		{
			if (!dealerId.HasValue ||
				!productId.HasValue ||
				!acknowledgementDate.HasValue)
			{
				return;
			}

			var key = BuildAckKey(
				prefix,
				dealerId.Value,
				productId.Value);

			if (!lookup.TryGetValue(key, out var currentDate) ||
				acknowledgementDate.Value > currentDate)
			{
				lookup[key] = acknowledgementDate.Value;
			}
		}

		private static string BuildAckKey(
			string prefix,
			int dealerId,
			int productId)
		{
			return $"{prefix}{dealerId}|{productId}";
		}

		private static int AgeForRow(
			IReadOnlyDictionary<string, DateTime> acknowledgementLookup,
			DateTime today,
			int? dealerRegistrationId,
			int? ifmsDealerId,
			int? productId,
			DateTime stockDate)
		{
			DateTime? acknowledgementDate = null;

			if (productId.HasValue)
			{
				if (dealerRegistrationId.HasValue &&
					acknowledgementLookup.TryGetValue(
						BuildAckKey(
							"R",
							dealerRegistrationId.Value,
							productId.Value),
						out var registeredAck))
				{
					acknowledgementDate = registeredAck;
				}
				else if (ifmsDealerId.HasValue &&
						 acknowledgementLookup.TryGetValue(
							 BuildAckKey(
								 "I",
								 ifmsDealerId.Value,
								 productId.Value),
							 out var ifmsAck))
				{
					acknowledgementDate = ifmsAck;
				}
			}

			var anchorDate = (acknowledgementDate ?? stockDate).Date;
			var ageingDays = (int)Math.Floor((today - anchorDate).TotalDays);

			return ageingDays < 0 ? 0 : ageingDays;
		}

		private static List<WholesalerStockAsOnToday> ApplyAgeingFilter(
			List<WholesalerStockAsOnToday> rows,
			StockReportFilter filter,
			DateTime today,
			IReadOnlyDictionary<string, DateTime> acknowledgementLookup)
		{
			if (filter.AgeingRanges.Count == 0)
			{
				return rows;
			}

			return rows
				.Where(row => MatchesAgeingRange(
					AgeForRow(
						acknowledgementLookup,
						today,
						row.DealerRegistrationId,
						row.IfmsDealerId,
						row.ProductId,
						row.StockDate),
					filter.AgeingRanges))
				.ToList();
		}

		private static bool MatchesAgeingRange(
			int ageingDays,
			IReadOnlyCollection<string> selectedRanges)
		{
			return
				(selectedRanges.Contains("0-30") && ageingDays <= 30) ||
				(selectedRanges.Contains("31-60") && ageingDays >= 31 && ageingDays <= 60) ||
				(selectedRanges.Contains("61-90") && ageingDays >= 61 && ageingDays <= 90) ||
				(selectedRanges.Contains("91-120") && ageingDays >= 91 && ageingDays <= 120) ||
				(selectedRanges.Contains("Above 120") && ageingDays > 120);
		}

		// =====================================================================
		// Grid and export
		// =====================================================================

		private async Task<PagedResult<StockRowDto>> BuildGridAsync(
			List<WholesalerStockAsOnToday> rows,
			StockReportFilter filter,
			DateTime today,
			IReadOnlyDictionary<string, DateTime> acknowledgementLookup,
			bool paged)
		{
			var stateIds = rows
				.Where(x => x.StateId.HasValue)
				.Select(x => x.StateId!.Value)
				.Distinct()
				.ToList();

			var productIds = rows
				.Where(x => x.ProductId.HasValue)
				.Select(x => x.ProductId!.Value)
				.Distinct()
				.ToList();

			var natureIds = rows
				.Where(x => x.DealershipNatureId.HasValue)
				.Select(x => x.DealershipNatureId!.Value)
				.Distinct()
				.ToList();

			var registrationIds = rows
				.Where(x => x.DealerRegistrationId.HasValue)
				.Select(x => x.DealerRegistrationId!.Value)
				.Distinct()
				.ToList();

			var ifmsIds = rows
				.Where(x => x.IfmsDealerId.HasValue)
				.Select(x => x.IfmsDealerId!.Value)
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

			var productNames = productIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<Product>()
					.AsNoTracking()
					.Where(x => productIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.Name ?? string.Empty);

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

			var projections = rows
				.Select(row =>
				{
					registeredDealers.TryGetValue(
						row.DealerRegistrationId ?? 0,
						out var registeredDealer);

					ifmsDealers.TryGetValue(
						row.IfmsDealerId ?? 0,
						out var ifmsDealer);

					var ageingDays = AgeForRow(
						acknowledgementLookup,
						today,
						row.DealerRegistrationId,
						row.IfmsDealerId,
						row.ProductId,
						row.StockDate);

					var dealerName = FirstNonBlank(
						row.AgencyName,
						registeredDealer?.FirmName,
						ifmsDealer?.Name)
						?? string.Empty;

					var whatsapp = Blank(registeredDealer?.WhatsAppNumber);
					var official = Blank(registeredDealer?.OfficialContactNumber);
					var alternative = Blank(registeredDealer?.AlternativeNumber);
					var ifmsMobile = Blank(ifmsDealer?.MobileNo);

					return new GridProjection
					{
						DealerRegistrationId = row.DealerRegistrationId,
						StateName = row.StateId.HasValue &&
									stateNames.TryGetValue(row.StateId.Value, out var stateName)
							? stateName
							: string.Empty,
						DealerName = dealerName,
						ProductName = row.ProductId.HasValue &&
									  productNames.TryGetValue(row.ProductId.Value, out var productName)
							? productName
							: string.Empty,
						Quantity = row.Stock,
						LyingWith = row.DealershipNatureId.HasValue &&
									natureNames.TryGetValue(row.DealershipNatureId.Value, out var natureName)
							? natureName
							: string.Empty,
						AgeingDays = ageingDays,
						Status = MapStatus(ageingDays),
						WhatsAppNumber = whatsapp,
						OfficialContactNumber = official,
						AlternativeNumber = alternative,
						MobileNo = FirstNonBlank(
							whatsapp,
							official,
							alternative,
							ifmsMobile)
					};
				})
				.ToList();

			if (!string.IsNullOrWhiteSpace(filter.Search))
			{
				var search = filter.Search.Trim();

				projections = projections
					.Where(x =>
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
						? rows.OrderByDescending(x => x.AgeingDays)
						: rows.OrderBy(x => x.AgeingDays);
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
			IDictionary<int, decimal> accumulator,
			IEnumerable<T> rows,
			Func<T, int?> productSelector,
			Func<T, decimal> quantitySelector)
		{
			foreach (var group in rows
						 .Where(x => productSelector(x).HasValue)
						 .GroupBy(x => productSelector(x)!.Value))
			{
				var quantity = group.Sum(quantitySelector);
				accumulator[group.Key] =
					(accumulator.TryGetValue(group.Key, out var current)
						? current
						: 0m) + quantity;
			}
		}

		private static string MapStatus(int ageingDays)
		{
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

			return "Dead Stock";
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

		private sealed class AckAggregateRow
		{
			public int? RegistrationId { get; set; }
			public int? IfmsId { get; set; }
			public int? ProductId { get; set; }
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
			public int? DealerRegistrationId { get; set; }
			public string StateName { get; set; } = string.Empty;
			public string DealerName { get; set; } = string.Empty;
			public string ProductName { get; set; } = string.Empty;
			public decimal Quantity { get; set; }
			public string LyingWith { get; set; } = string.Empty;
			public int AgeingDays { get; set; }
			public string Status { get; set; } = string.Empty;
			public string? MobileNo { get; set; }
			public string? WhatsAppNumber { get; set; }
			public string? OfficialContactNumber { get; set; }
			public string? AlternativeNumber { get; set; }
		}
	}
}