using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IStockDetailsService
	{
		/// <summary>
		/// Loads the KPI cards, dynamic date labels, state ledger, grand total,
		/// sorting, search and pagination for the Stock Details page.
		/// </summary>
		Task<StockDetailsDto> GetDashboardAsync(
			StockDetailsFilter filter,
			CancellationToken cancellationToken = default);
	}
}