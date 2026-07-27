// ============================================================================
//  PendingAckService (implementation)
//  Location: Spic.Infrastructure/Services/   (next to StockReportService.cs)
//
//  Same shape as StockReportService, but this report unifies TWO tables
//  (SalesCompanySale + SalesWholesaler). ID/date filters run in SQL; the rows
//  are then unified in memory, age-classified, and turned into the dashboard.
//
//  BEFORE IT COMPILES, confirm:
//   1. AppDbContext            -> already matches your StockReportService.
//   2. Lookup entity + name properties in LoadLookupsAsync (see the 5 TODOs).
//      State.StateName and Product.Name are taken from your StockReportService;
//      District / DealerType / Status are assumed and marked TODO.
//   3. The "acknowledged" rule in Finish().
//
//  PostgreSQL / Npgsql: date filters are normalised to Kind=Utc (Npgsql maps
//  DateTime -> timestamptz). If InvoiceDate is `timestamp WITHOUT time zone`,
//  switch Utc(...) to DateTimeKind.Unspecified — exactly like the StockReport note.
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
	public class PendingAckService : IPendingAckService
	{
		private readonly AppDbContext _db;

		public PendingAckService(AppDbContext db) => _db = db;

		// ---- Age-status thresholds (must match the view) ----
		//   Overdue  : age >= 20
		//   Critical : age 10..19
		//   Pending  : age 5..9
		//   Fresh    : age < 5
		private const int OverdueDays = 20;
		private const int CriticalDays = 10;
		private const int PendingDays = 5;

		private static DateTime Today() => DateTime.UtcNow.Date;
		private static DateTime? Utc(DateTime? d) =>
			d.HasValue ? DateTime.SpecifyKind(d.Value.Date, DateTimeKind.Utc) : (DateTime?)null;

		// ==========================================================
		//  Public API
		// ==========================================================
		public async Task<PendingAckDashboardDto> GetDashboardAsync(PendingAckFilter f)
		{
			// Base rows respect every filter EXCEPT the Source tab and Age-status,
			// so the KPI cards + chart stay stable while the grid tab switches.
			var baseRows = await BuildRowsAsync(f);

			return new PendingAckDashboardDto
			{
				Summary   = BuildSummary(baseRows),
				StateWise = BuildStateWise(baseRows),
				Grid      = BuildGrid(baseRows, f, paged: true)
			};
		}

		public async Task<List<PendingAckRowDto>> GetAllRowsAsync(PendingAckFilter f)
		{
			var baseRows = await BuildRowsAsync(f);
			return BuildGrid(baseRows, f, paged: false).Items;
		}

		// ---- Dealer Type dropdown (from DealerTypes) ----
		public async Task<List<PendingAckDealerTypeDto>> GetDealerTypesAsync()
		{
			// TODO: confirm DealerType entity + name property (assumed .Name).
			var raw = await _db.Set<DealerType>().AsNoTracking()
				.Select(x => new { x.Id, x.Name })
				.ToListAsync();

			return raw
				.Select(x => new PendingAckDealerTypeDto { Id = x.Id, Name = x.Name ?? "" })
				.Where(x => !string.IsNullOrWhiteSpace(x.Name))
				.OrderBy(x => x.Name)
				.ToList();
		}

		// ---- Dealer / Agency dropdown (DealerRegistrations + IfmsDealers) ----
		public async Task<List<PendingAckDealerDto>> GetDealersAsync()
		{
			// TODO: confirm the display-name property on each entity.
			//   DealerRegistration -> assumed FirmName
			//   IfmsDealer         -> assumed DealerName
			var regsRaw = await _db.Set<DealerRegistration>().AsNoTracking()
				.Select(x => new { x.Id, Name = x.FirmName })
				.ToListAsync();

			var ifmsRaw = await _db.Set<IfmsDealer>().AsNoTracking()
				.Select(x => new { x.Id, Name = x.Name })
				.ToListAsync();

			var dealers = new List<PendingAckDealerDto>();
			dealers.AddRange(regsRaw.Select(x => new PendingAckDealerDto { Id = "R" + x.Id, Name = x.Name ?? "" }));
			dealers.AddRange(ifmsRaw.Select(x => new PendingAckDealerDto { Id = "I" + x.Id, Name = x.Name ?? "" }));

			return dealers
				.Where(x => !string.IsNullOrWhiteSpace(x.Name))
				.OrderBy(x => x.Name)
				.ToList();
		}

		// "R123"/"I456" -> the integer ids for one source table.
		private static List<int> ParseKeys(List<string> keys, char prefix) =>
			keys.Where(k => !string.IsNullOrEmpty(k) && k.Length > 1 && k[0] == prefix)
				.Select(k => int.TryParse(k.Substring(1), out var n) ? n : 0)
				.Where(n => n > 0)
				.ToList();

		// ==========================================================
		//  Load both tables, resolve names, classify age
		// ==========================================================
		private async Task<List<UnifiedRow>> BuildRowsAsync(PendingAckFilter f)
		{
			var (states, districts, dealerTypes, products, statuses) = await LoadLookupsAsync();

			var from = Utc(f.DateFrom);
			var to = Utc(f.DateTo);
			var result = new List<UnifiedRow>();

			// Dealer / agency keys split back into their two source tables.
			//   "R{id}" -> DealerRegistrations.Id,  "I{id}" -> IfmsDealers.Id
			var regIds = ParseKeys(f.DealerKeys, 'R');
			var ifmsIds = ParseKeys(f.DealerKeys, 'I');
			var hasDealerFilter = regIds.Count > 0 || ifmsIds.Count > 0;

			// ---------- Company Sales ----------
			var cq = _db.Set<SalesCompanySale>().AsNoTracking();
			if (f.StateIds.Count > 0) cq = cq.Where(x => x.StateId != null && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Count > 0) cq = cq.Where(x => x.DistrictId != null && f.DistrictIds.Contains(x.DistrictId.Value));
			if (f.DealerTypeIds.Count > 0) cq = cq.Where(x => x.DealerTypeId != null && f.DealerTypeIds.Contains(x.DealerTypeId.Value));
			if (f.ProductIds.Count > 0) cq = cq.Where(x => x.ProductId != null && f.ProductIds.Contains(x.ProductId.Value));
			if (hasDealerFilter) cq = cq.Where(x =>
											  (x.DealerRegistrationId != null && regIds.Contains(x.DealerRegistrationId.Value)) ||
											  (x.IfmsDealerId != null && ifmsIds.Contains(x.IfmsDealerId.Value)));
			if (from.HasValue) cq = cq.Where(x => x.InvoiceDate >= from.Value);
			if (to.HasValue) cq = cq.Where(x => x.InvoiceDate <= to.Value);

			var company = await cq.Select(x => new
			{
				x.Id,
				x.TransactionId,
				x.InvoiceNo,
				x.InvoiceDate,
				x.EntryDate,
				x.CreatedAt,
				x.DealerName,
				x.DealerRegistrationId,
				x.IfmsDealerId,
				x.DealerTypeId,
				x.StateId,
				x.DistrictId,
				x.ProductId,
				x.QuantityMT,
				x.ReceivedQuantity,
				x.StatusId,
				x.RetailerReceiptDate,
				x.DdNo,
				x.MobileNo
			}).ToListAsync();

			foreach (var x in company)
			{
				result.Add(Finish(new UnifiedRow
				{
					Id               = x.Id,
					TransactionId    = x.TransactionId ?? "",
					Source           = "Company Sales",
					InvoiceNo        = x.InvoiceNo ?? "",
					InvoiceDate      = x.InvoiceDate,
					EntryDate        = x.EntryDate,
					Anchor           = x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt,
					AgencyName       = string.IsNullOrWhiteSpace(x.DealerName) ? "-" : x.DealerName,
					DealerCode       = (x.DealerRegistrationId ?? x.IfmsDealerId)?.ToString() ?? "-",
					DealerType       = Get(dealerTypes, x.DealerTypeId),
					StateName        = Get(states, x.StateId),
					District         = Get(districts, x.DistrictId),
					ProductName      = Get(products, x.ProductId),
					QuantityMT       = x.QuantityMT,
					ReceivedQuantity = x.ReceivedQuantity,
					WorkflowStatus   = Get(statuses, x.StatusId),
					HasReceipt       = x.RetailerReceiptDate.HasValue,
					DdNo             = x.DdNo,
					MobileNo         = x.MobileNo
				}));
			}

			// ---------- Wholesaler Sales ----------
			var wq = _db.Set<SalesWholesaler>().AsNoTracking();
			if (f.StateIds.Count > 0) wq = wq.Where(x => x.StateId != null && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Count > 0) wq = wq.Where(x =>
											  (x.BuyerDistrictId != null && f.DistrictIds.Contains(x.BuyerDistrictId.Value)) ||
											  (x.SellerDistrictId != null && f.DistrictIds.Contains(x.SellerDistrictId.Value)));
			if (f.DealerTypeIds.Count > 0) wq = wq.Where(x => x.DealerTypeId != null && f.DealerTypeIds.Contains(x.DealerTypeId.Value));
			if (f.ProductIds.Count > 0) wq = wq.Where(x => x.ProductId != null && f.ProductIds.Contains(x.ProductId.Value));
			if (hasDealerFilter) wq = wq.Where(x =>
											  (x.DealerId != null && regIds.Contains(x.DealerId.Value)) ||      // TODO: confirm DealerId -> DealerRegistrations
											  (x.IfmsDealerId != null && ifmsIds.Contains(x.IfmsDealerId.Value)));
			if (from.HasValue) wq = wq.Where(x => x.InvoiceDate >= from.Value);
			if (to.HasValue) wq = wq.Where(x => x.InvoiceDate <= to.Value);

			var wholesaler = await wq.Select(x => new
			{
				x.Id,
				x.TransactionId,
				x.InvoiceNo,
				x.InvoiceDate,
				x.EntryDate,
				x.CreatedAt,
				x.AgencyName,
				x.WholesalerAgencyName,
				x.DealerId,
				x.IfmsDealerId,
				x.DealerTypeId,
				x.StateId,
				x.BuyerDistrictId,
				x.SellerDistrictId,
				x.ProductId,
				x.QuantityMT,
				x.ReceivedQuantityMT,
				x.StatusId,
				x.RetailerReceiptDate,
				x.DispatchNo,
				x.MobileNo
			}).ToListAsync();

			foreach (var x in wholesaler)
			{
				result.Add(Finish(new UnifiedRow
				{
					Id               = x.Id,
					TransactionId    = x.TransactionId ?? "",
					Source           = "Wholesaler Sales",
					InvoiceNo        = x.InvoiceNo ?? "",
					InvoiceDate      = x.InvoiceDate,
					EntryDate        = x.EntryDate,
					Anchor           = x.InvoiceDate ?? x.EntryDate ?? x.CreatedAt,
					AgencyName       = FirstNonEmpty(x.AgencyName, x.WholesalerAgencyName, "-"),
					DealerCode       = (x.DealerId ?? x.IfmsDealerId)?.ToString() ?? "-",
					DealerType       = Get(dealerTypes, x.DealerTypeId),
					StateName        = Get(states, x.StateId),
					District         = Get(districts, x.BuyerDistrictId ?? x.SellerDistrictId),
					ProductName      = Get(products, x.ProductId),
					QuantityMT       = x.QuantityMT,
					ReceivedQuantity = x.ReceivedQuantityMT,
					WorkflowStatus   = Get(statuses, x.StatusId),
					HasReceipt       = x.RetailerReceiptDate.HasValue,
					DispatchNo       = x.DispatchNo,
					MobileNo         = x.MobileNo
				}));
			}

			return result;
		}

		// Compute acknowledged flag, age and age-status for one row.
		private UnifiedRow Finish(UnifiedRow r)
		{
			// TODO: replace with your real "acknowledged" rule (e.g. a specific StatusId).
			var acknowledged = (r.WorkflowStatus?.IndexOf("Acknowledg", StringComparison.OrdinalIgnoreCase) >= 0)
							   || r.HasReceipt;

			var age = acknowledged ? 0 : Math.Max(0, (Today() - r.Anchor.Date).Days);
			r.PendingAckAgeDays = age;
			r.AgeStatus = acknowledged ? "Completed"
						: age >= OverdueDays ? "Overdue"
						: age >= CriticalDays ? "Critical"
						: age >= PendingDays ? "Pending"
						: "Fresh";
			return r;
		}

		// ==========================================================
		//  Widgets
		// ==========================================================
		private PendingAckSummaryDto BuildSummary(List<UnifiedRow> rows)
		{
			int Count(IEnumerable<UnifiedRow> src, string s) => src.Count(x => x.AgeStatus == s);

			var company = rows.Where(x => x.Source == "Company Sales").ToList();
			var wholesaler = rows.Where(x => x.Source == "Wholesaler Sales").ToList();

			return new PendingAckSummaryDto
			{
				Completed    = Count(rows, "Completed"),
				Critical     = Count(rows, "Critical"),
				Overdue      = Count(rows, "Overdue"),
				ConsentBuyer = rows.Count(x => (x.WorkflowStatus ?? "").IndexOf("Consent", StringComparison.OrdinalIgnoreCase) >= 0),

				CompanyTotal     = company.Count,
				CompanyCompleted = Count(company, "Completed"),
				CompanyCritical  = Count(company, "Critical"),
				CompanyOverdue   = Count(company, "Overdue"),

				WholesalerTotal     = wholesaler.Count,
				WholesalerCompleted = Count(wholesaler, "Completed"),
				WholesalerCritical  = Count(wholesaler, "Critical"),
				WholesalerOverdue   = Count(wholesaler, "Overdue")
			};
		}

		private List<PendingAckStateDto> BuildStateWise(List<UnifiedRow> rows) =>
			rows.Where(x => !string.IsNullOrWhiteSpace(x.StateName) && x.StateName != "-")
				.GroupBy(x => x.StateName)
				.Select(g => new PendingAckStateDto
				{
					StateName = g.Key,
					Completed = g.Count(x => x.AgeStatus == "Completed"),
					Overdue   = g.Count(x => x.AgeStatus == "Overdue"),
					Critical  = g.Count(x => x.AgeStatus == "Critical")
				})
				.Where(s => s.Total > 0)
				.OrderByDescending(s => s.Total)
				.Take(12)
				.ToList();

		private PagedResult<PendingAckRowDto> BuildGrid(List<UnifiedRow> rows, PendingAckFilter f, bool paged)
		{
			IEnumerable<UnifiedRow> q = rows;

			if (!string.IsNullOrWhiteSpace(f.Source) && f.Source != "All")
				q = q.Where(x => x.Source == f.Source);

			if (f.AgeStatuses.Count > 0)
				q = q.Where(x => f.AgeStatuses.Contains(x.AgeStatus));

			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var s = f.Search.Trim();
				q = q.Where(x =>
					(x.InvoiceNo  ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
					(x.AgencyName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
					(x.StateName  ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
					(x.District   ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
			}

			var asc = string.Equals(f.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
			q = f.SortColumn?.ToLowerInvariant() switch
			{
				"quantity" => asc ? q.OrderBy(x => x.QuantityMT) : q.OrderByDescending(x => x.QuantityMT),
				"state" => asc ? q.OrderBy(x => x.StateName) : q.OrderByDescending(x => x.StateName),
				"dealer" => asc ? q.OrderBy(x => x.AgencyName) : q.OrderByDescending(x => x.AgencyName),
				"product" => asc ? q.OrderBy(x => x.ProductName) : q.OrderByDescending(x => x.ProductName),
				_ => asc ? q.OrderBy(x => x.PendingAckAgeDays) : q.OrderByDescending(x => x.PendingAckAgeDays)
			};

			var list = q.ToList();
			var total = list.Count;

			if (paged)
				list = list.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToList();

			return new PagedResult<PendingAckRowDto>
			{
				TotalCount = total,
				Page       = f.Page,
				PageSize   = f.PageSize,
				Items = list.Select(x => new PendingAckRowDto
				{
					Id                = x.Id,
					TransactionId     = x.TransactionId,
					InvoiceNo         = x.InvoiceNo,
					InvoiceDate       = x.InvoiceDate,
					AgencyName        = x.AgencyName,
					DealerCode        = x.DealerCode,
					Source            = x.Source,
					DealerType        = x.DealerType,
					StateName         = x.StateName,
					District          = x.District,
					ProductName       = x.ProductName,
					QuantityMT        = x.QuantityMT,
					ReceivedQuantity  = x.ReceivedQuantity,
					AgeStatus         = x.AgeStatus,
					PendingAckAgeDays = x.PendingAckAgeDays,
					WorkflowStatus    = x.WorkflowStatus,
					EntryDate         = x.EntryDate,
					DdNo              = x.DdNo,
					DispatchNo        = x.DispatchNo,
					MobileNo          = x.MobileNo
				}).ToList()
			};
		}

		// ==========================================================
		//  Lookups (Id -> Name)
		// ==========================================================
		private async Task<(Dictionary<int, string> states, Dictionary<int, string> districts,
							Dictionary<int, string> dealerTypes, Dictionary<int, string> products,
							Dictionary<int, string> statuses)> LoadLookupsAsync()
		{
			// State.StateName + Product.Name are taken from your StockReportService.
			var states = (await _db.Set<State>().AsNoTracking()
				.Select(x => new LookupPair { Id = x.Id, Name = x.StateName }).ToListAsync());

			// TODO: confirm District name property (assumed DistrictName).
			var districts = (await _db.Set<District>().AsNoTracking()
				.Select(x => new LookupPair { Id = x.Id, Name = x.DistrictName }).ToListAsync());

			// TODO: confirm DealerType entity + name property (may be Category / DealershipNature).
			var dealerTypes = (await _db.Set<DealerType>().AsNoTracking()
				.Select(x => new LookupPair { Id = x.Id, Name = x.Name }).ToListAsync());

			var products = (await _db.Set<Product>().AsNoTracking()
				.Select(x => new LookupPair { Id = x.Id, Name = x.Name }).ToListAsync());

			// TODO: confirm Status entity + name property (New / Consent Buyer / Acknowledged).
			var statuses = (await _db.Set<Status>().AsNoTracking()
				.Select(x => new LookupPair { Id = x.Id, Name = x.Name }).ToListAsync());

			return (ToDict(states), ToDict(districts), ToDict(dealerTypes), ToDict(products), ToDict(statuses));
		}

		private static Dictionary<int, string> ToDict(List<LookupPair> list) =>
			list.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Name ?? "");

		private static string Get(Dictionary<int, string> map, int? id) =>
			id.HasValue && map.TryGetValue(id.Value, out var n) && !string.IsNullOrWhiteSpace(n) ? n : "-";

		private static string FirstNonEmpty(params string[] values) =>
			values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "-";

		// In-memory shapes
		private class LookupPair
		{
			public int Id { get; set; }
			public string? Name { get; set; }
		}

		private class UnifiedRow
		{
			public int Id;
			public string TransactionId = "";
			public string Source = "";
			public string InvoiceNo = "";
			public DateTime? InvoiceDate;
			public DateTime? EntryDate;
			public DateTime Anchor;
			public string AgencyName = "";
			public string DealerCode = "";
			public string DealerType = "";
			public string StateName = "";
			public string District = "";
			public string ProductName = "";
			public decimal QuantityMT;
			public decimal ReceivedQuantity;
			public string WorkflowStatus = "";
			public bool HasReceipt;
			public string? DdNo;
			public string? DispatchNo;
			public string? MobileNo;
			public int PendingAckAgeDays;
			public string AgeStatus = "";
		}
	}
}