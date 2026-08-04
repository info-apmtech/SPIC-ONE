using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	/// <summary>
	/// Request used by the Stock Details dashboard, export and filter actions.
	/// </summary>
	public class StockDetailsFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		public List<int> FinancialYearIds { get; set; } = new();
		public List<int> StateIds { get; set; } = new();

		// Backward-compatible approved Product IDs used by existing clients.
		public List<int> ProductIds { get; set; } = new();

		// Combined product keys used by the updated Product dropdown:
		// P:10 = Product.Id 10, I:10 = IfmsProduct.Id 10.
		public List<string> ProductKeys { get; set; } = new();

		public string? Search { get; set; }
		public string? SortColumn { get; set; }
		public string? SortDir { get; set; } = "asc";

		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	public class StockDetailsDto
	{
		public StockDetailsSummaryDto Summary { get; set; } = new();
		public StockDetailsLabelsDto Labels { get; set; } = new();
		public StockDetailsRowDto GrandTotal { get; set; } = new();
		public PagedResult<StockDetailsRowDto> Grid { get; set; } = new();
	}

	public class StockDetailsSummaryDto
	{
		public decimal TotalStock { get; set; }
		public decimal TotalSales { get; set; }
		public decimal ClosingStock { get; set; }
		public double SalesPct { get; set; }
	}

	public class StockDetailsLabelsDto
	{
		public string OpeningAsOn { get; set; } = string.Empty;
		public string SuppliesMonth { get; set; } = string.Empty;
		public string SalesBeforeRange { get; set; } = string.Empty;
		public string SalesOnDay { get; set; } = string.Empty;
		public string ClosingAsOn { get; set; } = string.Empty;
	}

	public class StockDetailsRowDto
	{
		public int StateId { get; set; }
		public string StateName { get; set; } = string.Empty;

		public decimal OpeningStock { get; set; }
		public decimal Supplies { get; set; }
		public decimal TotalStock { get; set; }

		public decimal SalesBefore { get; set; }
		public decimal SalesOnDay { get; set; }
		public decimal TotalSales { get; set; }

		public decimal ClosingStock { get; set; }
		public double SalesPct { get; set; }
	}

	public class StockDetailsProductDto
	{
		public string Key { get; set; } = string.Empty;

		// Keeps Select2/JSON consumers that expect an Id-like field compatible.
		public string Id
		{
			get => Key;
			set => Key = value;
		}

		public string Name { get; set; } = string.Empty;
		public string Source { get; set; } = string.Empty;
	}
}