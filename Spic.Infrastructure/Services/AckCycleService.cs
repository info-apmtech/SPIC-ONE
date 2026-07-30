// ============================================================================
//  Spic.Infrastructure / Services / AckCycleService.cs
//
//  Acknowledgement Cycle Report.
//  Reads ACKNOWLEDGED rows (those with a RetailerReceiptDate) from both
//  SalesCompanySale and SalesWholesaler, computes the invoice -> receipt cycle
//  in days, buckets them Fast/Normal/Delayed/Critical, and produces the five
//  KPI cards, the two Top-5 state lists and the paged/sorted/searched grid.
//
//  Wiring confirmed against StockReportService / PendingAckService:
//    context = AppDbContext (Spic.Infrastructure.Data)
//    lookups = State.StateName, District.DistrictName, Product.Name, Status.Name
//    dealers = DealerRegistration.FirmName ("R"), IfmsDealer.Name ("I")
//  Register in Program.cs:
//    builder.Services.AddScoped<IAckCycleService, AckCycleService>();
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;

namespace Spic.Infrastructure.Services
{
	public class AckCycleService : IAckCycleService
	{
		// Cycle-day bucket boundaries (inclusive lower bounds).
		private const int FastMax = 2;      // 0-2  -> Fast
		private const int NormalMax = 5;    // 3-5  -> Normal
		private const int DelayedMax = 10;  // 6-10 -> Delayed
											// > 10 -> Critical

		private readonly AppDbContext _db;
		public AckCycleService(AppDbContext db) => _db = db;

		// Postgres columns are timestamptz; keep DateTimes as UTC to avoid Npgsql throwing.
		private static DateTime? Utc(DateTime? d) =>
			d.HasValue ? DateTime.SpecifyKind(d.Value.Date, DateTimeKind.Utc) : (DateTime?)null;

		// ==========================================================
		//  Public API
		// ==========================================================
		public async Task<AckCycleDashboardDto> GetDashboardAsync(AckCycleFilter f)
		{
			// Base rows honour every filter EXCEPT Source tab + Bucket, so the KPI
			// cards and Top-5 lists stay stable while the grid tab/bucket switches.
			var rows = await BuildRowsAsync(f);

			return new AckCycleDashboardDto
			{
				Summary          = BuildSummary(rows),
				TopFastStates    = BuildTopGroups(rows, f.GroupBy, delayed: false),
				TopDelayedStates = BuildTopGroups(rows, f.GroupBy, delayed: true),
				Grid             = BuildGrid(rows, f, paged: true)
			};
		}

		public async Task<List<AckCycleRowDto>> GetAllRowsAsync(AckCycleFilter f)
		{
			var rows = await BuildRowsAsync(f);
			return BuildGrid(rows, f, paged: false).Items;
		}

		// ==========================================================
		//  Load both tables, resolve names, keep only acknowledged, classify
		// ==========================================================
		private async Task<List<UnifiedRow>> BuildRowsAsync(AckCycleFilter f)
		{
			var (states, districts, products, statuses) = await LoadLookupsAsync();

			var from = Utc(f.DateFrom);
			var to = Utc(f.DateTo);
			var (regIds, ifmsIds) = SplitDealerKeys(f.DealerKeys);

			var result = new List<UnifiedRow>();

			// ---------------- Company Sales ----------------
			var cq = _db.Set<SalesCompanySale>().AsNoTracking()
						// Acknowledged = has both an invoice date and a retailer receipt date.
						.Where(x => x.InvoiceDate != null && x.RetailerReceiptDate != null);

			if (f.StateIds.Count > 0) cq = cq.Where(x => x.StateId != null && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Count > 0) cq = cq.Where(x => x.DistrictId != null && f.DistrictIds.Contains(x.DistrictId.Value));
			if (f.ProductIds.Count > 0) cq = cq.Where(x => x.ProductId != null && f.ProductIds.Contains(x.ProductId.Value));
			if (f.StatusIds.Count > 0) cq = cq.Where(x => x.StatusId != null && f.StatusIds.Contains(x.StatusId.Value));
			if (regIds.Count > 0 || ifmsIds.Count > 0)
				cq = cq.Where(x => (x.DealerRegistrationId != null && regIds.Contains(x.DealerRegistrationId.Value))
								|| (x.IfmsDealerId != null && ifmsIds.Contains(x.IfmsDealerId.Value)));
			if (from.HasValue) cq = cq.Where(x => x.InvoiceDate >= from.Value);
			if (to.HasValue) cq = cq.Where(x => x.InvoiceDate <= to.Value);

			var company = await cq.Select(x => new
			{
				x.Id,
				x.TransactionId,
				x.InvoiceNo,
				x.InvoiceDate,
				x.EntryDate,
				x.RetailerReceiptDate,
				x.DealerName,
				x.DealerRegistrationId,
				x.IfmsDealerId,
				x.StateId,
				x.DistrictId,
				x.ProductId,
				x.QuantityMT,
				x.ReceivedQuantity,
				x.StatusId,
				x.DdNo,
				x.MobileNo
			}).ToListAsync();

			foreach (var x in company)
			{
				result.Add(Classify(new UnifiedRow
				{
					Id             = x.Id,
					Source         = "Company Sales",
					TransactionId  = x.TransactionId ?? "",
					InvoiceNo      = x.InvoiceNo ?? "",
					InvoiceDate    = x.InvoiceDate,
					EntryDate      = x.EntryDate,
					ReceiptDate    = x.RetailerReceiptDate,
					DealerName     = FirstNonEmpty(x.DealerName, "-"),
					DealerCode     = (x.DealerRegistrationId ?? x.IfmsDealerId)?.ToString() ?? "-",
					StateName      = Get(states, x.StateId),
					District       = Get(districts, x.DistrictId),
					ProductName    = Get(products, x.ProductId),
					QuantityMT     = x.QuantityMT,
					ReceivedQty    = x.ReceivedQuantity,
					WorkflowStatus = Get(statuses, x.StatusId),
					DdNo           = x.DdNo,
					MobileNo       = x.MobileNo
				}));
			}

			// ---------------- Wholesaler Sales ----------------
			var wq = _db.Set<SalesWholesaler>().AsNoTracking()
						.Where(x => x.InvoiceDate != null && x.RetailerReceiptDate != null);

			if (f.StateIds.Count > 0) wq = wq.Where(x => x.StateId != null && f.StateIds.Contains(x.StateId.Value));
			// Wholesaler carries Seller + Buyer district; the buyer is the one acknowledging.
			if (f.DistrictIds.Count > 0) wq = wq.Where(x => (x.BuyerDistrictId != null && f.DistrictIds.Contains(x.BuyerDistrictId.Value))
														  || (x.SellerDistrictId != null && f.DistrictIds.Contains(x.SellerDistrictId.Value)));
			if (f.ProductIds.Count > 0) wq = wq.Where(x => x.ProductId != null && f.ProductIds.Contains(x.ProductId.Value));
			if (f.StatusIds.Count > 0) wq = wq.Where(x => x.StatusId != null && f.StatusIds.Contains(x.StatusId.Value));
			if (regIds.Count > 0 || ifmsIds.Count > 0)
				wq = wq.Where(x => (x.DealerId != null && regIds.Contains(x.DealerId.Value))
								|| (x.IfmsDealerId != null && ifmsIds.Contains(x.IfmsDealerId.Value)));
			if (from.HasValue) wq = wq.Where(x => x.InvoiceDate >= from.Value);
			if (to.HasValue) wq = wq.Where(x => x.InvoiceDate <= to.Value);

			var wholesaler = await wq.Select(x => new
			{
				x.Id,
				x.TransactionId,
				x.InvoiceNo,
				x.InvoiceDate,
				x.EntryDate,
				x.RetailerReceiptDate,
				x.AgencyName,
				x.WholesalerAgencyName,
				x.DealerId,
				x.IfmsDealerId,
				x.StateId,
				x.BuyerDistrictId,
				x.SellerDistrictId,
				x.ProductId,
				x.QuantityMT,
				x.ReceivedQuantityMT,
				x.StatusId,
				x.DispatchNo,
				x.MobileNo
			}).ToListAsync();

			foreach (var x in wholesaler)
			{
				result.Add(Classify(new UnifiedRow
				{
					Id             = x.Id,
					Source         = "Wholesaler Sales",
					TransactionId  = x.TransactionId ?? "",
					InvoiceNo      = x.InvoiceNo ?? "",
					InvoiceDate    = x.InvoiceDate,
					EntryDate      = x.EntryDate,
					ReceiptDate    = x.RetailerReceiptDate,
					DealerName     = FirstNonEmpty(x.AgencyName, x.WholesalerAgencyName, "-"),
					DealerCode     = (x.DealerId ?? x.IfmsDealerId)?.ToString() ?? "-",
					StateName      = Get(states, x.StateId),
					District       = Get(districts, x.BuyerDistrictId ?? x.SellerDistrictId),
					ProductName    = Get(products, x.ProductId),
					QuantityMT     = x.QuantityMT,
					ReceivedQty    = x.ReceivedQuantityMT,
					WorkflowStatus = Get(statuses, x.StatusId),
					DdNo           = x.DispatchNo,
					MobileNo       = x.MobileNo
				}));
			}

			// Source/channel filter (affects KPIs, Top-5 lists and grid alike — driven by the chart buttons).
			if (!string.IsNullOrWhiteSpace(f.Source) && f.Source != "All")
				result = result.Where(r => r.Source == f.Source).ToList();

			// Cycle-bucket multi-select filter (affects KPIs, Top-5 lists and grid alike).
			if (f.Buckets != null && f.Buckets.Count > 0)
				result = result.Where(r => f.Buckets.Contains(r.Bucket)).ToList();

			return result;
		}

		// Compute cycle days + bucket for one row.
		private UnifiedRow Classify(UnifiedRow r)
		{
			var days = 0;
			if (r.InvoiceDate.HasValue && r.ReceiptDate.HasValue)
				days = Math.Max(0, (r.ReceiptDate.Value.Date - r.InvoiceDate.Value.Date).Days);

			r.CycleDays = days;
			r.Bucket = days <= FastMax ? "Fast"
					 : days <= NormalMax ? "Normal"
					 : days <= DelayedMax ? "Delayed"
					 : "Critical";
			return r;
		}

		// ==========================================================
		//  KPI cards
		// ==========================================================
		private AckCycleSummaryDto BuildSummary(List<UnifiedRow> rows)
		{
			int C(string b) => rows.Count(x => x.Bucket == b);
			var avg = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.CycleDays), 2);

			return new AckCycleSummaryDto
			{
				Total            = rows.Count,
				Fast             = C("Fast"),
				Normal           = C("Normal"),
				Delayed          = C("Delayed"),
				Critical         = C("Critical"),
				AverageCycleDays = avg
			};
		}

		// ==========================================================
		//  Top-5 lists, grouped by the chosen dimension (State / Product / Dealer)
		//   Fast list    -> ranked by Fast rate    = Fast / Total
		//   Delayed list -> ranked by delay rate   = (Delayed + Critical) / Total
		// ==========================================================
		private List<AckCycleStateStatDto> BuildTopGroups(List<UnifiedRow> rows, string groupBy, bool delayed)
		{
			Func<UnifiedRow, string> key = (groupBy ?? "State").ToLowerInvariant() switch
			{
				"product" => x => x.ProductName,
				"dealer" => x => x.DealerName,
				_ => x => x.StateName,
			};

			var stats = rows
				.Where(x => { var k = key(x); return !string.IsNullOrWhiteSpace(k) && k != "-"; })
				.GroupBy(key)
				.Select(g =>
				{
					var total = g.Count();
					var fast = g.Count(x => x.Bucket == "Fast");
					var normal = g.Count(x => x.Bucket == "Normal");
					var del = g.Count(x => x.Bucket == "Delayed");
					var crit = g.Count(x => x.Bucket == "Critical");
					var rate = total == 0 ? 0
						: delayed ? Math.Round((del + crit) * 100.0 / total, 1)
								  : Math.Round(fast * 100.0 / total, 1);

					return new AckCycleStateStatDto
					{
						StateName = g.Key,
						Total = total,
						Fast = fast,
						Normal = normal,
						Delayed = del,
						Critical = crit,
						Rate = rate
					};
				})
				.OrderByDescending(s => s.Rate)
				.ThenByDescending(s => s.Total)
				.Take(5)
				.ToList();

			return stats;
		}

		// ==========================================================
		//  Grid (search -> Source/Bucket tab -> sort -> page)
		// ==========================================================
		private PagedResult<AckCycleRowDto> BuildGrid(List<UnifiedRow> rows, AckCycleFilter f, bool paged)
		{
			IEnumerable<UnifiedRow> q = rows;

			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var s = f.Search.Trim();
				q = q.Where(x =>
					(x.DealerName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(x.ProductName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(x.InvoiceNo?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(x.StateName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
			}

			q = Sort(q, f.SortColumn, f.SortDesc);

			var all = q.ToList();
			var total = all.Count;

			var pageItems = paged
				? all.Skip(Math.Max(0, (f.Page - 1)) * f.PageSize).Take(f.PageSize).ToList()
				: all;

			// Sequential S.No across the (possibly paged) slice.
			var start = paged ? Math.Max(0, (f.Page - 1)) * f.PageSize : 0;
			var items = pageItems.Select((x, i) => new AckCycleRowDto
			{
				SNo            = start + i + 1,
				Id             = x.Id,
				Source         = x.Source,
				TransactionId  = x.TransactionId,
				DealerName     = x.DealerName,
				DealerCode     = x.DealerCode,
				ProductName    = x.ProductName,
				InvoiceNo      = x.InvoiceNo,
				InvoiceDate    = x.InvoiceDate,
				EntryDate      = x.EntryDate,
				ReceiptDate    = x.ReceiptDate,
				CycleDays      = x.CycleDays,
				Bucket         = x.Bucket,
				StateName      = x.StateName,
				District       = x.District,
				WorkflowStatus = x.WorkflowStatus,
				QuantityMT     = x.QuantityMT,
				ReceivedQuantity = x.ReceivedQty,
				DdNo           = x.DdNo,
				MobileNo       = x.MobileNo
			}).ToList();

			return new PagedResult<AckCycleRowDto>
			{
				Items      = items,
				TotalCount = total,
				Page       = f.Page,
				PageSize   = f.PageSize
			};
		}

		private static IEnumerable<UnifiedRow> Sort(IEnumerable<UnifiedRow> q, string? col, bool desc)
		{
			Func<UnifiedRow, object> key = (col ?? "").ToLowerInvariant() switch
			{
				"dealer" => x => x.DealerName,
				"product" => x => x.ProductName,
				"invoiceno" => x => x.InvoiceNo,
				"invoicedate" => x => x.InvoiceDate ?? DateTime.MinValue,
				"cycledays" => x => x.CycleDays,
				"status" => x => x.Bucket,
				_ => x => x.ReceiptDate ?? DateTime.MinValue,   // default: most-recent receipt
			};
			return desc ? q.OrderByDescending(key) : q.OrderBy(key);
		}

		// ==========================================================
		//  Filter master data
		// ==========================================================
		public async Task<List<AckLookupItemDto>> GetStatesAsync()
		{
			var (states, _, _, _) = await LoadLookupsAsync();
			return states.Select(kv => new AckLookupItemDto { Id = kv.Key.ToString(), Name = kv.Value })
						 .OrderBy(x => x.Name).ToList();
		}

		public async Task<List<AckLookupItemDto>> GetDistrictsAsync(List<int> stateIds)
		{
			// District table is not assumed to carry a StateId link here, so we return all.
			// If your District entity has StateId, add a Where before ToDictionary in LoadLookupsAsync
			// or filter here. TODO: wire the state->district cascade to your District entity.
			var (_, districts, _, _) = await LoadLookupsAsync();
			return districts.Select(kv => new AckLookupItemDto { Id = kv.Key.ToString(), Name = kv.Value })
							.OrderBy(x => x.Name).ToList();
		}

		public async Task<List<AckLookupItemDto>> GetProductsAsync()
		{
			var (_, _, products, _) = await LoadLookupsAsync();
			return products.Select(kv => new AckLookupItemDto { Id = kv.Key.ToString(), Name = kv.Value })
						   .OrderBy(x => x.Name).ToList();
		}

		public async Task<List<AckLookupItemDto>> GetStatusesAsync()
		{
			var (_, _, _, statuses) = await LoadLookupsAsync();
			return statuses.Select(kv => new AckLookupItemDto { Id = kv.Key.ToString(), Name = kv.Value })
						   .OrderBy(x => x.Name).ToList();
		}

		public async Task<List<AckLookupItemDto>> GetDealersAsync()
		{
			// Keyed union: DealerRegistration -> "R{id}", IfmsDealer -> "I{id}".
			// Name columns confirmed from PendingAckService: registration = FirmName, ifms = Name.
			var reg = await _db.Set<DealerRegistration>().AsNoTracking()
				.Select(d => new { d.Id, d.FirmName })
				.ToListAsync();

			var ifms = await _db.Set<IfmsDealer>().AsNoTracking()
				.Select(d => new { d.Id, d.Name })
				.ToListAsync();

			var dealers = new List<AckLookupItemDto>();
			dealers.AddRange(reg.Select(d => new AckLookupItemDto { Id = "R" + d.Id, Name = d.FirmName ?? "" }));
			dealers.AddRange(ifms.Select(d => new AckLookupItemDto { Id = "I" + d.Id, Name = d.Name ?? "" }));

			return dealers
					  .Where(x => !string.IsNullOrWhiteSpace(x.Name))
					  .OrderBy(x => x.Name)
					  .ToList();
		}

		// ==========================================================
		//  Lookups  — COPY THE LAMBDAS 1:1 FROM PendingAckService.
		//  Change x.Name to x.StateName / x.ProductName / x.StatusName etc.
		//  to match your real entities.
		// ==========================================================
		private async Task<(Dictionary<int, string> states,
							Dictionary<int, string> districts,
							Dictionary<int, string> products,
							Dictionary<int, string> statuses)> LoadLookupsAsync()
		{
			var states = await LoadNames<State>(x => x.Id, x => x.StateName);       // fixed: State uses StateName
			var districts = await LoadNames<District>(x => x.Id, x => x.DistrictName); // fixed: District uses DistrictName
			var products = await LoadNames<Product>(x => x.Id, x => x.Name);          // Product has Name (didn't error)
			var statuses = await LoadNames<Status>(x => x.Id, x => x.Name);           // Status has Name (didn't error)
			return (states, districts, products, statuses);
		}

		private async Task<Dictionary<int, string>> LoadNames<TEntity>(
			Func<TEntity, int> idSel, Func<TEntity, string> nameSel) where TEntity : class
		{
			var list = await _db.Set<TEntity>().AsNoTracking().ToListAsync();
			var dict = new Dictionary<int, string>();
			foreach (var e in list) dict[idSel(e)] = nameSel(e);
			return dict;
		}

		// ==========================================================
		//  Helpers
		// ==========================================================
		private static (List<int> reg, List<int> ifms) SplitDealerKeys(List<string> keys)
		{
			var reg = new List<int>();
			var ifms = new List<int>();
			foreach (var k in keys ?? new List<string>())
			{
				if (string.IsNullOrWhiteSpace(k) || k.Length < 2) continue;
				if (!int.TryParse(k.Substring(1), out var id)) continue;
				if (k[0] == 'R' || k[0] == 'r') reg.Add(id);
				else if (k[0] == 'I' || k[0] == 'i') ifms.Add(id);
			}
			return (reg, ifms);
		}

		private static string Get(Dictionary<int, string> map, int? id) =>
			id.HasValue && map.TryGetValue(id.Value, out var n) && !string.IsNullOrWhiteSpace(n) ? n : "-";

		private static string FirstNonEmpty(params string[] values) =>
			values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "-";

		// In-memory unified shape (both tables collapse into this).
		private class UnifiedRow
		{
			public int Id;
			public string Source = "";
			public string TransactionId = "";
			public string InvoiceNo = "";
			public DateTime? InvoiceDate;
			public DateTime? EntryDate;
			public DateTime? ReceiptDate;
			public string DealerName = "";
			public string DealerCode = "";
			public string StateName = "";
			public string District = "";
			public string ProductName = "";
			public decimal QuantityMT;
			public decimal ReceivedQty;
			public string WorkflowStatus = "";
			public string? DdNo;
			public string? MobileNo;
			public int CycleDays;
			public string Bucket = "";
		}
	}
}