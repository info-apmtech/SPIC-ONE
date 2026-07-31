using SPIC.Core.DTOs;
using System.Threading.Tasks;

namespace SPIC.Core.Interfaces
{
	public interface IStockDetailsService
	{
		/// <summary>Loads the whole Stock Details dashboard (KPI cards, ledger grid, grand total) for the filter.</summary>
		Task<StockDetailsDto> GetDashboardAsync(StockDetailsFilter filter);
	}
}