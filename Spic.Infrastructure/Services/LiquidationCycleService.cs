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
	public class LiquidationCycleService : ILiquidationCycleService
	{
		private readonly AppDbContext _db;
		public LiquidationCycleService(AppDbContext db) => _db = db;

		private static DateTime? Utc(DateTime? d) =>
			d.HasValue ? DateTime.SpecifyKind(d.Value.Date, DateTimeKind.Utc) : (DateTime?)null;

		public async Task<LiqCycleDashboardDto> GetDashboardAsync(LiqCycleFilter f)
		{
			var rows = await BuildRowsAsync(f);

			return new LiqCycleDashboardDto
			{
				Summary = BuildSummary(rows),
				TopFastDealers = BuildTopGroups(rows, delayed: false),
				TopSlowDealers = BuildTopGroups(rows, delayed: true),
				Grid = BuildGrid(rows, f, paged: true)
			};
		}

		public async Task<List<LiqCycleRowDto>> GetAllRowsAsync(LiqCycleFilter f)
		{
			var rows = await BuildRowsAsync(f);
			return BuildGrid(rows, f, paged: false).Items;
		}

		private async Task<List<LiqCycleRowDto>> BuildRowsAsync(LiqCycleFilter f)
		{
			// Lookups
			var states = await _db.Set<State>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.StateName);
			var districts = await _db.Set<District>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.DistrictName);
			var products = await _db.Set<Product>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

			var from = Utc(f.DateFrom);
			var to = Utc(f.DateTo);
			var result = new List<LiqCycleRowDto>();

			// --- Company Sales ---
			var cq = _db.SalesCompanySales.AsNoTracking().AsQueryable();
			if (f.StateIds.Any()) cq = cq.Where(x => x.StateId != null && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Any()) cq = cq.Where(x => x.DistrictId != null && f.DistrictIds.Contains(x.DistrictId.Value));
			if (f.ProductIds.Any()) cq = cq.Where(x => x.ProductId != null && f.ProductIds.Contains(x.ProductId.Value));
			if (f.StatusIds.Any()) cq = cq.Where(x => x.StatusId != null && f.StatusIds.Contains(x.StatusId.Value));
			if (from.HasValue) cq = cq.Where(x => x.InvoiceDate >= from.Value);
			if (to.HasValue) cq = cq.Where(x => x.InvoiceDate <= to.Value);

			var company = await cq.Select(x => new { x.Id, x.DealerName, x.DealerRegistrationId, x.IfmsDealerId, x.DealerTypeId, x.StateId, x.DistrictId, x.ProductId, x.QuantityMT, x.ReceivedQuantity, x.InvoiceDate, x.RetailerReceiptDate, x.MobileNo }).ToListAsync();

			foreach (var x in company)
			{
				result.Add(Classify(new LiqCycleRowDto
				{
					Id = x.Id,
					Source = "Company Sales",
					DealerName = string.IsNullOrWhiteSpace(x.DealerName) ? "-" : x.DealerName,
					DealerCode = (x.DealerRegistrationId ?? x.IfmsDealerId)?.ToString() ?? "-",
					DealerType = x.DealerTypeId?.ToString() ?? "-", // Map to lookup if DealerType table exists
					StateName = GetName(states, x.StateId),
					District = GetName(districts, x.DistrictId),
					ProductName = GetName(products, x.ProductId),
					MobileNo = x.MobileNo ?? "-",
					Stock = x.QuantityMT,
					Sales = x.ReceivedQuantity, // Using Received as proxy for Liquidated/Sales
					AgeingDays = CalcDays(x.RetailerReceiptDate ?? x.InvoiceDate)
				}));
			}

			// --- Wholesaler Sales ---
			var wq = _db.SalesWholesalers.AsNoTracking().AsQueryable();
			if (f.StateIds.Any()) wq = wq.Where(x => x.StateId != null && f.StateIds.Contains(x.StateId.Value));
			if (f.DistrictIds.Any()) wq = wq.Where(x => (x.BuyerDistrictId != null && f.DistrictIds.Contains(x.BuyerDistrictId.Value)) || (x.SellerDistrictId != null && f.DistrictIds.Contains(x.SellerDistrictId.Value)));
			if (f.ProductIds.Any()) wq = wq.Where(x => x.ProductId != null && f.ProductIds.Contains(x.ProductId.Value));
			if (f.StatusIds.Any()) wq = wq.Where(x => x.StatusId != null && f.StatusIds.Contains(x.StatusId.Value));
			if (from.HasValue) wq = wq.Where(x => x.InvoiceDate >= from.Value);
			if (to.HasValue) wq = wq.Where(x => x.InvoiceDate <= to.Value);

			var wholesaler = await wq.Select(x => new { x.Id, x.AgencyName, x.WholesalerAgencyName, x.DealerId, x.IfmsDealerId, x.DealerTypeId, x.StateId, x.BuyerDistrictId, x.SellerDistrictId, x.ProductId, x.QuantityMT, x.ReceivedQuantityMT, x.InvoiceDate, x.RetailerReceiptDate, x.MobileNo }).ToListAsync();

			foreach (var x in wholesaler)
			{
				result.Add(Classify(new LiqCycleRowDto
				{
					Id = x.Id,
					Source = "Wholesaler Sales",
					DealerName = !string.IsNullOrWhiteSpace(x.AgencyName) ? x.AgencyName : (x.WholesalerAgencyName ?? "-"),
					DealerCode = (x.DealerId ?? x.IfmsDealerId)?.ToString() ?? "-",
					DealerType = x.DealerTypeId?.ToString() ?? "-",
					StateName = GetName(states, x.StateId),
					District = GetName(districts, x.BuyerDistrictId ?? x.SellerDistrictId),
					ProductName = GetName(products, x.ProductId),
					MobileNo = x.MobileNo ?? "-",
					Stock = x.QuantityMT,
					Sales = x.ReceivedQuantityMT,
					AgeingDays = CalcDays(x.RetailerReceiptDate ?? x.InvoiceDate)
				}));
			}

			if (!string.IsNullOrWhiteSpace(f.Source) && f.Source != "All")
				result = result.Where(r => r.Source == f.Source).ToList();

			return result;
		}

		private static string GetName(Dictionary<int, string> map, int? id) =>
			id.HasValue && map.TryGetValue(id.Value, out var n) ? n : "-";

		private static int CalcDays(DateTime? d) =>
			d.HasValue ? Math.Max(0, (DateTime.UtcNow - d.Value).Days) : 0;

		private LiqCycleRowDto Classify(LiqCycleRowDto r)
		{
			r.Bucket = r.AgeingDays <= 30 ? "Fast" : r.AgeingDays <= 60 ? "Normal" : r.AgeingDays <= 90 ? "Slow" : "Critical";
			r.Status = r.AgeingDays <= 45 ? "Active" : r.AgeingDays <= 75 ? "Monitoring" : "Critical";
			return r;
		}

		private LiqCycleSummaryDto BuildSummary(List<LiqCycleRowDto> rows)
		{
			return new LiqCycleSummaryDto
			{
				TotalStock = rows.Sum(x => x.Stock),
				Liquidated = rows.Sum(x => x.Sales)
			};
		}

		private List<LiqCycleStatDto> BuildTopGroups(List<LiqCycleRowDto> rows, bool delayed)
		{
			return rows.GroupBy(x => x.DealerName)
				.Where(g => g.Key != "-")
				.Select(g =>
				{
					var total = g.Sum(x => x.Stock);
					var fast = g.Where(x => x.Bucket == "Fast").Sum(x => x.Stock);
					var slow = g.Where(x => x.Bucket == "Slow" || x.Bucket == "Critical").Sum(x => x.Stock);

					return new LiqCycleStatDto
					{
						DealerName = g.Key,
						TotalStock = total,
						FastLiquidated = fast,
						SlowLiquidated = slow,
						Rate = total == 0 ? 0 : delayed ? (double)(slow * 100 / total) : (double)(fast * 100 / total)
					};
				})
				.OrderByDescending(s => s.Rate)
				.Take(5)
				.ToList();
		}

		private PagedResult<LiqCycleRowDto> BuildGrid(List<LiqCycleRowDto> rows, LiqCycleFilter f, bool paged)
		{
			IEnumerable<LiqCycleRowDto> q = rows;

			if (!string.IsNullOrWhiteSpace(f.Search))
			{
				var s = f.Search.Trim();
				q = q.Where(x => x.DealerName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
								 x.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase));
			}

			q = f.SortColumn?.ToLower() switch
			{
				"stock" => f.SortDesc ? q.OrderByDescending(x => x.Stock) : q.OrderBy(x => x.Stock),
				"sales" => f.SortDesc ? q.OrderByDescending(x => x.Sales) : q.OrderBy(x => x.Sales),
				"ageing" => f.SortDesc ? q.OrderByDescending(x => x.AgeingDays) : q.OrderBy(x => x.AgeingDays),
				_ => f.SortDesc ? q.OrderByDescending(x => x.AgeingDays) : q.OrderBy(x => x.AgeingDays)
			};

			var all = q.ToList();
			var pageItems = paged ? all.Skip(Math.Max(0, f.Page - 1) * f.PageSize).Take(f.PageSize).ToList() : all;

			return new PagedResult<LiqCycleRowDto>
			{
				Items = pageItems,
				TotalCount = all.Count,
				Page = f.Page,
				PageSize = f.PageSize
			};
		}
	}
}