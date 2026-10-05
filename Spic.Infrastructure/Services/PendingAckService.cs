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
	/// Business flow is preserved and clarified:
	/// - Company Sales + Wholesaler Sales + Retailer Sales are combined.
	/// - Retailer Sales comes from DptReport.SoldQuantity, but the UI never displays
	///   the internal DPT name. Its source is "Retailer Sales" and dealer type is "Retailer".
	/// - Pending age is based on InvoiceDate; Retailer Sales uses the DPT report date
	///   stored in CreatedAt.
	/// - Age status: Latest 0-10, Critical 11-20, Overdue 21+, or Consent of Buyer.
	/// - Completed is retained for acknowledged rows to preserve the existing dashboard.
	/// - The last grid status column displays the exact Status master name uploaded
	///   from Excel (for example New or Ack). Retailer Sales has no StatusId, so it
	///   uses the existing New fallback instead of introducing a synthetic status.
	/// - Age-status selections apply to cards, the state chart, grid and export.
	/// - Source tabs continue to affect the grid/export only, preserving the existing
	///   source-card comparison behaviour.
	/// - Search still affects cards, chart and grid.
	///
	/// Main performance improvement:
	/// the service no longer loads every matching sale and every lookup master
	/// into memory before paging. Aggregation, filtering, sorting and paging are
	/// executed by the database.
	/// </summary>
	public sealed class PendingAckService : IPendingAckService
	{
		private static readonly HashSet<string> InvalidDealerNameTokens =
			new(StringComparer.OrdinalIgnoreCase)
			{
				"0", "00", "000", "0000",
				"NA", "N/A", "NONE", "NULL", "NIL",
				"NOT AVAILABLE", "UNKNOWN", "-", "--", "."
			};

		private static readonly char[] DealerNameEdgeCharacters =
			".,;:-_/\\|\"'`~*#".ToCharArray();

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

			var workflowRules = await LoadWorkflowStatusRulesAsync(cancellationToken);

			// Build all three sources for the dashboard. Source tabs remain grid-only,
			// while the explicit Status (By Age) filter must affect cards and chart too.
			var baseQuery = BuildRawQuery(
				filter,
				today,
				includeAllSources: true,
				workflowRules.AckStatusIds,
				workflowRules.ConsentStatusIds);

			var dashboardQuery = ApplyAgeStatusFilter(
				baseQuery,
				filter.AgeStatuses);

			// Query 1: compact summary aggregates only.
			var summaryAggregates = await dashboardQuery
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
				from row in dashboardQuery
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

			// Grid applies the active source tab and the same status selections.
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
			var workflowRules = await LoadWorkflowStatusRulesAsync(cancellationToken);

			// Export does not require card/chart data. When a source tab is
			// selected, skip querying the other two sources completely.
			var rawQuery = BuildRawQuery(
				filter,
				today,
				includeAllSources: false,
				workflowRules.AckStatusIds,
				workflowRules.ConsentStatusIds);

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

		public async Task<List<PendingAckProductDto>> GetProductsAsync(
			CancellationToken cancellationToken = default)
		{
			// Keep the existing Product master and add IFMS products as a second
			// typed source. The prefixes prevent identical numeric IDs in the two
			// tables from being treated as the same product.
			var products = await _db.Set<Product>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != string.Empty)
				.Select(x => new PendingAckProductDto
				{
					Key = "P:" + x.Id,
					Name = x.Name!,
					Source = "Product"
				})
				.ToListAsync(cancellationToken);

			var ifmsProducts = await _db.Set<IfmsProduct>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != string.Empty)
				.Select(x => new PendingAckProductDto
				{
					Key = "I:" + x.Id,
					Name = x.Name! + " (IFMS)",
					Source = "IFMS"
				})
				.ToListAsync(cancellationToken);

			return products
				.Concat(ifmsProducts)
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public async Task<List<PendingAckDealerDto>> GetDealersAsync(
			CancellationToken cancellationToken = default)
		{
			// Read only the required columns in SQL. Cleaning and validation are done
			// in memory because the helper methods are not SQL-translatable.
			var registeredDealers = await _db.Set<DealerRegistration>()
				.AsNoTracking()
				.Where(x => x.FirmName != null && x.FirmName != string.Empty)
				.Select(x => new
				{
					Key = "R" + x.Id,
					RawName = x.FirmName
				})
				.ToListAsync(cancellationToken);

			var ifmsDealers = await _db.Set<IfmsDealer>()
				.AsNoTracking()
				.Where(x => x.Name != null && x.Name != string.Empty)
				.Select(x => new
				{
					Key = "I" + x.Id,
					RawName = x.Name
				})
				.ToListAsync(cancellationToken);

			return registeredDealers
				.Concat(ifmsDealers)
				.Select(x => new PendingAckDealerDto
				{
					Key = x.Key,
					Name = CleanDealerName(x.RawName)
				})
				.Where(x => IsValidDealerName(x.Name))
				.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First())
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		// =====================================================================
		// Base SQL query
		// =====================================================================

		private IQueryable<RawQueryRow> BuildRawQuery(
			PendingAckFilter filter,
			DateTime today,
			bool includeAllSources,
			List<int> ackStatusIds,
			List<int> consentStatusIds)
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
			var sourceIsDpt =
				string.Equals(
					source,
					"Retailer Sales",
					StringComparison.OrdinalIgnoreCase) ||
				string.Equals(
					source,
					"DPT Sales",
					StringComparison.OrdinalIgnoreCase); // backwards-compatible alias

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
					IfmsProductId = x.IfmsProductId,
					QuantityMT = x.QuantityMT,
					ReceivedQuantity = x.ReceivedQuantity,
					MobileNo = x.MobileNo,
					DdNo = x.DdNo,
					DispatchNo = (string?)null,
					RegistrationId = x.DealerRegistrationId,
					IfmsId = x.IfmsDealerId,
					WorkflowStatusId = x.StatusId,
					WorkflowStatusOverride = null,
					StatusCode =
						(x.StatusId.HasValue && ackStatusIds.Contains(x.StatusId.Value)) ||
						x.RetailerReceiptDate != null
							? StatusCompleted
							: x.StatusId.HasValue && consentStatusIds.Contains(x.StatusId.Value)
								? StatusConsentOfBuyer
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
					IfmsProductId = x.IfmsProductId,
					QuantityMT = x.QuantityMT,
					ReceivedQuantity = x.ReceivedQuantityMT,
					MobileNo = x.MobileNo,
					DdNo = (string?)null,
					DispatchNo = x.DispatchNo,
					RegistrationId = x.DealerId,
					IfmsId = x.IfmsDealerId,
					WorkflowStatusId = x.StatusId,
					WorkflowStatusOverride = null,
					StatusCode =
						(x.StatusId.HasValue && ackStatusIds.Contains(x.StatusId.Value)) ||
						x.RetailerReceiptDate != null
							? StatusCompleted
							: x.StatusId.HasValue && consentStatusIds.Contains(x.StatusId.Value)
								? StatusConsentOfBuyer
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

				// DptReport contains retailer SoldQuantity and a business report date, but
				// it has no StatusId or acknowledgement date. It is presented as Retailer
				// Sales, with pending age calculated from CreatedAt.
				dptQuery = query.Select(x => new RawQueryRow
				{
					SourceCode = SourceDpt,
					SalesId = x.Id,
					TransactionId = null,
					InvoiceNo = null,
					InvoiceDate = x.CreatedAt,
					EntryDate = x.CreatedAt,
					RetailerReceiptDate = null,
					AgencyName = x.RetailerName,
					DealerTypeId = null,
					DealerTypeName = "Retailer",
					StateId = x.StateId,
					DistrictId = x.DistrictId,
					ProductId = x.ProductId,
					IfmsProductId = x.IfmsProductId,
					QuantityMT = x.SoldQuantity,
					ReceivedQuantity = 0m,
					MobileNo = x.MobileNo,
					DdNo = null,
					DispatchNo = null,
					RegistrationId = x.DealerRegistrationId,
					IfmsId = x.IfmsDealerId,
					WorkflowStatusId = null,
					// DPT has no workflow StatusId. Leave the override null so ToDto
					// uses the existing non-completed fallback: "New".
					WorkflowStatusOverride = null,
					StatusCode = x.CreatedAt >= latestFrom
						? StatusLatest
						: x.CreatedAt >= criticalFrom
							? StatusCritical
							: StatusOverdue
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

			var (productIds, ifmsProductIds) = SplitProductKeys(filter);

			if (productIds.Count > 0 || ifmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					(x.ProductId.HasValue &&
					 productIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue &&
					 ifmsProductIds.Contains(x.IfmsProductId.Value)));
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

			var (productIds, ifmsProductIds) = SplitProductKeys(filter);

			if (productIds.Count > 0 || ifmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					(x.ProductId.HasValue &&
					 productIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue &&
					 ifmsProductIds.Contains(x.IfmsProductId.Value)));
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

		private IQueryable<DptReport> ApplyDptFilters(
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

			var (productIds, ifmsProductIds) = SplitProductKeys(filter);

			if (productIds.Count > 0 || ifmsProductIds.Count > 0)
			{
				query = query.Where(x =>
					(x.ProductId.HasValue &&
					 productIds.Contains(x.ProductId.Value)) ||
					(x.IfmsProductId.HasValue &&
					 ifmsProductIds.Contains(x.IfmsProductId.Value)));
			}

			if (registrationIds.Count > 0 || ifmsIds.Count > 0)
			{
				query = query.Where(x =>
					(x.DealerRegistrationId.HasValue &&
					 registrationIds.Contains(x.DealerRegistrationId.Value)) ||
					(x.IfmsDealerId.HasValue &&
					 ifmsIds.Contains(x.IfmsDealerId.Value)));
			}

			// DptReport has no DealerTypeId, but this source represents Retailer Sales.
			// When Dealer Type is filtered, keep DPT only if one of the selected
			// DealerType master rows is retailer-like. The EXISTS stays database-side
			// and avoids loading the dealer-type master into memory.
			if (filter.DealerTypeIds.Count > 0)
			{
				var selectedDealerTypeIds = filter.DealerTypeIds;
				var selectedRetailerTypes = _db.Set<DealerType>()
					.AsNoTracking()
					.Where(x =>
						selectedDealerTypeIds.Contains(x.Id) &&
						x.Name != null &&
						EF.Functions.ILike(x.Name, "%retail%"));

				query = query.Where(_ => selectedRetailerTypes.Any());
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

		private static IQueryable<RawQueryRow> ApplyAgeStatusFilter(
			IQueryable<RawQueryRow> query,
			IEnumerable<string>? ageStatuses)
		{
			var selectedStatuses = (ageStatuses ?? Enumerable.Empty<string>())
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (selectedStatuses.Count == 0)
			{
				return query;
			}

			var statusCodes = ResolveStatusCodes(selectedStatuses);
			if (statusCodes.Count == 0)
			{
				return query.Where(x => false);
			}

			return query.Where(x => statusCodes.Contains(x.StatusCode));
		}

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
			else if (
				string.Equals(
					filter.Source,
					"Retailer Sales",
					StringComparison.OrdinalIgnoreCase) ||
				string.Equals(
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

			return ApplyAgeStatusFilter(query, filter.AgeStatuses);
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

				join ifmsProductValue in _db.Set<IfmsProduct>().AsNoTracking()
					on row.IfmsProductId equals (int?)ifmsProductValue.Id into ifmsProductJoin
				from ifmsProduct in ifmsProductJoin.DefaultIfEmpty()

				join registrationValue in _db.Set<DealerRegistration>().AsNoTracking()
					on row.RegistrationId equals (int?)registrationValue.Id into registrationJoin
				from registration in registrationJoin.DefaultIfEmpty()

				join workflowStatusValue in _db.Set<Status>().AsNoTracking()
					on row.WorkflowStatusId equals (int?)workflowStatusValue.Id into workflowStatusJoin
				from workflowStatus in workflowStatusJoin.DefaultIfEmpty()

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
					IfmsProductId = row.IfmsProductId,
					ProductName = product != null
						? product.Name
						: ifmsProduct != null
							? ifmsProduct.Name
							: null,
					QuantityMT = row.QuantityMT,
					ReceivedQuantity = row.ReceivedQuantity,
					DdNo = row.DdNo,
					DispatchNo = row.DispatchNo,
					StatusCode = row.StatusCode,
					WorkflowStatusName = row.WorkflowStatusOverride ??
						(workflowStatus != null ? workflowStatus.Name : null)
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

			var workflowStatus = !string.IsNullOrWhiteSpace(row.WorkflowStatusName)
				? row.WorkflowStatusName.Trim()
				: completed
					? "Ack"
					: "New";

			var dealerCode = !string.IsNullOrWhiteSpace(row.DealerCode)
				? row.DealerCode
				: row.RegistrationId?.ToString();

			var cleanedDealerName = CleanDealerName(row.AgencyName);
			var displayDealerName = IsValidDealerName(cleanedDealerName)
				? cleanedDealerName
				: "Unknown Dealer";

			return new PendingAckRowDto
			{
				SNo = serialNumber,
				SalesId = row.SalesId,
				Source = SourceName(row.SourceCode),
				TransactionId = row.TransactionId,
				InvoiceNo = row.InvoiceNo,
				InvoiceDate = row.InvoiceDate,
				EntryDate = row.EntryDate,
				AgencyName = displayDealerName,
				DealerCode = dealerCode,
				DealerType = row.SourceCode == SourceDpt
					? "Retailer"
					: row.DealerType,
				MobileNo = string.IsNullOrWhiteSpace(row.MobileNo)
					? null
					: row.MobileNo.Trim(),
				StateId = row.StateId,
				StateName = row.StateName,
				DistrictId = row.DistrictId,
				District = row.DistrictName,
				ProductId = row.ProductId,
				IfmsProductId = row.IfmsProductId,
				ProductName = row.ProductName,
				QuantityMT = row.QuantityMT,
				ReceivedQuantity = row.ReceivedQuantity,
				DdNo = row.DdNo,
				DispatchNo = row.DispatchNo,
				PendingAckAgeDays = completed ? 0 : ageDays,
				AgeStatus = StatusName(row.StatusCode),
				WorkflowStatus = workflowStatus,
				BuyerConsentStatus = row.StatusCode == StatusConsentOfBuyer
					? "Required"
					: "Not Required"
			};
		}

		private async Task<WorkflowStatusRules> LoadWorkflowStatusRulesAsync(
			CancellationToken cancellationToken)
		{
			var statuses = await _db.Set<Status>()
				.AsNoTracking()
				.Select(x => new { x.Id, x.Name })
				.ToListAsync(cancellationToken);

			var ackStatusIds = statuses
				.Where(x =>
					string.Equals(x.Name?.Trim(), "Ack", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(x.Name?.Trim(), "Acknowledged", StringComparison.OrdinalIgnoreCase))
				.Select(x => x.Id)
				.ToList();

			var consentStatusIds = statuses
				.Where(x =>
					!string.IsNullOrWhiteSpace(x.Name) &&
					x.Name.IndexOf("consent", StringComparison.OrdinalIgnoreCase) >= 0)
				.Select(x => x.Id)
				.ToList();

			return new WorkflowStatusRules
			{
				AckStatusIds = ackStatusIds,
				ConsentStatusIds = consentStatusIds
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
				SourceDpt => "Retailer Sales",
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
			List<int> ProductIds,
			List<int> IfmsProductIds) SplitProductKeys(
			PendingAckFilter filter)
		{
			// ProductIds is retained for backward compatibility with older clients.
			var productIds = new HashSet<int>(
				filter.ProductIds.Where(id => id > 0));
			var ifmsProductIds = new HashSet<int>();

			foreach (var rawKey in filter.ProductKeys ?? Enumerable.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(rawKey))
				{
					continue;
				}

				var key = rawKey.Trim();
				var colonIndex = key.IndexOf(':');
				var prefix = colonIndex >= 0
					? key[..colonIndex]
					: key[..1];
				var idText = colonIndex >= 0
					? key[(colonIndex + 1)..]
					: key[1..];

				if (!int.TryParse(idText, out var id) || id <= 0)
				{
					continue;
				}

				if (string.Equals(prefix, "P", StringComparison.OrdinalIgnoreCase))
				{
					productIds.Add(id);
				}
				else if (string.Equals(prefix, "I", StringComparison.OrdinalIgnoreCase))
				{
					ifmsProductIds.Add(id);
				}
			}

			return (
				productIds.ToList(),
				ifmsProductIds.ToList());
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
				if (string.IsNullOrWhiteSpace(rawKey))
				{
					continue;
				}

				var key = rawKey.Trim();
				var colonIndex = key.IndexOf(':');
				var prefix = colonIndex >= 0
					? key[..colonIndex]
					: key[..1];
				var idText = colonIndex >= 0
					? key[(colonIndex + 1)..]
					: key[1..];

				if (!int.TryParse(idText, out var id) || id <= 0)
				{
					continue;
				}

				if (string.Equals(prefix, "R", StringComparison.OrdinalIgnoreCase))
				{
					registrationIds.Add(id);
				}
				else if (string.Equals(prefix, "I", StringComparison.OrdinalIgnoreCase))
				{
					ifmsIds.Add(id);
				}
			}

			return (
				registrationIds.ToList(),
				ifmsIds.ToList());
		}

		private static string CleanDealerName(string? rawName)
		{
			if (string.IsNullOrWhiteSpace(rawName))
				return string.Empty;

			var withoutControls = new string(rawName
				.Where(character => !char.IsControl(character) &&
					character != '\uFEFF' &&
					character != '\u200B')
				.ToArray());

			var normalizedSpacing = string.Join(
				" ",
				withoutControls.Split(
					new[] { ' ', '\t', '\r', '\n' },
					StringSplitOptions.RemoveEmptyEntries));

			return normalizedSpacing
				.Trim()
				.Trim(DealerNameEdgeCharacters)
				.Trim();
		}

		private static bool IsValidDealerName(string? dealerName)
		{
			var cleaned = CleanDealerName(dealerName);
			return cleaned.Length >= 2 &&
				!InvalidDealerNameTokens.Contains(cleaned) &&
				cleaned.Any(char.IsLetter);
		}

		private static void NormalizeFilter(PendingAckFilter filter)
		{
			filter.StateIds ??= new List<int>();
			filter.DistrictIds ??= new List<int>();
			filter.DealerTypeIds ??= new List<int>();
			filter.ProductIds ??= new List<int>();
			filter.ProductKeys ??= new List<string>();
			filter.DealerKeys ??= new List<string>();
			filter.AgeStatuses ??= new List<string>();

			filter.Page = Math.Max(1, filter.Page);
			filter.PageSize = filter.PageSize <= 0
				? 16
				: Math.Min(filter.PageSize, 500);
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
			public int? IfmsProductId { get; set; }
			public decimal QuantityMT { get; set; }
			public decimal ReceivedQuantity { get; set; }
			public string? MobileNo { get; set; }
			public string? DdNo { get; set; }
			public string? DispatchNo { get; set; }
			public int? RegistrationId { get; set; }
			public int? IfmsId { get; set; }
			public int? WorkflowStatusId { get; set; }
			public string? WorkflowStatusOverride { get; set; }
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
			public int? IfmsProductId { get; set; }
			public string? ProductName { get; set; }
			public decimal QuantityMT { get; set; }
			public decimal ReceivedQuantity { get; set; }
			public string? DdNo { get; set; }
			public string? DispatchNo { get; set; }
			public int StatusCode { get; set; }
			public string? WorkflowStatusName { get; set; }
		}

		private sealed class WorkflowStatusRules
		{
			public List<int> AckStatusIds { get; set; } = new();
			public List<int> ConsentStatusIds { get; set; } = new();
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