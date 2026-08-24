using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IAckCycleService
	{
		Task<AckCycleDashboardDto> GetDashboardAsync(
			AckCycleFilter filter,
			CancellationToken cancellationToken = default);

		Task<List<AckCycleRowDto>> GetAllRowsAsync(
			AckCycleFilter filter,
			CancellationToken cancellationToken = default);

		Task<List<AckLookupItemDto>> GetStatesAsync(
			CancellationToken cancellationToken = default);

		Task<List<AckLookupItemDto>> GetDistrictsAsync(
			List<int> stateIds,
			CancellationToken cancellationToken = default);

		Task<List<AckLookupItemDto>> GetProductsAsync(
			CancellationToken cancellationToken = default);

		Task<List<AckLookupItemDto>> GetStatusesAsync(
			CancellationToken cancellationToken = default);

		Task<List<AckLookupItemDto>> GetDealersAsync(
			CancellationToken cancellationToken = default);
	}
}
