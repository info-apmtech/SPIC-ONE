using System.Collections.Generic;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface ILiquidationCycleService
	{
		Task<LiqCycleDashboardDto> GetDashboardAsync(LiqCycleFilter filter);
		Task<List<LiqCycleRowDto>> GetAllRowsAsync(LiqCycleFilter filter);
	}
}