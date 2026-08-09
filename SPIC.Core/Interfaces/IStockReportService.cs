using System.Collections.Generic;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IStockReportService
	{
		Task<List<ProductFilterOptionDto>> GetProductOptionsAsync();
		Task<StockDashboardDto> GetDashboardAsync(StockReportFilter filter);
		Task<List<StockRowDto>> GetAllRowsAsync(StockReportFilter filter);
	}
}
