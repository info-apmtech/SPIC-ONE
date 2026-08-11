using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
	/// Performance-optimized Acknowledgement Cycle report.
	///
	/// Business flow retained:
	/// - Sources: SalesCompanySale + SalesWholesaler only.
	/// - Stock snapshot tables are intentionally excluded because they do not carry
	///   an invoice-to-receipt acknowledgement cycle.
	/// - Only workflow Status.Name == "Ack" rows having valid invoice and receipt
	///   dates are included.
	/// - Cycle = RetailerReceiptDate.Date - InvoiceDate.Date.
	/// - Fast 0-2, Normal 3-5, Delayed 6-10, Critical > 10.
	/// - KPI cards and Top-5 lists ignore Source/Bucket/Search grid controls.
	/// - Grid applies Source, Bucket, Search, sorting and paging.
	/// </summary>
	public class AckCycleService : IAckCycleService
	{
		private const int FastMax = 2;
		private const int NormalMax = 5;
		private const int DelayedMax = 10;
		private const string AckStatusName = "Ack";

		private const string CompanySource = "Company Sales";
		private const string WholesalerSource = "Wholesaler Sales";

		private readonly AppDbContext _db;

		public AckCycleService(AppDbContext db)
		{
			_db = db;
		}

		public async Task<AckCycleDashboardDto> GetDashboardAsync(
			AckCycleFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new AckCycleFilter();
			NormalizeFilter(filter);

			var baseQuery = await BuildBaseQueryAsync(filter, cancellationToken);
			if (baseQuery is null)
			{
				return EmptyDashboard(filter);
			}

			// Execute sequentially because one scoped DbContext must not run concurrent queries.
			var summary = await LoadSummaryAsync(baseQuery, cancellationToken);
			var groupAggregates = await LoadGroupAggregatesAsync(
				baseQuery,
				filter.GroupBy,
				cancellationToken);

			var grid = await LoadPagedGridAsync(
				baseQuery,
				filter,
				cancellationToken);

			return new AckCycleDashboardDto
			{
				Summary = summary,
				TopFastStates = BuildTopGroups(groupAggregates, delayed: false),
				TopDelayedStates = BuildTopGroups(groupAggregates, delayed: true),
				Grid = grid
			};
		}

		public async Task<List<AckCycleRowDto>> GetAllRowsAsync(
			AckCycleFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new AckCycleFilter();
			NormalizeFilter(filter);

			var baseQuery = await BuildBaseQueryAsync(filter, cancellationToken);
			if (baseQuery is null)
			{
				return new List<AckCycleRowDto>();
			}

			var query = ApplyGridFilters(baseQuery, filter);
			var ordered = ApplySort(query, filter.SortColumn, filter.SortDesc);
			var rawRows = await ordered.ToListAsync(cancellationToken);

			var result = new List<AckCycleRowDto>(rawRows.Count);
			for (var index = 0; index < rawRows.Count; index++)
			{
				result.Add(ToDto(rawRows[index], index + 1));
			}

			return result;
		}

		// --------------------------------------------------------------------
		// Base query: filters + lookup joins + UNION ALL, but no materialization.
		// --------------------------------------------------------------------
		private async Task<IQueryable<AckCycleQueryRow>?> BuildBaseQueryAsync(
			AckCycleFilter filter,
			CancellationToken cancellationToken)
		{
			var ackStatusIds = await GetAckStatusIdsAsync(filter, cancellationToken);
			if (ackStatusIds.Count == 0)
			{
				return null;
			}

			var (registrationIds, ifmsIds) = SplitDealerKeys(filter.DealerKeys);
			var (productIds, ifmsProductIds) = SplitProductKeys(filter);
			var dateFrom = ToUtcDate(filter.DateFrom);
			var dateToExclusive = ToUtcDate(filter.DateTo)?.AddDays(1);

			var companySales = _db.Set<SalesCompanySale>()
				.AsNoTracking()
				.Where(x =>
					x.StatusId.HasValue &&
					ackStatusIds.Contains(x.StatusId.Value) &&
					x.InvoiceDate.HasValue &&
					x.RetailerReceiptDate.HasValue &&
					x.RetailerReceiptDate.Value.Date >= x.InvoiceDate.Value.Date);

			if (filter.StateIds.Count > 0)
			{
				companySales = companySales.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				companySales = companySales.Where(x =>
					x.DistrictId.HasValue && filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (productIds.Count > 0 || ifmsProductIds.Count > 0)
			{
				companySales = companySales.Where(x =>
					(x.ProductId.HasValue && productIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue && ifmsProductIds.Contains(x.IfmsProductId.Value)));
			}

			if (registrationIds.Count > 0 || ifmsIds.Count > 0)
			{
				companySales = companySales.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (dateFrom.HasValue)
			{
				companySales = companySales.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				companySales = companySales.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value < dateToExclusive.Value);
			}

			var companyQuery =
				from sale in companySales
				join state in _db.Set<State>().AsNoTracking()
					on sale.StateId equals (int?)state.Id into stateJoin
				from state in stateJoin.DefaultIfEmpty()
				join district in _db.Set<District>().AsNoTracking()
					on sale.DistrictId equals (int?)district.Id into districtJoin
				from district in districtJoin.DefaultIfEmpty()
				join product in _db.Set<Product>().AsNoTracking()
					on sale.ProductId equals (int?)product.Id into productJoin
				from product in productJoin.DefaultIfEmpty()
				join ifmsProduct in _db.Set<IfmsProduct>().AsNoTracking()
					on sale.IfmsProductId equals (int?)ifmsProduct.Id into ifmsProductJoin
				from ifmsProduct in ifmsProductJoin.DefaultIfEmpty()
				join dealer in _db.Set<DealerRegistration>().AsNoTracking()
					on sale.DealerRegistrationId equals (int?)dealer.Id into dealerJoin
				from dealer in dealerJoin.DefaultIfEmpty()
				join ifms in _db.Set<IfmsDealer>().AsNoTracking()
					on sale.IfmsDealerId equals (int?)ifms.Id into ifmsJoin
				from ifms in ifmsJoin.DefaultIfEmpty()
				select new AckCycleQueryRow
				{
					Id = sale.Id,
					Source = CompanySource,
					TransactionId = sale.TransactionId ?? "",
					InvoiceNo = sale.InvoiceNo ?? "",
					InvoiceDate = sale.InvoiceDate,
					EntryDate = sale.EntryDate,
					ReceiptDate = sale.RetailerReceiptDate,
					DealerName = sale.DealerName ??
								 (dealer != null ? dealer.FirmName : null) ??
								 (ifms != null ? ifms.Name : null) ?? "-",
					DealerCode = dealer != null ? dealer.DealerCode : null,
					RegistrationId = sale.DealerRegistrationId,
					IfmsId = sale.IfmsDealerId,
					StateName = state != null ? state.StateName : "-",
					DistrictName = district != null ? district.DistrictName : "-",
					ProductName = product != null
						? product.Name
						: ifmsProduct != null ? ifmsProduct.Name : "-",
					QuantityMT = sale.QuantityMT,
					ReceivedQuantity = sale.ReceivedQuantity,
					WorkflowStatus = AckStatusName,
					DdNo = sale.DdNo,
					MobileNo = sale.MobileNo
				};

			var wholesalerSales = _db.Set<SalesWholesaler>()
				.AsNoTracking()
				.Where(x =>
					x.StatusId.HasValue &&
					ackStatusIds.Contains(x.StatusId.Value) &&
					x.InvoiceDate.HasValue &&
					x.RetailerReceiptDate.HasValue &&
					x.RetailerReceiptDate.Value.Date >= x.InvoiceDate.Value.Date);

			if (filter.StateIds.Count > 0)
			{
				wholesalerSales = wholesalerSales.Where(x =>
					x.StateId.HasValue && filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				wholesalerSales = wholesalerSales.Where(x =>
					filter.DistrictIds.Contains(
						x.BuyerDistrictId ?? x.SellerDistrictId ?? 0));
			}

			if (productIds.Count > 0 || ifmsProductIds.Count > 0)
			{
				wholesalerSales = wholesalerSales.Where(x =>
					(x.ProductId.HasValue && productIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue && ifmsProductIds.Contains(x.IfmsProductId.Value)));
			}

			if (registrationIds.Count > 0 || ifmsIds.Count > 0)
			{
				wholesalerSales = wholesalerSales.Where(x =>
					(x.DealerId.HasValue && registrationIds.Contains(x.DealerId.Value)) ||
					(x.IfmsDealerId.HasValue && ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			if (dateFrom.HasValue)
			{
				wholesalerSales = wholesalerSales.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value >= dateFrom.Value);
			}

			if (dateToExclusive.HasValue)
			{
				wholesalerSales = wholesalerSales.Where(x =>
					x.InvoiceDate.HasValue && x.InvoiceDate.Value < dateToExclusive.Value);
			}

			var wholesalerQuery =
				from sale in wholesalerSales
				let effectiveDistrictId = sale.BuyerDistrictId ?? sale.SellerDistrictId
				join state in _db.Set<State>().AsNoTracking()
					on sale.StateId equals (int?)state.Id into stateJoin
				from state in stateJoin.DefaultIfEmpty()
				join district in _db.Set<District>().AsNoTracking()
					on effectiveDistrictId equals (int?)district.Id into districtJoin
				from district in districtJoin.DefaultIfEmpty()
				join product in _db.Set<Product>().AsNoTracking()
					on sale.ProductId equals (int?)product.Id into productJoin
				from product in productJoin.DefaultIfEmpty()
				join ifmsProduct in _db.Set<IfmsProduct>().AsNoTracking()
					on sale.IfmsProductId equals (int?)ifmsProduct.Id into ifmsProductJoin
				from ifmsProduct in ifmsProductJoin.DefaultIfEmpty()
				join dealer in _db.Set<DealerRegistration>().AsNoTracking()
					on sale.DealerId equals (int?)dealer.Id into dealerJoin
				from dealer in dealerJoin.DefaultIfEmpty()
				join ifms in _db.Set<IfmsDealer>().AsNoTracking()
					on sale.IfmsDealerId equals (int?)ifms.Id into ifmsJoin
				from ifms in ifmsJoin.DefaultIfEmpty()
				select new AckCycleQueryRow
				{
					Id = sale.Id,
					Source = WholesalerSource,
					TransactionId = sale.TransactionId ?? "",
					InvoiceNo = sale.InvoiceNo ?? "",
					InvoiceDate = sale.InvoiceDate,
					EntryDate = sale.EntryDate,
					ReceiptDate = sale.RetailerReceiptDate,
					DealerName = sale.AgencyName ??
								 sale.WholesalerAgencyName ??
								 (dealer != null ? dealer.FirmName : null) ??
								 (ifms != null ? ifms.Name : null) ?? "-",
					DealerCode = dealer != null ? dealer.DealerCode : null,
					RegistrationId = sale.DealerId,
					IfmsId = sale.IfmsDealerId,
					StateName = state != null ? state.StateName : "-",
					DistrictName = district != null ? district.DistrictName : "-",
					ProductName = product != null
						? product.Name
						: ifmsProduct != null ? ifmsProduct.Name : "-",
					QuantityMT = sale.QuantityMT,
					ReceivedQuantity = sale.ReceivedQuantityMT,
					WorkflowStatus = AckStatusName,
					DdNo = sale.DispatchNo,
					MobileNo = sale.MobileNo
				};

			return companyQuery.Concat(wholesalerQuery);
		}

		private async Task<List<int>> GetAckStatusIdsAsync(
			AckCycleFilter filter,
			CancellationToken cancellationToken)
		{
			var statusRows = await _db.Set<Status>()
				.AsNoTracking()
				.Where(x => x.Name != null)
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			var ackIds = statusRows
				.Where(x => string.Equals(
					x.Name.Trim(),
					AckStatusName,
					StringComparison.OrdinalIgnoreCase))
				.Select(x => x.Id)
				.ToList();

			if (filter.StatusIds.Count > 0)
			{
				ackIds = ackIds
					.Where(filter.StatusIds.Contains)
					.ToList();
			}

			return ackIds;
		}

		// --------------------------------------------------------------------
		// Summary: one compact SQL aggregate; no full sales-row materialization.
		// --------------------------------------------------------------------
		private static async Task<AckCycleSummaryDto> LoadSummaryAsync(
			IQueryable<AckCycleQueryRow> query,
			CancellationToken cancellationToken)
		{
			var aggregate = await query
				.GroupBy(_ => 1)
				.Select(group => new SummaryAggregate
				{
					Total = group.Count(),
					Fast = group.Count(x =>
						x.InvoiceDate.HasValue &&
						x.ReceiptDate.HasValue &&
						x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date &&
						x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(FastMax + 1)),
					Normal = group.Count(x =>
						x.InvoiceDate.HasValue &&
						x.ReceiptDate.HasValue &&
						x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date.AddDays(FastMax + 1) &&
						x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(NormalMax + 1)),
					Delayed = group.Count(x =>
						x.InvoiceDate.HasValue &&
						x.ReceiptDate.HasValue &&
						x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date.AddDays(NormalMax + 1) &&
						x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(DelayedMax + 1)),
					Critical = group.Count(x =>
						x.InvoiceDate.HasValue &&
						x.ReceiptDate.HasValue &&
						x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date.AddDays(DelayedMax + 1)),
					AverageCycleDays = group
						.Where(x =>
							x.InvoiceDate.HasValue &&
							x.ReceiptDate.HasValue &&
							x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date)
						.Average(x => (double?)(
							x.ReceiptDate!.Value.Date -
							x.InvoiceDate!.Value.Date).TotalDays)
				})
				.FirstOrDefaultAsync(cancellationToken);

			if (aggregate is null)
			{
				return new AckCycleSummaryDto();
			}

			return new AckCycleSummaryDto
			{
				Total = aggregate.Total,
				Fast = aggregate.Fast,
				Normal = aggregate.Normal,
				Delayed = aggregate.Delayed,
				Critical = aggregate.Critical,
				AverageCycleDays = Math.Round(aggregate.AverageCycleDays ?? 0d, 2)
			};
		}

		// --------------------------------------------------------------------
		// Top grouping: one grouped SQL query, then derive both Top-5 lists.
		// --------------------------------------------------------------------
		private static Task<List<GroupAggregate>> LoadGroupAggregatesAsync(
			IQueryable<AckCycleQueryRow> query,
			string? groupBy,
			CancellationToken cancellationToken)
		{
			var normalized = (groupBy ?? "State").Trim().ToLowerInvariant();

			return normalized switch
			{
				"product" => AggregateByProductAsync(query, cancellationToken),
				"dealer" => AggregateByDealerAsync(query, cancellationToken),
				"district" => AggregateByDistrictAsync(query, cancellationToken),

				// Region / HeadQuarter / SubDistrict are not present on the two sales
				// rows in the supplied schema. Preserve the previous fallback to State.
				_ => AggregateByStateAsync(query, cancellationToken)
			};
		}

		private static Task<List<GroupAggregate>> AggregateByStateAsync(
			IQueryable<AckCycleQueryRow> query,
			CancellationToken cancellationToken)
			=> AggregateGroupsAsync(
				query,
				x => x.StateName,
				cancellationToken);

		private static Task<List<GroupAggregate>> AggregateByDistrictAsync(
			IQueryable<AckCycleQueryRow> query,
			CancellationToken cancellationToken)
			=> AggregateGroupsAsync(
				query,
				x => x.DistrictName,
				cancellationToken);

		private static Task<List<GroupAggregate>> AggregateByProductAsync(
			IQueryable<AckCycleQueryRow> query,
			CancellationToken cancellationToken)
			=> AggregateGroupsAsync(
				query,
				x => x.ProductName,
				cancellationToken);

		private static Task<List<GroupAggregate>> AggregateByDealerAsync(
			IQueryable<AckCycleQueryRow> query,
			CancellationToken cancellationToken)
			=> AggregateGroupsAsync(
				query,
				x => x.DealerName,
				cancellationToken);

		private static Task<List<GroupAggregate>> AggregateGroupsAsync(
			IQueryable<AckCycleQueryRow> query,
			Expression<Func<AckCycleQueryRow, string>> keySelector,
			CancellationToken cancellationToken)
		{
			return query
				.Where(x =>
					x.InvoiceDate.HasValue &&
					x.ReceiptDate.HasValue &&
					x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date)
				.GroupBy(keySelector)
				.Where(group => group.Key != "" && group.Key != "-")
				.Select(group => new GroupAggregate
				{
					Label = group.Key,
					Total = group.Count(),
					Fast = group.Count(x =>
						x.ReceiptDate!.Value.Date <
						x.InvoiceDate!.Value.Date.AddDays(FastMax + 1)),
					Normal = group.Count(x =>
						x.ReceiptDate!.Value.Date >=
						x.InvoiceDate!.Value.Date.AddDays(FastMax + 1) &&
						x.ReceiptDate.Value.Date <
						x.InvoiceDate.Value.Date.AddDays(NormalMax + 1)),
					Delayed = group.Count(x =>
						x.ReceiptDate!.Value.Date >=
						x.InvoiceDate!.Value.Date.AddDays(NormalMax + 1) &&
						x.ReceiptDate.Value.Date <
						x.InvoiceDate.Value.Date.AddDays(DelayedMax + 1)),
					Critical = group.Count(x =>
						x.ReceiptDate!.Value.Date >=
						x.InvoiceDate!.Value.Date.AddDays(DelayedMax + 1))
				})
				.ToListAsync(cancellationToken);
		}

		// --------------------------------------------------------------------
		// Grid: SQL filtering, counting, sorting and paging.
		// --------------------------------------------------------------------
		private async Task<PagedResult<AckCycleRowDto>> LoadPagedGridAsync(
			IQueryable<AckCycleQueryRow> baseQuery,
			AckCycleFilter filter,
			CancellationToken cancellationToken)
		{
			var query = ApplyGridFilters(baseQuery, filter);
			var totalCount = await query.CountAsync(cancellationToken);

			var ordered = ApplySort(query, filter.SortColumn, filter.SortDesc);
			var page = Math.Max(1, filter.Page);
			var pageSize = NormalizePageSize(filter.PageSize);
			var skip = (page - 1) * pageSize;

			var rawRows = await ordered
				.Skip(skip)
				.Take(pageSize)
				.ToListAsync(cancellationToken);

			var items = new List<AckCycleRowDto>(rawRows.Count);
			for (var index = 0; index < rawRows.Count; index++)
			{
				items.Add(ToDto(rawRows[index], skip + index + 1));
			}

			return new PagedResult<AckCycleRowDto>
			{
				Items = items,
				TotalCount = totalCount,
				Page = page,
				PageSize = pageSize
			};
		}

		private static IQueryable<AckCycleQueryRow> ApplyGridFilters(
			IQueryable<AckCycleQueryRow> query,
			AckCycleFilter filter)
		{
			if (!string.IsNullOrWhiteSpace(filter.Source) &&
				!string.Equals(filter.Source, "All", StringComparison.OrdinalIgnoreCase))
			{
				var source = filter.Source.Trim();
				query = query.Where(x => x.Source == source);
			}

			if (filter.Buckets.Count > 0)
			{
				var fast = ContainsIgnoreCase(filter.Buckets, "Fast");
				var normal = ContainsIgnoreCase(filter.Buckets, "Normal");
				var delayed = ContainsIgnoreCase(filter.Buckets, "Delayed");
				var critical = ContainsIgnoreCase(filter.Buckets, "Critical");

				query = query.Where(x =>
					x.InvoiceDate.HasValue &&
					x.ReceiptDate.HasValue &&
					x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date &&
					(
						(fast &&
						 x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(FastMax + 1)) ||
						(normal &&
						 x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date.AddDays(FastMax + 1) &&
						 x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(NormalMax + 1)) ||
						(delayed &&
						 x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date.AddDays(NormalMax + 1) &&
						 x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(DelayedMax + 1)) ||
						(critical &&
						 x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date.AddDays(DelayedMax + 1))
					));
			}

			if (!string.IsNullOrWhiteSpace(filter.Search))
			{
				var pattern = $"%{filter.Search.Trim()}%";
				query = query.Where(x =>
					EF.Functions.ILike(x.DealerName, pattern) ||
					EF.Functions.ILike(x.ProductName, pattern) ||
					EF.Functions.ILike(x.InvoiceNo, pattern) ||
					EF.Functions.ILike(x.TransactionId, pattern) ||
					EF.Functions.ILike(x.StateName, pattern));
			}

			return query;
		}

		private static IOrderedQueryable<AckCycleQueryRow> ApplySort(
			IQueryable<AckCycleQueryRow> query,
			string? sortColumn,
			bool descending)
		{
			var column = (sortColumn ?? "receiptdate").Trim().ToLowerInvariant();

			return column switch
			{
				"dealer" => descending
					? query.OrderByDescending(x => x.DealerName).ThenByDescending(x => x.Id)
					: query.OrderBy(x => x.DealerName).ThenBy(x => x.Id),

				"product" => descending
					? query.OrderByDescending(x => x.ProductName).ThenByDescending(x => x.Id)
					: query.OrderBy(x => x.ProductName).ThenBy(x => x.Id),

				"invoiceno" => descending
					? query.OrderByDescending(x => x.InvoiceNo).ThenByDescending(x => x.Id)
					: query.OrderBy(x => x.InvoiceNo).ThenBy(x => x.Id),

				"invoicedate" => descending
					? query.OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.Id)
					: query.OrderBy(x => x.InvoiceDate).ThenBy(x => x.Id),

				"cycledays" => descending
					? query.OrderByDescending(x =>
							x.InvoiceDate.HasValue &&
							x.ReceiptDate.HasValue &&
							x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date
								? (x.ReceiptDate.Value.Date - x.InvoiceDate.Value.Date).TotalDays
								: 0d)
						.ThenByDescending(x => x.Id)
					: query.OrderBy(x =>
							x.InvoiceDate.HasValue &&
							x.ReceiptDate.HasValue &&
							x.ReceiptDate.Value.Date >= x.InvoiceDate.Value.Date
								? (x.ReceiptDate.Value.Date - x.InvoiceDate.Value.Date).TotalDays
								: 0d)
						.ThenBy(x => x.Id),

				"status" => descending
					? query.OrderByDescending(x =>
							!x.InvoiceDate.HasValue ||
							!x.ReceiptDate.HasValue ||
							x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date
								? "Not Available"
								: x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(FastMax + 1)
									? "Fast"
									: x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(NormalMax + 1)
										? "Normal"
										: x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(DelayedMax + 1)
											? "Delayed"
											: "Critical")
						.ThenByDescending(x => x.Id)
					: query.OrderBy(x =>
							!x.InvoiceDate.HasValue ||
							!x.ReceiptDate.HasValue ||
							x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date
								? "Not Available"
								: x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(FastMax + 1)
									? "Fast"
									: x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(NormalMax + 1)
										? "Normal"
										: x.ReceiptDate.Value.Date < x.InvoiceDate.Value.Date.AddDays(DelayedMax + 1)
											? "Delayed"
											: "Critical")
						.ThenBy(x => x.Id),

				_ => descending
					? query.OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.Id)
					: query.OrderBy(x => x.ReceiptDate).ThenBy(x => x.Id)
			};
		}

		private static AckCycleRowDto ToDto(AckCycleQueryRow row, int serialNumber)
		{
			var cycle = GetCycle(row.InvoiceDate, row.ReceiptDate);
			var dealerCode = !string.IsNullOrWhiteSpace(row.DealerCode)
				? row.DealerCode.Trim()
				: row.RegistrationId.HasValue
					? $"R{row.RegistrationId.Value}"
					: row.IfmsId.HasValue
						? $"I{row.IfmsId.Value}"
						: "-";

			return new AckCycleRowDto
			{
				SNo = serialNumber,
				Id = row.Id,
				Source = row.Source,
				TransactionId = row.TransactionId,
				DealerName = row.DealerName,
				DealerCode = dealerCode,
				ProductName = row.ProductName,
				InvoiceNo = row.InvoiceNo,
				InvoiceDate = row.InvoiceDate,
				EntryDate = row.EntryDate,
				ReceiptDate = row.ReceiptDate,
				CycleDays = cycle.Days,
				Bucket = cycle.Bucket,
				StateName = row.StateName,
				District = row.DistrictName,
				WorkflowStatus = row.WorkflowStatus,
				QuantityMT = row.QuantityMT,
				ReceivedQuantity = row.ReceivedQuantity,
				DdNo = row.DdNo,
				MobileNo = string.IsNullOrWhiteSpace(row.MobileNo) ? null : row.MobileNo.Trim()
			};
		}

		private static CycleResult GetCycle(DateTime? invoiceDate, DateTime? receiptDate)
		{
			if (!invoiceDate.HasValue ||
				!receiptDate.HasValue ||
				receiptDate.Value.Date < invoiceDate.Value.Date)
			{
				return new CycleResult(0, "Not Available", false);
			}

			var days = (receiptDate.Value.Date - invoiceDate.Value.Date).Days;
			var bucket = days <= FastMax
				? "Fast"
				: days <= NormalMax
					? "Normal"
					: days <= DelayedMax
						? "Delayed"
						: "Critical";

			return new CycleResult(days, bucket, true);
		}

		private static List<AckCycleStateStatDto> BuildTopGroups(
			IEnumerable<GroupAggregate> aggregates,
			bool delayed)
		{
			return aggregates
				.Select(item =>
				{
					var rate = item.Total == 0
						? 0d
						: delayed
							? Math.Round((item.Delayed + item.Critical) * 100.0 / item.Total, 1)
							: Math.Round(item.Fast * 100.0 / item.Total, 1);

					return new AckCycleStateStatDto
					{
						StateName = item.Label,
						Label = item.Label,
						Total = item.Total,
						Fast = item.Fast,
						Normal = item.Normal,
						Delayed = item.Delayed,
						Critical = item.Critical,
						Rate = rate
					};
				})
				.OrderByDescending(x => x.Rate)
				.ThenByDescending(x => x.Total)
				.ThenBy(x => x.Label)
				.Take(5)
				.ToList();
		}

		// --------------------------------------------------------------------
		// Dropdown data.
		// --------------------------------------------------------------------
		public async Task<List<AckLookupItemDto>> GetStatesAsync(
			CancellationToken cancellationToken = default)
		{
			var rows = await _db.Set<State>()
				.AsNoTracking()
				.OrderBy(x => x.StateName)
				.Select(x => new { x.Id, x.StateName })
				.ToListAsync(cancellationToken);

			return rows.Select(x => new AckLookupItemDto
			{
				Id = x.Id.ToString(),
				Name = x.StateName
			}).ToList();
		}

		public async Task<List<AckLookupItemDto>> GetDistrictsAsync(
			List<int> stateIds,
			CancellationToken cancellationToken = default)
		{
			stateIds ??= new List<int>();

			IQueryable<District> districts = _db.Set<District>().AsNoTracking();

			// Avoid assuming District.StateId. Resolve relevant district IDs through
			// the two sales tables, using fields already confirmed by this report.
			if (stateIds.Count > 0)
			{
				var companyDistrictIds = _db.Set<SalesCompanySale>()
					.AsNoTracking()
					.Where(x =>
						x.StateId.HasValue && stateIds.Contains(x.StateId.Value) &&
						x.DistrictId.HasValue)
					.Select(x => x.DistrictId!.Value);

				var wholesalerBuyerDistrictIds = _db.Set<SalesWholesaler>()
					.AsNoTracking()
					.Where(x =>
						x.StateId.HasValue && stateIds.Contains(x.StateId.Value) &&
						x.BuyerDistrictId.HasValue)
					.Select(x => x.BuyerDistrictId!.Value);

				var wholesalerSellerDistrictIds = _db.Set<SalesWholesaler>()
					.AsNoTracking()
					.Where(x =>
						x.StateId.HasValue && stateIds.Contains(x.StateId.Value) &&
						x.SellerDistrictId.HasValue)
					.Select(x => x.SellerDistrictId!.Value);

				var districtIds = companyDistrictIds
					.Concat(wholesalerBuyerDistrictIds)
					.Concat(wholesalerSellerDistrictIds)
					.Distinct();

				districts = districts.Where(x => districtIds.Contains(x.Id));
			}

			var rows = await districts
				.OrderBy(x => x.DistrictName)
				.Select(x => new { x.Id, x.DistrictName })
				.ToListAsync(cancellationToken);

			return rows.Select(x => new AckLookupItemDto
			{
				Id = x.Id.ToString(),
				Name = x.DistrictName
			}).ToList();
		}

		public async Task<List<AckLookupItemDto>> GetProductsAsync(
			CancellationToken cancellationToken = default)
		{
			var approvedRows = await _db.Set<Product>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != "")
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			var ifmsRows = await _db.Set<IfmsProduct>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != "")
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			return approvedRows
				.Select(x => new AckLookupItemDto
				{
					Id = $"P:{x.Id}",
					Name = x.Name!.Trim()
				})
				.Concat(ifmsRows.Select(x => new AckLookupItemDto
				{
					Id = $"I:{x.Id}",
					Name = $"{x.Name!.Trim()} (IFMS)"
				}))
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public async Task<List<AckLookupItemDto>> GetStatusesAsync(
			CancellationToken cancellationToken = default)
		{
			var statuses = await _db.Set<Status>()
				.AsNoTracking()
				.Where(x => x.Name != null)
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			return statuses
				.Where(x => string.Equals(
					x.Name.Trim(),
					AckStatusName,
					StringComparison.OrdinalIgnoreCase))
				.Select(x => new AckLookupItemDto
				{
					Id = x.Id.ToString(),
					Name = x.Name
				})
				.OrderBy(x => x.Name)
				.ToList();
		}

		public async Task<List<AckLookupItemDto>> GetDealersAsync(
			CancellationToken cancellationToken = default)
		{
			// Keep the existing dealer identity contract unchanged:
			// R{id} = DealerRegistration.Id, I{id} = IfmsDealer.Id.
			//
			// Dealer names originate from uploaded/master data. Some legacy rows can
			// contain leading punctuation (for example ".VIMAL...") or placeholder
			// zero-only values ("0", "00", "000"). We clean the DISPLAY NAME only.
			// Database values and filter keys are not modified.
			var registeredDealers = await _db.Set<DealerRegistration>()
				.AsNoTracking()
				.Where(x => x.FirmName != null && x.FirmName != "")
				.Select(x => new { x.Id, x.FirmName })
				.ToListAsync(cancellationToken);

			var ifmsDealers = await _db.Set<IfmsDealer>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != "")
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			var raw = new List<AckLookupItemDto>(
				registeredDealers.Count + ifmsDealers.Count);

			foreach (var dealer in registeredDealers)
			{
				var displayName = NormalizeDealerLookupName(dealer.FirmName);
				if (displayName is null)
					continue;

				raw.Add(new AckLookupItemDto
				{
					Id = $"R{dealer.Id}",
					Name = displayName
				});
			}

			foreach (var dealer in ifmsDealers)
			{
				var displayName = NormalizeDealerLookupName(dealer.Name);
				if (displayName is null)
					continue;

				raw.Add(new AckLookupItemDto
				{
					Id = $"I{dealer.Id}",
					Name = displayName
				});
			}

			// Keep R{id}/I{id} only as the hidden option value used by filtering.
			// The user-facing dropdown shows the cleaned dealer name only.
			// Do not append "Registered", "IFMS", R-id or I-id to the visible label.
			// Duplicate display names are intentionally retained because each option can
			// represent a different transaction identity and removing one could hide data.
			return raw
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static string? NormalizeDealerLookupName(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var name = value.Trim();

			// Remove punctuation accidentally prefixed/suffixed by uploaded files.
			// Digits are intentionally NOT trimmed because names such as
			// "7 Hills Fertilisers" can be valid dealer names.
			name = name.Trim(DealerNameNoiseCharacters);

			// Collapse repeated whitespace without changing the actual words.
			name = string.Join(
				" ",
				name.Split(
					new[] { ' ', '	', '\r', '\n' },
					StringSplitOptions.RemoveEmptyEntries));

			if (string.IsNullOrWhiteSpace(name))
				return null;

			// Do not show legacy placeholder rows such as 0 / 00 / 000.
			// Non-zero numeric names are kept to avoid silently removing a potentially
			// valid legacy dealer identifier.
			if (name.All(char.IsDigit) && name.All(ch => ch == '0'))
				return null;

			// Punctuation-only values are not meaningful dropdown options.
			if (!name.Any(char.IsLetterOrDigit))
				return null;

			return name;
		}

		private static readonly char[] DealerNameNoiseCharacters =
		{
			'.', ',', ';', ':', '_', '-', '|', '/', '\\', '\'', '"', '`', '~'
		};

		// --------------------------------------------------------------------
		// Helpers.
		// --------------------------------------------------------------------
		private static void NormalizeFilter(AckCycleFilter filter)
		{
			filter.StateIds ??= new List<int>();
			filter.DistrictIds ??= new List<int>();
			filter.ProductIds ??= new List<int>();
			filter.ProductKeys ??= new List<string>();
			filter.StatusIds ??= new List<int>();
			filter.DealerKeys ??= new List<string>();
			filter.Buckets ??= new List<string>();

			filter.StateIds = filter.StateIds.Where(x => x > 0).Distinct().ToList();
			filter.DistrictIds = filter.DistrictIds.Where(x => x > 0).Distinct().ToList();
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
			filter.Buckets = filter.Buckets
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (filter.DateFrom.HasValue)
				filter.DateFrom = filter.DateFrom.Value.Date;
			if (filter.DateTo.HasValue)
				filter.DateTo = filter.DateTo.Value.Date;

			if (filter.DateFrom.HasValue &&
				filter.DateTo.HasValue &&
				filter.DateFrom.Value > filter.DateTo.Value)
			{
				var originalFrom = filter.DateFrom;
				filter.DateFrom = filter.DateTo;
				filter.DateTo = originalFrom;
			}

			filter.GroupBy = string.IsNullOrWhiteSpace(filter.GroupBy)
				? "State"
				: filter.GroupBy.Trim();
			filter.Page = Math.Max(1, filter.Page);
			filter.PageSize = NormalizePageSize(filter.PageSize);
		}

		private static int NormalizePageSize(int value)
			=> value <= 0 ? 16 : Math.Min(value, 500);

		private static DateTime? ToUtcDate(DateTime? value)
			=> value.HasValue
				? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
				: null;

		private static bool ContainsIgnoreCase(IEnumerable<string> values, string target)
			=> values.Any(x => string.Equals(
				x?.Trim(),
				target,
				StringComparison.OrdinalIgnoreCase));

		private static (List<int> ProductIds, List<int> IfmsProductIds) SplitProductKeys(
			AckCycleFilter filter)
		{
			var productIds = new HashSet<int>(
				(filter.ProductIds ?? new List<int>()).Where(x => x > 0));
			var ifmsProductIds = new HashSet<int>();

			foreach (var rawKey in filter.ProductKeys ?? Enumerable.Empty<string>())
			{
				var key = rawKey?.Trim();
				if (string.IsNullOrWhiteSpace(key))
					continue;

				if (key.StartsWith("P:", StringComparison.OrdinalIgnoreCase) &&
					int.TryParse(key[2..], out var productId) &&
					productId > 0)
				{
					productIds.Add(productId);
				}
				else if (key.StartsWith("I:", StringComparison.OrdinalIgnoreCase) &&
					int.TryParse(key[2..], out var ifmsProductId) &&
					ifmsProductId > 0)
				{
					ifmsProductIds.Add(ifmsProductId);
				}
				else if (int.TryParse(key, out var legacyProductId) && legacyProductId > 0)
				{
					productIds.Add(legacyProductId);
				}
			}

			return (productIds.ToList(), ifmsProductIds.ToList());
		}

		private static (List<int> RegistrationIds, List<int> IfmsIds) SplitDealerKeys(
			IEnumerable<string> keys)
		{
			var registrationIds = new HashSet<int>();
			var ifmsIds = new HashSet<int>();

			foreach (var key in keys ?? Enumerable.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(key) || key.Length < 2)
				{
					continue;
				}

				if (!int.TryParse(key.Substring(1), out var id) || id <= 0)
				{
					continue;
				}

				if (key[0] == 'R' || key[0] == 'r')
				{
					registrationIds.Add(id);
				}
				else if (key[0] == 'I' || key[0] == 'i')
				{
					ifmsIds.Add(id);
				}
			}

			return (registrationIds.ToList(), ifmsIds.ToList());
		}

		private static AckCycleDashboardDto EmptyDashboard(AckCycleFilter filter)
			=> new()
			{
				Summary = new AckCycleSummaryDto(),
				TopFastStates = new List<AckCycleStateStatDto>(),
				TopDelayedStates = new List<AckCycleStateStatDto>(),
				Grid = new PagedResult<AckCycleRowDto>
				{
					Items = new List<AckCycleRowDto>(),
					TotalCount = 0,
					Page = Math.Max(1, filter.Page),
					PageSize = NormalizePageSize(filter.PageSize)
				}
			};

		private sealed class AckCycleQueryRow
		{
			public int Id { get; set; }
			public string Source { get; set; } = "";
			public string TransactionId { get; set; } = "";
			public string InvoiceNo { get; set; } = "";
			public DateTime? InvoiceDate { get; set; }
			public DateTime? EntryDate { get; set; }
			public DateTime? ReceiptDate { get; set; }
			public string DealerName { get; set; } = "";
			public string? DealerCode { get; set; }
			public int? RegistrationId { get; set; }
			public int? IfmsId { get; set; }
			public string StateName { get; set; } = "";
			public string DistrictName { get; set; } = "";
			public string ProductName { get; set; } = "";
			public decimal QuantityMT { get; set; }
			public decimal ReceivedQuantity { get; set; }
			public string WorkflowStatus { get; set; } = "";
			public string? DdNo { get; set; }
			public string? MobileNo { get; set; }
		}

		private sealed class SummaryAggregate
		{
			public int Total { get; set; }
			public int Fast { get; set; }
			public int Normal { get; set; }
			public int Delayed { get; set; }
			public int Critical { get; set; }
			public double? AverageCycleDays { get; set; }
		}

		private sealed class GroupAggregate
		{
			public string Label { get; set; } = "";
			public int Total { get; set; }
			public int Fast { get; set; }
			public int Normal { get; set; }
			public int Delayed { get; set; }
			public int Critical { get; set; }
		}

		private readonly record struct CycleResult(
			int Days,
			string Bucket,
			bool IsValid);
	}
}