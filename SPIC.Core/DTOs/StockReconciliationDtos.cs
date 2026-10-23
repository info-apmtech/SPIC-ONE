using System;
using System.Collections.Generic;
using System.IO;

namespace SPIC.Core.DTOs
{
	/// <summary>
	/// One state row of the State-wise Global Stock Reconciliation table.
	///
	/// Every number here is already the final value: the derived columns
	/// (GlobalStockTotal, ChannelTotal) are calculated ONCE, in the
	/// reconciliation service, and then carried on the row. The screen, the
	/// exported workbook and the emailed workbook all read these same values,
	/// so the three can never drift apart.
	/// </summary>
	public sealed class StockReconciliationRow
	{
		/// <summary>Canonical state name, as spelled in the State master.</summary>
		public string State { get; set; } = string.Empty;

		public decimal ClosingStock { get; set; }
		public decimal ClosingGit { get; set; }

		/// <summary>ClosingStock + ClosingGit.</summary>
		public decimal GlobalStockTotal { get; set; }

		public decimal WholesalerStock { get; set; }
		public decimal RetailPointStock { get; set; }
		public decimal CompanySalesInGit { get; set; }
		public decimal WholesalerSalesInGit { get; set; }

		/// <summary>WholesalerStock + RetailPointStock + CompanySalesInGit + WholesalerSalesInGit.</summary>
		public decimal ChannelTotal { get; set; }

		/// <summary>Total Company Stock (GlobalStockTotal) + Stock Lying with Wholesalers & Retailers (ChannelTotal).</summary>
		public decimal OverallStock { get; set; }
	}

	/// <summary>The "Overall Total" row: the vertical sum of every numeric column.</summary>
	public sealed class StockReconciliationTotals
	{
		public decimal ClosingStock { get; set; }
		public decimal ClosingGit { get; set; }
		public decimal GlobalStockTotal { get; set; }
		public decimal WholesalerStock { get; set; }
		public decimal RetailPointStock { get; set; }
		public decimal CompanySalesInGit { get; set; }
		public decimal WholesalerSalesInGit { get; set; }
		public decimal ChannelTotal { get; set; }

		/// <summary>Total Company Stock (GlobalStockTotal) + Stock Lying with Wholesalers & Retailers (ChannelTotal).</summary>
		public decimal OverallStock { get; set; }
	}

	/// <summary>
	/// The finished reconciliation. This is THE object the screen renders, the
	/// workbook is written from and the email attaches - one pipeline, one result.
	/// </summary>
	public sealed class StockReconciliationResult
	{
		/// <summary>The "As On Date" the run was made for. Drives the ACK + Entry Date rule.</summary>
		public DateTime AsOnDate { get; set; }

		/// <summary>Report metadata read from the uploaded files, when they carry it.</summary>
		public string Company { get; set; } = string.Empty;
		public string Plant { get; set; } = string.Empty;
		public string Product { get; set; } = string.Empty;

		public List<StockReconciliationRow> Rows { get; set; } = new();
		public StockReconciliationTotals Totals { get; set; } = new();

		/// <summary>True when at least one source report provided data for this date.</summary>
		public bool HasAnyData { get; set; }
		public bool HasGlobalStock { get; set; }
		public bool HasWholesalerStock { get; set; }
		public bool HasRetailPointStock { get; set; }
		public bool HasCompanySalesGit { get; set; }
		public bool HasWholesalerSalesGit { get; set; }

		/// <summary>Detailed source-level availability notices (e.g. missing uploads for the date).</summary>
		public List<string> MissingSources { get; set; } = new();

		/// <summary>Non-blocking remarks (an unknown status, a row left out, ...).</summary>
		public List<string> Warnings { get; set; } = new();

		/// <summary>The workbook file name to use for the export and the email attachment.</summary>
		public string FileName { get; set; } = string.Empty;

		/// <summary>Dynamic GFL State x Product Stock matrix sourced from WholesalerStockAsOnToday.</summary>
		public GflStockTableDto? GflStock { get; set; }
	}

	/// <summary>What happened to the email that was asked for (manually or automatically).</summary>
	public sealed class StockReconciliationEmailOutcome
	{
		/// <summary>True when an email send was actually attempted.</summary>
		public bool Attempted { get; set; }

		public bool Sent { get; set; }

		/// <summary>Empty when the email was sent; the reason when it was not.</summary>
		public string Message { get; set; } = string.Empty;
	}

	/// <summary>The reply of "Generate Reconciliation".</summary>
	public sealed class StockReconciliationGenerateResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;

		/// <summary>Why the run was rejected. Nothing is calculated when this is not empty.</summary>
		public List<string> ValidationErrors { get; set; } = new();

		/// <summary>Non-blocking remarks (unknown statuses, ignored summary rows, ...).</summary>
		public List<string> Warnings { get; set; } = new();

		/// <summary>Null when the run failed.</summary>
		public StockReconciliationResult? Result { get; set; }

		public StockReconciliationEmailOutcome Email { get; set; } = new();
	}

	/// <summary>Body of "Send Email": the recipients plus the result to attach.</summary>
	public sealed class StockReconciliationSendEmailRequest
	{
		public string? To { get; set; }
		public string? Cc { get; set; }
		public string? Subject { get; set; }
		public StockReconciliationResult? Result { get; set; }
	}

	/// <summary>One uploaded report file, already opened for reading.</summary>
	public sealed class StockReconciliationSourceFile
	{
		public Stream Content { get; set; } = Stream.Null;
		public string FileName { get; set; } = string.Empty;
	}

	/// <summary>
	/// The five report files of one reconciliation run. The Retail Point (DPT)
	/// report is a LIST, because the users export one file per state.
	/// </summary>
	public sealed class StockReconciliationInput
	{
		public StockReconciliationSourceFile? GlobalStock { get; set; }
		public StockReconciliationSourceFile? SalesReceipt { get; set; }
		public List<StockReconciliationSourceFile> RetailPoint { get; set; } = new();
		public StockReconciliationSourceFile? CompanySales { get; set; }
		public StockReconciliationSourceFile? WholesalerSales { get; set; }
	}

	/// <summary>Filters for database-backed State-wise Global Stock Reconciliation.</summary>
	public sealed class StockReconciliationDatabaseQueryRequest
	{
		public DateTime? AsOnDate { get; set; }
		public int? CompanyId { get; set; }
		public int? PlantId { get; set; }
		public int? ProductId { get; set; }
		public string? To { get; set; }
		public string? Cc { get; set; }
		public string? Subject { get; set; }
		public bool AutoSendEmail { get; set; }
	}

	public sealed class StockReconciliationFilterItem
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	public sealed class StockReconciliationFilterOptions
	{
		public List<StockReconciliationFilterItem> Companies { get; set; } = new();
		public List<StockReconciliationFilterItem> Plants { get; set; } = new();
		public List<StockReconciliationFilterItem> Products { get; set; } = new();
		public List<DateTime> AvailableDates { get; set; } = new();
	}

	/// <summary>Request for manual or automatic daily combined stock email.</summary>
	public sealed class DailyCombinedStockMailRequest
	{
		public DateTime? AsOnDate { get; set; }
		public string? To { get; set; }
		public string? Cc { get; set; }
		public string? Subject { get; set; }
	}

	/// <summary>Column descriptor for GFL Product Stock table.</summary>
	public sealed class GflStockProductColumnDto
	{
		public int ProductId { get; set; }
		public string OriginalName { get; set; } = string.Empty;
		public string DisplayName { get; set; } = string.Empty;
	}

	/// <summary>State row in GFL Product Stock table.</summary>
	public sealed class GflStockTableRowDto
	{
		public string State { get; set; } = string.Empty;
		public Dictionary<int, decimal> Quantities { get; set; } = new();
		public decimal Total { get; set; }
	}

	/// <summary>Section 2 GFL Stock data model (reused from ProductwiseStockAvailability).</summary>
	public sealed class GflStockTableDto
	{
		public DateTime AsOnDate { get; set; }
		public List<GflStockProductColumnDto> Columns { get; set; } = new();
		public List<GflStockTableRowDto> Rows { get; set; } = new();
		public Dictionary<int, decimal> ColumnTotals { get; set; } = new();
		public decimal GrandTotal { get; set; }
		public bool HasAnyData { get; set; }
	}

	/// <summary>Result of generating the daily combined stock email and Excel workbook.</summary>
	public sealed class DailyCombinedStockMailResult
	{
		public bool IsValid { get; set; }
		public List<string> ValidationErrors { get; set; } = new();
		public string Subject { get; set; } = string.Empty;
		public string HtmlBody { get; set; } = string.Empty;
		public byte[]? ExcelAttachment { get; set; }
		public string AttachmentFileName { get; set; } = string.Empty;
		public DateTime CurrentBusinessDate { get; set; }
		public DateTime PreviousBusinessDate { get; set; }
		public StockReconciliationResult? UreaResult { get; set; }
		public GflStockTableDto? GflResult { get; set; }
	}
}

