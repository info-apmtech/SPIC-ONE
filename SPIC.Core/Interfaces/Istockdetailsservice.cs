using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IStockDetailsService
	{
		Task<StockDetailsDto> GetDashboardAsync(
			StockDetailsFilter filter,
			CancellationToken cancellationToken = default);
	}
}