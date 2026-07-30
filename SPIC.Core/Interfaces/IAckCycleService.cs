// ============================================================================
//  SPIC.Core / Interfaces / IAckCycleService.cs
// ============================================================================
using System.Collections.Generic;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IAckCycleService
	{
		// One call returns KPIs + both Top-5 lists + the paged grid.
		Task<AckCycleDashboardDto> GetDashboardAsync(AckCycleFilter filter);

		// Un-paged rows for Excel/PDF export (respects all filters, ignores paging).
		Task<List<AckCycleRowDto>> GetAllRowsAsync(AckCycleFilter filter);

		// Master data for the filter dropdowns.
		Task<List<AckLookupItemDto>> GetStatesAsync();
		Task<List<AckLookupItemDto>> GetDistrictsAsync(List<int> stateIds);
		Task<List<AckLookupItemDto>> GetProductsAsync();
		Task<List<AckLookupItemDto>> GetStatusesAsync();
		Task<List<AckLookupItemDto>> GetDealersAsync();
	}
}