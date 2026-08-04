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
	/// <summary>
	/// Performance-optimized Pending Acknowledgement service.
	///
	/// Business flow is preserved:
	/// - Company Sales + Wholesaler Sales + DPT Sold Quantity are combined.
	/// - Company/Wholesaler completed status is based on RetailerReceiptDate.
	/// - DPT SoldQuantity is already a reported retailer sale, so it is shown as Completed.
	/// - Pending age is based on InvoiceDate; DPT uses its report date (CreatedAt).
	/// - Cards and state chart ignore Source/AgeStatuses filters.
	/// - Grid and export apply Source/AgeStatuses filters.
	/// - Search still affects cards, chart and grid.
	///
	/// Main performance improvement:
	/// the service no longer loads every matching sale and every lookup master
	/// into memory before paging. Aggregation, filtering, sorting and paging are
	/// executed by the database.
	/// </summary>
	public sealed class PendingAckService : IPendingAckService
	{
		private readonly AppDbContext _db;

		private const int SourceCompany = 1;
		private const int SourceWholesaler = 2;
		private const int SourceDpt = 3;

		private const int StatusCompleted = 0;
		private const int StatusLatest = 1;
		private const int StatusCritical = 2;
		private const int StatusOverdue = 3;
		private const int StatusConsentOfBuyer = 4;

		public PendingAckService(AppDbContext db)
		{
			_db = db;
		}

		private static DateTime Today() => DateTime.UtcNow.Date;

		public async Task<PendingAckDashboardDto> GetDashboardAsync(
			PendingAckFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new PendingAckFilter();
			NormalizeFilter(filter);

			var today = Today();
			var page = Math.Max(1, filter.Page);
			var pageSize = filter.PageSize <= 0 ? 16 : filter.PageSize;

			// The base scope intentionally ignores Source and AgeStatuses so the
			// source cards and state chart keep their existing behaviour.
			var baseQuery = BuildRawQuery(
				filter,
				today,
				includeAllSources: true);

			// Query 1: compact summary aggregates only.
			var summaryAggregates = await baseQuery
				.GroupBy(x => new { x.SourceCode, x.StatusCode })
				.Select(group => new SummaryAggregateRow
				{
					SourceCode = group.Key.SourceCode,
					StatusCode = group.Key.StatusCode,
					Count = group.Count(),
					Quantity = group.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken);

			var overall = BuildRollup(summaryAggregates, sourceCode: null);
			var company = BuildRollup(summaryAggregates, SourceCompany);
			var wholesaler = BuildRollup(summaryAggregates, SourceWholesaler);
			var dpt = BuildRollup(summaryAggregates, SourceDpt);

			CopySourceCountsToOverall(overall, company, wholesaler, dpt);

			// Query 2: state/status aggregates only. This replaces materializing
			// every transaction and then grouping the full list in memory.
			var stateAggregates = await (
				from row in baseQuery
				join state in _db.Set<State>().AsNoTracking()
					on row.StateId equals (int?)state.Id
				group row by new
				{
					StateId = state.Id,
					state.StateName,
					row.StatusCode
				}
				into grouped
				select new StateAggregateRow
				{
					StateId = grouped.Key.StateId,
					StateName = grouped.Key.StateName,
					StatusCode = grouped.Key.StatusCode,
					Count = grouped.Count(),
					Quantity = grouped.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken);

			var stateWise = BuildStateWise(stateAggregates);

			// Grid alone applies the active source tab and status selections.
			var filteredGridQuery = ApplyGridFilters(baseQuery, filter);

			// Query 3: count only.
			var totalCount = await filteredGridQuery.CountAsync(cancellationToken);

			// Query 4: joined names + sorted database page only.
			var enrichedGridQuery = BuildEnrichedQuery(filteredGridQuery);
			var sortedGridQuery = ApplySort(enrichedGridQuery, filter);

			var pageRows = await sortedGridQuery
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync(cancellationToken);

			var items = new List<PendingAckRowDto>(pageRows.Count);
			for (var index = 0; index < pageRows.Count; index++)
			{
				items.Add(ToDto(
					pageRows[index],
					today,
					((page - 1) * pageSize) + index + 1));
			}

			return new PendingAckDashboardDto
			{
				Summary = overall,
				Overall = overall,
				CompanySales = company,
				WholesalerSales = wholesaler,
				DptSales = dpt,
				StateWise = stateWise,
				Grid = new PagedResult<PendingAckRowDto>
				{
					Items = items,
					TotalCount = totalCount,
					Page = page,
					PageSize = pageSize
				}
			};
		}

		public async Task<List<PendingAckRowDto>> GetAllRowsAsync(
			PendingAckFilter filter,
			CancellationToken cancellationToken = default)
		{
			filter ??= new PendingAckFilter();
			NormalizeFilter(filter);

			var today = Today();

			// Export does not require card/chart data. When a source tab is
			// selected, skip querying the other two sources completely.
			var rawQuery = BuildRawQuery(
				filter,
				today,
				includeAllSources: false);

			rawQuery = ApplyGridFilters(rawQuery, filter);

			var enrichedQuery = BuildEnrichedQuery(rawQuery);
			var sortedQuery = ApplySort(enrichedQuery, filter);
			var rows = await sortedQuery.ToListAsync(cancellationToken);

			var result = new List<PendingAckRowDto>(rows.Count);
			for (var index = 0; index < rows.Count; index++)
			{
				result.Add(ToDto(rows[index], today, index + 1));
			}

			return result;
		}

		public async Task<List<PendingAckDealerTypeDto>> GetDealerTypesAsync(
			CancellationToken cancellationToken = default)
		{
			return await _db.Set<DealerType>()
				.AsNoTracking()
				.OrderBy(x => x.Name)
				.Select(x => new PendingAckDealerTypeDto
				{
					Id = x.Id,
					Name = x.Name
				})
				.ToListAsync(cancellationToken);
		}

		public async Task<List<PendingAckDealerDto>> GetDealersAsync(
			CancellationToken cancellationToken = default)
		{
			// Keep the existing DealerRegistration + IFMS dealer combination.
			// Only the two fields required by the dropdown are selected.
			var registeredDealers = await _db.Set<DealerRegistration>()
				.AsNoTracking()
				.Where(x => x.FirmName != null && x.FirmName != string.Empty)
				.Select(x => new PendingAckDealerDto
				{
					Key = "R" + x.Id,
					Name = x.FirmName!
				})
				.ToListAsync(cancellationToken);

			var ifmsDealers = await _db.Set<IfmsDealer>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != string.Empty)
				.Select(x => new PendingAckDealerDto
				{
					Key = "I" + x.Id,
					Name = x.Name!
				})
				.ToListAsync(cancellationToken);

			return registeredDealers
				.Concat(ifmsDealers)
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		// =====================================================================
		// Base SQL query
		// =====================================================================

		private IQueryable<RawQueryRow> BuildRawQuery(
			PendingAckFilter filter,
			DateTime today,
			bool includeAllSources)
		{
			var (registrationIds, ifmsIds) = SplitDealerKeys(filter.DealerKeys);

			var latestFrom = today.AddDays(-10);
			var criticalFrom = today.AddDays(-20);

			var source = filter.Source?.Trim();
			var sourceIsCompany = string.Equals(
				source,
				"Company Sales",
				StringComparison.OrdinalIgnoreCase);
			var sourceIsWholesaler = string.Equals(
				source,
				"Wholesaler Sales",
				StringComparison.OrdinalIgnoreCase);
			var sourceIsDpt = string.Equals(
				source,
				"DPT Sales",
				StringComparison.OrdinalIgnoreCase);

			var hasSpecificSource = sourceIsCompany || sourceIsWholesaler || sourceIsDpt;
			var includeCompany = includeAllSources || !hasSpecificSource || sourceIsCompany;
			var includeWholesaler = includeAllSources || !hasSpecificSource || sourceIsWholesaler;
			var includeDpt = includeAllSources || !hasSpecificSource || sourceIsDpt;

			IQueryable<RawQueryRow>? companyQuery = null;
			IQueryable<RawQueryRow>? wholesalerQuery = null;
			IQueryable<RawQueryRow>? dptQuery = null;

			if (includeCompany)
			{
				var query = _db.Set<SalesCompanySale>()
					.AsNoTracking()
					.AsQueryable();

				query = ApplyCompanyFilters(
					query,
					filter,
					registrationIds,
					ifmsIds);

				companyQuery = query.Select(x => new RawQueryRow
				{
					SourceCode = SourceCompany,
					SalesId = x.Id,
					TransactionId = x.TransactionId,
					InvoiceNo = x.InvoiceNo,
					InvoiceDate = x.InvoiceDate,
					EntryDate = x.EntryDate,
					RetailerReceiptDate = x.RetailerReceiptDate,
					AgencyName = x.DealerName,
					DealerTypeId = x.DealerTypeId,
					DealerTypeName = null,
					StateId = x.StateId,
					DistrictId = x.DistrictId,
					ProductId = x.ProductId,
					QuantityMT = x.QuantityMT,
					ReceivedQuantity = x.ReceivedQuantity,
					MobileNo = x.MobileNo,
					DdNo = x.DdNo,
					DispatchNo = (string?)null,
					RegistrationId = x.DealerRegistrationId,
					IfmsId = x.IfmsDealerId,
					StatusCode = x.RetailerReceiptDate != null
						? StatusCompleted
						: x.InvoiceDate == null || x.InvoiceDate.Value >= latestFrom
							? StatusLatest
							: x.InvoiceDate.Value >= criticalFrom
								? StatusCritical
								: StatusOverdue
				});
			}

			if (includeWholesaler)
			{
				var query = _db.Set<SalesWholesaler>()
					.AsNoTracking()
					.AsQueryable();

				query = ApplyWholesalerFilters(
					query,
					filter,
					registrationIds,
					ifmsIds);

				wholesalerQuery = query.Select(x => new RawQueryRow
				{
					SourceCode = SourceWholesaler,
					SalesId = x.Id,
					TransactionId = x.TransactionId,
					InvoiceNo = x.InvoiceNo,
					InvoiceDate = x.InvoiceDate,
					EntryDate = x.EntryDate,
					RetailerReceiptDate = x.RetailerReceiptDate,
					AgencyName = x.AgencyName,
					DealerTypeId = x.DealerTypeId,
					DealerTypeName = null,
					StateId = x.StateId,
					DistrictId = x.BuyerDistrictId,
					ProductId = x.ProductId,
					QuantityMT = x.QuantityMT,
					ReceivedQuantity = x.ReceivedQuantityMT,
					MobileNo = x.MobileNo,
					DdNo = (string?)null,
					DispatchNo = x.DispatchNo,
					RegistrationId = x.DealerId,
					IfmsId = x.IfmsDealerId,
					StatusCode = x.RetailerReceiptDate != null
						? StatusCompleted
						: x.InvoiceDate == null || x.InvoiceDate.Value >= latestFrom
							? StatusLatest
							: x.InvoiceDate.Value >= criticalFrom
								? StatusCritical
								: StatusOverdue
				});
			}

			if (includeDpt)
			{
				var query = _db.Set<DptReport>()
					.AsNoTracking()
					.Where(x => x.SoldQuantity > 0m)
					.AsQueryable();

				query = ApplyDptFilters(
					query,
					filter,
					registrationIds,
					ifmsIds);

				// DPT contains daily retailer sales but has no invoice or acknowledgement
				// columns. SoldQuantity is therefore treated as already reported/completed.
				dptQuery = query.Select(x => new RawQueryRow
				{
					SourceCode = SourceDpt,
					SalesId = x.Id,
					TransactionId = null,
					InvoiceNo = null,
					InvoiceDate = x.CreatedAt,
					EntryDate = x.CreatedAt,
					RetailerReceiptDate = x.CreatedAt,
					AgencyName = x.RetailerName,
					DealerTypeId = null,
					DealerTypeName = "Retailer (DPT)",
					StateId = x.StateId,
					DistrictId = x.DistrictId,
					ProductId = x.ProductId,
					QuantityMT = x.SoldQuantity,
					ReceivedQuantity = 0m,
					MobileNo = x.MobileNo,
					DdNo = null,
					DispatchNo = null,
					RegistrationId = x.DealerRegistrationId,
					IfmsId = x.IfmsDealerId,
					StatusCode = StatusCompleted
				});
			}

			IQueryable<RawQueryRow>? combined = null;
			if (companyQuery is not null)
				combined = companyQuery;
			if (wholesalerQuery is not null)
				combined = combined is null ? wholesalerQuery : combined.Concat(wholesalerQuery);
			if (dptQuery is not null)
				combined = combined is null ? dptQuery : combined.Concat(dptQuery);

			if (combined is not null)
				return combined;

			return _db.Set<SalesCompanySale>()
				.AsNoTracking()
				.Where(x => false)
				.Select(x => new RawQueryRow
				{
					SourceCode = SourceCompany,
					SalesId = x.Id
				});
		}

		private static IQueryable<SalesCompanySale> ApplyCompanyFilters(
			IQueryable<SalesCompanySale> query,
			PendingAckFilter filter,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (filter.DateFrom.HasValue)
			{
				var dateFrom = filter.DateFrom.Value.Date;
				query = query.Where(x =>
					x.InvoiceDate != null &&
					x.InvoiceDate.Value >= dateFrom);
			}

			if (filter.DateTo.HasValue)
			{
				var dateToExclusive = filter.DateTo.Value.Date.AddDays(1);
				query = query.Where(x =>
					x.InvoiceDate != null &&
					x.InvoiceDate.Value < dateToExclusive);
			}

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.DealerTypeIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealerTypeId.HasValue &&
					filter.DealerTypeIds.Contains(x.DealerTypeId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
			}

			if (registrationIds.Count > 0 || ifmsIds.Count > 0)
			{
				query = query.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			ApplyCompanySearch(ref query, filter.Search);
			return query;
		}

		private static IQueryable<SalesWholesaler> ApplyWholesalerFilters(
			IQueryable<SalesWholesaler> query,
			PendingAckFilter filter,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (filter.DateFrom.HasValue)
			{
				var dateFrom = filter.DateFrom.Value.Date;
				query = query.Where(x =>
					x.InvoiceDate != null &&
					x.InvoiceDate.Value >= dateFrom);
			}

			if (filter.DateTo.HasValue)
			{
				var dateToExclusive = filter.DateTo.Value.Date.AddDays(1);
				query = query.Where(x =>
					x.InvoiceDate != null &&
					x.InvoiceDate.Value < dateToExclusive);
			}

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.BuyerDistrictId.HasValue &&
					filter.DistrictIds.Contains(x.BuyerDistrictId.Value));
			}

			if (filter.DealerTypeIds.Count > 0)
			{
				query = query.Where(x =>
					x.DealerTypeId.HasValue &&
					filter.DealerTypeIds.Contains(x.DealerTypeId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
			}

			if (registrationIds.Count > 0 || ifmsIds.Count > 0)
			{
				query = query.Where(x =>
					(x.DealerId.HasValue &&
					 registrationIds.Contains(x.DealerId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			ApplyWholesalerSearch(ref query, filter.Search);
			return query;
		}

		private static IQueryable<DptReport> ApplyDptFilters(
			IQueryable<DptReport> query,
			PendingAckFilter filter,
			List<int> registrationIds,
			List<int> ifmsIds)
		{
			if (filter.DateFrom.HasValue)
			{
				var dateFrom = filter.DateFrom.Value.Date;
				query = query.Where(x => x.CreatedAt >= dateFrom);
			}

			if (filter.DateTo.HasValue)
			{
				var dateToExclusive = filter.DateTo.Value.Date.AddDays(1);
				query = query.Where(x => x.CreatedAt < dateToExclusive);
			}

			if (filter.StateIds.Count > 0)
			{
				query = query.Where(x =>
					x.StateId.HasValue &&
					filter.StateIds.Contains(x.StateId.Value));
			}

			if (filter.DistrictIds.Count > 0)
			{
				query = query.Where(x =>
					x.DistrictId.HasValue &&
					filter.DistrictIds.Contains(x.DistrictId.Value));
			}

			if (filter.ProductIds.Count > 0)
			{
				query = query.Where(x =>
					x.ProductId.HasValue &&
					filter.ProductIds.Contains(x.ProductId.Value));
			}

			if (registrationIds.Count > 0 || ifmsIds.Count > 0)
			{
				query = query.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			// DptReport has DealershipNatureId but no DealerTypeId. Applying a
			// DealerType filter to it would compare unrelated master IDs, so DPT is
			// safely excluded only while that specific filter is active.
			if (filter.DealerTypeIds.Count > 0)
			{
				query = query.Where(x => false);
			}

			ApplyDptSearch(ref query, filter.Search);
			return query;
		}

		private static void ApplyCompanySearch(
			ref IQueryable<SalesCompanySale> query,
			string? search)
		{
			if (string.IsNullOrWhiteSpace(search))
			{
				return;
			}

			var pattern = $"%{search.Trim()}%";
			query = query.Where(x =>
				EF.Functions.ILike(x.InvoiceNo!, pattern) ||
				EF.Functions.ILike(x.TransactionId!, pattern) ||
				EF.Functions.ILike(x.DealerName!, pattern));
		}

		private static void ApplyWholesalerSearch(
			ref IQueryable<SalesWholesaler> query,
			string? search)
		{
			if (string.IsNullOrWhiteSpace(search))
			{
				return;
			}

			var pattern = $"%{search.Trim()}%";
			query = query.Where(x =>
				EF.Functions.ILike(x.InvoiceNo!, pattern) ||
				EF.Functions.ILike(x.TransactionId!, pattern) ||
				EF.Functions.ILike(x.AgencyName!, pattern));
		}


		private static void ApplyDptSearch(
			ref IQueryable<DptReport> query,
			string? search)
		{
			if (string.IsNullOrWhiteSpace(search))
			{
				return;
			}

			var pattern = $"%{search.Trim()}%";
			query = query.Where(x =>
				EF.Functions.ILike(x.RetailerName!, pattern) ||
				EF.Functions.ILike(x.MobileNo!, pattern));
		}

		// =====================================================================
		// Grid filters, joins and sorting
		// =====================================================================

		private static IQueryable<RawQueryRow> ApplyGridFilters(
			IQueryable<RawQueryRow> query,
			PendingAckFilter filter)
		{
			if (string.Equals(
				filter.Source,
				"Company Sales",
				StringComparison.OrdinalIgnoreCase))
			{
				query = query.Where(x => x.SourceCode == SourceCompany);
			}
			else if (string.Equals(
				filter.Source,
				"Wholesaler Sales",
				StringComparison.OrdinalIgnoreCase))
			{
				query = query.Where(x => x.SourceCode == SourceWholesaler);
			}
			else if (string.Equals(
				filter.Source,
				"DPT Sales",
				StringComparison.OrdinalIgnoreCase))
			{
				query = query.Where(x => x.SourceCode == SourceDpt);
			}
			else if (!string.IsNullOrWhiteSpace(filter.Source) &&
					 !string.Equals(
						 filter.Source,
						 "All",
						 StringComparison.OrdinalIgnoreCase))
			{
				return query.Where(x => false);
			}

			if (filter.AgeStatuses.Count > 0)
			{
				var statusCodes = ResolveStatusCodes(filter.AgeStatuses);

				if (statusCodes.Count == 0)
				{
					return query.Where(x => false);
				}

				query = query.Where(x => statusCodes.Contains(x.StatusCode));
			}

			return query;
		}

		private IQueryable<EnrichedQueryRow> BuildEnrichedQuery(
			IQueryable<RawQueryRow> query)
		{
			return
				from row in query
				join stateValue in _db.Set<State>().AsNoTracking()
					on row.StateId equals (int?)stateValue.Id into stateJoin
				from state in stateJoin.DefaultIfEmpty()

				join districtValue in _db.Set<District>().AsNoTracking()
					on row.DistrictId equals (int?)districtValue.Id into districtJoin
				from district in districtJoin.DefaultIfEmpty()

				join dealerTypeValue in _db.Set<DealerType>().AsNoTracking()
					on row.DealerTypeId equals (int?)dealerTypeValue.Id into dealerTypeJoin
				from dealerType in dealerTypeJoin.DefaultIfEmpty()

				join productValue in _db.Set<Product>().AsNoTracking()
					on row.ProductId equals (int?)productValue.Id into productJoin
				from product in productJoin.DefaultIfEmpty()

				join registrationValue in _db.Set<DealerRegistration>().AsNoTracking()
					on row.RegistrationId equals (int?)registrationValue.Id into registrationJoin
				from registration in registrationJoin.DefaultIfEmpty()

				select new EnrichedQueryRow
				{
					SourceCode = row.SourceCode,
					SalesId = row.SalesId,
					TransactionId = row.TransactionId,
					InvoiceNo = row.InvoiceNo,
					InvoiceDate = row.InvoiceDate,
					EntryDate = row.EntryDate,
					AgencyName = row.AgencyName,
					DealerCode = registration != null
						? registration.DealerCode
						: null,
					RegistrationId = row.RegistrationId,
					DealerType = row.DealerTypeName ?? (dealerType != null
						? dealerType.Name
						: null),
					MobileNo = row.MobileNo,
					StateId = row.StateId,
					StateName = state != null
						? state.StateName
						: null,
					DistrictId = row.DistrictId,
					DistrictName = district != null
						? district.DistrictName
						: null,
					ProductId = row.ProductId,
					ProductName = product != null
						? product.Name
						: null,
					QuantityMT = row.QuantityMT,
					ReceivedQuantity = row.ReceivedQuantity,
					DdNo = row.DdNo,
					DispatchNo = row.DispatchNo,
					StatusCode = row.StatusCode
				};
		}

		private static IOrderedQueryable<EnrichedQueryRow> ApplySort(
			IQueryable<EnrichedQueryRow> query,
			PendingAckFilter filter)
		{
			var column = filter.SortColumn?.Trim().ToLowerInvariant();
			var direction = filter.SortDir?.Trim().ToLowerInvariant() ?? "desc";
			var ascending = direction == "asc";

			return (column, ascending) switch
			{
				("invoiceno", true) => query
					.OrderBy(x => x.InvoiceNo)
					.ThenBy(x => x.SourceCode)
					.ThenBy(x => x.SalesId),

				("invoiceno", false) => query
					.OrderByDescending(x => x.InvoiceNo)
					.ThenByDescending(x => x.SourceCode)
					.ThenByDescending(x => x.SalesId),

				("dealer", true) => query
					.OrderBy(x => x.AgencyName)
					.ThenBy(x => x.SourceCode)
					.ThenBy(x => x.SalesId),

				("dealer", false) => query
					.OrderByDescending(x => x.AgencyName)
					.ThenByDescending(x => x.SourceCode)
					.ThenByDescending(x => x.SalesId),

				("quantity", true) => query
					.OrderBy(x => x.QuantityMT)
					.ThenBy(x => x.SourceCode)
					.ThenBy(x => x.SalesId),

				("quantity", false) => query
					.OrderByDescending(x => x.QuantityMT)
					.ThenByDescending(x => x.SourceCode)
					.ThenByDescending(x => x.SalesId),

				// Age is derived from InvoiceDate. Newer/null invoices have the
				// smallest clamped age; older invoices have the largest age.
				("age", true) => query
					.OrderByDescending(x => x.InvoiceDate == null)
					.ThenByDescending(x => x.InvoiceDate)
					.ThenByDescending(x => x.SalesId),

				("age", false) => query
					.OrderBy(x => x.InvoiceDate == null)
					.ThenBy(x => x.InvoiceDate)
					.ThenByDescending(x => x.SalesId),

				("invoicedate", true) => query
					.OrderBy(x => x.InvoiceDate == null)
					.ThenBy(x => x.InvoiceDate)
					.ThenBy(x => x.SalesId),

				_ => query
					.OrderBy(x => x.InvoiceDate == null)
					.ThenByDescending(x => x.InvoiceDate)
					.ThenByDescending(x => x.SalesId)
			};
		}

		// =====================================================================
		// Aggregate and DTO mapping helpers
		// =====================================================================

		private static PendingAckCategorySummaryDto BuildRollup(
			IEnumerable<SummaryAggregateRow> aggregates,
			int? sourceCode)
		{
			var summary = new PendingAckCategorySummaryDto();

			foreach (var item in aggregates)
			{
				if (sourceCode.HasValue && item.SourceCode != sourceCode.Value)
				{
					continue;
				}

				summary.TotalCount += item.Count;
				summary.TotalQuantity += item.Quantity;

				switch (item.StatusCode)
				{
					case StatusCompleted:
						summary.CompletedCount += item.Count;
						summary.CompletedQuantity += item.Quantity;
						break;

					case StatusLatest:
						summary.LatestCount += item.Count;
						summary.LatestQuantity += item.Quantity;
						break;

					case StatusCritical:
						summary.CriticalCount += item.Count;
						summary.CriticalQuantity += item.Quantity;
						break;

					case StatusOverdue:
						summary.OverdueCount += item.Count;
						summary.OverdueQuantity += item.Quantity;
						break;

					case StatusConsentOfBuyer:
						summary.ConsentBuyerCount += item.Count;
						summary.ConsentBuyerQuantity += item.Quantity;
						break;
				}
			}

			return summary;
		}

		private static void CopySourceCountsToOverall(
			PendingAckCategorySummaryDto overall,
			PendingAckCategorySummaryDto company,
			PendingAckCategorySummaryDto wholesaler,
			PendingAckCategorySummaryDto dpt)
		{
			overall.CompanyTotal = company.TotalCount;
			overall.CompanyCompleted = company.CompletedCount;
			overall.CompanyLatest = company.LatestCount;
			overall.CompanyCritical = company.CriticalCount;
			overall.CompanyOverdue = company.OverdueCount;
			overall.CompanyConsentBuyer = company.ConsentBuyerCount;

			overall.WholesalerTotal = wholesaler.TotalCount;
			overall.WholesalerCompleted = wholesaler.CompletedCount;
			overall.WholesalerLatest = wholesaler.LatestCount;
			overall.WholesalerCritical = wholesaler.CriticalCount;
			overall.WholesalerOverdue = wholesaler.OverdueCount;
			overall.WholesalerConsentBuyer = wholesaler.ConsentBuyerCount;

			overall.DptTotal = dpt.TotalCount;
			overall.DptCompleted = dpt.CompletedCount;
			overall.DptLatest = dpt.LatestCount;
			overall.DptCritical = dpt.CriticalCount;
			overall.DptOverdue = dpt.OverdueCount;
			overall.DptConsentBuyer = dpt.ConsentBuyerCount;
		}

		private static List<PendingAckStateWiseDto> BuildStateWise(
			IEnumerable<StateAggregateRow> aggregates)
		{
			var result = new List<PendingAckStateWiseDto>();

			foreach (var stateGroup in aggregates.GroupBy(x => new
			{
				x.StateId,
				x.StateName
			}))
			{
				var state = new PendingAckStateWiseDto
				{
					StateId = stateGroup.Key.StateId,
					StateName = stateGroup.Key.StateName ?? string.Empty
				};

				foreach (var item in stateGroup)
				{
					switch (item.StatusCode)
					{
						case StatusCompleted:
							state.CompletedCount += item.Count;
							state.CompletedQuantity += item.Quantity;
							break;

						case StatusLatest:
							state.LatestCount += item.Count;
							state.LatestQuantity += item.Quantity;
							break;

						case StatusCritical:
							state.CriticalCount += item.Count;
							state.CriticalQuantity += item.Quantity;
							break;

						case StatusOverdue:
							state.OverdueCount += item.Count;
							state.OverdueQuantity += item.Quantity;
							break;

						case StatusConsentOfBuyer:
							state.ConsentBuyerCount += item.Count;
							state.ConsentBuyerQuantity += item.Quantity;
							break;
					}
				}

				result.Add(state);
			}

			return result
				.OrderByDescending(x => x.TotalPendingQuantity)
				.ThenBy(x => x.StateName)
				.ToList();
		}

		private static PendingAckRowDto ToDto(
			EnrichedQueryRow row,
			DateTime today,
			int serialNumber)
		{
			var ageDays = CalculateAgeDays(today, row.InvoiceDate);
			var completed = row.StatusCode == StatusCompleted;
			var isDpt = row.SourceCode == SourceDpt;

			var dealerCode = !string.IsNullOrWhiteSpace(row.DealerCode)
				? row.DealerCode
				: row.RegistrationId?.ToString();

			return new PendingAckRowDto
			{
				SNo = serialNumber,
				SalesId = row.SalesId,
				Source = SourceName(row.SourceCode),
				TransactionId = row.TransactionId,
				InvoiceNo = row.InvoiceNo,
				InvoiceDate = row.InvoiceDate,
				EntryDate = row.EntryDate,
				AgencyName = row.AgencyName,
				DealerCode = dealerCode,
				DealerType = row.DealerType,
				MobileNo = string.IsNullOrWhiteSpace(row.MobileNo)
					? null
					: row.MobileNo.Trim(),
				StateId = row.StateId,
				StateName = row.StateName,
				DistrictId = row.DistrictId,
				District = row.DistrictName,
				ProductId = row.ProductId,
				ProductName = row.ProductName,
				QuantityMT = row.QuantityMT,
				ReceivedQuantity = row.ReceivedQuantity,
				DdNo = row.DdNo,
				DispatchNo = row.DispatchNo,
				PendingAckAgeDays = completed ? 0 : ageDays,
				AgeStatus = StatusName(row.StatusCode),
				WorkflowStatus = isDpt
					? "Reported"
					: completed ? "Acknowledged" : "New",
				BuyerConsentStatus = "Not Required"
			};
		}

		private static int CalculateAgeDays(
			DateTime today,
			DateTime? invoiceDate)
		{
			if (!invoiceDate.HasValue)
			{
				return 0;
			}

			var days = (int)Math.Floor(
				(today - invoiceDate.Value.Date).TotalDays);

			return Math.Max(0, days);
		}

		private static string SourceName(int sourceCode)
		{
			return sourceCode switch
			{
				SourceCompany => "Company Sales",
				SourceWholesaler => "Wholesaler Sales",
				SourceDpt => "DPT Sales",
				_ => "Unknown"
			};
		}

		private static string StatusName(int statusCode)
		{
			return statusCode switch
			{
				StatusCompleted => AgeStatus.Completed,
				StatusLatest => AgeStatus.Latest,
				StatusCritical => AgeStatus.Critical,
				StatusOverdue => AgeStatus.Overdue,
				StatusConsentOfBuyer => AgeStatus.ConsentOfBuyer,
				_ => AgeStatus.Latest
			};
		}

		private static List<int> ResolveStatusCodes(
			IEnumerable<string> statuses)
		{
			var result = new HashSet<int>();

			foreach (var rawStatus in statuses)
			{
				var status = rawStatus?.Trim();
				if (string.IsNullOrWhiteSpace(status))
				{
					continue;
				}

				if (string.Equals(
					status,
					AgeStatus.Completed,
					StringComparison.OrdinalIgnoreCase))
				{
					result.Add(StatusCompleted);
				}
				else if (
					string.Equals(status, AgeStatus.Latest, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(status, "Fresh", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
				{
					// The current Razor still contains the older Fresh/Pending
					// option names. Accept them as aliases without changing the
					// canonical service category (Latest).
					result.Add(StatusLatest);
				}
				else if (string.Equals(
					status,
					AgeStatus.Critical,
					StringComparison.OrdinalIgnoreCase))
				{
					result.Add(StatusCritical);
				}
				else if (string.Equals(
					status,
					AgeStatus.Overdue,
					StringComparison.OrdinalIgnoreCase))
				{
					result.Add(StatusOverdue);
				}
				else if (string.Equals(
					status,
					AgeStatus.ConsentOfBuyer,
					StringComparison.OrdinalIgnoreCase))
				{
					result.Add(StatusConsentOfBuyer);
				}
			}

			return result.ToList();
		}

		private static (
			List<int> RegistrationIds,
			List<int> IfmsIds) SplitDealerKeys(
			IEnumerable<string>? dealerKeys)
		{
			var registrationIds = new HashSet<int>();
			var ifmsIds = new HashSet<int>();

			foreach (var rawKey in dealerKeys ?? Enumerable.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(rawKey) || rawKey.Length < 2)
				{
					continue;
				}

				if (!int.TryParse(rawKey[1..], out var id) || id <= 0)
				{
					continue;
				}

				if (rawKey.StartsWith("R", StringComparison.OrdinalIgnoreCase))
				{
					registrationIds.Add(id);
				}
				else if (rawKey.StartsWith("I", StringComparison.OrdinalIgnoreCase))
				{
					ifmsIds.Add(id);
				}
			}

			return (
				registrationIds.ToList(),
				ifmsIds.ToList());
		}

		private static void NormalizeFilter(PendingAckFilter filter)
		{
			filter.StateIds ??= new List<int>();
			filter.DistrictIds ??= new List<int>();
			filter.DealerTypeIds ??= new List<int>();
			filter.ProductIds ??= new List<int>();
			filter.DealerKeys ??= new List<string>();
			filter.AgeStatuses ??= new List<string>();

			filter.Page = Math.Max(1, filter.Page);
			filter.PageSize = filter.PageSize <= 0 ? 16 : filter.PageSize;
		}

		// =====================================================================
		// Internal database projection types
		// =====================================================================

		private sealed class RawQueryRow
		{
			public int SourceCode { get; set; }
			public int SalesId { get; set; }
			public string? TransactionId { get; set; }
			public string? InvoiceNo { get; set; }
			public DateTime? InvoiceDate { get; set; }
			public DateTime? EntryDate { get; set; }
			public DateTime? RetailerReceiptDate { get; set; }
			public string? AgencyName { get; set; }
			public int? DealerTypeId { get; set; }
			public string? DealerTypeName { get; set; }
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? ProductId { get; set; }
			public decimal QuantityMT { get; set; }
			public decimal ReceivedQuantity { get; set; }
			public string? MobileNo { get; set; }
			public string? DdNo { get; set; }
			public string? DispatchNo { get; set; }
			public int? RegistrationId { get; set; }
			public int? IfmsId { get; set; }
			public int StatusCode { get; set; }
		}

		private sealed class EnrichedQueryRow
		{
			public int SourceCode { get; set; }
			public int SalesId { get; set; }
			public string? TransactionId { get; set; }
			public string? InvoiceNo { get; set; }
			public DateTime? InvoiceDate { get; set; }
			public DateTime? EntryDate { get; set; }
			public string? AgencyName { get; set; }
			public string? DealerCode { get; set; }
			public int? RegistrationId { get; set; }
			public string? DealerType { get; set; }
			public string? MobileNo { get; set; }
			public int? StateId { get; set; }
			public string? StateName { get; set; }
			public int? DistrictId { get; set; }
			public string? DistrictName { get; set; }
			public int? ProductId { get; set; }
			public string? ProductName { get; set; }
			public decimal QuantityMT { get; set; }
			public decimal ReceivedQuantity { get; set; }
			public string? DdNo { get; set; }
			public string? DispatchNo { get; set; }
			public int StatusCode { get; set; }
		}

		private sealed class SummaryAggregateRow
		{
			public int SourceCode { get; set; }
			public int StatusCode { get; set; }
			public int Count { get; set; }
			public decimal Quantity { get; set; }
		}

		private sealed class StateAggregateRow
		{
			public int StateId { get; set; }
			public string StateName { get; set; } = string.Empty;
			public int StatusCode { get; set; }
			public int Count { get; set; }
			public decimal Quantity { get; set; }
		}
	}
}