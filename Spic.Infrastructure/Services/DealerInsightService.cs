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
	/// Resolves a dealer's iFMS ID / PoS Liquidation / Pending Ack from its own Excel-upload
	/// transaction history (DptReport/SalesCompanySale/SalesWholesaler/WholesalerStockAsOnToday/
	/// SalesAndReceipt). A transaction row counts as "this dealer's" if EITHER:
	///   1. row.DealerRegistrationId == dealer.Id (the normal, direct link), or
	///   2. row's raw external id (IfmsId / IfmsDealerSalesId - the exact number typed into the
	///      Excel upload) equals one of the dealer's own DealerRegistration code fields
	///      (DealerCode/SPICCode/GreenStarCode/TnCode/NCode/WholesalemFMSCode/RetailmFMSCode).
	/// Path 2 exists because every historical upload in this database resolved to the separate
	/// IfmsDealer table instead of DealerRegistration (confirmed by direct query - zero rows
	/// anywhere have DealerRegistrationId set), so path 1 alone currently finds nothing. Path 2
	/// is still a verified, exact numeric match against fields ExcelBulkUploadService.ResolveDealer
	/// itself already treats as authoritative for this exact purpose - never a name or location
	/// guess. A code shared by more than one requested dealer is dropped from matching entirely
	/// rather than guessed at (see ResolveVerifiedCodesAsync). Today, this still resolves nothing
	/// for any dealer, because no DealerRegistration code value happens to equal any upload's raw
	/// external id (also confirmed by direct query) - the fields the two systems use were simply
	/// never populated with the same number for any dealer. The moment a DealerRegistration row's
	/// WholesalemFMSCode/RetailmFMSCode is populated with a dealer's real IFMS number matching a
	/// future upload's "dealerid" column, this picks it up with no further code change.
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

		private sealed class ResolvedRow
		{
			public int DealerId { get; set; }
			public int? RawExternalId { get; set; }
			public decimal Qty { get; set; }
			public DateTime Date { get; set; }
			public int? StatusId { get; set; }
		}

		public async Task<Dictionary<int, DealerInsightDto>> GetInsightsAsync(
			IEnumerable<int> dealerRegistrationIds, CancellationToken cancellationToken = default)
		{
			var ids = dealerRegistrationIds.Where(id => id > 0).Distinct().ToList();
			var result = ids.ToDictionary(id => id, _ => new DealerInsightDto());
			if (ids.Count == 0)
				return result;

			var codeToDealerId = await ResolveVerifiedCodesAsync(ids, cancellationToken);
			var codes = codeToDealerId.Keys.ToList();

			var dptRows = await FetchDptReportRowsAsync(ids, codes, codeToDealerId, cancellationToken);
			var companyRows = await FetchSalesCompanySaleRowsAsync(ids, codes, codeToDealerId, cancellationToken);
			var receiptRows = await FetchSalesAndReceiptRowsAsync(ids, codes, codeToDealerId, cancellationToken);
			var stockRows = await FetchWholesalerStockRowsAsync(ids, codes, codeToDealerId, cancellationToken);
			var wholesalerRows = await FetchSalesWholesalerRowsAsync(ids, codes, codeToDealerId, cancellationToken);

			// iFMS ID: latest raw external id across every table, for every dealer resolved either way.
			var ifmsCandidates = new List<(int DealerId, int ExternalId, DateTime Date)>();
			foreach (var r in dptRows.Concat(companyRows).Concat(receiptRows).Concat(stockRows).Concat(wholesalerRows))
			{
				if (r.RawExternalId.HasValue)
					ifmsCandidates.Add((r.DealerId, r.RawExternalId.Value, r.Date));
			}
			foreach (var g in ifmsCandidates.GroupBy(x => x.DealerId))
				result[g.Key].IfmsId = g.OrderByDescending(x => x.Date).First().ExternalId;

			// PoS Liquidation: latest sold/liquidated quantity - mirrors LiquidationCycleService's
			// own documented production mapping (Retailer: DptReport.SoldQuantity; Wholesaler:
			// SalesWholesaler.QuantityMT), picking whichever is more recent per dealer.
			var liqCandidates = dptRows.Concat(wholesalerRows).ToList();
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

			var ackCandidates = new List<ResolvedRow>();
			ackCandidates.AddRange(companyRows.Where(x => !x.StatusId.HasValue || !ackStatusIds.Contains(x.StatusId.Value)));
			ackCandidates.AddRange(wholesalerRows.Where(x => !x.StatusId.HasValue || !ackStatusIds.Contains(x.StatusId.Value)));
			ackCandidates.AddRange(dptRows.Where(x => x.Qty > 0));

			foreach (var g in ackCandidates.GroupBy(x => x.DealerId))
				result[g.Key].PendingAckQty = g.OrderByDescending(x => x.Date).First().Qty;

			return result;
		}

		// DealerRegistration's own external-id fields are exactly what ExcelBulkUploadService.
		// ResolveDealer already checks first, before ever falling back to IfmsDealer - so matching
		// against them is a verified, exact-value comparison, not a guess. A code value shared by
		// more than one DealerRegistration row is dropped entirely: there is no safe way to tell
		// which dealer an upload row using that code actually belongs to. Ambiguity is checked
		// GLOBALLY (every DealerRegistration row, not just the requested batch) - two dealers
		// outside the current batch could still share a code with one inside it, and that would
		// be just as unsafe to match on as a collision entirely within the batch.
		private async Task<Dictionary<int, int>> ResolveVerifiedCodesAsync(List<int> dealerIds, CancellationToken cancellationToken)
		{
			var rows = await _db.DealerRegistrations.AsNoTracking()
				.Select(d => new
				{
					d.Id,
					d.DealerCode,
					d.SPICCode,
					d.GreenStarCode,
					d.TnCode,
					d.NCode,
					d.WholesalemFMSCode,
					d.RetailmFMSCode
				})
				.ToListAsync(cancellationToken);

			var codeToDealerIds = new Dictionary<int, HashSet<int>>();
			void Add(string? raw, int dealerId)
			{
				if (string.IsNullOrWhiteSpace(raw)) return;
				if (!int.TryParse(raw.Trim(), out var code) || code <= 0) return;
				if (!codeToDealerIds.TryGetValue(code, out var set))
					codeToDealerIds[code] = set = new HashSet<int>();
				set.Add(dealerId);
			}

			foreach (var r in rows)
			{
				Add(r.DealerCode, r.Id);
				Add(r.SPICCode, r.Id);
				Add(r.GreenStarCode, r.Id);
				Add(r.TnCode, r.Id);
				Add(r.NCode, r.Id);
				Add(r.WholesalemFMSCode, r.Id);
				Add(r.RetailmFMSCode, r.Id);
			}

			var requested = new HashSet<int>(dealerIds);
			return codeToDealerIds
				.Where(kv => kv.Value.Count == 1)
				.Select(kv => (Code: kv.Key, DealerId: kv.Value.Single()))
				.Where(x => requested.Contains(x.DealerId))
				.ToDictionary(x => x.Code, x => x.DealerId);
		}

		private async Task<List<ResolvedRow>> FetchDptReportRowsAsync(
			List<int> dealerIds, List<int> codes, Dictionary<int, int> codeToDealerId, CancellationToken cancellationToken)
		{
			var rows = await _db.DptReports.AsNoTracking()
				.Where(x =>
					(x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsId.HasValue && codes.Contains(x.IfmsId.Value)))
				.Select(x => new { x.DealerRegistrationId, x.IfmsId, x.SoldQuantity, x.CreatedAt })
				.ToListAsync(cancellationToken);

			return rows
				.Select(x => new { x, DealerId = ResolveId(codeToDealerId, x.DealerRegistrationId, x.IfmsId) })
				.Where(x => x.DealerId.HasValue)
				.Select(x => new ResolvedRow { DealerId = x.DealerId!.Value, RawExternalId = x.x.IfmsId, Qty = x.x.SoldQuantity, Date = x.x.CreatedAt })
				.ToList();
		}

		private async Task<List<ResolvedRow>> FetchSalesCompanySaleRowsAsync(
			List<int> dealerIds, List<int> codes, Dictionary<int, int> codeToDealerId, CancellationToken cancellationToken)
		{
			var rows = await _db.SalesCompanySales.AsNoTracking()
				.Where(x =>
					(x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsId.HasValue && codes.Contains(x.IfmsId.Value)))
				.Select(x => new { x.DealerRegistrationId, x.IfmsId, x.QuantityMT, x.InvoiceDate, x.CreatedAt, x.StatusId })
				.ToListAsync(cancellationToken);

			return rows
				.Select(x => new { x, DealerId = ResolveId(codeToDealerId, x.DealerRegistrationId, x.IfmsId) })
				.Where(x => x.DealerId.HasValue && x.x.InvoiceDate.HasValue)
				.Select(x => new ResolvedRow { DealerId = x.DealerId!.Value, RawExternalId = x.x.IfmsId, Qty = x.x.QuantityMT, Date = x.x.InvoiceDate!.Value, StatusId = x.x.StatusId })
				.ToList();
		}

		private async Task<List<ResolvedRow>> FetchSalesAndReceiptRowsAsync(
			List<int> dealerIds, List<int> codes, Dictionary<int, int> codeToDealerId, CancellationToken cancellationToken)
		{
			var rows = await _db.SalesAndReceipts.AsNoTracking()
				.Where(x =>
					(x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsId.HasValue && codes.Contains(x.IfmsId.Value)))
				.Select(x => new { x.DealerRegistrationId, x.IfmsId, x.CreatedAt })
				.ToListAsync(cancellationToken);

			return rows
				.Select(x => new { x, DealerId = ResolveId(codeToDealerId, x.DealerRegistrationId, x.IfmsId) })
				.Where(x => x.DealerId.HasValue)
				.Select(x => new ResolvedRow { DealerId = x.DealerId!.Value, RawExternalId = x.x.IfmsId, Qty = 0, Date = x.x.CreatedAt })
				.ToList();
		}

		private async Task<List<ResolvedRow>> FetchWholesalerStockRowsAsync(
			List<int> dealerIds, List<int> codes, Dictionary<int, int> codeToDealerId, CancellationToken cancellationToken)
		{
			var rows = await _db.WholesalerStockAsOnTodays.AsNoTracking()
				.Where(x =>
					(x.DealerRegistrationId.HasValue && dealerIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsId.HasValue && codes.Contains(x.IfmsId.Value)))
				.Select(x => new { x.DealerRegistrationId, x.IfmsId, x.StockDate })
				.ToListAsync(cancellationToken);

			return rows
				.Select(x => new { x, DealerId = ResolveId(codeToDealerId, x.DealerRegistrationId, x.IfmsId) })
				.Where(x => x.DealerId.HasValue)
				.Select(x => new ResolvedRow { DealerId = x.DealerId!.Value, RawExternalId = x.x.IfmsId, Qty = 0, Date = x.x.StockDate })
				.ToList();
		}

		// Buyer/dealer side only (DealerId/IfmsDealerSalesId) - matches the scope the original
		// DealerRegistrationId-based lookup already used; the wholesaler side of this table
		// (WholesalerId/IfmsWholesalerSalesId) was never part of this service's matching and stays
		// out of scope here too.
		private async Task<List<ResolvedRow>> FetchSalesWholesalerRowsAsync(
			List<int> dealerIds, List<int> codes, Dictionary<int, int> codeToDealerId, CancellationToken cancellationToken)
		{
			var rows = await _db.SalesWholesalers.AsNoTracking()
				.Where(x =>
					(x.DealerId.HasValue && dealerIds.Contains(x.DealerId.Value)) ||
					(x.IfmsDealerSalesId.HasValue && codes.Contains(x.IfmsDealerSalesId.Value)))
				.Select(x => new { x.DealerId, x.IfmsDealerSalesId, x.QuantityMT, x.InvoiceDate, x.CreatedAt, x.StatusId })
				.ToListAsync(cancellationToken);

			return rows
				.Select(x => new { x, DealerId = ResolveId(codeToDealerId, x.DealerId, x.IfmsDealerSalesId) })
				.Where(x => x.DealerId.HasValue && x.x.InvoiceDate.HasValue)
				.Select(x => new ResolvedRow { DealerId = x.DealerId!.Value, RawExternalId = x.x.IfmsDealerSalesId, Qty = x.x.QuantityMT, Date = x.x.InvoiceDate!.Value, StatusId = x.x.StatusId })
				.ToList();
		}

		// Shared resolution rule: a direct DealerRegistrationId match wins (the row was already
		// correctly linked); otherwise fall back to the verified-code match for this row's raw
		// external id, via the codeToDealerId map built once per request in ResolveVerifiedCodesAsync.
		// Runs after ToListAsync (plain C#, not translated to SQL) - the SQL-side filtering above
		// only needed the flat dealerIds/codes lists, this is where they actually resolve to a dealer.
		private static int? ResolveId(Dictionary<int, int> codeToDealerId, int? directId, int? rawExternalId) =>
			directId ?? (rawExternalId.HasValue && codeToDealerId.TryGetValue(rawExternalId.Value, out var d) ? d : (int?)null);
	}
}
