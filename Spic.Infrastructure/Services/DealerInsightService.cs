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
	/// Resolves a dealer's iFMS ID / PoS Liquidation / Pending Ack straight from its own
	/// Excel-upload transaction history (DptReport/SalesCompanySale/SalesWholesaler/
	/// WholesalerStockAsOnToday/SalesAndReceipt), matched strictly by DealerRegistrationId - never
	/// by name, which could attach the wrong dealer's figures to a card.
	///
	/// This exists as its own small service, separate from LiquidationCycleService/PendingAckService,
	/// because those two are built for a different shape of caller: a handful of dealers on one
	/// allocation is fine to resolve one-by-one through their full dashboard pipeline, but the
	/// Tasks Allocation dealer PICKER can show up to ~1000 dealers at once, and their row DTOs
	/// carry no dealer-id field of their own (only name/code/type) - a multi-dealer call into
	/// either service would make it impossible to tell which returned row belongs to which dealer.
	/// Querying the same underlying tables directly, once per table for the whole requested id
	/// list, replicates their documented semantics (latest liquidation entry; "Ack"/"Acknowledged"
	/// -named Status = complete, everything else still pending) without needing a per-dealer call.
	/// </summary>
	public class DealerInsightService : IDealerInsightService
	{
		private readonly AppDbContext _db;

		public DealerInsightService(AppDbContext db)
		{
			_db = db;
		}

		public async Task<Dictionary<int, DealerInsightDto>> GetInsightsAsync(
			IEnumerable<int> dealerRegistrationIds, CancellationToken cancellationToken = default)
		{
			var ids = dealerRegistrationIds.Where(id => id > 0).Distinct().ToList();
			var result = ids.ToDictionary(id => id, _ => new DealerInsightDto());
			if (ids.Count == 0)
				return result;

			var ifmsIds = await GetLatestIfmsIdsAsync(ids, cancellationToken);
			foreach (var (dealerId, ifmsId) in ifmsIds)
				result[dealerId].IfmsId = ifmsId;

			// PoS Liquidation: latest sold/liquidated quantity - mirrors LiquidationCycleService's
			// own documented production mapping (Retailer: DptReport.SoldQuantity; Wholesaler:
			// SalesWholesaler.QuantityMT), picking whichever is more recent per dealer.
			var liqCandidates = new List<(int DealerId, decimal Qty, DateTime Date)>();

			var dptSales = await _db.DptReports.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue && ids.Contains(x.DealerRegistrationId.Value))
				.Select(x => new { DealerId = x.DealerRegistrationId!.Value, x.SoldQuantity, Date = x.CreatedAt })
				.ToListAsync(cancellationToken);
			liqCandidates.AddRange(dptSales.Select(x => (x.DealerId, x.SoldQuantity, x.Date)));

			var wholesalerSales = await _db.SalesWholesalers.AsNoTracking()
				.Where(x => x.DealerId.HasValue && ids.Contains(x.DealerId.Value) && x.InvoiceDate.HasValue)
				.Select(x => new { DealerId = x.DealerId!.Value, x.QuantityMT, Date = x.InvoiceDate!.Value })
				.ToListAsync(cancellationToken);
			liqCandidates.AddRange(wholesalerSales.Select(x => (x.DealerId, x.QuantityMT, x.Date)));

			foreach (var g in liqCandidates.GroupBy(x => x.DealerId))
			{
				var latest = g.OrderByDescending(x => x.Date).First();
				result[g.Key].PosLiquidationQty = latest.Qty;
				result[g.Key].PosLiquidationDate = latest.Date;
			}

			// Pending Ack: same 3 sources PendingAckService unions (Company Sales, Wholesaler
			// Sales, Retailer/DPT Sales). A Company/Wholesaler row counts as pending unless its
			// StatusId points at an "Ack"/"Acknowledged"-named Status - the exact same convention
			// PendingAckService.LoadWorkflowStatusRulesAsync uses. DptReport has no StatusId at
			// all, so every row with a sold quantity counts as pending, same as that service.
			var ackStatusIds = await _db.Set<Status>().AsNoTracking()
				.Where(x => x.Name != null &&
					(x.Name.ToLower() == "ack" || x.Name.ToLower() == "acknowledged"))
				.Select(x => x.Id)
				.ToListAsync(cancellationToken);

			var ackCandidates = new List<(int DealerId, decimal Qty, DateTime Date)>();

			var companyPending = await _db.SalesCompanySales.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue && ids.Contains(x.DealerRegistrationId.Value)
					&& x.InvoiceDate.HasValue
					&& (!x.StatusId.HasValue || !ackStatusIds.Contains(x.StatusId.Value)))
				.Select(x => new { DealerId = x.DealerRegistrationId!.Value, x.QuantityMT, Date = x.InvoiceDate!.Value })
				.ToListAsync(cancellationToken);
			ackCandidates.AddRange(companyPending.Select(x => (x.DealerId, x.QuantityMT, x.Date)));

			var wholesalerPending = await _db.SalesWholesalers.AsNoTracking()
				.Where(x => x.DealerId.HasValue && ids.Contains(x.DealerId.Value)
					&& x.InvoiceDate.HasValue
					&& (!x.StatusId.HasValue || !ackStatusIds.Contains(x.StatusId.Value)))
				.Select(x => new { DealerId = x.DealerId!.Value, x.QuantityMT, Date = x.InvoiceDate!.Value })
				.ToListAsync(cancellationToken);
			ackCandidates.AddRange(wholesalerPending.Select(x => (x.DealerId, x.QuantityMT, x.Date)));

			var dptPending = await _db.DptReports.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue && ids.Contains(x.DealerRegistrationId.Value) && x.SoldQuantity > 0)
				.Select(x => new { DealerId = x.DealerRegistrationId!.Value, x.SoldQuantity, Date = x.CreatedAt })
				.ToListAsync(cancellationToken);
			ackCandidates.AddRange(dptPending.Select(x => (x.DealerId, x.SoldQuantity, x.Date)));

			foreach (var g in ackCandidates.GroupBy(x => x.DealerId))
			{
				var latest = g.OrderByDescending(x => x.Date).First();
				result[g.Key].PendingAckQty = latest.Qty;
			}

			return result;
		}

		// Raw dealer id exactly as typed in the uploaded Excel row (IfmsId/IfmsDealerSalesId),
		// distinct from IfmsDealerId (the matched IfmsDealer master's own surrogate key) - see
		// ExcelBulkUploadService's ParseOptionalExternalId. Picks the single most recent row across
		// every upload table that carries a DealerRegistrationId match for this dealer.
		private async Task<Dictionary<int, int>> GetLatestIfmsIdsAsync(List<int> dealerIds, CancellationToken cancellationToken)
		{
			var candidates = new List<(int DealerId, int? IfmsId, DateTime Date)>();

			var dptRows = await _db.DptReports.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value))
				.Select(x => new { DealerId = x.DealerRegistrationId!.Value, x.IfmsId, Date = x.CreatedAt })
				.ToListAsync(cancellationToken);
			candidates.AddRange(dptRows.Select(x => (x.DealerId, x.IfmsId, x.Date)));

			var companySaleRows = await _db.SalesCompanySales.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value))
				.Select(x => new { DealerId = x.DealerRegistrationId!.Value, x.IfmsId, Date = x.InvoiceDate ?? x.CreatedAt })
				.ToListAsync(cancellationToken);
			candidates.AddRange(companySaleRows.Select(x => (x.DealerId, x.IfmsId, x.Date)));

			var salesReceiptRows = await _db.SalesAndReceipts.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value))
				.Select(x => new { DealerId = x.DealerRegistrationId!.Value, x.IfmsId, Date = x.CreatedAt })
				.ToListAsync(cancellationToken);
			candidates.AddRange(salesReceiptRows.Select(x => (x.DealerId, x.IfmsId, x.Date)));

			var wholesalerStockRows = await _db.WholesalerStockAsOnTodays.AsNoTracking()
				.Where(x => x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value))
				.Select(x => new { DealerId = x.DealerRegistrationId!.Value, x.IfmsId, Date = x.StockDate })
				.ToListAsync(cancellationToken);
			candidates.AddRange(wholesalerStockRows.Select(x => (x.DealerId, x.IfmsId, x.Date)));

			var wholesalerSaleRows = await _db.SalesWholesalers.AsNoTracking()
				.Where(x => x.DealerId.HasValue && dealerIds.Contains(x.DealerId.Value))
				.Select(x => new { DealerId = x.DealerId!.Value, IfmsId = x.IfmsDealerSalesId, Date = x.InvoiceDate ?? x.CreatedAt })
				.ToListAsync(cancellationToken);
			candidates.AddRange(wholesalerSaleRows.Select(x => (x.DealerId, x.IfmsId, x.Date)));

			return candidates
				.Where(x => x.IfmsId.HasValue)
				.GroupBy(x => x.DealerId)
				.ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Date).First().IfmsId!.Value);
		}
	}
}
