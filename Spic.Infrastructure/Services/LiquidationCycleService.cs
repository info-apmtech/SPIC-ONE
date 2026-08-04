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
	/// <summary>
	/// Performance-optimized implementation of the existing liquidation flow.
	///
	/// Production mapping:
	/// Company    : latest Warehouse ClosingStock + SalesCompanySale.QuantityMT
	/// Wholesaler : latest WholesalerStockAsOnToday.Stock + SalesWholesaler.QuantityMT
	/// Retailer   : latest DptReport.ClosingBalance + latest DptReport.SoldQuantity
	/// Historical stock snapshot dates are never added together.
	///
	/// Main optimization: historical/raw transaction rows are no longer fully loaded.
	/// Latest snapshots and sales totals are reduced in SQL before materialization.
	/// </summary>
	public class LiquidationCycleService : ILiquidationCycleService
	{
		private const string CompanySource = "Company Sales";
		private const string WholesalerSource = "Wholesaler Sales";
		private const string RetailerSource = "Retailer Sales";

		private const int DefaultPageSize = 16;
		private const int MaximumDashboardPageSize = 500;

		private readonly AppDbContext _db;

		public LiquidationCycleService(AppDbContext db)
		{
			_db = db;
		}

		public async Task<LiqCycleDashboardDto> GetDashboardAsync(
			LiqCycleFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new LiqCycleFilter();
			NormalizeFilter(filter);

			var rows = await BuildRowsAsync(filter, cancellationToken);

			return new LiqCycleDashboardDto
			{
				Summary = BuildSummary(rows),
				TopFastDealers = BuildTopGroups(rows, delayed: false),
				TopSlowDealers = BuildTopGroups(rows, delayed: true),
				Grid = BuildGrid(rows, filter, paged: true)
			};
		}

		public async Task<List<LiqCycleRowDto>> GetAllRowsAsync(
			LiqCycleFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new LiqCycleFilter();
			NormalizeFilter(filter);

			var rows = await BuildRowsAsync(filter, cancellationToken);
			return BuildGrid(rows, filter, paged: false).Items;
		}

		// =====================================================================
		// Unified compact loading
		// =====================================================================

		private async Task<List<LiqCycleRowDto>> BuildRowsAsync(
			LiqCycleFilter filter,
			CancellationToken cancellationToken)
		{
			var from = ToUtcStart(filter.DateFrom);
			var toExclusive = ToUtcExclusiveEnd(filter.DateTo);
			var dealerIds = SplitDealerKeys(filter.DealerKeys);

			// These are already aggregated/latest rows, not full transaction history.
			var stageRows = new List<StageRow>();

			// Keep queries sequential because all of them use the same scoped DbContext.
			if (SourceMatches(filter.Source, CompanySource))
			{
				stageRows.AddRange(await BuildCompanyStageRowsAsync(
					filter,
					from,
					toExclusive,
					dealerIds,
					cancellationToken));
			}

			if (SourceMatches(filter.Source, WholesalerSource))
			{
				stageRows.AddRange(await BuildWholesalerStageRowsAsync(
					filter,
					from,
					toExclusive,
					dealerIds,
					cancellationToken));
			}

			if (SourceMatches(filter.Source, RetailerSource))
			{
				stageRows.AddRange(await BuildRetailerStageRowsAsync(
					filter,
					from,
					toExclusive,
					dealerIds,
					cancellationToken));
			}

			if (stageRows.Count == 0)
			{
				return new List<LiqCycleRowDto>();
			}

			// Load only lookup rows referenced by the compact result set.
			var lookup = await LoadLookupMapsAsync(stageRows, cancellationToken);

			var rows = new List<LiqCycleRowDto>(stageRows.Count);
			foreach (var row in stageRows)
			{
				rows.Add(Classify(new LiqCycleRowDto
				{
					Id = row.Id,
					Source = row.Source,
					DealerName = FirstNonEmpty(row.DealerName, "-"),
					DealerCode = FirstNonEmpty(row.DealerCode, "-"),
					DealerType = row.DealerType,
					ProductName = GetName(lookup.Products, row.ProductId),
					StateName = GetName(lookup.States, row.StateId),
					District = GetName(lookup.Districts, row.DistrictId),
					MobileNo = FirstNonEmpty(row.MobileNo, "-"),
					Stock = row.Stock,
					Sales = row.Sales,
					AgeingDays = CalcDays(row.ActivityDate)
				}));
			}

			return rows;
		}

		// =====================================================================
		// Company / Warehouse
		// =====================================================================

		private async Task<List<StageRow>> BuildCompanyStageRowsAsync(
			LiqCycleFilter filter,
			DateTime? from,
			DateTime? toExclusive,
			DealerIdSelection dealerIds,
			CancellationToken cancellationToken)
		{
			// Warehouse reconciliation has no dealer identity. When a dealer filter is
			// selected, company/warehouse rows cannot be matched safely and are excluded.
			if (dealerIds.HasAny)
			{
				return new List<StageRow>();
			}

			var warehouseBase = _db.WarehouseDistrictGlobalStockReconciliations
				.AsNoTracking()
				.AsQueryable();

			if (filter.StateIds.Count > 0)
			{
				warehouseBase = warehouseBase.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				warehouseBase = warehouseBase.Where(x =>
					x.DistrictId.HasValue && filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				warehouseBase = warehouseBase.Where(x =>
					x.ProductId.HasValue && filter.ProductIds.Contains(x.ProductId.Value));
			}

			// CreatedAt carries the uploaded business report date for this snapshot table.
			if (toExclusive.HasValue)
			{
				warehouseBase = warehouseBase.Where(x => x.CreatedAt < toExclusive.Value);
			}

			var latestWarehouseReportValue = await warehouseBase
				.Select(x => (DateTime?)x.CreatedAt)
				.MaxAsync(cancellationToken);

			var warehouseBalances = new Dictionary<LocationProductKey, WarehouseBalance>();

			if (latestWarehouseReportValue.HasValue)
			{
				var snapshotStart = latestWarehouseReportValue.Value.Date;
				var snapshotEnd = snapshotStart.AddDays(1);

				var rawWarehouseRows = await warehouseBase
					.Where(x => x.CreatedAt >= snapshotStart && x.CreatedAt < snapshotEnd)
					.Select(x => new WarehouseSnapshot
					{
						Id = x.Id,
						WarehouseId = x.WarehouseId,
						PlantId = x.PlantId,
						StateId = x.StateId,
						DistrictId = x.DistrictId,
						ProductId = x.ProductId,
						ClosingStock = x.ClosingStock,
						ReportDate = x.CreatedAt,
						UpdatedAt = x.UpdatedAt
					})
					.ToListAsync(cancellationToken);

				// Same report date + same warehouse business key: use only the latest
				// updated row. This protects the report from historical database duplicates.
				var deduplicatedWarehouseRows = rawWarehouseRows
					.GroupBy(x => new
					{
						WarehouseId = x.WarehouseId ?? 0,
						PlantId = x.PlantId ?? 0,
						StateId = x.StateId ?? 0,
						DistrictId = x.DistrictId ?? 0,
						ProductId = x.ProductId ?? 0
					})
					.Select(group => group
						.OrderByDescending(x => x.UpdatedAt)
						.ThenByDescending(x => x.Id)
						.First())
					.ToList();

				warehouseBalances = deduplicatedWarehouseRows
					.GroupBy(x => new LocationProductKey(
						x.StateId ?? 0,
						x.DistrictId ?? 0,
						x.ProductId ?? 0))
					.ToDictionary(
						group => group.Key,
						group => new WarehouseBalance
						{
							Id = group.Max(x => x.Id),
							ClosingStock = group.Sum(x => x.ClosingStock),
							SnapshotDate = snapshotStart
						});
			}

			var companySalesQuery = _db.SalesCompanySales
				.AsNoTracking()
				.AsQueryable();

			if (filter.StateIds.Count > 0)
			{
				companySalesQuery = companySalesQuery.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				companySalesQuery = companySalesQuery.Where(x =>
					x.DistrictId.HasValue && filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				companySalesQuery = companySalesQuery.Where(x =>
					x.ProductId.HasValue && filter.ProductIds.Contains(x.ProductId.Value));
			}

			if (filter.StatusIds.Count > 0)
			{
				companySalesQuery = companySalesQuery.Where(x =>
					x.StatusId.HasValue && filter.StatusIds.Contains(x.StatusId.Value));
			}

			if (from.HasValue)
			{
				companySalesQuery = companySalesQuery.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value >= from.Value);
			}

			if (toExclusive.HasValue)
			{
				companySalesQuery = companySalesQuery.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value < toExclusive.Value);
			}

			var companySalesRows = await companySalesQuery
				.GroupBy(x => new
				{
					StateId = x.StateId ?? 0,
					DistrictId = x.DistrictId ?? 0,
					ProductId = x.ProductId ?? 0
				})
				.Select(group => new CompanySalesAggregate
				{
					StateId = group.Key.StateId,
					DistrictId = group.Key.DistrictId,
					ProductId = group.Key.ProductId,
					Quantity = group.Sum(x => x.QuantityMT),
					LastDate = group.Max(x => x.InvoiceDate)
				})
				.ToListAsync(cancellationToken);

			var companySales = companySalesRows.ToDictionary(
				x => new LocationProductKey(x.StateId, x.DistrictId, x.ProductId));

			var keys = warehouseBalances.Keys
				.Union(companySales.Keys)
				.Distinct()
				.ToList();

			var rows = new List<StageRow>(keys.Count);

			foreach (var key in keys)
			{
				warehouseBalances.TryGetValue(key, out var stock);
				companySales.TryGetValue(key, out var sales);

				rows.Add(new StageRow
				{
					Id = stock?.Id ?? 0,
					Source = CompanySource,
					DealerName = "Company / Warehouse",
					DealerCode = "-",
					DealerType = "Warehouse",
					StateId = NullIfZero(key.StateId),
					DistrictId = NullIfZero(key.DistrictId),
					ProductId = NullIfZero(key.ProductId),
					MobileNo = "-",
					Stock = stock?.ClosingStock ?? 0m,
					Sales = sales?.Quantity ?? 0m,
					ActivityDate = stock?.SnapshotDate ?? sales?.LastDate
				});
			}

			return rows;
		}

		// =====================================================================
		// Wholesaler
		// =====================================================================

		private async Task<List<StageRow>> BuildWholesalerStageRowsAsync(
			LiqCycleFilter filter,
			DateTime? from,
			DateTime? toExclusive,
			DealerIdSelection dealerIds,
			CancellationToken cancellationToken)
		{
			var stockBase = _db.WholesalerStockAsOnTodays
				.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue || x.IfmsDealerId.HasValue)
				.AsQueryable();

			if (filter.StateIds.Count > 0)
			{
				stockBase = stockBase.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				stockBase = stockBase.Where(x =>
					x.DistrictId.HasValue && filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				stockBase = stockBase.Where(x =>
					x.ProductId.HasValue && filter.ProductIds.Contains(x.ProductId.Value));
			}

			if (dealerIds.HasAny)
			{
				stockBase = stockBase.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 dealerIds.RegularIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 dealerIds.IfmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (toExclusive.HasValue)
			{
				stockBase = stockBase.Where(x => x.StockDate < toExclusive.Value);
			}

			var latestStockDateValue = await stockBase
				.Select(x => (DateTime?)x.StockDate)
				.MaxAsync(cancellationToken);

			var latestStock = new Dictionary<DealerProductKey, WholesalerBalance>();

			if (latestStockDateValue.HasValue)
			{
				var snapshotStart = latestStockDateValue.Value.Date;
				var snapshotEnd = snapshotStart.AddDays(1);

				var rawStockRows = await stockBase
					.Where(x => x.StockDate >= snapshotStart && x.StockDate < snapshotEnd)
					.Select(x => new WholesalerStockSnapshot
					{
						Id = x.Id,
						DealerRegistrationId = x.DealerRegistrationId,
						IfmsDealerId = x.IfmsDealerId,
						AgencyName = x.AgencyName,
						StateId = x.StateId,
						DistrictId = x.DistrictId,
						CompanyId = x.CompanyId,
						PlantId = x.PlantId,
						ProductId = x.ProductId,
						Stock = x.Stock,
						StockDate = x.StockDate,
						UpdatedAt = x.UpdatedAt
					})
					.ToListAsync(cancellationToken);

				var deduplicatedStockRows = rawStockRows
					.GroupBy(x => new
					{
						RegularId = x.DealerRegistrationId ?? 0,
						IfmsId = x.DealerRegistrationId.HasValue ? 0 : x.IfmsDealerId ?? 0,
						StateId = x.StateId ?? 0,
						DistrictId = x.DistrictId ?? 0,
						CompanyId = x.CompanyId ?? 0,
						PlantId = x.PlantId ?? 0,
						ProductId = x.ProductId ?? 0
					})
					.Select(group => group
						.OrderByDescending(x => x.UpdatedAt)
						.ThenByDescending(x => x.Id)
						.First())
					.ToList();

				latestStock = deduplicatedStockRows
					.GroupBy(x => new DealerProductKey(
						BuildDealerKey(x.DealerRegistrationId, x.IfmsDealerId),
						x.ProductId ?? 0))
					.Where(group => group.Key.DealerKey != "-")
					.ToDictionary(
						group => group.Key,
						group =>
						{
							var representative = group
								.OrderByDescending(x => x.UpdatedAt)
								.ThenByDescending(x => x.Id)
								.First();

							return new WholesalerBalance
							{
								Id = representative.Id,
								DealerRegistrationId = representative.DealerRegistrationId,
								IfmsDealerId = representative.IfmsDealerId,
								AgencyName = representative.AgencyName,
								StateId = representative.StateId,
								DistrictId = representative.DistrictId,
								ProductId = representative.ProductId,
								Stock = group.Sum(x => x.Stock),
								SnapshotDate = snapshotStart
							};
						});
			}

			var salesBase = _db.SalesWholesalers
				.AsNoTracking()
				.Where(x => x.WholesalerId.HasValue || x.IfmsWholesalerId.HasValue)
				.AsQueryable();

			if (filter.StateIds.Count > 0)
			{
				salesBase = salesBase.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				salesBase = salesBase.Where(x =>
					x.SellerDistrictId.HasValue &&
					filter.DistrictIds.Contains(x.SellerDistrictId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				salesBase = salesBase.Where(x =>
					x.ProductId.HasValue && filter.ProductIds.Contains(x.ProductId.Value));
			}

			if (filter.StatusIds.Count > 0)
			{
				salesBase = salesBase.Where(x =>
					x.StatusId.HasValue && filter.StatusIds.Contains(x.StatusId.Value));
			}

			if (dealerIds.HasAny)
			{
				salesBase = salesBase.Where(x =>
					(x.WholesalerId.HasValue &&
					 dealerIds.RegularIds.Contains(x.WholesalerId.Value)) ||
					(x.IfmsWholesalerId.HasValue &&
					 dealerIds.IfmsIds.Contains(x.IfmsWholesalerId.Value)));
			}

			if (from.HasValue)
			{
				salesBase = salesBase.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value >= from.Value);
			}

			if (toExclusive.HasValue)
			{
				salesBase = salesBase.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value < toExclusive.Value);
			}

			var salesAggregateRows = await salesBase
				.GroupBy(x => new
				{
					RegularId = x.WholesalerId,
					IfmsId = x.WholesalerId.HasValue ? null : x.IfmsWholesalerId,
					ProductId = x.ProductId ?? 0
				})
				.Select(group => new WholesalerSalesAggregate
				{
					WholesalerId = group.Key.RegularId,
					IfmsWholesalerId = group.Key.IfmsId,
					ProductId = group.Key.ProductId,
					Quantity = group.Sum(x => x.QuantityMT),
					LastDate = group.Max(x => x.InvoiceDate)
				})
				.ToListAsync(cancellationToken);

			var representativeSalesQuery = salesBase.Where(current =>
				!salesBase.Any(candidate =>
					(candidate.WholesalerId ?? 0) == (current.WholesalerId ?? 0) &&
					(candidate.WholesalerId.HasValue ? 0 : candidate.IfmsWholesalerId ?? 0) ==
					(current.WholesalerId.HasValue ? 0 : current.IfmsWholesalerId ?? 0) &&
					(candidate.ProductId ?? 0) == (current.ProductId ?? 0) &&
					candidate.Id < current.Id));

			var representativeSalesRows = await representativeSalesQuery
				.Select(x => new WholesalerSaleRepresentative
				{
					WholesalerId = x.WholesalerId,
					IfmsWholesalerId = x.IfmsWholesalerId,
					ProductId = x.ProductId,
					DealerName = x.WholesalerAgencyName,
					StateId = x.StateId,
					DistrictId = x.SellerDistrictId,
					MobileNo = x.MobileNo
				})
				.ToListAsync(cancellationToken);

			var representatives = representativeSalesRows
				.Select(x => new
				{
					Key = new DealerProductKey(
						BuildDealerKey(x.WholesalerId, x.IfmsWholesalerId),
						x.ProductId ?? 0),
					Row = x
				})
				.Where(x => x.Key.DealerKey != "-")
				.GroupBy(x => x.Key)
				.ToDictionary(group => group.Key, group => group.First().Row);

			var sales = salesAggregateRows
				.GroupBy(x => new DealerProductKey(
					BuildDealerKey(x.WholesalerId, x.IfmsWholesalerId),
					x.ProductId))
				.Where(group => group.Key.DealerKey != "-")
				.ToDictionary(
					group => group.Key,
					group => new WholesalerSalesAggregate
					{
						WholesalerId = group.First().WholesalerId,
						IfmsWholesalerId = group.First().IfmsWholesalerId,
						ProductId = group.Key.ProductId,
						Quantity = group.Sum(x => x.Quantity),
						LastDate = group.Max(x => x.LastDate)
					});

			var keys = latestStock.Keys
				.Union(sales.Keys)
				.Distinct()
				.ToList();

			var rows = new List<StageRow>(keys.Count);

			foreach (var key in keys)
			{
				latestStock.TryGetValue(key, out var stock);
				sales.TryGetValue(key, out var sale);
				representatives.TryGetValue(key, out var representative);

				rows.Add(new StageRow
				{
					Id = stock?.Id ?? 0,
					Source = WholesalerSource,
					DealerName = FirstNonEmpty(
						stock?.AgencyName,
						representative?.DealerName,
						"-"),
					DealerCode = key.DealerKey,
					DealerType = "Wholesaler",
					ProductId = NullIfZero(key.ProductId),
					StateId = stock?.StateId ?? representative?.StateId,
					DistrictId = stock?.DistrictId ?? representative?.DistrictId,
					MobileNo = FirstNonEmpty(representative?.MobileNo, "-"),
					Stock = stock?.Stock ?? 0m,
					Sales = sale?.Quantity ?? 0m,
					ActivityDate = stock?.SnapshotDate ?? sale?.LastDate
				});
			}

			return rows;
		}

		// =====================================================================
		// Retailer / DPT
		// =====================================================================

		private async Task<List<StageRow>> BuildRetailerStageRowsAsync(
			LiqCycleFilter filter,
			DateTime? from,
			DateTime? toExclusive,
			DealerIdSelection dealerIds,
			CancellationToken cancellationToken)
		{
			var dptBase = _db.DptReports
				.AsNoTracking()
				.AsQueryable();

			if (filter.StateIds.Count > 0)
			{
				dptBase = dptBase.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				dptBase = dptBase.Where(x =>
					x.DistrictId.HasValue && filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				dptBase = dptBase.Where(x =>
					x.ProductId.HasValue && filter.ProductIds.Contains(x.ProductId.Value));
			}

			if (dealerIds.HasAny)
			{
				dptBase = dptBase.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 dealerIds.RegularIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 dealerIds.IfmsIds.Contains(x.IfmsDealerId.Value)));
			}

			// CreatedAt carries the selected DPT report date.
			if (toExclusive.HasValue)
			{
				dptBase = dptBase.Where(x => x.CreatedAt < toExclusive.Value);
			}

			var latestDptReportValue = await dptBase
				.Select(x => (DateTime?)x.CreatedAt)
				.MaxAsync(cancellationToken);

			if (!latestDptReportValue.HasValue)
			{
				return new List<StageRow>();
			}

			var snapshotStart = latestDptReportValue.Value.Date;
			var snapshotEnd = snapshotStart.AddDays(1);

			var rawDptRows = await dptBase
				.Where(x => x.CreatedAt >= snapshotStart && x.CreatedAt < snapshotEnd)
				.Select(x => new RetailerSnapshot
				{
					Id = x.Id,
					DealerRegistrationId = x.DealerRegistrationId,
					IfmsDealerId = x.IfmsDealerId,
					RetailerName = x.RetailerName,
					StateId = x.StateId,
					DistrictId = x.DistrictId,
					SubDistrictId = x.SubDistrictId,
					CompanyId = x.CompanyId,
					PlantId = x.PlantId,
					ProductId = x.ProductId,
					MobileNo = x.MobileNo,
					SoldQuantity = x.SoldQuantity,
					ClosingBalance = x.ClosingBalance,
					ReportDate = x.CreatedAt,
					UpdatedAt = x.UpdatedAt
				})
				.ToListAsync(cancellationToken);

			var deduplicatedRows = rawDptRows
				.GroupBy(x => new
				{
					RegularId = x.DealerRegistrationId ?? 0,
					IfmsId = x.DealerRegistrationId.HasValue ? 0 : x.IfmsDealerId ?? 0,
					FallbackName = x.DealerRegistrationId.HasValue || x.IfmsDealerId.HasValue
						? string.Empty
						: NormalizeText(x.RetailerName),
					StateId = x.StateId ?? 0,
					DistrictId = x.DistrictId ?? 0,
					SubDistrictId = x.SubDistrictId ?? 0,
					CompanyId = x.CompanyId ?? 0,
					PlantId = x.PlantId ?? 0,
					ProductId = x.ProductId ?? 0
				})
				.Select(group => group
					.OrderByDescending(x => x.UpdatedAt)
					.ThenByDescending(x => x.Id)
					.First())
				.ToList();

			var includeDptSales = !from.HasValue || snapshotStart >= from.Value.Date;

			var balances = deduplicatedRows
				.GroupBy(x => new RetailerProductKey(
					BuildRetailerIdentity(
						x.DealerRegistrationId,
						x.IfmsDealerId,
						x.RetailerName,
						x.StateId,
						x.DistrictId),
					x.ProductId ?? 0))
				.ToList();

			var rows = new List<StageRow>(balances.Count);

			foreach (var group in balances)
			{
				var representative = group
					.OrderByDescending(x => x.UpdatedAt)
					.ThenByDescending(x => x.Id)
					.First();

				rows.Add(new StageRow
				{
					Id = representative.Id,
					Source = RetailerSource,
					DealerName = FirstNonEmpty(representative.RetailerName, "-"),
					DealerCode = BuildDealerKey(
						representative.DealerRegistrationId,
						representative.IfmsDealerId),
					DealerType = "Retailer",
					ProductId = NullIfZero(group.Key.ProductId),
					StateId = representative.StateId,
					DistrictId = representative.DistrictId,
					MobileNo = FirstNonEmpty(representative.MobileNo, "-"),
					Stock = group.Sum(x => x.ClosingBalance),
					Sales = includeDptSales ? group.Sum(x => x.SoldQuantity) : 0m,
					ActivityDate = snapshotStart
				});
			}

			return rows;
		}

		// =====================================================================
		// Lookup loading
		// =====================================================================

		private async Task<LookupMaps> LoadLookupMapsAsync(
			IReadOnlyCollection<StageRow> rows,
			CancellationToken cancellationToken)
		{
			var stateIds = rows
				.Where(x => x.StateId.HasValue)
				.Select(x => x.StateId!.Value)
				.Distinct()
				.ToList();

			var districtIds = rows
				.Where(x => x.DistrictId.HasValue)
				.Select(x => x.DistrictId!.Value)
				.Distinct()
				.ToList();

			var productIds = rows
				.Where(x => x.ProductId.HasValue)
				.Select(x => x.ProductId!.Value)
				.Distinct()
				.ToList();

			var states = stateIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<State>()
					.AsNoTracking()
					.Where(x => stateIds.Contains(x.Id))
					.ToDictionaryAsync(x => x.Id, x => x.StateName, cancellationToken);

			var districts = districtIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<District>()
					.AsNoTracking()
					.Where(x => districtIds.Contains(x.Id))
					.ToDictionaryAsync(x => x.Id, x => x.DistrictName, cancellationToken);

			var products = productIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<Product>()
					.AsNoTracking()
					.Where(x => productIds.Contains(x.Id))
					.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

			return new LookupMaps(states, districts, products);
		}

		// =====================================================================
		// Existing calculations preserved
		// =====================================================================

		private static LiqCycleRowDto Classify(LiqCycleRowDto row)
		{
			row.Bucket = row.AgeingDays <= 30
				? "Fast"
				: row.AgeingDays <= 60
					? "Normal"
					: row.AgeingDays <= 90
						? "Slow"
						: "Critical";

			row.Status = row.AgeingDays <= 45
				? "Active"
				: row.AgeingDays <= 75
					? "Monitoring"
					: "Critical";

			return row;
		}

		private static LiqCycleSummaryDto BuildSummary(List<LiqCycleRowDto> rows)
		{
			var balanceStock = rows.Sum(x => x.Stock);
			var liquidated = rows.Sum(x => x.Sales);

			return new LiqCycleSummaryDto
			{
				// DTO BalanceStock remains TotalStock - Liquidated.
				TotalStock = balanceStock + liquidated,
				Liquidated = liquidated
			};
		}

		private static List<LiqCycleStatDto> BuildTopGroups(
			List<LiqCycleRowDto> rows,
			bool delayed)
		{
			return rows
				.Where(x =>
					!string.IsNullOrWhiteSpace(x.DealerName) &&
					x.DealerName != "-")
				.GroupBy(x => x.DealerName)
				.Select(group =>
				{
					var totalStock = group.Sum(x => x.Stock + x.Sales);

					var fastLiquidated = group
						.Where(x => x.Bucket == "Fast")
						.Sum(x => x.Sales);

					var slowLiquidated = group
						.Where(x => x.Bucket == "Slow" || x.Bucket == "Critical")
						.Sum(x => x.Sales);

					var selectedLiquidated = delayed
						? slowLiquidated
						: fastLiquidated;

					return new LiqCycleStatDto
					{
						DealerName = group.Key,
						TotalStock = totalStock,
						FastLiquidated = fastLiquidated,
						SlowLiquidated = slowLiquidated,
						Rate = totalStock == 0m
							? 0d
							: Math.Round(
								(double)(selectedLiquidated * 100m / totalStock),
								2)
					};
				})
				.OrderByDescending(x => x.Rate)
				.ThenByDescending(x => x.TotalStock)
				.Take(5)
				.ToList();
		}

		private static PagedResult<LiqCycleRowDto> BuildGrid(
			List<LiqCycleRowDto> rows,
			LiqCycleFilter filter,
			bool paged)
		{
			IEnumerable<LiqCycleRowDto> query = rows;

			if (!string.IsNullOrWhiteSpace(filter.Search))
			{
				var search = filter.Search.Trim();

				query = query.Where(x =>
					x.DealerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					x.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					x.StateName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					x.District.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					x.Source.Contains(search, StringComparison.OrdinalIgnoreCase));
			}

			query = (filter.SortColumn ?? string.Empty).ToLowerInvariant() switch
			{
				"dealer" => filter.SortDesc
					? query.OrderByDescending(x => x.DealerName)
					: query.OrderBy(x => x.DealerName),

				"product" => filter.SortDesc
					? query.OrderByDescending(x => x.ProductName)
					: query.OrderBy(x => x.ProductName),

				"stock" => filter.SortDesc
					? query.OrderByDescending(x => x.Stock)
					: query.OrderBy(x => x.Stock),

				"sales" => filter.SortDesc
					? query.OrderByDescending(x => x.Sales)
					: query.OrderBy(x => x.Sales),

				"ageing" => filter.SortDesc
					? query.OrderByDescending(x => x.AgeingDays)
					: query.OrderBy(x => x.AgeingDays),

				_ => filter.SortDesc
					? query.OrderByDescending(x => x.AgeingDays)
					: query.OrderBy(x => x.AgeingDays)
			};

			var filteredAndSorted = query.ToList();
			var safePage = Math.Max(1, filter.Page);
			var requestedPageSize = filter.PageSize <= 0
				? DefaultPageSize
				: filter.PageSize;

			var safePageSize = paged
				? Math.Min(requestedPageSize, MaximumDashboardPageSize)
				: requestedPageSize;

			var items = paged
				? filteredAndSorted
					.Skip((safePage - 1) * safePageSize)
					.Take(safePageSize)
					.ToList()
				: filteredAndSorted;

			return new PagedResult<LiqCycleRowDto>
			{
				Items = items,
				TotalCount = filteredAndSorted.Count,
				Page = safePage,
				PageSize = safePageSize
			};
		}

		// =====================================================================
		// Helpers
		// =====================================================================

		private static bool SourceMatches(string? selectedSource, string source)
		{
			return string.IsNullOrWhiteSpace(selectedSource) ||
				   string.Equals(selectedSource, "All", StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(selectedSource, source, StringComparison.OrdinalIgnoreCase);
		}

		private static void NormalizeFilter(LiqCycleFilter filter)
		{
			filter.StateIds ??= new List<int>();
			filter.DistrictIds ??= new List<int>();
			filter.ProductIds ??= new List<int>();
			filter.StatusIds ??= new List<int>();
			filter.DealerKeys ??= new List<string>();

			filter.StateIds = filter.StateIds.Where(x => x > 0).Distinct().ToList();
			filter.DistrictIds = filter.DistrictIds.Where(x => x > 0).Distinct().ToList();
			filter.ProductIds = filter.ProductIds.Where(x => x > 0).Distinct().ToList();
			filter.StatusIds = filter.StatusIds.Where(x => x > 0).Distinct().ToList();
			filter.DealerKeys = filter.DealerKeys
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			filter.Page = Math.Max(1, filter.Page);
			filter.PageSize = filter.PageSize <= 0
				? DefaultPageSize
				: filter.PageSize;
		}

		private static DateTime? ToUtcStart(DateTime? value)
		{
			return value.HasValue
				? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
				: null;
		}

		private static DateTime? ToUtcExclusiveEnd(DateTime? value)
		{
			return value.HasValue
				? DateTime.SpecifyKind(value.Value.Date.AddDays(1), DateTimeKind.Utc)
				: null;
		}

		private static int CalcDays(DateTime? date)
		{
			return date.HasValue
				? Math.Max(0, (DateTime.UtcNow.Date - date.Value.Date).Days)
				: 0;
		}

		private static string GetName(
			IReadOnlyDictionary<int, string> lookup,
			int? id)
		{
			return id.HasValue &&
				   lookup.TryGetValue(id.Value, out var name) &&
				   !string.IsNullOrWhiteSpace(name)
				? name
				: "-";
		}

		private static int? NullIfZero(int value)
		{
			return value == 0 ? null : value;
		}

		private static string BuildDealerKey(
			int? regularDealerId,
			int? ifmsDealerId)
		{
			if (regularDealerId.HasValue)
			{
				return $"R{regularDealerId.Value}";
			}

			if (ifmsDealerId.HasValue)
			{
				return $"I{ifmsDealerId.Value}";
			}

			return "-";
		}

		private static DealerIdSelection SplitDealerKeys(IEnumerable<string>? keys)
		{
			var regularIds = new List<int>();
			var ifmsIds = new List<int>();

			foreach (var key in keys ?? Enumerable.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(key) ||
					key.Length < 2 ||
					!int.TryParse(key.Substring(1), out var id) ||
					id <= 0)
				{
					continue;
				}

				if (key.StartsWith("R", StringComparison.OrdinalIgnoreCase))
				{
					regularIds.Add(id);
				}
				else if (key.StartsWith("I", StringComparison.OrdinalIgnoreCase))
				{
					ifmsIds.Add(id);
				}
			}

			return new DealerIdSelection
			{
				RegularIds = regularIds.Distinct().ToList(),
				IfmsIds = ifmsIds.Distinct().ToList()
			};
		}

		private static string BuildRetailerIdentity(
			int? regularDealerId,
			int? ifmsDealerId,
			string? retailerName,
			int? stateId,
			int? districtId)
		{
			var dealerKey = BuildDealerKey(regularDealerId, ifmsDealerId);
			if (dealerKey != "-")
			{
				return dealerKey;
			}

			return $"N:{NormalizeText(retailerName)}|S:{stateId ?? 0}|D:{districtId ?? 0}";
		}

		private static string NormalizeText(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim().ToUpperInvariant();
		}

		private static string FirstNonEmpty(params string?[] values)
		{
			return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "-";
		}

		// =====================================================================
		// Internal compact models
		// =====================================================================

		private sealed class StageRow
		{
			public int Id { get; set; }
			public string Source { get; set; } = "";
			public string? DealerName { get; set; }
			public string? DealerCode { get; set; }
			public string DealerType { get; set; } = "";
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public string? MobileNo { get; set; }
			public decimal Stock { get; set; }
			public decimal Sales { get; set; }
			public DateTime? ActivityDate { get; set; }
		}

		private sealed class DealerIdSelection
		{
			public List<int> RegularIds { get; set; } = new();
			public List<int> IfmsIds { get; set; } = new();
			public bool HasAny => RegularIds.Count > 0 || IfmsIds.Count > 0;
		}

		private sealed record LookupMaps(
			Dictionary<int, string> States,
			Dictionary<int, string> Districts,
			Dictionary<int, string> Products);

		private readonly record struct LocationProductKey(
			int StateId,
			int DistrictId,
			int ProductId);

		private readonly record struct DealerProductKey(
			string DealerKey,
			int ProductId);

		private sealed class WarehouseSnapshot
		{
			public int Id { get; set; }
			public int? WarehouseId { get; set; }
			public int? PlantId { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public decimal ClosingStock { get; set; }
			public DateTime ReportDate { get; set; }
			public DateTime UpdatedAt { get; set; }
		}

		private sealed class WarehouseBalance
		{
			public int Id { get; set; }
			public decimal ClosingStock { get; set; }
			public DateTime SnapshotDate { get; set; }
		}

		private sealed class CompanySalesAggregate
		{
			public int StateId { get; set; }
			public int DistrictId { get; set; }
			public int ProductId { get; set; }
			public decimal Quantity { get; set; }
			public DateTime? LastDate { get; set; }
		}

		private sealed class WholesalerStockSnapshot
		{
			public int Id { get; set; }
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public string? AgencyName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? CompanyId { get; set; }
			public int? PlantId { get; set; }
			public int? ProductId { get; set; }
			public decimal Stock { get; set; }
			public DateTime StockDate { get; set; }
			public DateTime UpdatedAt { get; set; }
		}

		private sealed class WholesalerBalance
		{
			public int Id { get; set; }
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public string? AgencyName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public decimal Stock { get; set; }
			public DateTime SnapshotDate { get; set; }
		}

		private sealed class WholesalerSalesAggregate
		{
			public int? WholesalerId { get; set; }
			public int? IfmsWholesalerId { get; set; }
			public int ProductId { get; set; }
			public decimal Quantity { get; set; }
			public DateTime? LastDate { get; set; }
		}

		private sealed class WholesalerSaleRepresentative
		{
			public int? WholesalerId { get; set; }
			public int? IfmsWholesalerId { get; set; }
			public int? ProductId { get; set; }
			public string? DealerName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public string? MobileNo { get; set; }
		}

		private readonly record struct RetailerProductKey(
			string RetailerKey,
			int ProductId);

		private sealed class RetailerSnapshot
		{
			public int Id { get; set; }
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public string? RetailerName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? SubDistrictId { get; set; }
			public int? CompanyId { get; set; }
			public int? PlantId { get; set; }
			public int? ProductId { get; set; }
			public string? MobileNo { get; set; }
			public decimal SoldQuantity { get; set; }
			public decimal ClosingBalance { get; set; }
			public DateTime ReportDate { get; set; }
			public DateTime UpdatedAt { get; set; }
		}

	}
}