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
	/// Builds the existing state-wise Stock Details ledger without changing its
	/// business flow.
	///
	/// Existing mapping preserved:
	/// - Opening Stock = SUM(StateGlobalStockReconciliation.OpeningStock)
	/// - Supplies      = SUM(Receipt + ProductionImports)
	/// - Sales         = SUM(QuantityMT) from SalesWholesaler + SalesCompanySale
	/// - Closing Stock = Opening + Supplies - Total Sales
	///
	/// The reconciliation rows are filtered by CreatedAt and sales rows by
	/// InvoiceDate, exactly as in the supplied implementation.
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

			// Keep EF operations sequential because the same scoped DbContext
			// cannot execute multiple database commands concurrently.
			var stockByState = await LoadStockByStateAsync(
				filter.StateIds,
				period.PeriodStart,
				period.AsOnNextDay,
				cancellationToken);

			// Both sales tables are combined before grouping, reducing the two
			// original sales round trips to one grouped database query.
			var salesByState = await LoadSalesByStateAsync(
				filter.StateIds,
				period.PeriodStart,
				period.AsOnDate,
				period.AsOnNextDay,
				cancellationToken);

			var involvedStateIds = stockByState.Keys
				.Union(salesByState.Keys)
				.Distinct()
				.ToList();

			// Only the State rows referenced by the compact aggregates are loaded.
			var stateNames = await LoadStateNamesAsync(
				involvedStateIds,
				cancellationToken);

			var rows = BuildRows(
				involvedStateIds,
				stockByState,
				salesByState,
				stateNames);

			// Previous flow preserved: search also affects the KPI cards and
			// grand total because it is applied before those calculations.
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

		/// <summary>
		/// Preserves the existing stock rule while returning only one compact row
		/// per state from SQL.
		///
		/// IMPORTANT: this is correct only when rows inside the selected CreatedAt
		/// window are additive movement/component rows. If this table stores repeated
		/// daily snapshots, OpeningStock must instead be selected from the first/latest
		/// snapshot per business key rather than summed across dates.
		/// </summary>
		private async Task<Dictionary<int, StockAggregate>> LoadStockByStateAsync(
			IReadOnlyCollection<int> stateIds,
			DateTime periodStart,
			DateTime asOnNextDay,
			CancellationToken cancellationToken)
		{
			var query = _db.Set<StateGlobalStockReconciliation>()
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.CreatedAt >= periodStart &&
					x.CreatedAt < asOnNextDay);

			if (stateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));
			}

			var aggregates = await query
				.GroupBy(x => x.StateId!.Value)
				.Select(group => new StockAggregate
				{
					StateId = group.Key,
					OpeningStock = group.Sum(x => x.OpeningStock),
					Supplies = group.Sum(x => x.Receipt + x.ProductionImports)
				})
				.ToListAsync(cancellationToken);

			return aggregates.ToDictionary(x => x.StateId);
		}

		/// <summary>
		/// Combines SalesWholesaler and SalesCompanySale and calculates both sales
		/// columns in one SQL GROUP BY query.
		/// </summary>
		private async Task<Dictionary<int, SalesAggregate>> LoadSalesByStateAsync(
			IReadOnlyCollection<int> stateIds,
			DateTime periodStart,
			DateTime asOnDate,
			DateTime asOnNextDay,
			CancellationToken cancellationToken)
		{
			var wholesalerQuery = _db.Set<SalesWholesaler>()
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.InvoiceDate.HasValue &&
					x.InvoiceDate.Value >= periodStart &&
					x.InvoiceDate.Value < asOnNextDay);

			var companyQuery = _db.Set<SalesCompanySale>()
				.AsNoTracking()
				.Where(x =>
					x.StateId.HasValue &&
					x.InvoiceDate.HasValue &&
					x.InvoiceDate.Value >= periodStart &&
					x.InvoiceDate.Value < asOnNextDay);

			if (stateIds.Count > 0)
			{
				wholesalerQuery = wholesalerQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));

				companyQuery = companyQuery.Where(x =>
					x.StateId.HasValue &&
					stateIds.Contains(x.StateId.Value));
			}

			var combinedQuery = wholesalerQuery
				.Select(x => new SalesSourceRow
				{
					StateId = x.StateId!.Value,
					InvoiceDate = x.InvoiceDate!.Value,
					Quantity = x.QuantityMT
				})
				.Concat(
					companyQuery.Select(x => new SalesSourceRow
					{
						StateId = x.StateId!.Value,
						InvoiceDate = x.InvoiceDate!.Value,
						Quantity = x.QuantityMT
					}));

			var aggregates = await combinedQuery
				.GroupBy(x => x.StateId)
				.Select(group => new SalesAggregate
				{
					StateId = group.Key,
					SalesBefore = group.Sum(x =>
						x.InvoiceDate < asOnDate ? x.Quantity : 0m),
					SalesOnDay = group.Sum(x =>
						x.InvoiceDate >= asOnDate ? x.Quantity : 0m)
				})
				.ToListAsync(cancellationToken);

			return aggregates.ToDictionary(x => x.StateId);
		}

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
			IReadOnlyDictionary<int, StockAggregate> stockByState,
			IReadOnlyDictionary<int, SalesAggregate> salesByState,
			IReadOnlyDictionary<int, string> stateNames)
		{
			var rows = new List<StockDetailsRowDto>(stateIds.Count);

			foreach (var stateId in stateIds)
			{
				stockByState.TryGetValue(stateId, out var stock);
				salesByState.TryGetValue(stateId, out var sales);

				rows.Add(MergeRow(
					stateId,
					stateNames.TryGetValue(stateId, out var stateName)
						? stateName
						: "-",
					stock?.OpeningStock ?? 0m,
					stock?.Supplies ?? 0m,
					sales?.SalesBefore ?? 0m,
					sales?.SalesOnDay ?? 0m));
			}

			return rows;
		}

		/// <summary>
		/// Row calculation is intentionally unchanged from the supplied code.
		/// </summary>
		private static StockDetailsRowDto MergeRow(
			int stateId,
			string stateName,
			decimal openingStock,
			decimal supplies,
			decimal salesBefore,
			decimal salesOnDay)
		{
			var totalStock = openingStock + supplies;
			var totalSales = salesBefore + salesOnDay;
			var closingStock = totalStock - totalSales;

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

			// Preserve the previous behavior: an invalid reversed range becomes
			// a single-day range anchored on DateFrom.
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
				salesBeforeRange = "—";
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

		private sealed class StockAggregate
		{
			public int StateId { get; set; }
			public decimal OpeningStock { get; set; }
			public decimal Supplies { get; set; }
		}

		private sealed class SalesSourceRow
		{
			public int StateId { get; set; }
			public DateTime InvoiceDate { get; set; }
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