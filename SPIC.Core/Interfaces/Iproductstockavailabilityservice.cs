using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IProductStockAvailabilityService
	{
		Task<ProductStockAvailabilityDto> GetDashboardAsync(
			ProductStockAvailabilityFilter filter,
			CancellationToken cancellationToken = default);
	}
}