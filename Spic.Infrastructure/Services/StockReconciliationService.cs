using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.MasterData;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spic.Infrastructure.Services
{
	/// <summary>
	/// State-wise Global Stock Reconciliation.
	///
	/// ONE pipeline: uploaded reports -> parse -> validate -> normalize state ->
	/// aggregate every source -> merge by state -> StockReconciliationResult.
	/// The screen, the workbook and the email all read that one result, so the
	/// three can never show different numbers.
	///
	/// Nothing is written to the database. The daily report files are read and
	/// aggregated on the fly; the existing State / Status masters are only READ
	/// (to resolve a state name to its canonical spelling and a status name to
	/// its master entry) and no master row is ever created.
	/// </summary>
	public class StockReconciliationService : IStockReconciliationService
	{
		private const int MaxHeaderScanRows = 50;
		private const int MaxColumns = 100;

		private const string GlobalStockLabel = "Global Stock";
		private const string SalesReceiptLabel = "Sales & Receipt";
		private const string RetailPointLabel = "Retailer Stock / DPT";
		private const string CompanySalesLabel = "Company Sales";
		private const string WholesalerSalesLabel = "Wholesaler Sales";

		private const string NewStatus = "NEW";
		private const string AckStatus = "ACK";

		private readonly AppDbContext _db;
		private readonly IProductStockAvailabilityService _productStockService;
		private readonly ILogger<StockReconciliationService> _logger;

		public StockReconciliationService(
			AppDbContext db,
			IProductStockAvailabilityService productStockService,
			ILogger<StockReconciliationService> logger)
		{
			_db = db;
			_productStockService = productStockService;
			_logger = logger;
		}

		// ------------------------------------------------------------------ public

		public async Task<StockReconciliationResult> BuildAsync(
			StockReconciliationInput input,
			DateTime asOnDate,
			CancellationToken cancellationToken = default)
		{
			var errors = new List<string>();
			var warnings = new List<string>();

			// The masters are read ONCE, before any row is looked at, so a file with
			// thousands of rows never turns into thousands of queries.
			var states = await LoadStatesAsync(cancellationToken);
			var statuses = await LoadStatusesAsync(cancellationToken);

			var amounts = new Dictionary<string, StateAmounts>(StringComparer.Ordinal);

			string? company = null;
			string? plant = null;
			string? product = null;

			if (input.GlobalStock is not null)
			{
				var meta = ReadGlobalStock(input.GlobalStock, states, amounts, errors, warnings, cancellationToken);
				plant = Meta(plant, meta.Plant);
				product = Meta(product, meta.Product);
			}

			if (input.SalesReceipt is not null)
			{
				var meta = ReadSalesAndReceipt(input.SalesReceipt, states, amounts, errors, warnings, cancellationToken);
				company = Meta(company, meta.Company);
				plant = Meta(plant, meta.Plant);
				product = Meta(product, meta.Product);
			}

			foreach (var file in input.RetailPoint)
			{
				ReadRetailPoint(file, states, amounts, errors, warnings, cancellationToken);
			}

			if (input.CompanySales is not null)
			{
				ReadSales(input.CompanySales, ColumnKind.CompanySalesInGit, states, statuses, amounts, errors, warnings, asOnDate, cancellationToken);
			}

			if (input.WholesalerSales is not null)
			{
				ReadSales(input.WholesalerSales, ColumnKind.WholesalerSalesInGit, states, statuses, amounts, errors, warnings, asOnDate, cancellationToken);
			}

			if (errors.Count > 0)
			{
				throw new StockReconciliationValidationException(
					errors.Count == 1
						? errors[0]
						: $"The uploaded files could not be reconciled ({errors.Count} problems).",
					errors);
			}

			var rows = amounts.Values
				.OrderBy(x => x.State, StringComparer.OrdinalIgnoreCase)
				.Select(x => new StockReconciliationRow
				{
					State = x.State,
					ClosingStock = x.ClosingStock,
					ClosingGit = x.ClosingGit,
					GlobalStockTotal = x.ClosingStock + x.ClosingGit,
					WholesalerStock = x.WholesalerStock,
					RetailPointStock = x.RetailPointStock,
					CompanySalesInGit = x.CompanySalesInGit,
					WholesalerSalesInGit = x.WholesalerSalesInGit,
					ChannelTotal = x.WholesalerStock + x.RetailPointStock + x.CompanySalesInGit + x.WholesalerSalesInGit,
					OverallStock = (x.ClosingStock + x.ClosingGit) + (x.WholesalerStock + x.RetailPointStock + x.CompanySalesInGit + x.WholesalerSalesInGit)
				})
				.ToList();

			var totals = new StockReconciliationTotals
			{
				ClosingStock = rows.Sum(r => r.ClosingStock),
				ClosingGit = rows.Sum(r => r.ClosingGit),
				GlobalStockTotal = rows.Sum(r => r.GlobalStockTotal),
				WholesalerStock = rows.Sum(r => r.WholesalerStock),
				RetailPointStock = rows.Sum(r => r.RetailPointStock),
				CompanySalesInGit = rows.Sum(r => r.CompanySalesInGit),
				WholesalerSalesInGit = rows.Sum(r => r.WholesalerSalesInGit),
				ChannelTotal = rows.Sum(r => r.ChannelTotal),
				OverallStock = rows.Sum(r => r.OverallStock)
			};

			var result = new StockReconciliationResult
			{
				AsOnDate = asOnDate.Date,
				Company = company ?? string.Empty,
				Plant = plant ?? string.Empty,
				Product = product ?? string.Empty,
				Rows = rows,
				Totals = totals,
				Warnings = warnings,
				FileName = BuildFileName(asOnDate)
			};

			_logger.LogInformation(
				"State-wise global stock reconciliation built for {AsOnDate:yyyy-MM-dd}: {States} states, " +
				"global stock {GlobalStock}, channel {Channel}.",
				result.AsOnDate, rows.Count, totals.GlobalStockTotal, totals.ChannelTotal);

			foreach (var warning in warnings)
				_logger.LogWarning("Reconciliation warning: {Warning}", warning);

			return result;
		}

		public async Task<StockReconciliationFilterOptions> GetFilterOptionsAsync(
			CancellationToken cancellationToken = default)
		{
			var companies = await _db.Companies
				.AsNoTracking()
				.Where(c => c.IsActive)
				.OrderBy(c => c.Name)
				.Select(c => new StockReconciliationFilterItem { Id = c.Id, Name = c.Name })
				.ToListAsync(cancellationToken);

			var plants = await _db.Plants
				.AsNoTracking()
				.Where(p => p.IsActive)
				.OrderBy(p => p.Name)
				.Select(p => new StockReconciliationFilterItem { Id = p.Id, Name = p.Name })
				.ToListAsync(cancellationToken);

			var products = await _db.Products
				.AsNoTracking()
				.Where(p => p.IsActive)
				.OrderBy(p => p.Name)
				.Select(p => new StockReconciliationFilterItem { Id = p.Id, Name = p.Name })
				.ToListAsync(cancellationToken);

			var dates = await _db.StateGlobalStockReconciliations
				.AsNoTracking()
				.Select(r => r.CreatedAt.Date)
				.Distinct()
				.OrderByDescending(d => d)
				.Take(30)
				.ToListAsync(cancellationToken);

			if (dates.Count == 0)
			{
				dates = await _db.SalesAndReceipts
					.AsNoTracking()
					.Select(r => r.CreatedAt.Date)
					.Distinct()
					.OrderByDescending(d => d)
					.Take(30)
					.ToListAsync(cancellationToken);
			}

			return new StockReconciliationFilterOptions
			{
				Companies = companies,
				Plants = plants,
				Products = products,
				AvailableDates = dates
			};
		}

		public async Task<StockReconciliationResult> BuildFromDatabaseAsync(
			StockReconciliationDatabaseQueryRequest request,
			CancellationToken cancellationToken = default)
		{
			if (request is null)
				throw new ArgumentNullException(nameof(request));

			// Current local business date is the primary operational mode.
			var businessDate = (request.AsOnDate ?? DateTime.Today).Date;

			// Indian Standard Time (IST, UTC+05:30) business day window:
			// 00:00:00 IST to 23:59:59 IST corresponds to [businessDate - 5.5h, businessDate + 18.5h] in UTC.
			// This matches CreatedAt (timestamp with time zone) stored during normal file uploads.
			var utcStart = DateTime.SpecifyKind(businessDate.AddHours(-5.5), DateTimeKind.Utc);
			var utcEnd = DateTime.SpecifyKind(businessDate.AddHours(18.5), DateTimeKind.Utc);

			// EntryDate in SalesCompanySales and SalesWholesalers:
			var entryDateStart = DateTime.SpecifyKind(businessDate, DateTimeKind.Utc);
			var entryDateEnd = entryDateStart.AddDays(1);

			// Canonical state lookup: State master is canonical source
			var states = await _db.States
				.AsNoTracking()
				.ToDictionaryAsync(s => s.Id, s => s.StateName, cancellationToken);

			// Status master lookup - resolve dynamically without hardcoding status IDs
			var statuses = await LoadStatusesAsync(cancellationToken);
			var allStatuses = await _db.Statuses
				.AsNoTracking()
				.Where(s => s.Name != null)
				.ToListAsync(cancellationToken);

			var newStatusIds = allStatuses
				.Where(s => statuses.Resolve(s.Name) == NewStatus)
				.Select(s => s.Id)
				.ToList();

			var ackStatusIds = allStatuses
				.Where(s => statuses.Resolve(s.Name) == AckStatus)
				.Select(s => s.Id)
				.ToList();

			// 1. Global Stock from StateGlobalStockReconciliation
			var reconQuery = _db.StateGlobalStockReconciliations
				.AsNoTracking()
				.Where(x => x.CreatedAt >= utcStart && x.CreatedAt < utcEnd);

			if (request.PlantId.HasValue && request.PlantId.Value > 0)
				reconQuery = reconQuery.Where(x => x.PlantId == request.PlantId.Value);

			if (request.ProductId.HasValue && request.ProductId.Value > 0)
				reconQuery = reconQuery.Where(x => x.ProductId == request.ProductId.Value || x.IfmsProductId == request.ProductId.Value);

			var reconGroups = await reconQuery
				.GroupBy(x => x.StateId)
				.Select(g => new
				{
					StateId = g.Key,
					ClosingStock = g.Sum(x => x.ClosingStock),
					ClosingGit = g.Sum(x => x.ClosingGIT)
				})
				.ToListAsync(cancellationToken);

			// 2. Wholesaler Stock from SalesAndReceipt (BalanceWithWs > 0 strictly)
			var srQuery = _db.SalesAndReceipts
				.AsNoTracking()
				.Where(x => x.CreatedAt >= utcStart && x.CreatedAt < utcEnd && x.BalanceWithWs > 0);

			if (request.CompanyId.HasValue && request.CompanyId.Value > 0)
				srQuery = srQuery.Where(x => x.CompanyId == request.CompanyId.Value);

			if (request.PlantId.HasValue && request.PlantId.Value > 0)
				srQuery = srQuery.Where(x => x.PlantId == request.PlantId.Value);

			if (request.ProductId.HasValue && request.ProductId.Value > 0)
				srQuery = srQuery.Where(x => x.ProductId == request.ProductId.Value || x.IfmsProductId == request.ProductId.Value);

			var srGroups = await srQuery
				.GroupBy(x => x.StateId)
				.Select(g => new
				{
					StateId = g.Key,
					WholesalerStock = g.Sum(x => x.BalanceWithWs)
				})
				.ToListAsync(cancellationToken);

			// 3. Retail Point Stock from DptReport (aggregates across multiple state reports)
			var dptQuery = _db.DptReports
				.AsNoTracking()
				.Where(x => x.CreatedAt >= utcStart && x.CreatedAt < utcEnd);

			if (request.CompanyId.HasValue && request.CompanyId.Value > 0)
				dptQuery = dptQuery.Where(x => x.CompanyId == request.CompanyId.Value);

			if (request.PlantId.HasValue && request.PlantId.Value > 0)
				dptQuery = dptQuery.Where(x => x.PlantId == request.PlantId.Value);

			if (request.ProductId.HasValue && request.ProductId.Value > 0)
				dptQuery = dptQuery.Where(x => x.ProductId == request.ProductId.Value || x.IfmsProductId == request.ProductId.Value);

			var dptGroups = await dptQuery
				.GroupBy(x => x.StateId)
				.Select(g => new
				{
					StateId = g.Key,
					RetailPointStock = g.Sum(x => x.ClosingBalance)
				})
				.ToListAsync(cancellationToken);

			// 4. Company Sales in GIT from SalesCompanySale
			// include NEW OR include ACK only when EntryDate matches asOnDate
			var compQuery = _db.SalesCompanySales
				.AsNoTracking()
				.AsQueryable();

			if (request.CompanyId.HasValue && request.CompanyId.Value > 0)
				compQuery = compQuery.Where(x => x.ManufacturerId == request.CompanyId.Value || x.MarketerId == request.CompanyId.Value);

			if (request.PlantId.HasValue && request.PlantId.Value > 0)
				compQuery = compQuery.Where(x => x.PlantId == request.PlantId.Value);

			if (request.ProductId.HasValue && request.ProductId.Value > 0)
				compQuery = compQuery.Where(x => x.ProductId == request.ProductId.Value || x.IfmsProductId == request.ProductId.Value);

			compQuery = compQuery.Where(x =>
				(x.StatusId.HasValue && newStatusIds.Contains(x.StatusId.Value)) ||
				(x.StatusId.HasValue && ackStatusIds.Contains(x.StatusId.Value) &&
				 x.EntryDate.HasValue && x.EntryDate.Value >= entryDateStart && x.EntryDate.Value < entryDateEnd));

			var compGroups = await compQuery
				.GroupBy(x => x.StateId)
				.Select(g => new
				{
					StateId = g.Key,
					CompanySalesInGit = g.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken);

			// 5. Wholesaler Sales in GIT from SalesWholesaler
			// include NEW OR include ACK only when EntryDate matches asOnDate
			var wsQuery = _db.SalesWholesalers
				.AsNoTracking()
				.AsQueryable();

			if (request.CompanyId.HasValue && request.CompanyId.Value > 0)
				wsQuery = wsQuery.Where(x => x.ManufacturerId == request.CompanyId.Value || x.MarketerId == request.CompanyId.Value);

			if (request.PlantId.HasValue && request.PlantId.Value > 0)
				wsQuery = wsQuery.Where(x => x.PlantId == request.PlantId.Value);

			if (request.ProductId.HasValue && request.ProductId.Value > 0)
				wsQuery = wsQuery.Where(x => x.ProductId == request.ProductId.Value || x.IfmsProductId == request.ProductId.Value);

			wsQuery = wsQuery.Where(x =>
				(x.StatusId.HasValue && newStatusIds.Contains(x.StatusId.Value)) ||
				(x.StatusId.HasValue && ackStatusIds.Contains(x.StatusId.Value) &&
				 x.EntryDate.HasValue && x.EntryDate.Value >= entryDateStart && x.EntryDate.Value < entryDateEnd));

			var wsGroups = await wsQuery
				.GroupBy(x => x.StateId)
				.Select(g => new
				{
					StateId = g.Key,
					WholesalerSalesInGit = g.Sum(x => x.QuantityMT)
				})
				.ToListAsync(cancellationToken);

			// Track which source datasets have been uploaded for this business date
			var hasGlobal = reconGroups.Count > 0;
			var hasSr = srGroups.Count > 0;
			var hasDpt = dptGroups.Count > 0;
			var hasComp = compGroups.Count > 0;
			var hasWs = wsGroups.Count > 0;

			var missingSources = new List<string>();
			if (!hasGlobal) missingSources.Add($"Global Stock data is not available for {businessDate:dd-MM-yyyy}.");
			if (!hasSr) missingSources.Add($"Sales & Receipt data is not available for {businessDate:dd-MM-yyyy}.");
			if (!hasDpt) missingSources.Add($"Retail Point Stock (DPT) data is not available for {businessDate:dd-MM-yyyy}.");
			if (!hasComp) missingSources.Add($"Company Sales in GIT data is not available for {businessDate:dd-MM-yyyy}.");
			if (!hasWs) missingSources.Add($"Wholesaler Sales in GIT data is not available for {businessDate:dd-MM-yyyy}.");

			// Merge state-wise
			var stateMap = new Dictionary<string, StateAmounts>(StringComparer.OrdinalIgnoreCase);

			StateAmounts GetOrCreate(int? stateId, bool plantFallback = false)
			{
				string name;
				if (stateId.HasValue && states.TryGetValue(stateId.Value, out var sName) && !string.IsNullOrWhiteSpace(sName))
				{
					name = sName.Trim();
				}
				else
				{
					name = plantFallback ? "Plant" : "Other / Unassigned";
				}

				if (!stateMap.TryGetValue(name, out var amt))
				{
					amt = new StateAmounts { State = name };
					stateMap[name] = amt;
				}
				return amt;
			}

			foreach (var item in reconGroups)
			{
				var amt = GetOrCreate(item.StateId, plantFallback: !item.StateId.HasValue);
				amt.ClosingStock += item.ClosingStock;
				amt.ClosingGit += item.ClosingGit;
			}

			foreach (var item in srGroups)
			{
				var amt = GetOrCreate(item.StateId);
				amt.WholesalerStock += item.WholesalerStock;
			}

			foreach (var item in dptGroups)
			{
				var amt = GetOrCreate(item.StateId);
				amt.RetailPointStock += item.RetailPointStock;
			}

			foreach (var item in compGroups)
			{
				var amt = GetOrCreate(item.StateId);
				amt.CompanySalesInGit += item.CompanySalesInGit;
			}

			foreach (var item in wsGroups)
			{
				var amt = GetOrCreate(item.StateId);
				amt.WholesalerSalesInGit += item.WholesalerSalesInGit;
			}

			var rows = stateMap.Values
				.OrderBy(x => x.State.Equals("Plant", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
				.ThenBy(x => x.State, StringComparer.OrdinalIgnoreCase)
				.Select(x => new StockReconciliationRow
				{
					State = x.State,
					ClosingStock = x.ClosingStock,
					ClosingGit = x.ClosingGit,
					GlobalStockTotal = x.ClosingStock + x.ClosingGit,
					WholesalerStock = x.WholesalerStock,
					RetailPointStock = x.RetailPointStock,
					CompanySalesInGit = x.CompanySalesInGit,
					WholesalerSalesInGit = x.WholesalerSalesInGit,
					ChannelTotal = x.WholesalerStock + x.RetailPointStock + x.CompanySalesInGit + x.WholesalerSalesInGit,
					OverallStock = (x.ClosingStock + x.ClosingGit) + (x.WholesalerStock + x.RetailPointStock + x.CompanySalesInGit + x.WholesalerSalesInGit)
				})
				.ToList();

			var totals = new StockReconciliationTotals
			{
				ClosingStock = rows.Sum(r => r.ClosingStock),
				ClosingGit = rows.Sum(r => r.ClosingGit),
				GlobalStockTotal = rows.Sum(r => r.GlobalStockTotal),
				WholesalerStock = rows.Sum(r => r.WholesalerStock),
				RetailPointStock = rows.Sum(r => r.RetailPointStock),
				CompanySalesInGit = rows.Sum(r => r.CompanySalesInGit),
				WholesalerSalesInGit = rows.Sum(r => r.WholesalerSalesInGit),
				ChannelTotal = rows.Sum(r => r.ChannelTotal),
				OverallStock = rows.Sum(r => r.OverallStock)
			};

			var hasAnyData = rows.Count > 0 && (totals.GlobalStockTotal > 0 || totals.ChannelTotal > 0);

			string? companyName = null;
			if (request.CompanyId.HasValue && request.CompanyId.Value > 0)
				companyName = await _db.Companies.Where(c => c.Id == request.CompanyId.Value).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);

			string? plantName = null;
			if (request.PlantId.HasValue && request.PlantId.Value > 0)
				plantName = await _db.Plants.Where(p => p.Id == request.PlantId.Value).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken);

			string? productName = null;
			if (request.ProductId.HasValue && request.ProductId.Value > 0)
				productName = await _db.Products.Where(p => p.Id == request.ProductId.Value).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken);

			var warnings = new List<string>();
			if (!hasAnyData)
			{
				warnings.Add($"No reconciliation data is available for {businessDate:dd-MM-yyyy}. Please complete today's report uploads.");
			}

			var result = new StockReconciliationResult
			{
				AsOnDate = businessDate,
				Company = companyName ?? string.Empty,
				Plant = plantName ?? string.Empty,
				Product = productName ?? string.Empty,
				Rows = rows,
				Totals = totals,
				HasAnyData = hasAnyData,
				HasGlobalStock = hasGlobal,
				HasWholesalerStock = hasSr,
				HasRetailPointStock = hasDpt,
				HasCompanySalesGit = hasComp,
				HasWholesalerSalesGit = hasWs,
				MissingSources = missingSources,
				Warnings = warnings,
				FileName = BuildFileName(businessDate)
			};

			result.GflStock = await BuildGflStockMatrixFromDatabaseAsync(businessDate, cancellationToken);

			_logger.LogInformation(
				"State-wise global stock reconciliation built from DB for {AsOnDate:yyyy-MM-dd}: {States} states, " +
				"global stock {GlobalStock}, channel {Channel}. Any data: {HasAnyData}.",
				result.AsOnDate, rows.Count, totals.GlobalStockTotal, totals.ChannelTotal, hasAnyData);

			return result;
		}

		public string BuildFileName(DateTime asOnDate) =>
			$"State_Wise_Stock_Reconciliation_{asOnDate:yyyyMMdd}.xlsx";

		public byte[] BuildExcel(StockReconciliationResult result)
		{
			if (result is null)
				throw new ArgumentNullException(nameof(result));

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Stock Reconciliation");

			var titleRow = worksheet.Cell(1, 1);
			titleRow.Value = $"Urea Stock As on {result.AsOnDate:dd-MMM-yy} (in Mt)";
			titleRow.Style.Font.Bold = true;
			titleRow.Style.Font.FontSize = 14;
			titleRow.Style.Font.FontColor = XLColor.FromHtml("#0f172a");

			var row = 2;
			worksheet.Cell(row, 1).Value = "As On Date";
			worksheet.Cell(row, 1).Style.Font.Bold = true;
			worksheet.Cell(row, 2).Value = result.AsOnDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
			row++;

			// The report metadata is only written when the uploaded files carried it.
			row = WriteMetaRow(worksheet, row, "Company", result.Company);
			row = WriteMetaRow(worksheet, row, "Plant", result.Plant);
			row = WriteMetaRow(worksheet, row, "Product", result.Product);

			row++;

			var headerRow = row;
			var headers = new[]
			{
				"State",
				"Company Stock",
				"GIT From Plant despatches",
				"Total Company Stock",
				"Wholesaler Stock",
				"Retail Point Stock",
				"Wholesaler Sales in GIT",
				"Company Sales in GIT",
				"Stock Lying with Wholesalers & Retailers including GIT",
				"Overall Stock"
			};

			for (var column = 0; column < headers.Length; column++)
			{
				var cell = worksheet.Cell(headerRow, column + 1);
				cell.Value = headers[column];
				cell.Style.Font.Bold = true;
				cell.Style.Font.FontColor = XLColor.White;
				cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
				cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
				cell.Style.Alignment.WrapText = true;
				cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
			}
			worksheet.Row(headerRow).Height = 30;

			// The column format is set BEFORE the numbers are written, because a
			// column style applied afterwards would overwrite the per-cell
			// format that keeps a whole number free of a trailing decimal point.
			const string numberFormat = "#,##0.###;-#,##0.###;0";

			for (var column = 2; column <= headers.Length; column++)
				worksheet.Column(column).Style.NumberFormat.Format = numberFormat;

			var dataRow = headerRow + 1;
			foreach (var reconciliationRow in result.Rows ?? new List<StockReconciliationRow>())
			{
				worksheet.Cell(dataRow, 1).Value = reconciliationRow.State;
				SetNumber(worksheet, dataRow, 2, reconciliationRow.ClosingStock);
				SetNumber(worksheet, dataRow, 3, reconciliationRow.ClosingGit);
				SetNumber(worksheet, dataRow, 4, reconciliationRow.GlobalStockTotal);
				SetNumber(worksheet, dataRow, 5, reconciliationRow.WholesalerStock);
				SetNumber(worksheet, dataRow, 6, reconciliationRow.RetailPointStock);
				SetNumber(worksheet, dataRow, 7, reconciliationRow.WholesalerSalesInGit);
				SetNumber(worksheet, dataRow, 8, reconciliationRow.CompanySalesInGit);
				SetNumber(worksheet, dataRow, 9, reconciliationRow.ChannelTotal);
				SetNumber(worksheet, dataRow, 10, reconciliationRow.OverallStock);
				dataRow++;
			}

			var totals = result.Totals ?? new StockReconciliationTotals();
			worksheet.Cell(dataRow, 1).Value = "Total";
			SetNumber(worksheet, dataRow, 2, totals.ClosingStock);
			SetNumber(worksheet, dataRow, 3, totals.ClosingGit);
			SetNumber(worksheet, dataRow, 4, totals.GlobalStockTotal);
			SetNumber(worksheet, dataRow, 5, totals.WholesalerStock);
			SetNumber(worksheet, dataRow, 6, totals.RetailPointStock);
			SetNumber(worksheet, dataRow, 7, totals.WholesalerSalesInGit);
			SetNumber(worksheet, dataRow, 8, totals.CompanySalesInGit);
			SetNumber(worksheet, dataRow, 9, totals.ChannelTotal);
			SetNumber(worksheet, dataRow, 10, totals.OverallStock);

			var totalsRow = worksheet.Range(dataRow, 1, dataRow, headers.Length);
			totalsRow.Style.Font.Bold = true;
			totalsRow.Style.Border.TopBorder = XLBorderStyleValues.Double;
			totalsRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9");

			worksheet.SheetView.FreezeRows(headerRow);

			worksheet.Column(1).Width = 30;
			for (var column = 2; column <= headers.Length; column++)
				worksheet.Column(column).Width = 19;

			if (result.GflStock != null)
			{
				var wsGfl = workbook.Worksheets.Add("GFL Stock");
				PopulateGflWorksheet(wsGfl, result.GflStock, result.GflStock.AsOnDate > DateTime.MinValue ? result.GflStock.AsOnDate : result.AsOnDate);
			}

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);
			return stream.ToArray();
		}

		private static readonly string[] PreferredGflProductOrder = new[]
		{
			"DAP",
			"20-20-0-13",
			"10-26-26",
			"SSP-P",
			"SSP-G",
			"17-17-17",
			"MOP",
			"Imp DAP",
			"Imp 10-26-26",
			"Imp 20-20-0-13",
			"Imp 16-20-0-13"
		};

		public async Task<DailyCombinedStockMailResult> BuildDailyStockMailAsync(
			DateTime currentBusinessDate,
			CancellationToken cancellationToken = default)
		{
			var currentDate = currentBusinessDate.Date;
			var previousDate = currentDate.AddDays(-1);

			var mailResult = new DailyCombinedStockMailResult
			{
				CurrentBusinessDate = currentDate,
				PreviousBusinessDate = previousDate,
				Subject = $"Urea & GFL Stock - {currentDate:dd-MM-yyyy}",
				AttachmentFileName = $"Urea_GFL_Stock_{currentDate:yyyyMMdd}.xlsx"
			};

			// 1. Section 1 Urea stock position as on PreviousDate (CurrentDate - 1 day)
			// Uses existing database reconciliation query pipeline
			var ureaRequest = new StockReconciliationDatabaseQueryRequest
			{
				AsOnDate = previousDate
			};
			var ureaResult = await BuildFromDatabaseAsync(ureaRequest, cancellationToken);
			mailResult.UreaResult = ureaResult;

			if (ureaResult is null || ureaResult.Rows.Count == 0 || !ureaResult.HasAnyData)
			{
				mailResult.ValidationErrors.Add($"Urea stock data for {previousDate:dd-MM-yyyy} is not available.");
			}

			// 2. Section 2 GFL stock as on CurrentDate
			// Primary source: WholesalerStockAsOnToday
			var gflTable = await BuildGflStockMatrixFromDatabaseAsync(currentDate, cancellationToken);
			mailResult.GflResult = gflTable;

			if (!gflTable.HasAnyData)
			{
				mailResult.ValidationErrors.Add($"GFL stock data for {currentDate:dd-MM-yyyy} is not available.");
			}

			// Do not silently send an incomplete daily report if required dataset is missing
			if (mailResult.ValidationErrors.Count > 0)
			{
				mailResult.IsValid = false;
				return mailResult;
			}

			mailResult.IsValid = true;
			mailResult.HtmlBody = BuildCombinedHtmlBody(currentDate, previousDate, ureaResult!, gflTable);
			mailResult.ExcelAttachment = BuildCombinedExcelWorkbook(currentDate, previousDate, ureaResult!, gflTable);

			return mailResult;
		}

		public async Task<GflStockTableDto> BuildGflStockMatrixFromDatabaseAsync(
			DateTime asOnDate,
			CancellationToken cancellationToken = default)
		{
			var targetDate = asOnDate.Date;
			var dto = new GflStockTableDto
			{
				AsOnDate = targetDate
			};

			// 1. Resolve GFL Company dynamically from Company master (NO HARDCODING)
			var companies = await _db.Companies.AsNoTracking().ToListAsync(cancellationToken);
			var gflCompany = companies.FirstOrDefault(c =>
			{
				var norm = MasterNormalizer.Normalize(c.Name);
				return norm == "GFL" || norm.Contains("GFL") || norm.Contains("GREENSTAR");
			});

			if (gflCompany == null)
			{
				_logger.LogWarning("GFL Company could not be resolved from Companies master.");
				return dto;
			}

			var gflCompanyId = gflCompany.Id;

			// 2. Query WholesalerStockAsOnToday strictly for the resolved GFL CompanyId
			var stockRows = await _db.WholesalerStockAsOnTodays
				.AsNoTracking()
				.Where(w => w.StockDate >= targetDate && w.StockDate < targetDate.AddDays(1) &&
				            w.CompanyId == gflCompanyId)
				.ToListAsync(cancellationToken);

			// 3. Resolve all master products and IFMS products dynamically
			var allProducts = await _db.Products
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			var allIfmsProducts = await _db.IfmsProducts
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			// 4. Stable business product columns: always use the complete canonical GFL product list
			var columns = new List<GflStockProductColumnDto>(PreferredGflProductOrder.Length);
			for (var i = 0; i < PreferredGflProductOrder.Length; i++)
			{
				var businessName = PreferredGflProductOrder[i];
				var matchedP = allProducts.FirstOrDefault(p => GetProductDisplayName(p.Name).Equals(businessName, StringComparison.OrdinalIgnoreCase));
				var matchedIfms = allIfmsProducts.FirstOrDefault(p => GetProductDisplayName(p.Name).Equals(businessName, StringComparison.OrdinalIgnoreCase));

				int colId;
				string origName;
				if (matchedP != null)
				{
					colId = matchedP.Id;
					origName = matchedP.Name;
				}
				else if (matchedIfms != null)
				{
					colId = -matchedIfms.Id;
					origName = matchedIfms.Name ?? businessName;
				}
				else
				{
					colId = 1000 + i;
					origName = businessName;
				}

				columns.Add(new GflStockProductColumnDto
				{
					ProductId = colId,
					OriginalName = origName,
					DisplayName = businessName
				});
			}

			dto.Columns = columns;

			if (stockRows.Count == 0)
			{
				return dto;
			}

			var stateIds = stockRows
				.Where(r => r.StateId.HasValue && r.StateId.Value > 0)
				.Select(r => r.StateId!.Value)
				.Distinct()
				.ToList();

			var stateMaster = stateIds.Count == 0
				? new Dictionary<int, string>()
				: await _db.States
					.AsNoTracking()
					.Where(s => stateIds.Contains(s.Id))
					.ToDictionaryAsync(s => s.Id, s => s.StateName ?? string.Empty, cancellationToken);

			string ResolveRowDisplayName(SPIC.Core.Entities.WholesalerStockAsOnToday r)
			{
				if (r.ProductId.HasValue && r.ProductId.Value > 0)
				{
					var p = allProducts.FirstOrDefault(x => x.Id == r.ProductId.Value);
					return GetProductDisplayName(p?.Name);
				}
				if (r.IfmsProductId.HasValue && r.IfmsProductId.Value > 0)
				{
					var ip = allIfmsProducts.FirstOrDefault(x => x.Id == r.IfmsProductId.Value);
					return GetProductDisplayName(ip?.Name);
				}
				return "-";
			}

			var rowGroups = stockRows
				.Where(r => r.StateId.HasValue && r.StateId.Value > 0)
				.GroupBy(r => r.StateId!.Value)
				.OrderBy(g => stateMaster.TryGetValue(g.Key, out var name) ? name : $"State {g.Key}", StringComparer.OrdinalIgnoreCase)
				.ToList();

			foreach (var g in rowGroups)
			{
				var sName = stateMaster.TryGetValue(g.Key, out var name) ? name : $"State {g.Key}";
				var rowDto = new GflStockTableRowDto
				{
					State = sName.Trim()
				};

				decimal rowTotal = 0m;
				foreach (var col in dto.Columns)
				{
					var sum = g.Where(r => col.DisplayName.Equals(ResolveRowDisplayName(r), StringComparison.OrdinalIgnoreCase)).Sum(r => r.Stock);
					rowDto.Quantities[col.ProductId] = sum;
					rowTotal += sum;
				}

				rowDto.Total = rowTotal;
				dto.Rows.Add(rowDto);
			}

			decimal grandTotal = 0m;
			foreach (var col in dto.Columns)
			{
				var colSum = dto.Rows.Sum(r => r.Quantities.TryGetValue(col.ProductId, out var v) ? v : 0m);
				dto.ColumnTotals[col.ProductId] = colSum;
				grandTotal += colSum;
			}

			dto.GrandTotal = grandTotal;
			dto.HasAnyData = stockRows.Count > 0 && (grandTotal > 0 || dto.Rows.Count > 0);

			return dto;
		}

		private static string GetProductDisplayName(string? name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return "-";

			var trimmed = name.Trim();

			if (trimmed.Equals("SSP-Powdered", StringComparison.OrdinalIgnoreCase) ||
				trimmed.Equals("SSP - Powder", StringComparison.OrdinalIgnoreCase) ||
				trimmed.Equals("SSP-Powder", StringComparison.OrdinalIgnoreCase))
			{
				return "SSP-P";
			}

			if (trimmed.Equals("SSP-Granules", StringComparison.OrdinalIgnoreCase) ||
				trimmed.Equals("SSP-Granular", StringComparison.OrdinalIgnoreCase) ||
				trimmed.Equals("SSP - Granules", StringComparison.OrdinalIgnoreCase) ||
				trimmed.Equals("SSP - Granular", StringComparison.OrdinalIgnoreCase))
			{
				return "SSP-G";
			}

			if (trimmed.StartsWith("Imported ", StringComparison.OrdinalIgnoreCase))
			{
				var remainder = trimmed.Substring("Imported ".Length).Trim();
				if (remainder.Equals("20-20-0", StringComparison.OrdinalIgnoreCase))
					return "Imp 20-20-0-13";

				return "Imp " + remainder;
			}

			return trimmed;
		}

		private static string BuildCombinedHtmlBody(
			DateTime currentDate,
			DateTime previousDate,
			StockReconciliationResult ureaResult,
			GflStockTableDto gflTable)
		{
			var sb = new StringBuilder();
			sb.Append("<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#1e293b;line-height:1.5;\">");
			sb.Append("<p style=\"margin:0 0 12px 0;\">Dear Sir,</p>");
			sb.Append($"<p style=\"margin:0 0 12px 0;\">Please find attached the overall Urea stock position as on <strong>{previousDate:dd.MM.yyyy}</strong>, along with the GFL state and product-wise opening balance as on today.</p>");
			sb.Append("<p style=\"margin:0 0 20px 0;\">Kindly find the attached report for your reference.</p>");

			// -------------------------------------------------------------
			// Section 1: Urea Stock As on {previousDate:dd-MMM-yy} (in Mt)
			// -------------------------------------------------------------
			sb.Append($"<div style=\"font-size:14px;font-weight:bold;color:#0f172a;margin-bottom:8px;padding-top:4px;\">Urea Stock As on {previousDate:dd-MMM-yy} (in Mt)</div>");
			sb.Append("<table cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;font-size:12px;font-family:Arial,Helvetica,sans-serif;margin-bottom:24px;border:1px solid #475569;\">");
			sb.Append("<thead><tr style=\"background-color:#1e293b;color:#ffffff;font-size:11px;font-weight:bold;\">");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:left;\">State</th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Company Stock</th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\"><span style=\"color:#ef4444;\">GIT</span> From Plant despatches</th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Total Company Stock</th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Wholesaler Stock</th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Retail Point Stock</th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Wholesaler Sales in <span style=\"color:#ef4444;\">GIT</span></th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Company Sales in <span style=\"color:#ef4444;\">GIT</span></th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Stock Lying with Wholesalers &amp; Retailers including <span style=\"color:#ef4444;\">GIT</span></th>");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Overall Stock</th>");
			sb.Append("</tr></thead><tbody>");

			var orderedUreaRows = (ureaResult.Rows ?? new List<StockReconciliationRow>())
				.OrderBy(r => r.State.Equals("Plant", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
				.ThenBy(r => r.State, StringComparer.OrdinalIgnoreCase)
				.ToList();

			var isEven = false;
			foreach (var row in orderedUreaRows)
			{
				var bg = isEven ? "#f8fafc" : "#ffffff";
				isEven = !isEven;
				sb.Append($"<tr style=\"background-color:{bg};\">");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:left;font-weight:600;\">{Escape(row.State)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;\">{FormatNumber(row.ClosingStock)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;\">{FormatNumber(row.ClosingGit)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;background-color:#f1f5f9;font-weight:600;\">{FormatNumber(row.GlobalStockTotal)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;\">{FormatNumber(row.WholesalerStock)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;\">{FormatNumber(row.RetailPointStock)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;\">{FormatNumber(row.WholesalerSalesInGit)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;\">{FormatNumber(row.CompanySalesInGit)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;background-color:#f1f5f9;font-weight:600;\">{FormatNumber(row.ChannelTotal)}</td>");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;background-color:#f1f5f9;font-weight:bold;\">{FormatNumber(row.OverallStock)}</td>");
				sb.Append("</tr>");
			}

			var ureaTotals = ureaResult.Totals ?? new StockReconciliationTotals();
			sb.Append("<tr style=\"background-color:#e2e8f0;font-weight:bold;border-top:2px solid #334155;border-bottom:2px solid #334155;\">");
			sb.Append("<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:left;\">Total</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.ClosingStock)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.ClosingGit)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.GlobalStockTotal)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.WholesalerStock)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.RetailPointStock)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.WholesalerSalesInGit)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.CompanySalesInGit)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.ChannelTotal)}</td>");
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(ureaTotals.OverallStock)}</td>");
			sb.Append("</tr></tbody></table>");

			// -------------------------------------------------------------
			// Section 2: GFL Stock As on {currentDate:dd-MMM-yy} (in Mt)
			// -------------------------------------------------------------
			sb.Append($"<div style=\"font-size:14px;font-weight:bold;color:#0f172a;margin:28px 0 8px 0;\">GFL Stock As on {currentDate:dd-MMM-yy} (in Mt)</div>");
			sb.Append("<table cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;font-size:12px;font-family:Arial,Helvetica,sans-serif;margin-bottom:24px;border:1px solid #475569;\">");
			sb.Append("<thead><tr style=\"background-color:#1e293b;color:#ffffff;font-size:11px;font-weight:bold;\">");
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:left;\">State</th>");
			foreach (var col in gflTable.Columns)
			{
				sb.Append($"<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">{Escape(col.DisplayName)}</th>");
			}
			sb.Append("<th style=\"border:1px solid #475569;padding:6px 8px;text-align:right;\">Total</th>");
			sb.Append("</tr></thead><tbody>");

			isEven = false;
			foreach (var row in gflTable.Rows)
			{
				var bg = isEven ? "#f8fafc" : "#ffffff";
				isEven = !isEven;
				sb.Append($"<tr style=\"background-color:{bg};\">");
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:left;font-weight:600;\">{Escape(row.State)}</td>");
				foreach (var col in gflTable.Columns)
				{
					var qty = row.Quantities.TryGetValue(col.ProductId, out var val) ? val : 0m;
					sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;\">{FormatNumber(qty)}</td>");
				}
				sb.Append($"<td style=\"border:1px solid #cbd5e1;padding:5px 8px;text-align:right;background-color:#f1f5f9;font-weight:bold;\">{FormatNumber(row.Total)}</td>");
				sb.Append("</tr>");
			}

			sb.Append("<tr style=\"background-color:#e2e8f0;font-weight:bold;border-top:2px solid #334155;border-bottom:2px solid #334155;\">");
			sb.Append("<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:left;\">Total</td>");
			foreach (var col in gflTable.Columns)
			{
				var colSum = gflTable.ColumnTotals.TryGetValue(col.ProductId, out var sum) ? sum : 0m;
				sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(colSum)}</td>");
			}
			sb.Append($"<td style=\"border:1px solid #94a3b8;padding:6px 8px;text-align:right;\">{FormatNumberPlain(gflTable.GrandTotal)}</td>");
			sb.Append("</tr></tbody></table>");

			sb.Append("</div>");
			return sb.ToString();
		}

		private static byte[] BuildCombinedExcelWorkbook(
			DateTime currentDate,
			DateTime previousDate,
			StockReconciliationResult ureaResult,
			GflStockTableDto gflTable)
		{
			using var workbook = new XLWorkbook();
			const string numberFormat = "#,##0.###;-#,##0.###;0";

			// -------------------------------------------------------------
			// Sheet 1: Urea Stock
			// -------------------------------------------------------------
			var ureaSheet = workbook.Worksheets.Add("Urea Stock");
			ureaSheet.Cell(1, 1).Value = $"Urea Stock As on {previousDate:dd-MMM-yy} (in Mt)";
			ureaSheet.Range(1, 1, 1, 10).Merge();
			ureaSheet.Cell(1, 1).Style.Font.Bold = true;
			ureaSheet.Cell(1, 1).Style.Font.FontSize = 13;
			ureaSheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
			ureaSheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
			ureaSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			ureaSheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			ureaSheet.Row(1).Height = 26;

			var ureaHeaders = new[]
			{
				"State",
				"Company Stock",
				"GIT From Plant despatches",
				"Total Company Stock",
				"Wholesaler Stock",
				"Retail Point Stock",
				"Wholesaler Sales in GIT",
				"Company Sales in GIT",
				"Stock Lying with Wholesalers & Retailers including GIT",
				"Overall Stock"
			};

			var ureaHeaderRow = 3;
			for (var c = 0; c < ureaHeaders.Length; c++)
			{
				var cell = ureaSheet.Cell(ureaHeaderRow, c + 1);
				cell.Value = ureaHeaders[c];
				cell.Style.Font.Bold = true;
				cell.Style.Font.FontColor = XLColor.White;
				cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
				cell.Style.Alignment.Horizontal = c == 0 ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
				cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
				cell.Style.Alignment.WrapText = true;
			}
			ureaSheet.Row(ureaHeaderRow).Height = 28;

			for (var c = 2; c <= ureaHeaders.Length; c++)
				ureaSheet.Column(c).Style.NumberFormat.Format = numberFormat;

			var ureaDataRow = ureaHeaderRow + 1;
			var orderedUreaRows = (ureaResult.Rows ?? new List<StockReconciliationRow>())
				.OrderBy(r => r.State.Equals("Plant", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
				.ThenBy(r => r.State, StringComparer.OrdinalIgnoreCase)
				.ToList();

			foreach (var row in orderedUreaRows)
			{
				ureaSheet.Cell(ureaDataRow, 1).Value = row.State;
				SetNumber(ureaSheet, ureaDataRow, 2, row.ClosingStock);
				SetNumber(ureaSheet, ureaDataRow, 3, row.ClosingGit);
				SetNumber(ureaSheet, ureaDataRow, 4, row.GlobalStockTotal);
				SetNumber(ureaSheet, ureaDataRow, 5, row.WholesalerStock);
				SetNumber(ureaSheet, ureaDataRow, 6, row.RetailPointStock);
				SetNumber(ureaSheet, ureaDataRow, 7, row.WholesalerSalesInGit);
				SetNumber(ureaSheet, ureaDataRow, 8, row.CompanySalesInGit);
				SetNumber(ureaSheet, ureaDataRow, 9, row.ChannelTotal);
				SetNumber(ureaSheet, ureaDataRow, 10, row.OverallStock);

				for (var c = 1; c <= ureaHeaders.Length; c++)
					ureaSheet.Cell(ureaDataRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

				ureaDataRow++;
			}

			var ureaTotals = ureaResult.Totals ?? new StockReconciliationTotals();
			ureaSheet.Cell(ureaDataRow, 1).Value = "Total";
			SetNumber(ureaSheet, ureaDataRow, 2, ureaTotals.ClosingStock);
			SetNumber(ureaSheet, ureaDataRow, 3, ureaTotals.ClosingGit);
			SetNumber(ureaSheet, ureaDataRow, 4, ureaTotals.GlobalStockTotal);
			SetNumber(ureaSheet, ureaDataRow, 5, ureaTotals.WholesalerStock);
			SetNumber(ureaSheet, ureaDataRow, 6, ureaTotals.RetailPointStock);
			SetNumber(ureaSheet, ureaDataRow, 7, ureaTotals.WholesalerSalesInGit);
			SetNumber(ureaSheet, ureaDataRow, 8, ureaTotals.CompanySalesInGit);
			SetNumber(ureaSheet, ureaDataRow, 9, ureaTotals.ChannelTotal);
			SetNumber(ureaSheet, ureaDataRow, 10, ureaTotals.OverallStock);

			var ureaTotalRange = ureaSheet.Range(ureaDataRow, 1, ureaDataRow, ureaHeaders.Length);
			ureaTotalRange.Style.Font.Bold = true;
			ureaTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e2e8f0");
			ureaTotalRange.Style.Border.TopBorder = XLBorderStyleValues.Double;
			ureaTotalRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

			ureaSheet.Column(1).Width = 26;
			for (var c = 2; c <= ureaHeaders.Length; c++)
				ureaSheet.Column(c).Width = 16;
			ureaSheet.SheetView.FreezeRows(ureaHeaderRow);

			// -------------------------------------------------------------
			// Sheet 2: GFL Stock
			// -------------------------------------------------------------
			var gflSheet = workbook.Worksheets.Add("GFL Stock");
			PopulateGflWorksheet(gflSheet, gflTable, currentDate);

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);
			return stream.ToArray();
		}

		private static void PopulateGflWorksheet(IXLWorksheet gflSheet, GflStockTableDto gflTable, DateTime asOnDate)
		{
			const string numberFormat = "#,##0.###;-#,##0.###;0";
			var totalGflCols = Math.Max(3, 1 + gflTable.Columns.Count + 1); // State + Products + Total

			gflSheet.Cell(1, 1).Value = $"GFL Stock As on {asOnDate:dd-MMM-yy} (in Mt)";
			gflSheet.Range(1, 1, 1, totalGflCols).Merge();
			gflSheet.Cell(1, 1).Style.Font.Bold = true;
			gflSheet.Cell(1, 1).Style.Font.FontSize = 13;
			gflSheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
			gflSheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
			gflSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			gflSheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			gflSheet.Row(1).Height = 26;

			if (!gflTable.HasAnyData || gflTable.Columns.Count == 0)
			{
				gflSheet.Cell(3, 1).Value = $"GFL stock data is not available for {asOnDate:dd-MM-yyyy}.";
				gflSheet.Cell(3, 1).Style.Font.Italic = true;
				gflSheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#64748b");
				gflSheet.Column(1).Width = 40;
				return;
			}

			var gflHeaderRow = 3;
			gflSheet.Cell(gflHeaderRow, 1).Value = "State";
			gflSheet.Cell(gflHeaderRow, 1).Style.Font.Bold = true;
			gflSheet.Cell(gflHeaderRow, 1).Style.Font.FontColor = XLColor.White;
			gflSheet.Cell(gflHeaderRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");

			for (var c = 0; c < gflTable.Columns.Count; c++)
			{
				var cell = gflSheet.Cell(gflHeaderRow, c + 2);
				cell.Value = gflTable.Columns[c].DisplayName;
				cell.Style.Font.Bold = true;
				cell.Style.Font.FontColor = XLColor.White;
				cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
				cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			}
			var totalHeaderCell = gflSheet.Cell(gflHeaderRow, totalGflCols);
			totalHeaderCell.Value = "Total";
			totalHeaderCell.Style.Font.Bold = true;
			totalHeaderCell.Style.Font.FontColor = XLColor.White;
			totalHeaderCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
			totalHeaderCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			gflSheet.Row(gflHeaderRow).Height = 24;

			for (var c = 2; c <= totalGflCols; c++)
				gflSheet.Column(c).Style.NumberFormat.Format = numberFormat;

			var gflDataRow = gflHeaderRow + 1;
			foreach (var row in gflTable.Rows)
			{
				gflSheet.Cell(gflDataRow, 1).Value = row.State;
				for (var c = 0; c < gflTable.Columns.Count; c++)
				{
					var colId = gflTable.Columns[c].ProductId;
					var val = row.Quantities.TryGetValue(colId, out var q) ? q : 0m;
					SetNumber(gflSheet, gflDataRow, c + 2, val);
				}
				SetNumber(gflSheet, gflDataRow, totalGflCols, row.Total);

				for (var c = 1; c <= totalGflCols; c++)
					gflSheet.Cell(gflDataRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

				gflDataRow++;
			}

			gflSheet.Cell(gflDataRow, 1).Value = "Total";
			for (var c = 0; c < gflTable.Columns.Count; c++)
			{
				var colId = gflTable.Columns[c].ProductId;
				var colSum = gflTable.ColumnTotals.TryGetValue(colId, out var sum) ? sum : 0m;
				SetNumber(gflSheet, gflDataRow, c + 2, colSum);
			}
			SetNumber(gflSheet, gflDataRow, totalGflCols, gflTable.GrandTotal);

			var gflTotalRange = gflSheet.Range(gflDataRow, 1, gflDataRow, totalGflCols);
			gflTotalRange.Style.Font.Bold = true;
			gflTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e2e8f0");
			gflTotalRange.Style.Border.TopBorder = XLBorderStyleValues.Double;
			gflTotalRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

			gflSheet.Column(1).Width = 24;
			for (var c = 2; c <= totalGflCols; c++)
				gflSheet.Column(c).Width = 14;
			gflSheet.SheetView.FreezeRows(gflHeaderRow);
		}

		private static string FormatNumber(decimal value)
		{
			if (value == 0m) return "-";
			return value == decimal.Truncate(value)
				? value.ToString("#,##0", CultureInfo.InvariantCulture)
				: value.ToString("#,##0.###", CultureInfo.InvariantCulture);
		}

		private static string FormatNumberPlain(decimal value)
		{
			return value == decimal.Truncate(value)
				? value.ToString("#,##0", CultureInfo.InvariantCulture)
				: value.ToString("#,##0.###", CultureInfo.InvariantCulture);
		}

		private static string Escape(string? text)
		{
			if (string.IsNullOrEmpty(text)) return string.Empty;
			return System.Net.WebUtility.HtmlEncode(text);
		}

		/// <summary>
		/// Writes one number. A whole number gets the plain "#,##0" form,
		/// because "#,##0.###" would show a trailing decimal point ("130.").
		/// </summary>
		private static void SetNumber(IXLWorksheet worksheet, int row, int column, decimal value)
		{
			var cell = worksheet.Cell(row, column);
			cell.Value = value;
			cell.Style.NumberFormat.Format = value == decimal.Truncate(value)
				? "#,##0;-#,##0;0"
				: "#,##0.###;-#,##0.###;0";
		}

		private static int WriteMetaRow(IXLWorksheet worksheet, int row, string label, string value)		{
			if (string.IsNullOrWhiteSpace(value))
				return row;

			worksheet.Cell(row, 1).Value = label;
			worksheet.Cell(row, 1).Style.Font.Bold = true;
			worksheet.Cell(row, 2).Value = value;
			return row + 1;
		}

		// ------------------------------------------------------------------ readers

		/// <summary>State-wise Global Stock Reconciliation file: Closing Stock and Closing GIT.</summary>
		private FileMeta ReadGlobalStock(
			StockReconciliationSourceFile file,
			StateLookup states,
			Dictionary<string, StateAmounts> amounts,
			List<string> errors,
			List<string> warnings,
			CancellationToken cancellationToken)
		{
			var meta = new FileMeta();

			var table = OpenTable(file, GlobalStockLabel, errors, warnings, cancellationToken);
			if (table is null)
				return meta;

			// The title of the global stock report carries the plant and the
			// product ("... for <plant> for <product>").
			var columns = MapColumns(
				table,
				new[] { ColumnKind.State, ColumnKind.ClosingStock, ColumnKind.ClosingGit },
				Array.Empty<ColumnKind>(),
				GlobalStockLabel,
				errors);
			if (columns is null)
				return meta;

			ReadTitleMeta(table, meta);

			foreach (var row in table.Rows.Where(r => r.Number > table.HeaderRowNumber))
			{
				cancellationToken.ThrowIfCancellationRequested();

				var stateText = Cell(row, columns, ColumnKind.State);
				if (IsIgnorableState(stateText, columns))
					continue;

				if (!states.TryResolve(stateText, out var stateName))
				{
					errors.Add($"{GlobalStockLabel} file: state '{stateText.Trim()}' (row {row.Number}) is not in the State master.");
					continue;
				}

				var closingStock = ReadDecimal(row, columns, ColumnKind.ClosingStock, GlobalStockLabel, errors);
				var closingGit = ReadDecimal(row, columns, ColumnKind.ClosingGit, GlobalStockLabel, errors);
				if (closingStock is null || closingGit is null)
					continue;

				var amount = Amount(amounts, states, stateName);
				amount.ClosingStock += closingStock.Value;
				amount.ClosingGit += closingGit.Value;
			}

			return meta;
		}

		/// <summary>Company Dealer Wise "Sales &amp; Receipt" file: Balance With WS.</summary>
		private FileMeta ReadSalesAndReceipt(
			StockReconciliationSourceFile file,
			StateLookup states,
			Dictionary<string, StateAmounts> amounts,
			List<string> errors,
			List<string> warnings,
			CancellationToken cancellationToken)
		{
			var meta = new FileMeta();

			var table = OpenTable(file, SalesReceiptLabel, errors, warnings, cancellationToken);
			if (table is null)
				return meta;

			var columns = MapColumns(
				table,
				new[] { ColumnKind.State, ColumnKind.BalanceWithWs },
				new[] { ColumnKind.Company, ColumnKind.Plant, ColumnKind.Product },
				SalesReceiptLabel,
				errors);
			if (columns is null)
				return meta;

			foreach (var row in table.Rows.Where(r => r.Number > table.HeaderRowNumber))
			{
				cancellationToken.ThrowIfCancellationRequested();

				var stateText = Cell(row, columns, ColumnKind.State);
				if (IsIgnorableState(stateText, columns))
					continue;

				if (!states.TryResolve(stateText, out var stateName))
				{
					errors.Add($"{SalesReceiptLabel} file: state '{stateText.Trim()}' (row {row.Number}) is not in the State master.");
					continue;
				}

				var balance = ReadDecimal(row, columns, ColumnKind.BalanceWithWs, SalesReceiptLabel, errors);
				if (balance is null)
					continue;

				// Only a positive balance with the wholesaler is channel stock.
				if (balance.Value <= 0m)
					continue;

				Amount(amounts, states, stateName).WholesalerStock += balance.Value;

				meta.Company = Meta(meta.Company, Cell(row, columns, ColumnKind.Company));
				meta.Plant = Meta(meta.Plant, Cell(row, columns, ColumnKind.Plant));
				meta.Product = Meta(meta.Product, Cell(row, columns, ColumnKind.Product));
			}

			return meta;
		}

		/// <summary>Retailer Stock / DPT file: Closing Balance. Several of these are merged together.</summary>
		private void ReadRetailPoint(
			StockReconciliationSourceFile file,
			StateLookup states,
			Dictionary<string, StateAmounts> amounts,
			List<string> errors,
			List<string> warnings,
			CancellationToken cancellationToken)
		{
			var table = OpenTable(file, RetailPointLabel, errors, warnings, cancellationToken);
			if (table is null)
				return;

			var columns = MapColumns(
				table,
				new[] { ColumnKind.State, ColumnKind.ClosingBalance },
				Array.Empty<ColumnKind>(),
				RetailPointLabel,
				errors);
			if (columns is null)
				return;

			foreach (var row in table.Rows.Where(r => r.Number > table.HeaderRowNumber))
			{
				cancellationToken.ThrowIfCancellationRequested();

				var stateText = Cell(row, columns, ColumnKind.State);
				if (IsIgnorableState(stateText, columns))
					continue;

				if (!states.TryResolve(stateText, out var stateName))
				{
					errors.Add($"{RetailPointLabel} file '{file.FileName}': state '{stateText.Trim()}' (row {row.Number}) is not in the State master.");
					continue;
				}

				var closingBalance = ReadDecimal(row, columns, ColumnKind.ClosingBalance, RetailPointLabel, errors);
				if (closingBalance is null)
					continue;

				Amount(amounts, states, stateName).RetailPointStock += closingBalance.Value;
			}
		}

		/// <summary>
		/// Company Sales / Wholesaler Sales file: Quantity(MT), Status and Entry Date.
		///
		/// The GIT rule is the same for both: a NEW row always counts, an ACK row
		/// only counts when its Entry Date is the As On Date. REVERSED and every
		/// other status are left out.
		/// </summary>
		private void ReadSales(
			StockReconciliationSourceFile file,
			ColumnKind target,
			StateLookup states,
			StatusLookup statuses,
			Dictionary<string, StateAmounts> amounts,
			List<string> errors,
			List<string> warnings,
			DateTime asOnDate,
			CancellationToken cancellationToken)
		{
			var label = target == ColumnKind.CompanySalesInGit ? CompanySalesLabel : WholesalerSalesLabel;

			var table = OpenTable(file, label, errors, warnings, cancellationToken);
			if (table is null)
				return;

			var columns = MapColumns(
				table,
				new[] { ColumnKind.State, ColumnKind.QuantityMt, ColumnKind.Status, ColumnKind.EntryDate },
				Array.Empty<ColumnKind>(),
				label,
				errors);
			if (columns is null)
				return;

			foreach (var row in table.Rows.Where(r => r.Number > table.HeaderRowNumber))
			{
				cancellationToken.ThrowIfCancellationRequested();

				var stateText = Cell(row, columns, ColumnKind.State);
				if (IsIgnorableState(stateText, columns))
					continue;

				if (!states.TryResolve(stateText, out var stateName))
				{
					errors.Add($"{label} file: state '{stateText.Trim()}' (row {row.Number}) is not in the State master.");
					continue;
				}

				var quantity = ReadDecimal(row, columns, ColumnKind.QuantityMt, label, errors);
				if (quantity is null)
					continue;

				var statusText = Cell(row, columns, ColumnKind.Status).Trim();
				var statusKey = statuses.Resolve(statusText);

				if (statusKey == NewStatus)
				{
					AddSales(amounts, states, stateName, target, quantity.Value);
					continue;
				}

				if (statusKey == AckStatus)
				{
					var entryDateText = Cell(row, columns, ColumnKind.EntryDate);
					if (string.IsNullOrWhiteSpace(entryDateText))
						continue;

					if (!TryParseDate(entryDateText, out var entryDate))
					{
						errors.Add($"{label} file: entry date '{entryDateText.Trim()}' (row {row.Number}) is not a valid date.");
						continue;
					}

					if (entryDate.Date == asOnDate.Date)
						AddSales(amounts, states, stateName, target, quantity.Value);

					continue;
				}

				// REVERSED and every unrelated status are excluded, but an unknown
				// status name is worth pointing out.
				if (!statuses.IsKnown(statusKey) && !string.IsNullOrEmpty(statusText))
				{
					var warning = $"{label} file: status '{statusText}' (row {row.Number}) is not a known status; the row was left out.";
					if (!warnings.Contains(warning))
						warnings.Add(warning);
				}
			}
		}

		private static void AddSales(
			Dictionary<string, StateAmounts> amounts,
			StateLookup states,
			string stateName,
			ColumnKind target,
			decimal quantity)
		{
			var amount = Amount(amounts, states, stateName);

			if (target == ColumnKind.CompanySalesInGit)
				amount.CompanySalesInGit += quantity;
			else
				amount.WholesalerSalesInGit += quantity;
		}

		// ------------------------------------------------------------------ tables

		private sealed class SheetRow
		{
			public int Number { get; set; }
			public string[] Values { get; set; } = Array.Empty<string>();
		}

		private sealed class ReportTable
		{
			public List<SheetRow> Rows { get; set; } = new();
			public int HeaderRowNumber { get; set; }
		}

		private sealed class FileMeta
		{
			public string? Company { get; set; }
			public string? Plant { get; set; }
			public string? Product { get; set; }
		}

		/// <summary>
		/// Reads the whole file once and finds its header row. The header is the
		/// row that carries the most of the required columns, so a title row, a
		/// blank row or a repeated header does not confuse it.
		/// </summary>
		private ReportTable? OpenTable(
			StockReconciliationSourceFile file,
			string label,
			List<string> errors,
			List<string> warnings,
			CancellationToken cancellationToken)
		{
			if (file is null || file.Content is null)
				return null;

			var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();

			List<SheetRow> rows;
			try
			{
				rows = ReadRows(file, extension, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				errors.Add($"{label} file '{file.FileName}' could not be read: {ex.Message}");
				return null;
			}

			if (rows.Count == 0)
			{
				errors.Add($"{label} file '{file.FileName}' is empty.");
				return null;
			}

			return new ReportTable { Rows = rows, HeaderRowNumber = 0 };
		}

		private static List<SheetRow> ReadRows(
			StockReconciliationSourceFile file,
			string extension,
			CancellationToken cancellationToken)
		{
			// ClosedXML needs a seekable stream, so the upload is buffered once
			// and read from memory afterwards.
			using var buffer = new MemoryStream();

			if (file.Content.CanSeek)
				file.Content.Position = 0;

			file.Content.CopyTo(buffer);
			buffer.Position = 0;

			var rows = new List<SheetRow>();

			if (extension == ".csv")
			{
				using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
				using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					HasHeaderRecord = false,
					BadDataFound = null,
					MissingFieldFound = null
				});

				var rowNumber = 0;
				while (csv.Read())
				{
					cancellationToken.ThrowIfCancellationRequested();
					rowNumber++;

					// The record length is taken from the parser. Asking for a field
					// that the record does not have would keep returning an empty
					// value for ever, so the length has to bound the row.
					var fieldCount = csv.Parser.Count;
					if (fieldCount > MaxColumns)
						fieldCount = MaxColumns;

					var values = new string[fieldCount];
					for (var i = 0; i < fieldCount; i++)
						values[i] = csv.GetField(i) ?? string.Empty;

					rows.Add(new SheetRow { Number = rowNumber, Values = values });
				}

				return rows;
			}

			using var workbook = new XLWorkbook(buffer);
			var worksheet = workbook.Worksheets.First();

			foreach (var row in worksheet.RowsUsed())
			{
				cancellationToken.ThrowIfCancellationRequested();

				var lastColumn = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
				if (lastColumn > MaxColumns)
					lastColumn = MaxColumns;

				var values = new string[lastColumn];
				for (var column = 1; column <= lastColumn; column++)
				{
					var cell = row.Cell(column);
					values[column - 1] = cell.DataType == XLDataType.DateTime
						? cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
						: cell.GetString();
				}

				rows.Add(new SheetRow { Number = row.RowNumber(), Values = values });
			}

			return rows;
		}

		/// <summary>
		/// Finds the header row and maps the required columns onto it. When a
		/// required column is absent the file is rejected with a message that
		/// names the column, so nothing misleading is ever calculated.
		///
		/// The optional columns (the report metadata) are mapped when the file
		/// carries them and simply left out when it does not.
		/// </summary>
		private static Dictionary<ColumnKind, int>? MapColumns(
			ReportTable table,
			IReadOnlyList<ColumnKind> required,
			IReadOnlyList<ColumnKind> optional,
			string label,
			List<string> errors)
		{
			SheetRow? bestRow = null;
			Dictionary<ColumnKind, int>? bestMap = null;
			var bestMatches = 0;

			foreach (var row in table.Rows.Take(MaxHeaderScanRows))
			{
				var map = BuildColumnMap(row.Values, required, optional);
				var matches = required.Count(kind => map.ContainsKey(kind));

				if (matches > bestMatches)
				{
					bestMatches = matches;
					bestRow = row;
					bestMap = map;
				}

				if (bestMatches == required.Count)
					break;
			}

			if (bestRow is null || bestMap is null)
			{
				errors.Add($"{label} file has no readable header row.");
				return null;
			}

			var missing = required.Where(kind => !bestMap.ContainsKey(kind)).ToList();
			if (missing.Count > 0)
			{
				foreach (var kind in missing)
					errors.Add($"{label} file is missing the {ColumnLabel(kind)} column.");

				return null;
			}

			table.HeaderRowNumber = bestRow.Number;
			return bestMap;
		}

		private static Dictionary<ColumnKind, int> BuildColumnMap(
			string[] headerCells,
			IReadOnlyList<ColumnKind> required,
			IReadOnlyList<ColumnKind> optional)
		{
			var map = new Dictionary<ColumnKind, int>();
			var kinds = required.Concat(optional).Distinct();

			for (var index = 0; index < headerCells.Length; index++)
			{
				var key = HeaderKey(headerCells[index]);
				if (key.Length == 0)
					continue;

				foreach (var kind in kinds)
				{
					if (map.ContainsKey(kind))
						continue;

					if (ColumnAliases[kind].Contains(key, StringComparer.Ordinal))
						map[kind] = index;
				}
			}

			return map;
		}

		/// <summary>
		/// Reads the plant and the product out of the report title, e.g.
		/// "State-Wise Global Stock Reconciliation for Plant1 for Product1".
		/// Only the rows ABOVE the header are read, so the title is never mixed
		/// up with the data.
		/// </summary>
		private static void ReadTitleMeta(ReportTable table, FileMeta meta)
		{
			var title = string.Join(" ",
				table.Rows
					.Where(row => row.Number < table.HeaderRowNumber)
					.SelectMany(row => row.Values)
					.Where(value => !string.IsNullOrWhiteSpace(value)));

			var parts = title.Split(new[] { " for " }, StringSplitOptions.None);

			if (parts.Length >= 3)
			{
				meta.Plant = Meta(meta.Plant, parts[^2]);
				meta.Product = Meta(meta.Product, parts[^1]);
			}
			else if (parts.Length == 2)
			{
				meta.Plant = Meta(meta.Plant, parts[^1]);
			}
		}

		/// <summary>Header names differ only in spacing, casing and punctuation.</summary>
		private static string HeaderKey(string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
				return string.Empty;

			var builder = new StringBuilder(raw.Length);
			foreach (var character in raw)
			{
				if (char.IsLetterOrDigit(character))
					builder.Append(char.ToLowerInvariant(character));
			}

			return builder.ToString();
		}

		// ------------------------------------------------------------------ values

		private static string Cell(
			SheetRow row,
			IReadOnlyDictionary<ColumnKind, int> columns,
			ColumnKind kind)
		{
			if (!columns.TryGetValue(kind, out var index))
				return string.Empty;

			if (index < 0 || index >= row.Values.Length)
				return string.Empty;

			return (row.Values[index] ?? string.Empty).Trim();
		}

		/// <summary>
		/// True for the rows that are not data: a blank state, a repeated header,
		/// or a summary label such as PLANT / TOTAL.
		/// </summary>
		private static bool IsIgnorableState(string stateText, IReadOnlyDictionary<ColumnKind, int> columns)
		{
			var trimmed = (stateText ?? string.Empty).Trim();
			if (trimmed.Length == 0)
				return true;

			// A repeated header row in the middle of the file.
			if (columns.ContainsKey(ColumnKind.State) &&
				ColumnAliases[ColumnKind.State].Contains(HeaderKey(trimmed), StringComparer.Ordinal))
			{
				return true;
			}

			var key = MasterNormalizer.Normalize(trimmed);
			return key is "PLANT" or "PLANTS" or "TOTAL" or "TOTALS" or "GRAND TOTAL" or
				"SUB TOTAL" or "SUBTOTAL" or "ALL INDIA" or "STATE" or "STATES" ||
				key.EndsWith(" TOTAL", StringComparison.Ordinal) ||
				key.EndsWith("-TOTAL", StringComparison.Ordinal);
		}

		private static decimal? ReadDecimal(
			SheetRow row,
			IReadOnlyDictionary<ColumnKind, int> columns,
			ColumnKind kind,
			string label,
			List<string> errors)
		{
			var raw = Cell(row, columns, kind);
			if (string.IsNullOrWhiteSpace(raw))
				return 0m;

			if (TryParseDecimal(raw, out var value))
				return value;

			errors.Add($"{label} file: '{ColumnLabel(kind)}' value '{raw}' (row {row.Number}) is not a valid number.");
			return null;
		}

		private static bool TryParseDecimal(string? raw, out decimal value)
		{
			value = 0m;
			if (string.IsNullOrWhiteSpace(raw))
				return true;

			var text = raw.Trim();
			var negative = false;

			// Accounting style: (1,234.00) means -1234.00
			if (text.StartsWith("(", StringComparison.Ordinal) && text.EndsWith(")", StringComparison.Ordinal))
			{
				negative = true;
				text = text[1..^1].Trim();
			}

			if (text.StartsWith("-", StringComparison.Ordinal))
			{
				negative = !negative;
				text = text[1..].Trim();
			}
			else if (text.StartsWith("+", StringComparison.Ordinal))
			{
				text = text[1..].Trim();
			}

			if (decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var parsed) ||
				decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.CurrentCulture, out parsed))
			{
				value = negative ? -parsed : parsed;
				return true;
			}

			// Last resort: drop the thousands separators and try again.
			var cleaned = text.Replace(",", string.Empty).Replace(" ", string.Empty);
			if (cleaned.Length > 0 &&
				decimal.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out parsed))
			{
				value = negative ? -parsed : parsed;
				return true;
			}

			return false;
		}

		private static bool TryParseDate(string? raw, out DateTime date)
		{
			date = default;

			if (string.IsNullOrWhiteSpace(raw))
				return false;

			var text = raw.Trim();
			if (text.Contains('T'))
				text = text.Replace('T', ' ');

			var datePart = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
				.FirstOrDefault() ?? text;

			var formats = new[]
			{
				"dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy",
				"yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "M/d/yyyy",
				"dd-MMM-yyyy", "dd MMM yyyy", "MMM dd yyyy"
			};

			if (DateTime.TryParseExact(datePart, formats, CultureInfo.InvariantCulture,
					DateTimeStyles.AllowWhiteSpaces, out var parsed) ||
				DateTime.TryParse(datePart, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed) ||
				DateTime.TryParse(datePart, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.AllowWhiteSpaces, out parsed))
			{
				date = parsed.Date;
				return true;
			}

			// Excel stores dates as a number when the cell is not date formatted.
			if (double.TryParse(datePart, NumberStyles.Float, CultureInfo.InvariantCulture, out var oaDate) &&
				oaDate > 0 && oaDate < 2958466)
			{
				date = DateTime.FromOADate(oaDate).Date;
				return true;
			}

			return false;
		}

		// ------------------------------------------------------------------ masters

		private async Task<StateLookup> LoadStatesAsync(CancellationToken cancellationToken)
		{
			var states = await _db.States
				.AsNoTracking()
				.Where(s => s.StateName != null)
				.Select(s => new { s.Id, s.StateName })
				.ToListAsync(cancellationToken);

			return new StateLookup(states.Select(s => s.StateName!).ToList());
		}

		private async Task<StatusLookup> LoadStatusesAsync(CancellationToken cancellationToken)
		{
			var statuses = await _db.Statuses
				.AsNoTracking()
				.Where(s => s.Name != null)
				.Select(s => new { s.Id, s.Name })
				.ToListAsync(cancellationToken);

			return new StatusLookup(statuses.Select(s => s.Name!).ToList());
		}

		/// <summary>
		/// The State master, read once. A name from an uploaded file is matched
		/// case-insensitively with its spacing normalized; a name that does not
		/// exist in the master is NEVER invented - it is reported as a validation
		/// error instead of being merged into something else.
		/// </summary>
		private sealed class StateLookup
		{
			/// <summary>
			/// Spelling differences seen in the daily exports. Each entry maps the
			/// misspelled name onto the state name the master is expected to hold;
			/// the target still has to exist in the master, so this can never merge
			/// a state that is not there.
			/// </summary>
			private static readonly Dictionary<string, string[]> Aliases =
				new(StringComparer.Ordinal)
				{
					{ "ANDRA PRADESH", new[] { "ANDHRA PRADESH" } },
					{ "MAHARASTRA", new[] { "MAHARASHTRA" } },
					{ "MAHARASHTRA", new[] { "MAHARASHTRA" } },
					{ "PUDUCHERI", new[] { "PUDUCHERRY", "PONDICHERRY" } },
					{ "PUDUCHERY", new[] { "PUDUCHERRY", "PONDICHERRY" } },
					{ "PONDICHERRY", new[] { "PUDUCHERRY" } },
					{ "TELUNGANA", new[] { "TELANGANA" } },
					{ "ANDHAMAN AND NICOBAR ISLANDS", new[] { "ANDAMAN AND NICOBAR ISLANDS" } },
					{ "UTTER PRADESH", new[] { "UTTAR PRADESH" } },
					{ "UTTAR PRADESH", new[] { "UTTAR PRADESH" } },
					{ "ORISSA", new[] { "ODISHA" } },
					{ "TAMILNADU", new[] { "TAMIL NADU" } },
					{ "WESTBENGAL", new[] { "WEST BENGAL" } },
					{ "CHATTISGARH", new[] { "CHHATTISGARH" } },
					{ "JAMMU KASHMIR", new[] { "JAMMU AND KASHMIR" } }
				};

			private readonly Dictionary<string, string> _byKey;

			public StateLookup(IEnumerable<string> stateNames)
			{
				_byKey = new Dictionary<string, string>(StringComparer.Ordinal);

				foreach (var stateName in stateNames)
				{
					var key = StateKey(stateName);
					if (key.Length == 0)
						continue;

					// The first row wins, so a duplicated master row cannot throw.
					if (!_byKey.ContainsKey(key))
						_byKey[key] = stateName.Trim();
				}
			}

			public bool TryResolve(string? raw, out string canonicalName)
			{
				canonicalName = string.Empty;

				var key = StateKey(raw);
				if (key.Length == 0)
					return false;

				if (_byKey.TryGetValue(key, out var name))
				{
					canonicalName = name;
					return true;
				}

				if (Aliases.TryGetValue(key, out var targets))
				{
					foreach (var target in targets)
					{
						if (_byKey.TryGetValue(target, out var aliasName))
						{
							canonicalName = aliasName;
							return true;
						}
					}
				}

				return false;
			}

			/// <summary>
			/// The comparison key of a state name: trimmed, upper case, with the
			/// spacing normalized and "&" read as "and", so "Andaman &amp; Nicobar
			/// Islands" and "Andaman and Nicobar Islands" are the same state.
			/// </summary>
			public static string StateKey(string? raw)
			{
				var normalized = MasterNormalizer.Normalize(raw);
				if (normalized.Length == 0)
					return string.Empty;

				var builder = new StringBuilder(normalized.Length + 4);
				foreach (var character in normalized)
				{
					if (character == '&')
					{
						builder.Append(" AND ");
						continue;
					}

					builder.Append(character);
				}

				return string.Join(" ", builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
			}
		}

		/// <summary>
		/// The Status master, read once. The status name from the uploaded file is
		/// resolved against the master so the comparison uses the master's own
		/// spelling; no numeric StatusId is ever assumed.
		/// </summary>
		private sealed class StatusLookup
		{
			private readonly Dictionary<string, string> _byKey;

			public StatusLookup(IEnumerable<string> statusNames)
			{
				_byKey = new Dictionary<string, string>(StringComparer.Ordinal);

				foreach (var statusName in statusNames)
				{
					if (string.IsNullOrWhiteSpace(statusName))
						continue;

					var key = statusName.Trim().ToUpperInvariant();
					if (key.Length == 0)
						continue;

					if (!_byKey.ContainsKey(key))
						_byKey[key] = statusName.Trim();
				}
			}

			/// <summary>The upper case status name to compare against NEW / ACK.</summary>
			public string Resolve(string? raw)
			{
				if (string.IsNullOrWhiteSpace(raw))
					return string.Empty;

				var trimmed = raw.Trim();

				if (_byKey.TryGetValue(trimmed.ToUpperInvariant(), out var masterName))
					return masterName.Trim().ToUpperInvariant();

				return trimmed.ToUpperInvariant();
			}

			public bool IsKnown(string key) => _byKey.ContainsKey(key);
		}

		// ------------------------------------------------------------------ helpers

		private enum ColumnKind
		{
			State,
			ClosingStock,
			ClosingGit,
			BalanceWithWs,
			ClosingBalance,
			QuantityMt,
			Status,
			EntryDate,
			Company,
			Plant,
			Product,
			CompanySalesInGit,
			WholesalerSalesInGit
		}

		private static readonly Dictionary<ColumnKind, string[]> ColumnAliases =
			new()
			{
				[ColumnKind.State] = new[] { "state", "statename" },
				[ColumnKind.ClosingStock] = new[] { "closingstock", "closingstockmt", "stockclosing" },
				[ColumnKind.ClosingGit] = new[] { "closinggit", "closinggitmt", "gitclosing" },
				[ColumnKind.BalanceWithWs] = new[] { "balancewithws", "balancewithwholesaler", "balwithws", "balancewithwsmt" },
				[ColumnKind.ClosingBalance] = new[] { "closingbalance", "closingbalancemt", "closingbal" },
				[ColumnKind.QuantityMt] = new[] { "quantitymt", "quantityinmt", "qtymt" },
				[ColumnKind.Status] = new[] { "status", "statusname", "transactionstatus", "txnstatus" },
				[ColumnKind.EntryDate] = new[] { "entrydate", "entrydatetime", "entrydateandtime" },
				[ColumnKind.Company] = new[] { "company", "companyname" },
				[ColumnKind.Plant] = new[] { "plant", "plantname" },
				[ColumnKind.Product] = new[] { "product", "productname", "companyproduct" }
			};

		private static string ColumnLabel(ColumnKind kind) => kind switch
		{
			ColumnKind.State => "STATE",
			ColumnKind.ClosingStock => "CLOSING STOCK",
			ColumnKind.ClosingGit => "CLOSING GIT",
			ColumnKind.BalanceWithWs => "BALANCE WITH WS",
			ColumnKind.ClosingBalance => "CLOSING BALANCE",
			ColumnKind.QuantityMt => "QUANTITY(MT)",
			ColumnKind.Status => "STATUS",
			ColumnKind.EntryDate => "ENTRY DATE",
			_ => kind.ToString().ToUpperInvariant()
		};

		private static StateAmounts Amount(
			Dictionary<string, StateAmounts> amounts,
			StateLookup states,
			string canonicalName)
		{
			var key = StateLookup.StateKey(canonicalName);

			if (!amounts.TryGetValue(key, out var amount))
			{
				amount = new StateAmounts { State = canonicalName };
				amounts[key] = amount;
			}

			return amount;
		}

		private sealed class StateAmounts
		{
			public string State { get; set; } = string.Empty;
			public decimal ClosingStock { get; set; }
			public decimal ClosingGit { get; set; }
			public decimal WholesalerStock { get; set; }
			public decimal RetailPointStock { get; set; }
			public decimal CompanySalesInGit { get; set; }
			public decimal WholesalerSalesInGit { get; set; }
		}

		private static string? Meta(string? current, string? candidate)
		{
			if (string.IsNullOrWhiteSpace(candidate))
				return current;

			return current ?? candidate.Trim();
		}
	}
}
