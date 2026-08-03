using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IAgeingReportService
	{
		Task<AgeingDashboardDto> GetDashboardAsync(
			AgeingReportFilter filter,
			CancellationToken cancellationToken = default);

		Task<List<AgeingRowDto>> GetAllRowsAsync(
			AgeingReportFilter filter,
			CancellationToken cancellationToken = default);
	}
}