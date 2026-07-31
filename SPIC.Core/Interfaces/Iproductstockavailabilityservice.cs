using SPIC.Core.DTOs;
using System.Threading.Tasks;

namespace SPIC.Core.Interfaces
{
	public interface IProductStockAvailabilityService
	{
		/// <summary>Loads the whole dashboard (KPI cards, pivot columns, grid, grand total) for the filter.</summary>
		Task<ProductStockAvailabilityDto> GetDashboardAsync(ProductStockAvailabilityFilter filter);
	}
}