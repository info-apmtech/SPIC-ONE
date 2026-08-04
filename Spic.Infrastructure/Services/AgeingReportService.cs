// ============================================================================
//  AgeingReportService — Spic.Infrastructure/Services/
//
//  Production-safe stock and sales sources:
//    CURRENT STOCK
//      * WholesalerStockAsOnToday.Stock
//      * DptReport.ClosingBalance
//      * WarehouseDistrictGlobalStockReconciliation.ClosingStock
//
//    SALES
//      * SalesCompanySale.QuantityMT
//      * SalesWholesaler.QuantityMT
//      * DptReport.SoldQuantity
//
//  Important snapshot rule:
//    Stock is taken only from the latest available report date of each stock
//    source. Historical stock snapshots are not added together.
//
//  Existing ageing flow is preserved:
//    * Company and wholesaler ageing rows require RetailerReceiptDate.
//    * Ageing is calculated from RetailerReceiptDate.
//    * DPT SoldQuantity is included in sales totals, but not in acknowledgement
//      ageing buckets/grid because DptReport has no RetailerReceiptDate.
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

		public AgeingReportService(AppDbContext db)
		{
			_db = db;
		}

		private static DateTime Today() => DateTime.UtcNow.Date;

		public async Task<AgeingDashboardDto> GetDashboardAsync(
			AgeingReportFilter filter,
			CancellationToken cancellationToken = default)
		{
			var f = NormalizeFilter(filter);
			var today = Today();

			// Existing ageing calculation: acknowledged company + wholesaler rows.
			var ageingRows = BuildResolvedAgeingQuery(f, today);

			// The same scoped DbContext must not execute concurrent operations.
			var currentStockByState = await LoadCurrentStockByStateAsync(
				f,
				cancellationToken);

			var salesByState = await LoadSalesByStateAsync(
				f,
				today,
				cancellationToken);

			var stateNames = await LoadStateNamesAsync(
				currentStockByState,
				salesByState,
				cancellationToken);

			var global = await LoadGlobalAggregateAsync(
				ageingRows,
				today,
				cancellationToken);

			var ageingStateAggregates = await LoadAgeingStateAggregatesAsync(
				ageingRows,
				today,
				cancellationToken);

			var averageAgeing = await LoadAverageAgeingAsync(
				ageingRows,
				today,
				cancellationToken);

			var grid = await LoadGridAsync(
				ageingRows,
				f,
				today,
				cancellationToken);

			var totalStock = currentStockByState.Sum(x => x.Amount);

			return new AgeingDashboardDto
			{
				Summary = new AgeingSummaryDto
				{
					TotalStock = totalStock,
					TotalStockChangePct = 0m,
					AverageAgeing = averageAgeing,
					AverageAgeingChange = 0d,
					Stock30To60 = global.Stock30To60,
					Stock30To60ChangePct = 0m,
					Stock60Plus = global.Stock60Plus,
					Stock60PlusChangePct = 0m
				},
				StateWise = BuildStockVsSalesStateWise(
					currentStockByState,
					salesByState,
					stateNames),
				StateBuckets = ageingStateAggregates
					.Where(x => !string.IsNullOrWhiteSpace(x.StateName))
					.Select(x => new AgeingStateBucketDto
					{
						StateName = x.StateName!,
						Fresh = x.Fresh,
						Medium = x.Medium,
						SlowMoving = x.SlowMoving,
						LongAged = x.LongAged,
						Critical = x.Critical
					})
					.OrderByDescending(x => x.Total)
					.ThenBy(x => x.StateName)
					.ToList(),
				DayBuckets = BuildDayBuckets(global),
				Grid = grid
			};
		}

		public async Task<List<AgeingRowDto>> GetAllRowsAsync(
			AgeingReportFilter filter,
			CancellationToken cancellationToken = default)
		{
			var f = NormalizeFilter(filter);
			var today = Today();
			var query = BuildResolvedAgeingQuery(f, today);

			var rawRows = await query
				.OrderBy(x => x.AckDate)
				.ThenBy(x => x.DealerName)
				.Select(x => new GridRaw
				{
					DealerRegistrationId = x.DealerRegistrationId,
					StateName = x.StateName,
					DistrictName = x.DistrictName,
					DealerName = x.DealerName,
					DealerCode = x.DealerCode,
					MobileNo = x.MobileNo,
					ProductName = x.ProductName,
					Quantity = x.Quantity,
					AckDate = x.AckDate
				})
				.ToListAsync(cancellationToken);

			return rawRows.Select(x => ToRow(x, today)).ToList();
		}

		// ====================================================================
		//  Existing ageing rows — acknowledged company and wholesaler sales
		// ====================================================================

		private IQueryable<ResolvedAgeingRow> BuildResolvedAgeingQuery(
			AgeingReportFilter f,
			DateTime today)
		{
			var company = ApplyCompanyAgeingFilters(
					_db.Set<SalesCompanySale>().AsNoTracking(),
					f,
					today)
				.Select(x => new SalesAgeingRaw
				{
					DealerRegistrationId = x.DealerRegistrationId,
					IfmsDealerId = x.IfmsDealerId,
					DealerName = x.DealerName,
					StateId = x.StateId,
					DistrictId = x.DistrictId,
					ProductId = x.ProductId,
					MobileNo = x.MobileNo,
					Quantity = x.ReceivedQuantity > 0m
						? x.ReceivedQuantity
						: x.QuantityMT,
					AckDate = x.RetailerReceiptDate!.Value
				});

			var wholesaler = ApplyWholesalerAgeingFilters(
					_db.Set<SalesWholesaler>().AsNoTracking(),
					f,
					today)
				.Select(x => new SalesAgeingRaw
				{
					DealerRegistrationId = x.DealerId,
					IfmsDealerId = x.IfmsDealerId,
					DealerName = x.AgencyName,
					StateId = x.StateId,
					DistrictId = x.BuyerDistrictId,
					ProductId = x.ProductId,
					MobileNo = x.MobileNo,
					Quantity = x.ReceivedQuantityMT > 0m
						? x.ReceivedQuantityMT
						: x.QuantityMT,
					AckDate = x.RetailerReceiptDate!.Value
				});

			var sales = company.Concat(wholesaler);

			var states = _db.Set<State>().AsNoTracking();
			var districts = _db.Set<District>().AsNoTracking();
			var products = _db.Set<Product>().AsNoTracking();
			var registeredDealers = _db.Set<DealerRegistration>().AsNoTracking();
			var ifmsDealers = _db.Set<IfmsDealer>().AsNoTracking();

			var resolved =
				from sale in sales
				join stateValue in states
					on sale.StateId equals (int?)stateValue.Id into stateJoin
				from state in stateJoin.DefaultIfEmpty()
				join districtValue in districts
					on sale.DistrictId equals (int?)districtValue.Id into districtJoin
				from district in districtJoin.DefaultIfEmpty()
				join productValue in products
					on sale.ProductId equals (int?)productValue.Id into productJoin
				from product in productJoin.DefaultIfEmpty()
				join registrationValue in registeredDealers
					on sale.DealerRegistrationId equals (int?)registrationValue.Id into registrationJoin
				from registration in registrationJoin.DefaultIfEmpty()
				join ifmsValue in ifmsDealers
					on sale.IfmsDealerId equals (int?)ifmsValue.Id into ifmsJoin
				from ifms in ifmsJoin.DefaultIfEmpty()
				select new ResolvedAgeingRow
				{
					DealerRegistrationId = sale.DealerRegistrationId,
					DealerName = sale.DealerName ??
						(registration != null ? registration.FirmName : null) ??
						(ifms != null ? ifms.Name : null) ??
						string.Empty,
					DealerCode = registration != null
						? registration.DealerCode
						: null,
					StateId = sale.StateId,
					StateName = state != null
						? state.StateName
						: string.Empty,
					DistrictName = district != null
						? district.DistrictName
						: null,
					ProductName = product != null
						? product.Name
						: string.Empty,
					MobileNo = sale.MobileNo ??
						(ifms != null ? ifms.MobileNo : null),
					Quantity = sale.Quantity,
					AckDate = sale.AckDate
				};

			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var pattern = $"%{f.Search.Trim()}%";

				resolved = resolved.Where(x =>
					EF.Functions.ILike(x.DealerName, pattern) ||
					EF.Functions.ILike(x.StateName, pattern) ||
					EF.Functions.ILike(x.ProductName, pattern));
			}

			return resolved;
		}

		private static IQueryable<SalesCompanySale> ApplyCompanyAgeingFilters(
			IQueryable<SalesCompanySale> query,
			AgeingReportFilter f,
			DateTime today)
		{
			query = query.Where(x => x.RetailerReceiptDate != null);

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
				// SalesCompanySale has no SubDistrictId.
				query = query.Where(_ => false);
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

			return ApplyCompanyAcknowledgementAgeRanges(
				query,
				f.AgeingRanges,
				today);
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerAgeingFilters(
			IQueryable<SalesWholesaler> query,
			AgeingReportFilter f,
			DateTime today)
		{
			query = query.Where(x => x.RetailerReceiptDate != null);

			if (f.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					f.StateIds.Contains(x.StateId.Value));
			}

			if (f.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.BuyerDistrictId.HasValue &&
					f.DistrictIds.Contains(x.BuyerDistrictId.Value));
			}

			if (f.SubDistrictIds.Count > 0)
			{
				// SalesWholesaler has no SubDistrictId.
				query = query.Where(_ => false);
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealerNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealerNatureId.Value));
			}

			return ApplyWholesalerAcknowledgementAgeRanges(
				query,
				f.AgeingRanges,
				today);
		}

		private static IQueryable<SalesCompanySale> ApplyCompanyAcknowledgementAgeRanges(
			IQueryable<SalesCompanySale> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
			{
				return query;
			}

			var r030 = ranges.Contains("0-30");
			var r3160 = ranges.Contains("31-60");
			var r6190 = ranges.Contains("61-90");
			var r91120 = ranges.Contains("91-120");
			var rAbove120 = ranges.Contains("Above 120");

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);

			return query.Where(x =>
				(r030 && x.RetailerReceiptDate >= d30) ||
				(r3160 && x.RetailerReceiptDate < d30 && x.RetailerReceiptDate >= d60) ||
				(r6190 && x.RetailerReceiptDate < d60 && x.RetailerReceiptDate >= d90) ||
				(r91120 && x.RetailerReceiptDate < d90 && x.RetailerReceiptDate >= d120) ||
				(rAbove120 && x.RetailerReceiptDate < d120));
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerAcknowledgementAgeRanges(
			IQueryable<SalesWholesaler> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
			{
				return query;
			}

			var r030 = ranges.Contains("0-30");
			var r3160 = ranges.Contains("31-60");
			var r6190 = ranges.Contains("61-90");
			var r91120 = ranges.Contains("91-120");
			var rAbove120 = ranges.Contains("Above 120");

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);

			return query.Where(x =>
				(r030 && x.RetailerReceiptDate >= d30) ||
				(r3160 && x.RetailerReceiptDate < d30 && x.RetailerReceiptDate >= d60) ||
				(r6190 && x.RetailerReceiptDate < d60 && x.RetailerReceiptDate >= d90) ||
				(r91120 && x.RetailerReceiptDate < d90 && x.RetailerReceiptDate >= d120) ||
				(rAbove120 && x.RetailerReceiptDate < d120));
		}

		// ====================================================================
		//  Current stock — latest snapshot only from all three stock sources
		// ====================================================================

		private async Task<List<StateAmount>> LoadCurrentStockByStateAsync(
			AgeingReportFilter f,
			CancellationToken cancellationToken)
		{
			var combined = new List<StateAmount>();

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

				combined.AddRange(await query
					.GroupBy(x => x.StateId)
					.Select(group => new StateAmount
					{
						StateId = group.Key,
						Amount = group.Sum(x => x.Stock)
					})
					.ToListAsync(cancellationToken));
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

				combined.AddRange(await query
					.GroupBy(x => x.StateId)
					.Select(group => new StateAmount
					{
						StateId = group.Key,
						Amount = group.Sum(x => x.ClosingBalance)
					})
					.ToListAsync(cancellationToken));
			}

			var latestWarehouseTimestamp = await _db
				.Set<WarehouseDistrictGlobalStockReconciliation>()
				.AsNoTracking()
				.MaxAsync(x => (DateTime?)x.CreatedAt, cancellationToken);

			if (latestWarehouseTimestamp.HasValue)
			{
				var start = latestWarehouseTimestamp.Value.Date;
				var end = start.AddDays(1);

				var query = _db.Set<WarehouseDistrictGlobalStockReconciliation>()
					.AsNoTracking()
					.Where(x =>
						x.CreatedAt >= start &&
						x.CreatedAt < end &&
						x.ClosingStock > 0m);

				query = ApplyWarehouseStockFilters(query, f);

				combined.AddRange(await query
					.GroupBy(x => x.StateId)
					.Select(group => new StateAmount
					{
						StateId = group.Key,
						Amount = group.Sum(x => x.ClosingStock)
					})
					.ToListAsync(cancellationToken));
			}

			return combined
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.Amount)
				})
				.ToList();
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

			if (f.SubDistrictIds.Count > 0)
			{
				query = query.Where(_ => false);
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

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

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

			return query;
		}

		private static IQueryable<WarehouseDistrictGlobalStockReconciliation> ApplyWarehouseStockFilters(
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

			if (f.SubDistrictIds.Count > 0 || f.LyingWithIds.Count > 0)
			{
				// Warehouse rows do not have SubDistrictId or DealershipNatureId.
				query = query.Where(_ => false);
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			return query;
		}

		// ====================================================================
		//  Sales totals by state — company + wholesaler + DPT SoldQuantity
		// ====================================================================

		private async Task<List<StateAmount>> LoadSalesByStateAsync(
			AgeingReportFilter f,
			DateTime today,
			CancellationToken cancellationToken)
		{
			var combined = new List<StateAmount>();

			var company = ApplyCompanySalesTotalFilters(
				_db.Set<SalesCompanySale>()
					.AsNoTracking()
					.Where(x => x.QuantityMT > 0m),
				f,
				today);

			combined.AddRange(await company
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken));

			var wholesaler = ApplyWholesalerSalesTotalFilters(
				_db.Set<SalesWholesaler>()
					.AsNoTracking()
					.Where(x => x.QuantityMT > 0m),
				f,
				today);

			combined.AddRange(await wholesaler
				.GroupBy(x => x.StateId)
				.Select(group => new StateAmount
				{
					StateId = group.Key,
					Amount = group.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken));

			var dpt = ApplyDptSalesTotalFilters(
				_db.Set<DptReport>()
					.AsNoTracking()
					.Where(x => x.SoldQuantity > 0m),
				f,
				today);

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

		private static IQueryable<SalesCompanySale> ApplyCompanySalesTotalFilters(
			IQueryable<SalesCompanySale> query,
			AgeingReportFilter f,
			DateTime today)
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
				query = query.Where(_ => false);
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));
			}

			return ApplyCompanySaleDateRanges(query, f.AgeingRanges, today);
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerSalesTotalFilters(
			IQueryable<SalesWholesaler> query,
			AgeingReportFilter f,
			DateTime today)
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
					x.BuyerDistrictId.HasValue &&
					f.DistrictIds.Contains(x.BuyerDistrictId.Value));
			}

			if (f.SubDistrictIds.Count > 0)
			{
				query = query.Where(_ => false);
			}

			if (f.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealerNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealerNatureId.Value));
			}

			return ApplyWholesalerSaleDateRanges(query, f.AgeingRanges, today);
		}

		private static IQueryable<DptReport> ApplyDptSalesTotalFilters(
			IQueryable<DptReport> query,
			AgeingReportFilter f,
			DateTime today)
		{
			query = ApplyDptStockFilters(query, f);
			return ApplyDptSaleDateRanges(query, f.AgeingRanges, today);
		}

		private static IQueryable<SalesCompanySale> ApplyCompanySaleDateRanges(
			IQueryable<SalesCompanySale> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
			{
				return query;
			}

			var r030 = ranges.Contains("0-30");
			var r3160 = ranges.Contains("31-60");
			var r6190 = ranges.Contains("61-90");
			var r91120 = ranges.Contains("91-120");
			var rAbove120 = ranges.Contains("Above 120");

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);

			return query.Where(x =>
				(r030 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d30) ||
				(r3160 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d30 &&
					(x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d60) ||
				(r6190 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d60 &&
					(x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d90) ||
				(r91120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d90 &&
					(x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d120) ||
				(rAbove120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d120));
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerSaleDateRanges(
			IQueryable<SalesWholesaler> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
			{
				return query;
			}

			var r030 = ranges.Contains("0-30");
			var r3160 = ranges.Contains("31-60");
			var r6190 = ranges.Contains("61-90");
			var r91120 = ranges.Contains("91-120");
			var rAbove120 = ranges.Contains("Above 120");

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);

			return query.Where(x =>
				(r030 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d30) ||
				(r3160 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d30 &&
					(x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d60) ||
				(r6190 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d60 &&
					(x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d90) ||
				(r91120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d90 &&
					(x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) >= d120) ||
				(rAbove120 && (x.RetailerReceiptDate ?? x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt) < d120));
		}

		private static IQueryable<DptReport> ApplyDptSaleDateRanges(
			IQueryable<DptReport> query,
			IReadOnlyCollection<string> ranges,
			DateTime today)
		{
			if (ranges.Count == 0)
			{
				return query;
			}

			var r030 = ranges.Contains("0-30");
			var r3160 = ranges.Contains("31-60");
			var r6190 = ranges.Contains("61-90");
			var r91120 = ranges.Contains("91-120");
			var rAbove120 = ranges.Contains("Above 120");

			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d120 = today.AddDays(-120);

			return query.Where(x =>
				(r030 && x.CreatedAt >= d30) ||
				(r3160 && x.CreatedAt < d30 && x.CreatedAt >= d60) ||
				(r6190 && x.CreatedAt < d60 && x.CreatedAt >= d90) ||
				(r91120 && x.CreatedAt < d90 && x.CreatedAt >= d120) ||
				(rAbove120 && x.CreatedAt < d120));
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
			{
				return new Dictionary<int, string>();
			}

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
					Stock = stock.TryGetValue(stateId, out var stockAmount)
						? stockAmount
						: 0m,
					Sales = sales.TryGetValue(stateId, out var salesAmount)
						? salesAmount
						: 0m
				})
				.Where(x => !string.IsNullOrWhiteSpace(x.StateName))
				.OrderByDescending(x => x.Stock)
				.ThenByDescending(x => x.Sales)
				.ThenBy(x => x.StateName)
				.ToList();
		}

		// ====================================================================
		//  Ageing aggregates, buckets and grid
		// ====================================================================

		private static async Task<GlobalAggregate> LoadGlobalAggregateAsync(
			IQueryable<ResolvedAgeingRow> rows,
			DateTime today,
			CancellationToken cancellationToken)
		{
			var d30 = today.AddDays(-30);
			var d60 = today.AddDays(-60);
			var d90 = today.AddDays(-90);
			var d180 = today.AddDays(-180);
			var d365 = today.AddDays(-365);

			return await rows
				.GroupBy(_ => 1)
				.Select(group => new GlobalAggregate
				{
					TotalQuantity = group.Sum(x => x.Quantity),
					Stock30To60 = group.Sum(x =>
						x.AckDate < d30 && x.AckDate >= d60
							? x.Quantity
							: 0m),
					Stock60Plus = group.Sum(x =>
						x.AckDate < d60
							? x.Quantity
							: 0m),
					Fresh = group.Sum(x =>
						x.AckDate >= d30
							? x.Quantity
							: 0m),
					Medium = group.Sum(x =>
						x.AckDate < d30 && x.AckDate >= d90
							? x.Quantity
							: 0m),
					SlowMoving = group.Sum(x =>
						x.AckDate < d90 && x.AckDate >= d180
							? x.Quantity
							: 0m),
					LongAged = group.Sum(x =>
						x.AckDate < d180 && x.AckDate >= d365
							? x.Quantity
							: 0m),
					Critical = group.Sum(x =>
						x.AckDate < d365
							? x.Quantity
							: 0m)
				})
				.FirstOrDefaultAsync(cancellationToken)
				?? new GlobalAggregate();
		}

		private static async Task<List<StateAggregate>> LoadAgeingStateAggregatesAsync(
			IQueryable<ResolvedAgeingRow> rows,
			DateTime today,
			CancellationToken cancellationToken)
		{
			var d30 = today.AddDays(-30);
			var d90 = today.AddDays(-90);
			var d180 = today.AddDays(-180);
			var d365 = today.AddDays(-365);

			return await rows
				.GroupBy(x => new
				{
					x.StateId,
					x.StateName
				})
				.Select(group => new StateAggregate
				{
					StateId = group.Key.StateId,
					StateName = group.Key.StateName,
					Total = group.Sum(x => x.Quantity),
					Fresh = group.Sum(x =>
						x.AckDate >= d30
							? x.Quantity
							: 0m),
					Medium = group.Sum(x =>
						x.AckDate < d30 && x.AckDate >= d90
							? x.Quantity
							: 0m),
					SlowMoving = group.Sum(x =>
						x.AckDate < d90 && x.AckDate >= d180
							? x.Quantity
							: 0m),
					LongAged = group.Sum(x =>
						x.AckDate < d180 && x.AckDate >= d365
							? x.Quantity
							: 0m),
					Critical = group.Sum(x =>
						x.AckDate < d365
							? x.Quantity
							: 0m)
				})
				.ToListAsync(cancellationToken);
		}

		private static async Task<double> LoadAverageAgeingAsync(
			IQueryable<ResolvedAgeingRow> rows,
			DateTime today,
			CancellationToken cancellationToken)
		{
			var histogram = await rows
				.GroupBy(x => x.AckDate.Date)
				.Select(group => new AgeDateCount
				{
					Date = group.Key,
					Count = group.Count()
				})
				.ToListAsync(cancellationToken);

			long count = 0;
			double weightedDays = 0d;

			foreach (var item in histogram)
			{
				var days = Math.Max(0, (today - item.Date.Date).Days);
				count += item.Count;
				weightedDays += (double)days * item.Count;
			}

			return count == 0
				? 0d
				: Math.Round(weightedDays / count, 1);
		}

		private static List<AgeingBucketDto> BuildDayBuckets(GlobalAggregate aggregate)
		{
			var total = aggregate.Fresh +
				aggregate.Medium +
				aggregate.SlowMoving +
				aggregate.LongAged +
				aggregate.Critical;

			static double Percentage(decimal value, decimal totalValue)
			{
				return totalValue == 0m
					? 0d
					: Math.Round((double)(value / totalValue) * 100d, 1);
			}

			return new List<AgeingBucketDto>
			{
				new()
				{
					Label = "Fresh (0-30)",
					Category = "Fresh",
					Stock = aggregate.Fresh,
					Percentage = Percentage(aggregate.Fresh, total),
					Color = "#059669"
				},
				new()
				{
					Label = "Medium (30-90)",
					Category = "Medium",
					Stock = aggregate.Medium,
					Percentage = Percentage(aggregate.Medium, total),
					Color = "#34d399"
				},
				new()
				{
					Label = "Slow Moving (90-180)",
					Category = "Slow Moving",
					Stock = aggregate.SlowMoving,
					Percentage = Percentage(aggregate.SlowMoving, total),
					Color = "#f59e0b"
				},
				new()
				{
					Label = "Long Aged (180-365)",
					Category = "Long Aged",
					Stock = aggregate.LongAged,
					Percentage = Percentage(aggregate.LongAged, total),
					Color = "#ef4444"
				},
				new()
				{
					Label = "Critical (365+)",
					Category = "Critical",
					Stock = aggregate.Critical,
					Percentage = Percentage(aggregate.Critical, total),
					Color = "#b91c1c"
				}
			};
		}

		private static async Task<PagedResult<AgeingRowDto>> LoadGridAsync(
			IQueryable<ResolvedAgeingRow> rows,
			AgeingReportFilter f,
			DateTime today,
			CancellationToken cancellationToken)
		{
			var totalCount = await rows.CountAsync(cancellationToken);
			var sorted = ApplySorting(rows, f);
			var skip = (f.Page - 1) * f.PageSize;

			var pageRows = await sorted
				.Skip(skip)
				.Take(f.PageSize)
				.Select(x => new GridRaw
				{
					DealerRegistrationId = x.DealerRegistrationId,
					StateName = x.StateName,
					DistrictName = x.DistrictName,
					DealerName = x.DealerName,
					DealerCode = x.DealerCode,
					MobileNo = x.MobileNo,
					ProductName = x.ProductName,
					Quantity = x.Quantity,
					AckDate = x.AckDate
				})
				.ToListAsync(cancellationToken);

			return new PagedResult<AgeingRowDto>
			{
				Items = pageRows.Select(x => ToRow(x, today)).ToList(),
				TotalCount = totalCount,
				Page = f.Page,
				PageSize = f.PageSize
			};
		}

		private static IOrderedQueryable<ResolvedAgeingRow> ApplySorting(
			IQueryable<ResolvedAgeingRow> rows,
			AgeingReportFilter f)
		{
			var column = f.SortColumn?.Trim().ToLowerInvariant();
			var descending = string.Equals(
				f.SortDir,
				"desc",
				StringComparison.OrdinalIgnoreCase);

			return column switch
			{
				"dealer" when descending => rows
					.OrderByDescending(x => x.DealerName)
					.ThenBy(x => x.AckDate),
				"dealer" => rows
					.OrderBy(x => x.DealerName)
					.ThenBy(x => x.AckDate),
				"product" when descending => rows
					.OrderByDescending(x => x.ProductName)
					.ThenBy(x => x.DealerName),
				"product" => rows
					.OrderBy(x => x.ProductName)
					.ThenBy(x => x.DealerName),
				"quantity" when descending => rows
					.OrderByDescending(x => x.Quantity)
					.ThenBy(x => x.DealerName),
				"quantity" => rows
					.OrderBy(x => x.Quantity)
					.ThenBy(x => x.DealerName),
				"state" when descending => rows
					.OrderByDescending(x => x.StateName)
					.ThenBy(x => x.DealerName),
				"state" => rows
					.OrderBy(x => x.StateName)
					.ThenBy(x => x.DealerName),
				"ageing" when !descending => rows
					.OrderByDescending(x => x.AckDate)
					.ThenBy(x => x.DealerName),
				_ => rows
					.OrderBy(x => x.AckDate)
					.ThenBy(x => x.DealerName)
			};
		}

		private static AgeingRowDto ToRow(GridRaw raw, DateTime today)
		{
			var ageingDays = Math.Max(0, (today - raw.AckDate.Date).Days);

			return new AgeingRowDto
			{
				DealerRegistrationId = raw.DealerRegistrationId,
				StateName = raw.StateName ?? string.Empty,
				DistrictName = raw.DistrictName,
				HeadQuarterName = null,
				SubDistrictName = null,
				DealerName = raw.DealerName ?? string.Empty,
				DealerCode = raw.DealerCode,
				MobileNo = string.IsNullOrWhiteSpace(raw.MobileNo)
					? null
					: raw.MobileNo.Trim(),
				ProductName = raw.ProductName ?? string.Empty,
				Quantity = raw.Quantity,
				EntryDate = raw.AckDate,
				AgeingDays = ageingDays,
				Status = MapStatus(ageingDays)
			};
		}

		private static string MapStatus(int days)
		{
			return days switch
			{
				<= 30 => "Fresh",
				<= 90 => "Medium",
				<= 180 => "Slow Moving",
				<= 365 => "Long Aged",
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

			f.SortDir = string.Equals(
				f.SortDir,
				"asc",
				StringComparison.OrdinalIgnoreCase)
				? "asc"
				: "desc";

			return f;
		}

		// ====================================================================
		//  Internal query/aggregate shapes
		// ====================================================================

		private sealed class SalesAgeingRaw
		{
			public int? DealerRegistrationId { get; set; }
			public int? IfmsDealerId { get; set; }
			public string? DealerName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public string? MobileNo { get; set; }
			public decimal Quantity { get; set; }
			public DateTime AckDate { get; set; }
		}

		private sealed class ResolvedAgeingRow
		{
			public int? DealerRegistrationId { get; set; }
			public string DealerName { get; set; } = string.Empty;
			public string? DealerCode { get; set; }
			public int? StateId { get; set; }
			public string StateName { get; set; } = string.Empty;
			public string? DistrictName { get; set; }
			public string ProductName { get; set; } = string.Empty;
			public string? MobileNo { get; set; }
			public decimal Quantity { get; set; }
			public DateTime AckDate { get; set; }
		}

		private sealed class StateAmount
		{
			public int? StateId { get; set; }
			public decimal Amount { get; set; }
		}

		private sealed class GlobalAggregate
		{
			public decimal TotalQuantity { get; set; }
			public decimal Stock30To60 { get; set; }
			public decimal Stock60Plus { get; set; }
			public decimal Fresh { get; set; }
			public decimal Medium { get; set; }
			public decimal SlowMoving { get; set; }
			public decimal LongAged { get; set; }
			public decimal Critical { get; set; }
		}

		private sealed class StateAggregate
		{
			public int? StateId { get; set; }
			public string? StateName { get; set; }
			public decimal Total { get; set; }
			public decimal Fresh { get; set; }
			public decimal Medium { get; set; }
			public decimal SlowMoving { get; set; }
			public decimal LongAged { get; set; }
			public decimal Critical { get; set; }
		}

		private sealed class AgeDateCount
		{
			public DateTime Date { get; set; }
			public int Count { get; set; }
		}

		private sealed class GridRaw
		{
			public int? DealerRegistrationId { get; set; }
			public string? StateName { get; set; }
			public string? DistrictName { get; set; }
			public string? DealerName { get; set; }
			public string? DealerCode { get; set; }
			public string? MobileNo { get; set; }
			public string? ProductName { get; set; }
			public decimal Quantity { get; set; }
			public DateTime AckDate { get; set; }
		}
	}
}