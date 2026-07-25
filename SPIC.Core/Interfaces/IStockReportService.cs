using SPIC.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Interfaces
{
	public interface IStockReportService
	{
		/// <summary>Loads every dashboard widget (cards, charts, grid) for the given filter.</summary>
		Task<StockDashboardDto> GetDashboardAsync(StockReportFilter filter);

		/// <summary>Returns all filtered rows with no paging - used by Excel / PDF export.</summary>
		Task<List<StockRowDto>> GetAllRowsAsync(StockReportFilter filter);
	}
}
