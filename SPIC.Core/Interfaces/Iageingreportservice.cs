// ============================================================================
//  IAgeingReportService  — SPIC.Core/Interfaces/ (beside IStockReportService)
// ============================================================================

using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IAgeingReportService
	{
		Task<AgeingDashboardDto> GetDashboardAsync(AgeingReportFilter filter);

		// All filtered rows (no paging) — used by Excel / PDF export.
		Task<List<AgeingRowDto>> GetAllRowsAsync(AgeingReportFilter filter);
	}
}