// ============================================================================
//  AgeingReportService — Spic.Infrastructure/Services/
//
//  Current-stock sources (latest snapshot only):
//    1. WholesalerStockAsOnToday.Stock
//    2. DptReport.ClosingBalance
//    3. WarehouseDistrictGlobalStockReconciliation.ClosingStock
//
//  Sales sources:
//    1. SalesCompanySale.QuantityMT
//    2. SalesWholesaler.QuantityMT
//    3. DptReport.SoldQuantity
//
//  ACK-based stock-ageing rule:
//    - Company / wholesaler sales start ageing only when Status.Name = "Ack"
//      and RetailerReceiptDate is present.
//    - Stock is matched by DealerRegistrationId or IfmsDealerId + ProductId.
//    - DPT retailer stock first uses a strict workflow ACK. When none exists,
//      the latest DptReport row with SoldQuantity > 0 is used as the retailer-
//      sale ACK-equivalent date because DptReport has no StatusId or
//      RetailerReceiptDate.
//    - Warehouse stock remains part of Total Stock / State-wise stock, but is
//      excluded from ageing buckets and the ageing list because the supplied
//      warehouse reconciliation has no dealer/status/receipt relationship.
//
//  Status buckets (non-overlapping):
//    Fresh       0-30 days
//    Medium      31-90 days
//    Slow Moving 91-180 days
//    Long Aged   181-364 days
//    Critical    365+ days
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
	public sealed class AgeingReportService : IAgeingReportService
	{
		private readonly AppDbContext _db;

		private const int FreshMax = 30;
		private const int MediumMax = 90;
		private const int SlowMovingMax = 180;
		private const int CriticalMin = 365;

		public AgeingReportService(AppDbContext db)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
		}

		private static DateTime Today() => DateTime.UtcNow.Date;

		public async Task<AgeingDashboardDto> GetDashboardAsync(
			AgeingReportFilter filter,
			CancellationToken cancellationToken = default)
		{
			var f = NormalizeFilter(filter);
			var today = Today();

			// Load only the latest available stock day from each snapshot source.
			var snapshots = await LoadCurrentSnapshotsAsync(f, cancellationToken);

			// Build dealer + product acknowledgement dates only for the current
			// dealer-held stock rows that can actually be aged.
			var acknowledgementLookup = await BuildAcknowledgementLookupAsync(
				snapshots.WholesalerRows,
				snapshots.DptRows,
				cancellationToken);

			var allAgeableRows = await BuildAgeableStockRowsAsync(
				snapshots.WholesalerRows,
				snapshots.DptRows,
				acknowledgementLookup,
				today,
				cancellationToken);

			// Search and ageing-range filters apply to ageing rows, just as in the
			// existing page. Total Stock remains the complete current snapshot.
			var filteredAgeingRows = ApplyAgeingRowFilters(allAgeableRows, f);
			var sortedAgeingRows = ApplySorting(filteredAgeingRows, f);

			var totalStock =
				snapshots.WholesalerRows.Sum(x => x.Stock) +
				snapshots.DptRows.Sum(x => x.ClosingBalance) +
				snapshots.WarehouseRows.Sum(x => x.ClosingStock);

			var averageAgeing = filteredAgeingRows.Count == 0
				? 0d
				: Math.Round(filteredAgeingRows.Average(x => x.AgeingDays), 1);

			var stock30To60 = filteredAgeingRows
				.Where(x => x.AgeingDays >= 31 && x.AgeingDays <= 60)
				.Sum(x => x.Quantity);

			var stock60Plus = filteredAgeingRows
				.Where(x => x.AgeingDays >= 61)
				.Sum(x => x.Quantity);

			var currentStockByState = BuildCurrentStockByState(snapshots);
			var salesByState = await LoadSalesByStateAsync(
				f,
				today,
				cancellationToken);

			var stateNames = await LoadStateNamesAsync(
				currentStockByState,
				salesByState,
				cancellationToken);

			var stateBuckets = BuildStateBuckets(filteredAgeingRows);
			var dayBuckets = BuildDayBuckets(filteredAgeingRows);

			var page = Math.Max(1, f.Page);
			var pageSize = Math.Max(1, f.PageSize);
			var pagedItems = sortedAgeingRows
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(ToDto)
				.ToList();

			return new AgeingDashboardDto
			{
				Summary = new AgeingSummaryDto
				{
					TotalStock = totalStock,
					TotalStockChangePct = 0m,
					AverageAgeing = averageAgeing,
					AverageAgeingChange = 0d,
					Stock30To60 = stock30To60,
					Stock30To60ChangePct = 0m,
					Stock60Plus = stock60Plus,
					Stock60PlusChangePct = 0m
				},
				StateWise = BuildStockVsSalesStateWise(
					currentStockByState,
					salesByState,
					stateNames),
				StateBuckets = stateBuckets,
				DayBuckets = dayBuckets,
				Grid = new PagedResult<AgeingRowDto>
				{
					Items = pagedItems,
					TotalCount = filteredAgeingRows.Count,
					Page = page,
					PageSize = pageSize
				}
			};
		}

		public async Task<List<AgeingRowDto>> GetAllRowsAsync(
			AgeingReportFilter filter,
			CancellationToken cancellationToken = default)
		{
			var f = NormalizeFilter(filter);
			var today = Today();

			var snapshots = await LoadCurrentSnapshotsAsync(f, cancellationToken);
			var acknowledgementLookup = await BuildAcknowledgementLookupAsync(
				snapshots.WholesalerRows,
				snapshots.DptRows,
				cancellationToken);

			var rows = await BuildAgeableStockRowsAsync(
				snapshots.WholesalerRows,
				snapshots.DptRows,
				acknowledgementLookup,
				today,
				cancellationToken);

			return ApplySorting(ApplyAgeingRowFilters(rows, f), f)
				.Select(ToDto)
				.ToList();
		}

		// ====================================================================
		// Latest current-stock snapshots
		// ====================================================================

		private async Task<CurrentSnapshotBundle> LoadCurrentSnapshotsAsync(
			AgeingReportFilter f,
			CancellationToken cancellationToken)
		{
			var result = new CurrentSnapshotBundle();

			var latestWholesalerTimestamp = await _db
				.Set<WholesalerStockAsOnToday>()
				.AsNoTracking()
				.MaxAsync(x => (DateTime?)x.StockDate, cancellationToken);

			if (latestWholesalerTimestamp.HasValue)
			{
				var start = latestWholesalerTimestamp.Value.Date;
				var end = start.AddDays(1);

				var query = _db.Set<WholesalerStockAsOnToday>()
					.AsNoTracking()
					.Where(x =>
						x.StockDate >= start &&
						x.StockDate < end &&
						x.Stock > 0m);

				query = ApplyWholesalerStockFilters(query, f);
				result.WholesalerRows = await query.ToListAsync(cancellationToken);
			}

			var latestDptTimestamp = await _db
				.Set<DptReport>()
				.AsNoTracking()
				.MaxAsync(x => (DateTime?)x.CreatedAt, cancellationToken);

			if (latestDptTimestamp.HasValue)
			{
				var start = latestDptTimestamp.Value.Date;
				var end = start.AddDays(1);

				var query = _db.Set<DptReport>()
					.AsNoTracking()
					.Where(x =>
						x.CreatedAt >= start &&
						x.CreatedAt < end &&
						x.ClosingBalance > 0m);

				query = ApplyDptStockFilters(query, f);
				result.DptRows = await query.ToListAsync(cancellationToken);
			}

			var latestWarehouseTimestamp = await _db
				.Set<WarehouseDistrictGlobalStockReconciliation>()
				.AsNoTracking()
				.MaxAsync(x => (DateTime?)x.CreatedAt, cancellationToken);

			if (latestWarehouseTimestamp.HasValue)
			{
				var start = latestWarehouseTimestamp.Value.Date;
				var end = start.AddDays(1);

				var query = _db
					.Set<WarehouseDistrictGlobalStockReconciliation>()
					.AsNoTracking()
					.Where(x =>
						x.CreatedAt >= start &&
						x.CreatedAt < end &&
						x.ClosingStock > 0m);

				query = ApplyWarehouseStockFilters(query, f);
				result.WarehouseRows = await query.ToListAsync(cancellationToken);
			}

			return result;
		}

		private static IQueryable<WholesalerStockAsOnToday> ApplyWholesalerStockFilters(
			IQueryable<WholesalerStockAsOnToday> query,
			AgeingReportFilter f)
		{
			if (f.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					f.StateIds.Contains(x.StateId.Value));
			}

			if (f.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					f.DistrictIds.Contains(x.DistrictId.Value));
			}

			// The wholesaler snapshot does not contain SubDistrictId.
			if (f.SubDistrictIds.Count > 0)
			{
				query = query.Where(_ => false);
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			// Region/HQ selections are resolved to StateIds by the existing Razor
			// cascade, so no entity-model change is required here.
			return query;
		}

		private static IQueryable<DptReport> ApplyDptStockFilters(
			IQueryable<DptReport> query,
			AgeingReportFilter f)
		{
			if (f.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					f.StateIds.Contains(x.StateId.Value));
			}

			if (f.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					f.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (f.SubDistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.SubDistrictId.HasValue &&
					f.SubDistrictIds.Contains(x.SubDistrictId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			return query;
		}

		private static IQueryable<WarehouseDistrictGlobalStockReconciliation>
			ApplyWarehouseStockFilters(
				IQueryable<WarehouseDistrictGlobalStockReconciliation> query,
				AgeingReportFilter f)
		{
			if (f.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					f.StateIds.Contains(x.StateId.Value));
			}

			if (f.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					f.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			// Warehouse rows do not have SubDistrictId or DealershipNatureId.
			if (f.SubDistrictIds.Count > 0 || f.LyingWithIds.Count > 0)
			{
				query = query.Where(_ => false);
			}

			return query;
		}

		// ====================================================================
		// ACK lookup: status Ack + receipt date, with DPT retailer-sale fallback
		// ====================================================================

		private async Task<AcknowledgementLookup> BuildAcknowledgementLookupAsync(
			IEnumerable<WholesalerStockAsOnToday> wholesalerRows,
			IEnumerable<DptReport> dptRows,
			CancellationToken cancellationToken)
		{
			var keys = wholesalerRows
				.Select(x => new DealerProductKey
				{
					DealerRegistrationId = x.DealerRegistrationId,
					IfmsDealerId = x.IfmsDealerId,
					ProductId = x.ProductId
				})
				.Concat(dptRows.Select(x => new DealerProductKey
				{
					DealerRegistrationId = x.DealerRegistrationId,
					IfmsDealerId = x.IfmsDealerId,
					ProductId = x.ProductId
				}))
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
				.Where(x => x.ProductId.HasValue)
				.Select(x => x.ProductId!.Value)
				.Distinct()
				.ToList();

			var result = new AcknowledgementLookup();

			if (productIds.Count == 0 ||
				(registrationIds.Count == 0 && ifmsIds.Count == 0))
			{
				return result;
			}

			var statusRows = await _db.Set<Status>()
				.AsNoTracking()
				.Where(x => x.Name != null)
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

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
						x.ProductId.HasValue &&
						productIds.Contains(x.ProductId.Value));

				companyQuery = ApplyCompanyDealerScope(
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
					.Select(group => new AckAggregate
					{
						DealerRegistrationId = group.Key.DealerRegistrationId,
						IfmsDealerId = group.Key.IfmsDealerId,
						ProductId = group.Key.ProductId,
						AckDate = group.Max(x => x.RetailerReceiptDate)
					})
					.ToListAsync(cancellationToken);

				foreach (var row in companyRows)
				{
					AddAck(result.StrictAckDates, "R", row.DealerRegistrationId, row.ProductId, row.AckDate);
					AddAck(result.StrictAckDates, "I", row.IfmsDealerId, row.ProductId, row.AckDate);
				}

				var wholesalerQuery = _db.Set<SalesWholesaler>()
					.AsNoTracking()
					.Where(x =>
						x.StatusId.HasValue &&
						ackStatusIds.Contains(x.StatusId.Value) &&
						x.RetailerReceiptDate.HasValue &&
						x.ProductId.HasValue &&
						productIds.Contains(x.ProductId.Value));

				wholesalerQuery = ApplyWholesalerDealerScope(
					wholesalerQuery,
					registrationIds,
					ifmsIds);

				var wholesalerSalesRows = await wholesalerQuery
					.GroupBy(x => new
					{
						DealerRegistrationId = x.DealerId,
						x.IfmsDealerId,
						x.ProductId
					})
					.Select(group => new AckAggregate
					{
						DealerRegistrationId = group.Key.DealerRegistrationId,
						IfmsDealerId = group.Key.IfmsDealerId,
						ProductId = group.Key.ProductId,
						AckDate = group.Max(x => x.RetailerReceiptDate)
					})
					.ToListAsync(cancellationToken);

				foreach (var row in wholesalerSalesRows)
				{
					AddAck(result.StrictAckDates, "R", row.DealerRegistrationId, row.ProductId, row.AckDate);
					AddAck(result.StrictAckDates, "I", row.IfmsDealerId, row.ProductId, row.AckDate);
				}
			}

			// DPT has no StatusId or RetailerReceiptDate. SoldQuantity > 0 is the
			// confirmed retailer-sale signal available in the supplied DPT report.
			var dptSalesQuery = _db.Set<DptReport>()
				.AsNoTracking()
				.Where(x =>
					x.SoldQuantity > 0m &&
					x.ProductId.HasValue &&
					productIds.Contains(x.ProductId.Value) &&
					x.CreatedAt < DateTime.UtcNow.Date.AddDays(1));

			dptSalesQuery = ApplyDptDealerScope(
				dptSalesQuery,
				registrationIds,
				ifmsIds);

			var dptSalesRows = await dptSalesQuery
				.GroupBy(x => new
				{
					x.DealerRegistrationId,
					x.IfmsDealerId,
					x.ProductId
				})
				.Select(group => new AckAggregate
				{
					DealerRegistrationId = group.Key.DealerRegistrationId,
					IfmsDealerId = group.Key.IfmsDealerId,
					ProductId = group.Key.ProductId,
					AckDate = group.Max(x => (DateTime?)x.CreatedAt)
				})
				.ToListAsync(cancellationToken);

			foreach (var row in dptSalesRows)
			{
				AddAck(result.DptRetailerSaleDates, "R", row.DealerRegistrationId, row.ProductId, row.AckDate);
				AddAck(result.DptRetailerSaleDates, "I", row.IfmsDealerId, row.ProductId, row.AckDate);
			}

			return result;
		}

		private static IQueryable<SalesCompanySale> ApplyCompanyDealerScope(
			IQueryable<SalesCompanySale> query,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (registrationIds.Count > 0 && ifmsIds.Count > 0)
			{
				return query.Where(x =>
					(x.DealerRegistrationId.HasValue && registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue && ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (registrationIds.Count > 0)
			{
				return query.Where(x =>
					x.DealerRegistrationId.HasValue &&
					registrationIds.Contains(x.DealerRegistrationId.Value));
			}

			if (ifmsIds.Count > 0)
			{
				return query.Where(x =>
					x.IfmsDealerId.HasValue &&
					ifmsIds.Contains(x.IfmsDealerId.Value));
			}

			return query.Where(_ => false);
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerDealerScope(
			IQueryable<SalesWholesaler> query,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (registrationIds.Count > 0 && ifmsIds.Count > 0)
			{
				return query.Where(x =>
					(x.DealerId.HasValue && registrationIds.Contains(x.DealerId.Value)) ||
					(x.IfmsDealerId.HasValue && ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (registrationIds.Count > 0)
			{
				return query.Where(x =>
					x.DealerId.HasValue &&
					registrationIds.Contains(x.DealerId.Value));
			}

			if (ifmsIds.Count > 0)
			{
				return query.Where(x =>
					x.IfmsDealerId.HasValue &&
					ifmsIds.Contains(x.IfmsDealerId.Value));
			}

			return query.Where(_ => false);
		}

		private static IQueryable<DptReport> ApplyDptDealerScope(
			IQueryable<DptReport> query,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (registrationIds.Count > 0 && ifmsIds.Count > 0)
			{
				return query.Where(x =>
					(x.DealerRegistrationId.HasValue && registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue && ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (registrationIds.Count > 0)
			{
				return query.Where(x =>
					x.DealerRegistrationId.HasValue &&
					registrationIds.Contains(x.DealerRegistrationId.Value));
			}

			if (ifmsIds.Count > 0)
			{
				return query.Where(x =>
					x.IfmsDealerId.HasValue &&
					ifmsIds.Contains(x.IfmsDealerId.Value));
			}

			return query.Where(_ => false);
		}

		private static void AddAck(
			IDictionary<string, DateTime> lookup,
			string prefix,
			int? dealerId,
			int? productId,
			DateTime? ackDate)
		{
			if (!dealerId.HasValue || !productId.HasValue || !ackDate.HasValue)
			{
				return;
			}

			var key = BuildAckKey(prefix, dealerId.Value, productId.Value);

			// Current snapshots are not linked to invoice lots. The latest valid ACK
			// for dealer + product is therefore used, matching the established flow.
			if (!lookup.TryGetValue(key, out var current) || ackDate.Value > current)
			{
				lookup[key] = ackDate.Value;
			}
		}

		private static string BuildAckKey(string prefix, int dealerId, int productId) =>
			$"{prefix}{dealerId}|{productId}";

		private static DateTime? FindAckDate(
			IReadOnlyDictionary<string, DateTime> lookup,
			int? dealerRegistrationId,
			int? ifmsDealerId,
			int? productId)
		{
			if (!productId.HasValue)
			{
				return null;
			}

			DateTime? result = null;

			if (dealerRegistrationId.HasValue &&
				lookup.TryGetValue(
					BuildAckKey("R", dealerRegistrationId.Value, productId.Value),
					out var registrationDate))
			{
				result = registrationDate;
			}

			if (ifmsDealerId.HasValue &&
				lookup.TryGetValue(
					BuildAckKey("I", ifmsDealerId.Value, productId.Value),
					out var ifmsDate) &&
				(!result.HasValue || ifmsDate > result.Value))
			{
				result = ifmsDate;
			}

			return result;
		}

		// ====================================================================
		// Build current-stock ageing rows
		// ====================================================================

		private async Task<List<AgeingStockRow>> BuildAgeableStockRowsAsync(
			IEnumerable<WholesalerStockAsOnToday> wholesalerRows,
			IEnumerable<DptReport> dptRows,
			AcknowledgementLookup acknowledgementLookup,
			DateTime today,
			CancellationToken cancellationToken)
		{
			var wholesalerList = wholesalerRows.ToList();
			var dptList = dptRows.ToList();

			var stateIds = wholesalerList
				.Where(x => x.StateId.HasValue)
				.Select(x => x.StateId!.Value)
				.Concat(dptList.Where(x => x.StateId.HasValue).Select(x => x.StateId!.Value))
				.Distinct()
				.ToList();

			var districtIds = wholesalerList
				.Where(x => x.DistrictId.HasValue)
				.Select(x => x.DistrictId!.Value)
				.Concat(dptList.Where(x => x.DistrictId.HasValue).Select(x => x.DistrictId!.Value))
				.Distinct()
				.ToList();

			var subDistrictIds = dptList
				.Where(x => x.SubDistrictId.HasValue)
				.Select(x => x.SubDistrictId!.Value)
				.Distinct()
				.ToList();

			var productIds = wholesalerList
				.Where(x => x.ProductId.HasValue)
				.Select(x => x.ProductId!.Value)
				.Concat(dptList.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value))
				.Distinct()
				.ToList();

			var registrationIds = wholesalerList
				.Where(x => x.DealerRegistrationId.HasValue)
				.Select(x => x.DealerRegistrationId!.Value)
				.Concat(dptList.Where(x => x.DealerRegistrationId.HasValue).Select(x => x.DealerRegistrationId!.Value))
				.Distinct()
				.ToList();

			var ifmsIds = wholesalerList
				.Where(x => x.IfmsDealerId.HasValue)
				.Select(x => x.IfmsDealerId!.Value)
				.Concat(dptList.Where(x => x.IfmsDealerId.HasValue).Select(x => x.IfmsDealerId!.Value))
				.Distinct()
				.ToList();

			var states = stateIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<State>()
					.AsNoTracking()
					.Where(x => stateIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.StateName ?? string.Empty,
						cancellationToken);

			var districts = districtIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<District>()
					.AsNoTracking()
					.Where(x => districtIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.DistrictName ?? string.Empty,
						cancellationToken);

			var subDistricts = subDistrictIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<SubDistrict>()
					.AsNoTracking()
					.Where(x => subDistrictIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.SubDistrictName ?? string.Empty,
						cancellationToken);

			var products = productIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.Set<Product>()
					.AsNoTracking()
					.Where(x => productIds.Contains(x.Id))
					.ToDictionaryAsync(
						x => x.Id,
						x => x.Name ?? string.Empty,
						cancellationToken);

			var registeredDealers = registrationIds.Count == 0
				? new Dictionary<int, RegisteredDealerLookup>()
				: await _db.Set<DealerRegistration>()
					.AsNoTracking()
					.Where(x => registrationIds.Contains(x.Id))
					.Select(x => new RegisteredDealerLookup
					{
						Id = x.Id,
						Name = x.FirmName,
						DealerCode = x.DealerCode,
						MobileNo = x.WhatsAppNumber ??
							x.OfficialContactNumber ??
							x.AlternativeNumber
					})
					.ToDictionaryAsync(x => x.Id, cancellationToken);

			var ifmsDealers = ifmsIds.Count == 0
				? new Dictionary<int, IfmsDealerLookup>()
				: await _db.Set<IfmsDealer>()
					.AsNoTracking()
					.Where(x => ifmsIds.Contains(x.Id))
					.Select(x => new IfmsDealerLookup
					{
						Id = x.Id,
						Name = x.Name,
						MobileNo = x.MobileNo
					})
					.ToDictionaryAsync(x => x.Id, cancellationToken);

			var result = new List<AgeingStockRow>(wholesalerList.Count + dptList.Count);

			foreach (var row in wholesalerList)
			{
				var ackDate = FindAckDate(
					acknowledgementLookup.StrictAckDates,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId);

				// Pending acknowledgement belongs to the Pending ACK report and is not
				// inserted into ageing buckets/list until ACK starts the ageing clock.
				if (!ackDate.HasValue)
				{
					continue;
				}

				registeredDealers.TryGetValue(row.DealerRegistrationId ?? 0, out var registered);
				ifmsDealers.TryGetValue(row.IfmsDealerId ?? 0, out var ifms);

				var ageingDays = Math.Max(0, (today - ackDate.Value.Date).Days);

				result.Add(new AgeingStockRow
				{
					DealerRegistrationId = row.DealerRegistrationId,
					StateId = row.StateId,
					StateName = Lookup(states, row.StateId),
					DistrictName = Lookup(districts, row.DistrictId),
					SubDistrictName = null,
					HeadQuarterName = null,
					DealerName = FirstNonBlank(row.AgencyName, registered?.Name, ifms?.Name),
					DealerCode = registered?.DealerCode,
					MobileNo = FirstNonBlank(registered?.MobileNo, ifms?.MobileNo),
					ProductName = Lookup(products, row.ProductId),
					Quantity = row.Stock,
					AckDate = ackDate.Value,
					AgeingDays = ageingDays,
					Status = MapStatus(ageingDays)
				});
			}

			foreach (var row in dptList)
			{
				var ackDate = FindAckDate(
					acknowledgementLookup.StrictAckDates,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId);

				ackDate ??= FindAckDate(
					acknowledgementLookup.DptRetailerSaleDates,
					row.DealerRegistrationId,
					row.IfmsDealerId,
					row.ProductId);

				if (!ackDate.HasValue)
				{
					continue;
				}

				registeredDealers.TryGetValue(row.DealerRegistrationId ?? 0, out var registered);
				ifmsDealers.TryGetValue(row.IfmsDealerId ?? 0, out var ifms);

				var ageingDays = Math.Max(0, (today - ackDate.Value.Date).Days);

				result.Add(new AgeingStockRow
				{
					DealerRegistrationId = row.DealerRegistrationId,
					StateId = row.StateId,
					StateName = Lookup(states, row.StateId),
					DistrictName = Lookup(districts, row.DistrictId),
					SubDistrictName = Lookup(subDistricts, row.SubDistrictId),
					HeadQuarterName = null,
					DealerName = FirstNonBlank(row.RetailerName, registered?.Name, ifms?.Name),
					DealerCode = registered?.DealerCode,
					MobileNo = FirstNonBlank(row.MobileNo, registered?.MobileNo, ifms?.MobileNo),
					ProductName = Lookup(products, row.ProductId),
					Quantity = row.ClosingBalance,
					AckDate = ackDate.Value,
					AgeingDays = ageingDays,
					Status = MapStatus(ageingDays)
				});
			}

			return result;
		}

		// ====================================================================
		// Sales totals by state: company + wholesaler + DPT SoldQuantity
		// ====================================================================

		private async Task<List<StateAmount>> LoadSalesByStateAsync(
			AgeingReportFilter f,
			DateTime today,
			CancellationToken cancellationToken)
		{
			var combined = new List<StateAmount>();

			var company = _db.Set<SalesCompanySale>()
				.AsNoTracking()
				.Where(x => x.QuantityMT > 0m);
			company = ApplyCompanySalesFilters(company, f, today);

			combined.AddRange(await company
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken));

			var wholesaler = _db.Set<SalesWholesaler>()
				.AsNoTracking()
				.Where(x => x.QuantityMT > 0m);
			wholesaler = ApplyWholesalerSalesFilters(wholesaler, f, today);

			combined.AddRange(await wholesaler
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken));

			var dpt = _db.Set<DptReport>()
				.AsNoTracking()
				.Where(x => x.SoldQuantity > 0m);
			dpt = ApplyDptSalesFilters(dpt, f, today);

			combined.AddRange(await dpt
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.SoldQuantity)
				})
				.ToListAsync(cancellationToken));

			return combined
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.Amount)
				})
				.ToList();
		}

		private static IQueryable<SalesCompanySale> ApplyCompanySalesFilters(
			IQueryable<SalesCompanySale> query,
			AgeingReportFilter f,
			DateTime today)
		{
			if (f.StateIds.Count > 0)
				query = query.Where(x => x.StateId.HasValue && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Count > 0)
				query = query.Where(x => x.DistrictId.HasValue && f.DistrictIds.Contains(x.DistrictId.Value));
			if (f.SubDistrictIds.Count > 0)
				query = query.Where(_ => false);
			if (f.ProductIds.Count > 0)
				query = query.Where(x => x.ProductId.HasValue && f.ProductIds.Contains(x.ProductId.Value));
			if (f.LyingWithIds.Count > 0)
				query = query.Where(x => x.DealershipNatureId.HasValue && f.LyingWithIds.Contains(x.DealershipNatureId.Value));

			return ApplyCompanySaleAgeRange(query, f.AgeingRanges, today);
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerSalesFilters(
			IQueryable<SalesWholesaler> query,
			AgeingReportFilter f,
			DateTime today)
		{
			if (f.StateIds.Count > 0)
				query = query.Where(x => x.StateId.HasValue && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Count > 0)
				query = query.Where(x => x.BuyerDistrictId.HasValue && f.DistrictIds.Contains(x.BuyerDistrictId.Value));
			if (f.SubDistrictIds.Count > 0)
				query = query.Where(_ => false);
			if (f.ProductIds.Count > 0)
				query = query.Where(x => x.ProductId.HasValue && f.ProductIds.Contains(x.ProductId.Value));
			if (f.LyingWithIds.Count > 0)
				query = query.Where(x => x.DealerNatureId.HasValue && f.LyingWithIds.Contains(x.DealerNatureId.Value));

			return ApplyWholesalerSaleAgeRange(query, f.AgeingRanges, today);
		}

		private static IQueryable<DptReport> ApplyDptSalesFilters(
			IQueryable<DptReport> query,
			AgeingReportFilter f,
			DateTime today)
		{
			query = ApplyDptStockFilters(query, f);
			return ApplyDptSaleAgeRange(query, f.AgeingRanges, today);
		}

		private static IQueryable<SalesCompanySale> ApplyCompanySaleAgeRange(
			IQueryable<SalesCompanySale> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
				return query;

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);
			var d180 = today.AddDays(-180);
			var d364 = today.AddDays(-364);
			var d365 = today.AddDays(-365);

			var n030 = ranges.Contains("0-30");
			var n3190 = ranges.Contains("31-90");
			var n91180 = ranges.Contains("91-180");
			var n181364 = ranges.Contains("181-364");
			var n365 = ranges.Contains("365+");
			var o3160 = ranges.Contains("31-60");
			var o6190 = ranges.Contains("61-90");
			var o91120 = ranges.Contains("91-120");
			var oAbove120 = ranges.Contains("Above 120");

			return query.Where(x =>
				(n030 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d30) ||
				(n3190 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d30 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d90) ||
				(n91180 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d90 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d180) ||
				(n181364 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d180 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d364) ||
				(n365 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) <= d365) ||
				(o3160 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d30 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d60) ||
				(o6190 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d60 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d90) ||
				(o91120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d90 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d120) ||
				(oAbove120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d120));
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerSaleAgeRange(
			IQueryable<SalesWholesaler> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
				return query;

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);
			var d180 = today.AddDays(-180);
			var d364 = today.AddDays(-364);
			var d365 = today.AddDays(-365);

			var n030 = ranges.Contains("0-30");
			var n3190 = ranges.Contains("31-90");
			var n91180 = ranges.Contains("91-180");
			var n181364 = ranges.Contains("181-364");
			var n365 = ranges.Contains("365+");
			var o3160 = ranges.Contains("31-60");
			var o6190 = ranges.Contains("61-90");
			var o91120 = ranges.Contains("91-120");
			var oAbove120 = ranges.Contains("Above 120");

			return query.Where(x =>
				(n030 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d30) ||
				(n3190 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d30 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d90) ||
				(n91180 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d90 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d180) ||
				(n181364 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d180 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d364) ||
				(n365 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) <= d365) ||
				(o3160 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d30 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d60) ||
				(o6190 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d60 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d90) ||
				(o91120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d90 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d120) ||
				(oAbove120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d120));
		}

		private static IQueryable<DptReport> ApplyDptSaleAgeRange(
			IQueryable<DptReport> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
				return query;

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);
			var d180 = today.AddDays(-180);
			var d364 = today.AddDays(-364);
			var d365 = today.AddDays(-365);

			return query.Where(x =>
				(ranges.Contains("0-30") && x.CreatedAt >= d30) ||
				(ranges.Contains("31-90") && x.CreatedAt < d30 && x.CreatedAt >= d90) ||
				(ranges.Contains("91-180") && x.CreatedAt < d90 && x.CreatedAt >= d180) ||
				(ranges.Contains("181-364") && x.CreatedAt < d180 && x.CreatedAt >= d364) ||
				(ranges.Contains("365+") && x.CreatedAt <= d365) ||
				(ranges.Contains("31-60") && x.CreatedAt < d30 && x.CreatedAt >= d60) ||
				(ranges.Contains("61-90") && x.CreatedAt < d60 && x.CreatedAt >= d90) ||
				(ranges.Contains("91-120") && x.CreatedAt < d90 && x.CreatedAt >= d120) ||
				(ranges.Contains("Above 120") && x.CreatedAt < d120));
		}

		// ====================================================================
		// Aggregates, filtering, sorting and mapping
		// ====================================================================

		private static List<StateAmount> BuildCurrentStockByState(CurrentSnapshotBundle snapshots)
		{
			var rows = snapshots.WholesalerRows
				.Select(x => new StateAmount { StateId = x.StateId, Amount = x.Stock })
				.Concat(snapshots.DptRows.Select(x => new StateAmount
				{
					StateId = x.StateId,
					Amount = x.ClosingBalance
				}))
				.Concat(snapshots.WarehouseRows.Select(x => new StateAmount
				{
					StateId = x.StateId,
					Amount = x.ClosingStock
				}));

			return rows
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.Amount)
				})
				.ToList();
		}

		private async Task<Dictionary<int, string>> LoadStateNamesAsync(
			IEnumerable<StateAmount> stockRows,
			IEnumerable<StateAmount> salesRows,
			CancellationToken cancellationToken)
		{
			var ids = stockRows
				.Concat(salesRows)
				.Where(x => x.StateId.HasValue)
				.Select(x => x.StateId!.Value)
				.Distinct()
				.ToList();

			if (ids.Count == 0)
				return new Dictionary<int, string>();

			return await _db.Set<State>()
				.AsNoTracking()
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(
					x => x.Id,
					x => x.StateName ?? string.Empty,
					cancellationToken);
		}

		private static List<AgeingStateDto> BuildStockVsSalesStateWise(
			IEnumerable<StateAmount> stockRows,
			IEnumerable<StateAmount> salesRows,
			IReadOnlyDictionary<int, string> stateNames)
		{
			var stock = stockRows
				.Where(x => x.StateId.HasValue)
				.ToDictionary(x => x.StateId!.Value, x => x.Amount);

			var sales = salesRows
				.Where(x => x.StateId.HasValue)
				.ToDictionary(x => x.StateId!.Value, x => x.Amount);

			return stock.Keys
				.Union(sales.Keys)
				.Select(stateId => new AgeingStateDto
				{
					StateName = stateNames.TryGetValue(stateId, out var name)
						? name
						: string.Empty,
					Stock = stock.TryGetValue(stateId, out var stockValue)
						? stockValue
						: 0m,
					Sales = sales.TryGetValue(stateId, out var salesValue)
						? salesValue
						: 0m
				})
				.Where(x => !string.IsNullOrWhiteSpace(x.StateName))
				.OrderByDescending(x => x.Stock)
				.ThenByDescending(x => x.Sales)
				.ThenBy(x => x.StateName)
				.ToList();
		}

		private static List<AgeingStockRow> ApplyAgeingRowFilters(
			IEnumerable<AgeingStockRow> rows,
			AgeingReportFilter f)
		{
			var query = rows;

			if (f.AgeingRanges.Count > 0)
			{
				query = query.Where(x => MatchesAgeingRange(x.AgeingDays, f.AgeingRanges));
			}

			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var search = f.Search.Trim();
				query = query.Where(x =>
					ContainsIgnoreCase(x.StateName, search) ||
					ContainsIgnoreCase(x.DistrictName, search) ||
					ContainsIgnoreCase(x.SubDistrictName, search) ||
					ContainsIgnoreCase(x.DealerName, search) ||
					ContainsIgnoreCase(x.DealerCode, search) ||
					ContainsIgnoreCase(x.ProductName, search) ||
					ContainsIgnoreCase(x.MobileNo, search) ||
					ContainsIgnoreCase(x.Status, search));
			}

			return query.ToList();
		}

		private static List<AgeingStockRow> ApplySorting(
			IEnumerable<AgeingStockRow> rows,
			AgeingReportFilter f)
		{
			var column = f.SortColumn?.Trim().ToLowerInvariant() ?? "ageing";
			var descending = string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

			IOrderedEnumerable<AgeingStockRow> ordered = (column, descending) switch
			{
				("state", true) => rows.OrderByDescending(x => x.StateName),
				("state", false) => rows.OrderBy(x => x.StateName),
				("dealer", true) => rows.OrderByDescending(x => x.DealerName),
				("dealer", false) => rows.OrderBy(x => x.DealerName),
				("product", true) => rows.OrderByDescending(x => x.ProductName),
				("product", false) => rows.OrderBy(x => x.ProductName),
				("quantity", true) => rows.OrderByDescending(x => x.Quantity),
				("quantity", false) => rows.OrderBy(x => x.Quantity),
				("ageing", false) => rows.OrderBy(x => x.AgeingDays),
				_ => rows.OrderByDescending(x => x.AgeingDays)
			};

			return ordered
				.ThenBy(x => x.StateName)
				.ThenBy(x => x.DealerName)
				.ThenBy(x => x.ProductName)
				.ToList();
		}

		private static List<AgeingStateBucketDto> BuildStateBuckets(
			IEnumerable<AgeingStockRow> rows)
		{
			return rows
				.GroupBy(x => x.StateName ?? string.Empty)
				.Where(group => !string.IsNullOrWhiteSpace(group.Key))
				.Select(group => new AgeingStateBucketDto
				{
					StateName = group.Key,
					Fresh = group.Where(x => x.AgeingDays <= FreshMax).Sum(x => x.Quantity),
					Medium = group.Where(x => x.AgeingDays >= 31 && x.AgeingDays <= MediumMax).Sum(x => x.Quantity),
					SlowMoving = group.Where(x => x.AgeingDays >= 91 && x.AgeingDays <= SlowMovingMax).Sum(x => x.Quantity),
					LongAged = group.Where(x => x.AgeingDays >= 181 && x.AgeingDays < CriticalMin).Sum(x => x.Quantity),
					Critical = group.Where(x => x.AgeingDays >= CriticalMin).Sum(x => x.Quantity)
				})
				.OrderByDescending(x => x.Total)
				.ThenBy(x => x.StateName)
				.ToList();
		}

		private static List<AgeingBucketDto> BuildDayBuckets(
			IEnumerable<AgeingStockRow> rows)
		{
			var list = rows.ToList();

			var fresh = list.Where(x => x.AgeingDays <= FreshMax).Sum(x => x.Quantity);
			var medium = list.Where(x => x.AgeingDays >= 31 && x.AgeingDays <= MediumMax).Sum(x => x.Quantity);
			var slow = list.Where(x => x.AgeingDays >= 91 && x.AgeingDays <= SlowMovingMax).Sum(x => x.Quantity);
			var longAged = list.Where(x => x.AgeingDays >= 181 && x.AgeingDays < CriticalMin).Sum(x => x.Quantity);
			var critical = list.Where(x => x.AgeingDays >= CriticalMin).Sum(x => x.Quantity);
			var total = fresh + medium + slow + longAged + critical;

			static double Pct(decimal value, decimal totalValue) =>
				totalValue == 0m
					? 0d
					: Math.Round((double)(value / totalValue) * 100d, 1);

			return new List<AgeingBucketDto>
			{
				new()
				{
					Label = "Fresh (0-30)",
					Category = "Fresh",
					Stock = fresh,
					Percentage = Pct(fresh, total),
					Color = "#059669"
				},
				new()
				{
					Label = "Medium (31-90)",
					Category = "Medium",
					Stock = medium,
					Percentage = Pct(medium, total),
					Color = "#34d399"
				},
				new()
				{
					Label = "Slow Moving (91-180)",
					Category = "Slow Moving",
					Stock = slow,
					Percentage = Pct(slow, total),
					Color = "#f59e0b"
				},
				new()
				{
					Label = "Long Aged (181-364)",
					Category = "Long Aged",
					Stock = longAged,
					Percentage = Pct(longAged, total),
					Color = "#ef4444"
				},
				new()
				{
					Label = "Critical (365+)",
					Category = "Critical",
					Stock = critical,
					Percentage = Pct(critical, total),
					Color = "#b91c1c"
				}
			};
		}

		private static bool MatchesAgeingRange(
			int days,
			IReadOnlyCollection<string> ranges)
		{
			return
				(ranges.Contains("0-30") && days <= 30) ||
				(ranges.Contains("31-90") && days >= 31 && days <= 90) ||
				(ranges.Contains("91-180") && days >= 91 && days <= 180) ||
				(ranges.Contains("181-364") && days >= 181 && days <= 364) ||
				(ranges.Contains("365+") && days >= 365) ||

				// Backward-compatible aliases used by the previous Razor page.
				(ranges.Contains("31-60") && days >= 31 && days <= 60) ||
				(ranges.Contains("61-90") && days >= 61 && days <= 90) ||
				(ranges.Contains("91-120") && days >= 91 && days <= 120) ||
				(ranges.Contains("Above 120") && days > 120);
		}

		private static AgeingRowDto ToDto(AgeingStockRow row)
		{
			return new AgeingRowDto
			{
				DealerRegistrationId = row.DealerRegistrationId,
				StateName = row.StateName ?? string.Empty,
				DealerName = row.DealerName ?? string.Empty,
				ProductName = row.ProductName ?? string.Empty,
				Quantity = row.Quantity,
				AgeingDays = row.AgeingDays,
				Status = row.Status,
				MobileNo = Blank(row.MobileNo),
				DealerCode = Blank(row.DealerCode),
				HeadQuarterName = Blank(row.HeadQuarterName),
				DistrictName = Blank(row.DistrictName),
				SubDistrictName = Blank(row.SubDistrictName),
				EntryDate = row.AckDate
			};
		}

		private static string MapStatus(int days)
		{
			return days switch
			{
				<= FreshMax => "Fresh",
				<= MediumMax => "Medium",
				<= SlowMovingMax => "Slow Moving",
				< CriticalMin => "Long Aged",
				_ => "Critical"
			};
		}

		private static AgeingReportFilter NormalizeFilter(AgeingReportFilter? filter)
		{
			var f = filter ?? new AgeingReportFilter();

			f.StateIds ??= new List<int>();
			f.RegionIds ??= new List<int>();
			f.HeadQuarterIds ??= new List<int>();
			f.DistrictIds ??= new List<int>();
			f.SubDistrictIds ??= new List<int>();
			f.LyingWithIds ??= new List<int>();
			f.ProductIds ??= new List<int>();
			f.AgeingRanges ??= new List<string>();

			f.StateIds = f.StateIds.Distinct().ToList();
			f.RegionIds = f.RegionIds.Distinct().ToList();
			f.HeadQuarterIds = f.HeadQuarterIds.Distinct().ToList();
			f.DistrictIds = f.DistrictIds.Distinct().ToList();
			f.SubDistrictIds = f.SubDistrictIds.Distinct().ToList();
			f.LyingWithIds = f.LyingWithIds.Distinct().ToList();
			f.ProductIds = f.ProductIds.Distinct().ToList();
			f.AgeingRanges = f.AgeingRanges
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			f.Page = Math.Max(1, f.Page);
			f.PageSize = f.PageSize switch
			{
				<= 0 => 16,
				> 500 => 500,
				_ => f.PageSize
			};

			f.SortColumn = string.IsNullOrWhiteSpace(f.SortColumn)
				? "ageing"
				: f.SortColumn.Trim();

			f.SortDir = string.Equals(f.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
				? "asc"
				: "desc";

			return f;
		}

		private static bool ContainsIgnoreCase(string? value, string search) =>
			!string.IsNullOrWhiteSpace(value) &&
			value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

		private static string Lookup(
			IReadOnlyDictionary<int, string> lookup,
			int? id)
		{
			return id.HasValue && lookup.TryGetValue(id.Value, out var value)
				? value ?? string.Empty
				: string.Empty;
		}

		private static string FirstNonBlank(params string?[] values)
		{
			return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim()
				?? string.Empty;
		}

		private static string? Blank(string? value) =>
			string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		// ====================================================================
		// Internal shapes
		// ====================================================================

		private sealed class CurrentSnapshotBundle
		{
			public List<WholesalerStockAsOnToday> WholesalerRows { get; set; } = new();
			public List<DptReport> DptRows { get; set; } = new();
			public List<WarehouseDistrictGlobalStockReconciliation> WarehouseRows { get; set; } = new();
		}

		private sealed class AcknowledgementLookup
		{
			public Dictionary<string, DateTime> StrictAckDates { get; } = new();
			public Dictionary<string, DateTime> DptRetailerSaleDates { get; } = new();
		}

		private sealed class DealerProductKey
		{
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public int? ProductId { get; set; }
		}

		private sealed class AckAggregate
		{
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public int? ProductId { get; set; }
			public DateTime? AckDate { get; set; }
		}

		private sealed class RegisteredDealerLookup
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public string? DealerCode { get; set; }
			public string? MobileNo { get; set; }
		}

		private sealed class IfmsDealerLookup
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public string? MobileNo { get; set; }
		}

		private sealed class AgeingStockRow
		{
			public int? DealerRegistrationId { get; set; }
			public int? StateId { get; set; }
			public string? StateName { get; set; }
			public string? DistrictName { get; set; }
			public string? SubDistrictName { get; set; }
			public string? HeadQuarterName { get; set; }
			public string? DealerName { get; set; }
			public string? DealerCode { get; set; }
			public string? MobileNo { get; set; }
			public string? ProductName { get; set; }
			public decimal Quantity { get; set; }
			public DateTime AckDate { get; set; }
			public int AgeingDays { get; set; }
			public string Status { get; set; } = string.Empty;
		}

		private sealed class StateAmount
		{
			public int? StateId { get; set; }
			public decimal Amount { get; set; }
		}
	}
}