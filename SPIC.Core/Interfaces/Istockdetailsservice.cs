using System.Collections.Generic;
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

		Task<List<StockDetailsProductDto>> GetProductsAsync(
			CancellationToken cancellationToken = default);
	}
}