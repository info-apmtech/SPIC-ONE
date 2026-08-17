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
	/// Retailer   : latest DptReport.ClosingBalance + DptReport.SoldQuantity for the
	///              selected report-date period.
	/// Historical stock snapshot dates are never added together.
	///
	/// Liquidation ageing in this implementation means days since the most recent
	/// sales/liquidation activity for the row. When a row has no sales, its latest
	/// stock snapshot date is used as the existing fallback. DateTo is the as-of date.
	/// This preserves the current DTO/UI flow; it is not lot/FIFO receipt-to-sale ageing.
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
				TopFastDealers = BuildTopDealerGroups(rows, delayed: false),
				TopSlowDealers = BuildTopDealerGroups(rows, delayed: true),
				TopFastStates = BuildTopGroups(rows, x => x.StateName, delayed: false),
				TopSlowStates = BuildTopGroups(rows, x => x.StateName, delayed: true),
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

		public async Task<List<LiqCycleProductDto>> GetProductsAsync(
			CancellationToken cancellationToken = default)
		{
			// Keep the existing Product master and add IFMS products as a second source.
			// P:<id> and I:<id> prevent equal numeric IDs from colliding.
			var approvedRows = await _db.Set<Product>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != string.Empty)
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			var ifmsRows = await _db.Set<IfmsProduct>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != string.Empty)
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			return approvedRows
				.Select(x => new LiqCycleProductDto
				{
					Key = $"P:{x.Id}",
					Name = x.Name!,
					Source = "Product"
				})
				.Concat(ifmsRows.Select(x => new LiqCycleProductDto
				{
					Key = $"I:{x.Id}",
					Name = x.Name!,
					Source = "IFMS"
				}))
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public async Task<List<AckLookupItemDto>> GetDealersAsync(
			CancellationToken cancellationToken = default)
		{
			// Keep the existing dealer-key contract used by every report:
			// R{id} = DealerRegistration.Id, I{id} = IfmsDealer.Id.
			// Only the visible name is cleaned; database values are never changed here.
			var registeredDealers = await _db.Set<DealerRegistration>()
				.AsNoTracking()
				.Where(x => x.FirmName != null && x.FirmName != string.Empty)
				.Select(x => new { x.Id, x.FirmName })
				.ToListAsync(cancellationToken);

			var ifmsDealers = await _db.Set<IfmsDealer>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != string.Empty)
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			var result = new List<AckLookupItemDto>(
				registeredDealers.Count + ifmsDealers.Count);

			foreach (var dealer in registeredDealers)
			{
				var name = NormalizeDealerLookupName(dealer.FirmName);
				if (name is null)
					continue;

				result.Add(new AckLookupItemDto
				{
					Id = $"R{dealer.Id}",
					Name = name
				});
			}

			foreach (var dealer in ifmsDealers)
			{
				var name = NormalizeDealerLookupName(dealer.Name);
				if (name is null)
					continue;

				result.Add(new AckLookupItemDto
				{
					Id = $"I{dealer.Id}",
					Name = name
				});
			}

			// Do not expose Registered/IFMS or R/I ids in the visible dropdown label.
			// Duplicate names are retained because their hidden keys can refer to
			// different transaction identities.
			return result
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static string? NormalizeDealerLookupName(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var name = value.Trim().Trim(DealerNameNoiseCharacters);
			name = string.Join(
				" ",
				name.Split(
					new[] { ' ', '\t', '\r', '\n' },
					StringSplitOptions.RemoveEmptyEntries));

			if (string.IsNullOrWhiteSpace(name))
				return null;

			if (name.All(char.IsDigit) && name.All(ch => ch == '0'))
				return null;

			if (!name.Any(char.IsLetterOrDigit))
				return null;

			return name;
		}

		private static readonly char[] DealerNameNoiseCharacters =
		{
			'.', ',', ';', ':', '_', '-', '|', '/', '\\', '\'', '"', '`', '~'
		};

		// =====================================================================
		// Unified compact loading
		// =====================================================================

		private async Task<List<LiqCycleRowDto>> BuildRowsAsync(
			LiqCycleFilter filter,
			CancellationToken cancellationToken)
		{
			var from = ToUtcStart(filter.DateFrom);
			var toExclusive = ToUtcExclusiveEnd(filter.DateTo);
			var asOfDate = filter.DateTo?.Date ?? DateTime.UtcNow.Date;
			var dealerIds = SplitDealerKeys(filter.DealerKeys);
			var productIds = SplitProductKeys(filter);

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
					productIds,
					cancellationToken));
			}

			if (SourceMatches(filter.Source, WholesalerSource))
			{
				stageRows.AddRange(await BuildWholesalerStageRowsAsync(
					filter,
					from,
					toExclusive,
					dealerIds,
					productIds,
					cancellationToken));
			}

			if (SourceMatches(filter.Source, RetailerSource))
			{
				stageRows.AddRange(await BuildRetailerStageRowsAsync(
					filter,
					from,
					toExclusive,
					dealerIds,
					productIds,
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
					ProductId = row.ProductId,
					IfmsProductId = row.IfmsProductId,
					ProductName = GetProductName(
						lookup.Products,
						lookup.IfmsProducts,
						row.ProductId,
						row.IfmsProductId),
					StateName = GetName(lookup.States, row.StateId),
					District = GetName(lookup.Districts, row.DistrictId),
					MobileNo = FirstNonEmpty(row.MobileNo, "-"),
					Stock = row.Stock,
					Sales = row.Sales,
					AgeingDays = CalcDays(row.ActivityDate, asOfDate)
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
			ProductIdSelection productIds,
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

			if (productIds.HasAny)
			{
				warehouseBase = warehouseBase.Where(x =>
					(x.ProductId.HasValue && productIds.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue && productIds.IfmsProductIds.Contains(x.IfmsProductId.Value)));
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
						IfmsProductId = x.IfmsProductId,
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
						ProductId = x.ProductId ?? 0,
						IfmsProductId = x.IfmsProductId ?? 0
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
						x.ProductId ?? 0,
						x.IfmsProductId ?? 0))
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

			if (productIds.HasAny)
			{
				companySalesQuery = companySalesQuery.Where(x =>
					(x.ProductId.HasValue && productIds.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue && productIds.IfmsProductIds.Contains(x.IfmsProductId.Value)));
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
					ProductId = x.ProductId ?? 0,
					IfmsProductId = x.IfmsProductId ?? 0
				})
				.Select(group => new CompanySalesAggregate
				{
					StateId = group.Key.StateId,
					DistrictId = group.Key.DistrictId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					Quantity = group.Sum(x => x.QuantityMT),
					LastDate = group.Max(x => x.InvoiceDate)
				})
				.ToListAsync(cancellationToken);

			var companySales = companySalesRows.ToDictionary(
				x => new LocationProductKey(
					x.StateId,
					x.DistrictId,
					x.ProductId,
					x.IfmsProductId));

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
					IfmsProductId = NullIfZero(key.IfmsProductId),
					MobileNo = "-",
					Stock = stock?.ClosingStock ?? 0m,
					Sales = sales?.Quantity ?? 0m,
					ActivityDate = sales?.LastDate ?? stock?.SnapshotDate
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
			ProductIdSelection productIds,
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

			if (productIds.HasAny)
			{
				stockBase = stockBase.Where(x =>
					(x.ProductId.HasValue && productIds.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue && productIds.IfmsProductIds.Contains(x.IfmsProductId.Value)));
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
						IfmsProductId = x.IfmsProductId,
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
						ProductId = x.ProductId ?? 0,
						IfmsProductId = x.IfmsProductId ?? 0
					})
					.Select(group => group
						.OrderByDescending(x => x.UpdatedAt)
						.ThenByDescending(x => x.Id)
						.First())
					.ToList();

				latestStock = deduplicatedStockRows
					.GroupBy(x => new DealerProductKey(
						BuildDealerKey(x.DealerRegistrationId, x.IfmsDealerId),
						x.ProductId ?? 0,
						x.IfmsProductId ?? 0))
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
								IfmsProductId = representative.IfmsProductId,
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

			if (productIds.HasAny)
			{
				salesBase = salesBase.Where(x =>
					(x.ProductId.HasValue && productIds.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue && productIds.IfmsProductIds.Contains(x.IfmsProductId.Value)));
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
					ProductId = x.ProductId ?? 0,
					IfmsProductId = x.IfmsProductId ?? 0
				})
				.Select(group => new WholesalerSalesAggregate
				{
					WholesalerId = group.Key.RegularId,
					IfmsWholesalerId = group.Key.IfmsId,
					ProductId = group.Key.ProductId,
					IfmsProductId = group.Key.IfmsProductId,
					Quantity = group.Sum(x => x.QuantityMT),
					LastDate = group.Max(x => x.InvoiceDate)
				})
				.ToListAsync(cancellationToken);

			// Pick the latest representative row. The previous comparison used
			// candidate.Id < current.Id, which selected the oldest row.
			var representativeSalesQuery = salesBase.Where(current =>
				!salesBase.Any(candidate =>
					(candidate.WholesalerId ?? 0) == (current.WholesalerId ?? 0) &&
					(candidate.WholesalerId.HasValue ? 0 : candidate.IfmsWholesalerId ?? 0) ==
					(current.WholesalerId.HasValue ? 0 : current.IfmsWholesalerId ?? 0) &&
					(candidate.ProductId ?? 0) == (current.ProductId ?? 0) &&
					(candidate.IfmsProductId ?? 0) == (current.IfmsProductId ?? 0) &&
					candidate.Id > current.Id));

			var representativeSalesRows = await representativeSalesQuery
				.Select(x => new WholesalerSaleRepresentative
				{
					WholesalerId = x.WholesalerId,
					IfmsWholesalerId = x.IfmsWholesalerId,
					ProductId = x.ProductId,
					IfmsProductId = x.IfmsProductId,
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
						x.ProductId ?? 0,
						x.IfmsProductId ?? 0),
					Row = x
				})
				.Where(x => x.Key.DealerKey != "-")
				.GroupBy(x => x.Key)
				.ToDictionary(group => group.Key, group => group.First().Row);

			var sales = salesAggregateRows
				.GroupBy(x => new DealerProductKey(
					BuildDealerKey(x.WholesalerId, x.IfmsWholesalerId),
					x.ProductId,
					x.IfmsProductId))
				.Where(group => group.Key.DealerKey != "-")
				.ToDictionary(
					group => group.Key,
					group => new WholesalerSalesAggregate
					{
						WholesalerId = group.First().WholesalerId,
						IfmsWholesalerId = group.First().IfmsWholesalerId,
						ProductId = group.Key.ProductId,
						IfmsProductId = group.Key.IfmsProductId,
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
					IfmsProductId = NullIfZero(key.IfmsProductId),
					StateId = stock?.StateId ?? representative?.StateId,
					DistrictId = stock?.DistrictId ?? representative?.DistrictId,
					MobileNo = FirstNonEmpty(representative?.MobileNo, "-"),
					Stock = stock?.Stock ?? 0m,
					Sales = sale?.Quantity ?? 0m,
					ActivityDate = sale?.LastDate ?? stock?.SnapshotDate
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
			ProductIdSelection productIds,
			CancellationToken cancellationToken)
		{
			var filteredBase = _db.DptReports
				.AsNoTracking()
				.AsQueryable();

			if (filter.StateIds.Count > 0)
			{
				filteredBase = filteredBase.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				filteredBase = filteredBase.Where(x =>
					x.DistrictId.HasValue && filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.SubDistrictIds.Count > 0)
			{
				filteredBase = filteredBase.Where(x =>
					x.SubDistrictId.HasValue && filter.SubDistrictIds.Contains(x.SubDistrictId.Value));
			}

			if (productIds.HasAny)
			{
				filteredBase = filteredBase.Where(x =>
					(x.ProductId.HasValue && productIds.ProductIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue && productIds.IfmsProductIds.Contains(x.IfmsProductId.Value)));
			}

			if (dealerIds.HasAny)
			{
				filteredBase = filteredBase.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 dealerIds.RegularIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 dealerIds.IfmsIds.Contains(x.IfmsDealerId.Value)));
			}

			// DPT has no StatusId. A Status filter therefore cannot safely be applied
			// to retailer rows; the existing flow leaves DPT unaffected by that filter.

			// ---------------------------------------------------------------
			// Current retailer stock: latest DPT snapshot on/before DateTo.
			// ---------------------------------------------------------------
			var stockBase = filteredBase;
			if (toExclusive.HasValue)
			{
				stockBase = stockBase.Where(x => x.CreatedAt < toExclusive.Value);
			}

			var latestDptReportValue = await stockBase
				.Select(x => (DateTime?)x.CreatedAt)
				.MaxAsync(cancellationToken);

			var stockByKey = new Dictionary<RetailerProductKey, RetailerBalance>();

			if (latestDptReportValue.HasValue)
			{
				var snapshotStart = latestDptReportValue.Value.Date;
				var snapshotEnd = snapshotStart.AddDays(1);

				var rawSnapshotRows = await stockBase
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
						IfmsProductId = x.IfmsProductId,
						MobileNo = x.MobileNo,
						SoldQuantity = x.SoldQuantity,
						ClosingBalance = x.ClosingBalance,
						ReportDate = x.CreatedAt,
						UpdatedAt = x.UpdatedAt
					})
					.ToListAsync(cancellationToken);

				var deduplicatedSnapshotRows = DeduplicateDptRows(rawSnapshotRows);

				stockByKey = deduplicatedSnapshotRows
					.GroupBy(x => new RetailerProductKey(
						BuildRetailerIdentity(
							x.DealerRegistrationId,
							x.IfmsDealerId,
							x.RetailerName,
							x.StateId,
							x.DistrictId),
						x.ProductId ?? 0,
						x.IfmsProductId ?? 0))
					.ToDictionary(
						group => group.Key,
						group =>
						{
							var representative = group
								.OrderByDescending(x => x.UpdatedAt)
								.ThenByDescending(x => x.Id)
								.First();

							return new RetailerBalance
							{
								Id = representative.Id,
								DealerRegistrationId = representative.DealerRegistrationId,
								IfmsDealerId = representative.IfmsDealerId,
								RetailerName = representative.RetailerName,
								StateId = representative.StateId,
								DistrictId = representative.DistrictId,
								ProductId = representative.ProductId,
								IfmsProductId = representative.IfmsProductId,
								MobileNo = representative.MobileNo,
								ClosingBalance = group.Sum(x => x.ClosingBalance),
								SnapshotDate = snapshotStart
							};
						});
			}

			// ---------------------------------------------------------------
			// Retailer liquidation: sum DPT SoldQuantity for the selected period.
			// DPT is a daily report/snapshot, so only the closing balance is latest;
			// sold quantities from separate report dates are transactional period data.
			// Same-day reuploads are deduplicated before summing.
			// ---------------------------------------------------------------
			var salesBase = filteredBase.Where(x => x.SoldQuantity != 0m);

			if (from.HasValue)
			{
				salesBase = salesBase.Where(x => x.CreatedAt >= from.Value);
			}

			if (toExclusive.HasValue)
			{
				salesBase = salesBase.Where(x => x.CreatedAt < toExclusive.Value);
			}

			var rawSalesRows = await salesBase
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
					IfmsProductId = x.IfmsProductId,
					MobileNo = x.MobileNo,
					SoldQuantity = x.SoldQuantity,
					ClosingBalance = x.ClosingBalance,
					ReportDate = x.CreatedAt,
					UpdatedAt = x.UpdatedAt
				})
				.ToListAsync(cancellationToken);

			var deduplicatedSalesRows = DeduplicateDptRows(rawSalesRows, includeReportDate: true);

			var salesByKey = deduplicatedSalesRows
				.GroupBy(x => new RetailerProductKey(
					BuildRetailerIdentity(
						x.DealerRegistrationId,
						x.IfmsDealerId,
						x.RetailerName,
						x.StateId,
						x.DistrictId),
					x.ProductId ?? 0,
					x.IfmsProductId ?? 0))
				.ToDictionary(
					group => group.Key,
					group =>
					{
						var representative = group
							.OrderByDescending(x => x.ReportDate)
							.ThenByDescending(x => x.UpdatedAt)
							.ThenByDescending(x => x.Id)
							.First();

						return new RetailerSalesAggregate
						{
							DealerRegistrationId = representative.DealerRegistrationId,
							IfmsDealerId = representative.IfmsDealerId,
							RetailerName = representative.RetailerName,
							StateId = representative.StateId,
							DistrictId = representative.DistrictId,
							ProductId = representative.ProductId,
							IfmsProductId = representative.IfmsProductId,
							MobileNo = representative.MobileNo,
							Quantity = group.Sum(x => x.SoldQuantity),
							LastDate = group.Max(x => x.ReportDate)
						};
					});

			var keys = stockByKey.Keys
				.Union(salesByKey.Keys)
				.Distinct()
				.ToList();

			var rows = new List<StageRow>(keys.Count);

			foreach (var key in keys)
			{
				stockByKey.TryGetValue(key, out var stock);
				salesByKey.TryGetValue(key, out var sale);

				rows.Add(new StageRow
				{
					Id = stock?.Id ?? 0,
					Source = RetailerSource,
					DealerName = FirstNonEmpty(stock?.RetailerName, sale?.RetailerName, "-"),
					DealerCode = BuildDealerKey(
						stock?.DealerRegistrationId ?? sale?.DealerRegistrationId,
						stock?.IfmsDealerId ?? sale?.IfmsDealerId),
					DealerType = "Retailer",
					ProductId = NullIfZero(key.ProductId),
					IfmsProductId = NullIfZero(key.IfmsProductId),
					StateId = stock?.StateId ?? sale?.StateId,
					DistrictId = stock?.DistrictId ?? sale?.DistrictId,
					MobileNo = FirstNonEmpty(stock?.MobileNo, sale?.MobileNo, "-"),
					Stock = stock?.ClosingBalance ?? 0m,
					Sales = sale?.Quantity ?? 0m,
					ActivityDate = sale?.LastDate ?? stock?.SnapshotDate
				});
			}

			return rows;
		}

		private static List<RetailerSnapshot> DeduplicateDptRows(
			IEnumerable<RetailerSnapshot> rows,
			bool includeReportDate = false)
		{
			return rows
				.GroupBy(x => new
				{
					ReportDate = includeReportDate ? x.ReportDate.Date : DateTime.MinValue,
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
					ProductId = x.ProductId ?? 0,
					IfmsProductId = x.IfmsProductId ?? 0
				})
				.Select(group => group
					.OrderByDescending(x => x.UpdatedAt)
					.ThenByDescending(x => x.Id)
					.First())
				.ToList();
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

			var ifmsProductIds = rows
				.Where(x => x.IfmsProductId.HasValue)
				.Select(x => x.IfmsProductId!.Value)
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

			var ifmsProducts = ifmsProductIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<IfmsProduct>()
					.AsNoTracking()
					.Where(x => ifmsProductIds.Contains(x.Id))
					.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

			return new LookupMaps(states, districts, products, ifmsProducts);
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

		private static List<LiqCycleStatDto> BuildTopDealerGroups(
			List<LiqCycleRowDto> rows,
			bool delayed)
		{
			return rows
				.Where(x => !string.IsNullOrWhiteSpace(x.DealerName) && x.DealerName != "-")
				.GroupBy(BuildDealerGroupKey)
				.Select(group =>
				{
					var totalStock = group.Sum(x => x.Stock + x.Sales);
					var fastLiquidated = group
						.Where(x => x.Bucket == "Fast")
						.Sum(x => x.Sales);
					var slowLiquidated = group
						.Where(x => x.Bucket == "Slow" || x.Bucket == "Critical")
						.Sum(x => x.Sales);
					var selectedLiquidated = delayed ? slowLiquidated : fastLiquidated;

					return new LiqCycleStatDto
					{
						DealerName = group
							.Select(x => x.DealerName)
							.FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "-",
						TotalStock = totalStock,
						FastLiquidated = fastLiquidated,
						SlowLiquidated = slowLiquidated,
						Rate = totalStock == 0m
							? 0d
							: Math.Round((double)(selectedLiquidated * 100m / totalStock), 2)
					};
				})
				.OrderByDescending(x => x.Rate)
				.ThenByDescending(x => x.TotalStock)
				.Take(5)
				.ToList();
		}

		private static string BuildDealerGroupKey(LiqCycleRowDto row)
		{
			if (!string.IsNullOrWhiteSpace(row.DealerCode) && row.DealerCode != "-")
				return row.DealerCode.Trim().ToUpperInvariant();

			return $"N:{NormalizeText(row.DealerName)}";
		}

		private static List<LiqCycleStatDto> BuildTopGroups(
			List<LiqCycleRowDto> rows,
			Func<LiqCycleRowDto, string> keySelector,
			bool delayed)
		{
			return rows
				.Where(x =>
					!string.IsNullOrWhiteSpace(keySelector(x)) &&
					keySelector(x) != "-")
				.GroupBy(keySelector)
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
			filter.SubDistrictIds ??= new List<int>();
			filter.ProductIds ??= new List<int>();
			filter.ProductKeys ??= new List<string>();
			filter.StatusIds ??= new List<int>();
			filter.DealerKeys ??= new List<string>();

			filter.StateIds = filter.StateIds.Where(x => x > 0).Distinct().ToList();
			filter.DistrictIds = filter.DistrictIds.Where(x => x > 0).Distinct().ToList();
			filter.SubDistrictIds = filter.SubDistrictIds.Where(x => x > 0).Distinct().ToList();
			filter.ProductIds = filter.ProductIds.Where(x => x > 0).Distinct().ToList();
			filter.ProductKeys = filter.ProductKeys
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
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

		private static int CalcDays(DateTime? date, DateTime asOfDate)
		{
			return date.HasValue
				? Math.Max(0, (asOfDate.Date - date.Value.Date).Days)
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

		private static string GetProductName(
			IReadOnlyDictionary<int, string> products,
			IReadOnlyDictionary<int, string> ifmsProducts,
			int? productId,
			int? ifmsProductId)
		{
			var approvedName = GetName(products, productId);
			if (approvedName != "-")
			{
				return approvedName;
			}

			return GetName(ifmsProducts, ifmsProductId);
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

		private static ProductIdSelection SplitProductKeys(LiqCycleFilter filter)
		{
			// ProductIds is retained for all existing clients. ProductKeys is used by
			// the new combined dropdown and keeps Product/IFMS numeric IDs separate.
			var approvedIds = new HashSet<int>(filter.ProductIds.Where(x => x > 0));
			var ifmsIds = new HashSet<int>();

			foreach (var rawKey in filter.ProductKeys ?? Enumerable.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(rawKey))
				{
					continue;
				}

				var parts = rawKey.Trim().Split(':', 2);
				if (parts.Length != 2 ||
					!int.TryParse(parts[1], out var id) ||
					id <= 0)
				{
					continue;
				}

				if (string.Equals(parts[0], "P", StringComparison.OrdinalIgnoreCase))
				{
					approvedIds.Add(id);
				}
				else if (string.Equals(parts[0], "I", StringComparison.OrdinalIgnoreCase))
				{
					ifmsIds.Add(id);
				}
			}

			return new ProductIdSelection
			{
				ProductIds = approvedIds.ToList(),
				IfmsProductIds = ifmsIds.ToList()
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
			public int? IfmsProductId { get; set; }
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

		private sealed class ProductIdSelection
		{
			public List<int> ProductIds { get; set; } = new();
			public List<int> IfmsProductIds { get; set; } = new();
			public bool HasAny => ProductIds.Count > 0 || IfmsProductIds.Count > 0;
		}

		private sealed record LookupMaps(
			Dictionary<int, string> States,
			Dictionary<int, string> Districts,
			Dictionary<int, string> Products,
			Dictionary<int, string> IfmsProducts);

		private readonly record struct LocationProductKey(
			int StateId,
			int DistrictId,
			int ProductId,
			int IfmsProductId);

		private readonly record struct DealerProductKey(
			string DealerKey,
			int ProductId,
			int IfmsProductId);

		private sealed class WarehouseSnapshot
		{
			public int Id { get; set; }
			public int? WarehouseId { get; set; }
			public int? PlantId { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
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
			public int IfmsProductId { get; set; }
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
			public int? IfmsProductId { get; set; }
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
			public int? IfmsProductId { get; set; }
			public decimal Stock { get; set; }
			public DateTime SnapshotDate { get; set; }
		}

		private sealed class WholesalerSalesAggregate
		{
			public int? WholesalerId { get; set; }
			public int? IfmsWholesalerId { get; set; }
			public int ProductId { get; set; }
			public int IfmsProductId { get; set; }
			public decimal Quantity { get; set; }
			public DateTime? LastDate { get; set; }
		}

		private sealed class WholesalerSaleRepresentative
		{
			public int? WholesalerId { get; set; }
			public int? IfmsWholesalerId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
			public string? DealerName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public string? MobileNo { get; set; }
		}

		private sealed class RetailerBalance
		{
			public int Id { get; set; }
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public string? RetailerName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
			public string? MobileNo { get; set; }
			public decimal ClosingBalance { get; set; }
			public DateTime SnapshotDate { get; set; }
		}

		private sealed class RetailerSalesAggregate
		{
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public string? RetailerName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
			public string? MobileNo { get; set; }
			public decimal Quantity { get; set; }
			public DateTime? LastDate { get; set; }
		}

		private readonly record struct RetailerProductKey(
			string RetailerKey,
			int ProductId,
			int IfmsProductId);

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
			public int? IfmsProductId { get; set; }
			public string? MobileNo { get; set; }
			public decimal SoldQuantity { get; set; }
			public decimal ClosingBalance { get; set; }
			public DateTime ReportDate { get; set; }
			public DateTime UpdatedAt { get; set; }
		}

	}
}