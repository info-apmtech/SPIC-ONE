using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IDealerInsightService
	{
		Task<Dictionary<int, DealerInsightDto>> GetInsightsAsync(
			IEnumerable<int> dealerRegistrationIds,
			CancellationToken cancellationToken = default);
	}
}
