using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface ILiquidationCycleService
	{
		Task<LiqCycleDashboardDto> GetDashboardAsync(
			LiqCycleFilter filter,
			CancellationToken cancellationToken = default);

		Task<List<LiqCycleRowDto>> GetAllRowsAsync(
			LiqCycleFilter filter,
			CancellationToken cancellationToken = default);

		Task<List<LiqCycleProductDto>> GetProductsAsync(
			CancellationToken cancellationToken = default);

		// Same hidden dealer-key contract already used by the existing page:
		// R{id} = DealerRegistration, I{id} = IfmsDealer.
		Task<List<AckLookupItemDto>> GetDealersAsync(
			CancellationToken cancellationToken = default);
	}
}
