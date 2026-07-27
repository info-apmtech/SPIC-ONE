// ============================================================================
//  IPendingAckService
//  Location: SPIC.Core/Interfaces/  (same folder as IStockReportService.cs)
//  Adjust the namespace to match your project if it differs.
// ============================================================================

using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IPendingAckService
	{
		Task<PendingAckDashboardDto> GetDashboardAsync(PendingAckFilter filter);

		// All filtered rows (no paging) — used by Excel / PDF export.
		Task<List<PendingAckRowDto>> GetAllRowsAsync(PendingAckFilter filter);

		// Filter dropdowns that need custom queries.
		Task<List<PendingAckDealerTypeDto>> GetDealerTypesAsync();          // from DealerTypes
		Task<List<PendingAckDealerDto>> GetDealersAsync();                  // DealerRegistrations + IfmsDealers
	}
}