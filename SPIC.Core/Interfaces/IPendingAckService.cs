using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IPendingAckService
	{
		Task<PendingAckDashboardDto> GetDashboardAsync(
			PendingAckFilter filter,
			CancellationToken cancellationToken = default);

		Task<List<PendingAckRowDto>> GetAllRowsAsync(
			PendingAckFilter filter,
			CancellationToken cancellationToken = default);

		Task<List<PendingAckDealerTypeDto>> GetDealerTypesAsync(
			CancellationToken cancellationToken = default);

		// Product + IFMS Product dropdown options.
		Task<List<PendingAckProductDto>> GetProductsAsync(
			CancellationToken cancellationToken = default);

		Task<List<PendingAckDealerDto>> GetDealersAsync(
			CancellationToken cancellationToken = default);
	}
}