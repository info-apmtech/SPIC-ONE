// ============================================================================
//  AgeingReportService — Spic.Infrastructure/Services/
//
//  PERFORMANCE-OPTIMIZED IMPLEMENTATION
//
//  Existing business flow is preserved:
//    * SalesCompanySale + SalesWholesaler are combined.
//    * Only acknowledged rows are aged.
//    * Acknowledgement date = RetailerReceiptDate.
//    * Ageing quantity = ReceivedQuantity, falling back to QuantityMT.
//    * Total Stock comes from the three stock tables.
//    * Search, filters, cards, charts, sorting, paging and export remain intact.
//
//  Main performance changes:
//    * No full sales-table materialization for dashboard requests.
//    * Summary and chart values are aggregated in SQL.
//    * Only the requested grid page is loaded with lookup names.
//    * Average ageing loads a compact date histogram instead of every sale row.
//    * Total stock is calculated through one UNION ALL aggregate query.
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
	public class AgeingReportService : IAgeingReportService
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

			// Unified, acknowledged, filtered sales query. This remains IQueryable,
			// so summary, charts, count, sort and paging execute in the database.
			var rows = BuildResolvedSalesQuery(f, today);

			// Keep queries sequential because the same scoped DbContext is used.
			var totalStock = await ComputeTotalStockAsync(f, cancellationToken);
			var global = await LoadGlobalAggregateAsync(rows, today, cancellationToken);
			var stateAggregates = await LoadStateAggregatesAsync(rows, today, cancellationToken);
			var averageAgeing = await LoadAverageAgeingAsync(rows, today, cancellationToken);
			var grid = await LoadGridAsync(rows, f, today, cancellationToken);

			var stateWise = stateAggregates
				.Select(x => new AgeingStateDto
				{
					StateName = x.StateName ?? string.Empty,
					Stock = x.Total,
					Sales = 0m
				})
				.OrderByDescending(x => x.Stock)
				.ToList();

			var stateBuckets = stateAggregates
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
				.ToList();

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
				StateWise = stateWise,
				StateBuckets = stateBuckets,
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
			var query = BuildResolvedSalesQuery(f, today);

			// Preserve the previous export flow: oldest/highest-ageing rows first.
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
		//  Unified sales query
		// ====================================================================

		private IQueryable<ResolvedAgeingRow> BuildResolvedSalesQuery(
			AgeingReportFilter f,
			DateTime today)
		{
			var company = ApplyCompanyFilters(
					_db.Set<SalesCompanySale>().AsNoTracking(),
					f,
					today)
				.Select(x => new SalesAgeingRaw
				{
					DealerRegistrationId = x.DealerRegistrationId,
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

			var wholesaler = ApplyWholesalerFilters(
					_db.Set<SalesWholesaler>().AsNoTracking(),
					f,
					today)
				.Select(x => new SalesAgeingRaw
				{
					// Existing flow treats SalesWholesaler.DealerId as the
					// dealer-registration ID.
					DealerRegistrationId = x.DealerId,
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
			var dealers = _db.Set<DealerRegistration>().AsNoTracking();

			var resolved =
				from sale in sales
				join state in states
					on sale.StateId equals state.Id into stateJoin
				from state in stateJoin.DefaultIfEmpty()
				join district in districts
					on sale.DistrictId equals district.Id into districtJoin
				from district in districtJoin.DefaultIfEmpty()
				join product in products
					on sale.ProductId equals product.Id into productJoin
				from product in productJoin.DefaultIfEmpty()
				join dealer in dealers
					on sale.DealerRegistrationId equals dealer.Id into dealerJoin
				from dealer in dealerJoin.DefaultIfEmpty()
				select new ResolvedAgeingRow
				{
					DealerRegistrationId = sale.DealerRegistrationId,
					DealerName = sale.DealerName ?? string.Empty,
					DealerCode = dealer != null ? dealer.DealerCode : null,
					StateId = sale.StateId,
					StateName = state != null ? state.StateName : string.Empty,
					DistrictName = district != null ? district.DistrictName : null,
					ProductName = product != null ? product.Name : string.Empty,
					MobileNo = sale.MobileNo,
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

		private static IQueryable<SalesCompanySale> ApplyCompanyFilters(
			IQueryable<SalesCompanySale> query,
			AgeingReportFilter f,
			DateTime today)
		{
			// Only acknowledged sales enter the ageing report.
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

			return ApplyCompanyAgeingRanges(query, f.AgeingRanges, today);
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerFilters(
			IQueryable<SalesWholesaler> query,
			AgeingReportFilter f,
			DateTime today)
		{
			// Only acknowledged sales enter the ageing report.
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

			return ApplyWholesalerAgeingRanges(query, f.AgeingRanges, today);
		}

		private static IQueryable<SalesCompanySale> ApplyCompanyAgeingRanges(
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

		private static IQueryable<SalesWholesaler> ApplyWholesalerAgeingRanges(
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
		//  Dashboard aggregates
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

		private static async Task<List<StateAggregate>> LoadStateAggregatesAsync(
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
			// One result per acknowledgement date instead of one result per sale.
			// This preserves the exact row-weighted average while keeping the
			// transferred result set very small.
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
				var days = (today - item.Date.Date).Days;
				if (days < 0)
				{
					days = 0;
				}

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

		// ====================================================================
		//  Grid
		// ====================================================================

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
			var direction = f.SortDir?.Trim().ToLowerInvariant();
			var descending = direction == "desc";

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

				// A higher ageing value means an older/smaller acknowledgement date.
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
			var ageingDays = (today - raw.AckDate.Date).Days;
			if (ageingDays < 0)
			{
				ageingDays = 0;
			}

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

		// ====================================================================
		//  Total stock from the three stock sources
		// ====================================================================

		private async Task<decimal> ComputeTotalStockAsync(
			AgeingReportFilter f,
			CancellationToken cancellationToken)
		{
			var wholesaler = _db.Set<WholesalerStockAsOnToday>()
				.AsNoTracking()
				.Where(x => x.Stock > 0m);

			var retailer = _db.Set<DptReport>()
				.AsNoTracking()
				.Where(x => x.ClosingBalance > 0m);

			var warehouse = _db.Set<WarehouseDistrictGlobalStockReconciliation>()
				.AsNoTracking()
				.Where(x => x.ClosingStock > 0m);

			if (f.StateIds.Count > 0)
			{
				wholesaler = wholesaler.Where(x =>
					x.StateId.HasValue &&
					f.StateIds.Contains(x.StateId.Value));

				retailer = retailer.Where(x =>
					x.StateId.HasValue &&
					f.StateIds.Contains(x.StateId.Value));

				warehouse = warehouse.Where(x =>
					x.StateId.HasValue &&
					f.StateIds.Contains(x.StateId.Value));
			}

			if (f.DistrictIds.Count > 0)
			{
				wholesaler = wholesaler.Where(x =>
					x.DistrictId.HasValue &&
					f.DistrictIds.Contains(x.DistrictId.Value));

				retailer = retailer.Where(x =>
					x.DistrictId.HasValue &&
					f.DistrictIds.Contains(x.DistrictId.Value));

				warehouse = warehouse.Where(x =>
					x.DistrictId.HasValue &&
					f.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (f.ProductIds.Count > 0)
			{
				wholesaler = wholesaler.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));

				retailer = retailer.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));

				warehouse = warehouse.Where(x =>
					x.ProductId.HasValue &&
					f.ProductIds.Contains(x.ProductId.Value));
			}

			if (f.LyingWithIds.Count > 0)
			{
				wholesaler = wholesaler.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));

				retailer = retailer.Where(x =>
					x.DealershipNatureId.HasValue &&
					f.LyingWithIds.Contains(x.DealershipNatureId.Value));

				// Warehouse reconciliation has no DealershipNatureId.
			}

			var allStockValues = wholesaler
				.Select(x => (decimal?)x.Stock)
				.Concat(retailer.Select(x => (decimal?)x.ClosingBalance))
				.Concat(warehouse.Select(x => (decimal?)x.ClosingStock));

			return await allStockValues.SumAsync(cancellationToken) ?? 0m;
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

			f.SortDir = string.Equals(f.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
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