using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Server-side Designation page permission for the employee / dealer Guest House endpoints of
	/// <see cref="GuestHouseBookingController"/>. Hiding a menu item is never the only control.
	///
	/// The required permission is declared per endpoint:
	///  - <see cref="PagePermission.GuestHouseBooking"/> for the room booking flow (houses, availability,
	///    room details, booking creation, payment, temporary document upload);
	///  - <see cref="PagePermission.MyBookings"/> for the signed-in user's own bookings (list, details,
	///    invoice, cancellation preview / request, refund status).
	///
	/// Rule, the same one the client applies (PageGuard / LoginState):
	///  - Admin and SuperAdmin keep their role bypass;
	///  - every other role, Dealer included, needs the permission in its Designation.RoleAccess
	///    (UserInfo.DesignationId -> Designation.RoleAccess, via RoleAccessPermissions.HasPage).
	///
	/// This filter only decides WHETHER the caller may use the page's endpoints. Booking ownership
	/// (IsBookingOwner) is still enforced inside each action: MyBookings never lets a user read or
	/// cancel another user's booking.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class GuestHouseBookingAccessAttribute : Attribute, IAsyncAuthorizationFilter
	{
		public PagePermission Permission { get; }

		public GuestHouseBookingAccessAttribute(PagePermission permission)
		{
			Permission = permission;
		}

		/// <summary>The access decision, free of HTTP and database types so it can be tested directly.</summary>
		public static bool IsGranted(bool isAdminOrSuperAdmin, string? designationRoleAccess, PagePermission permission) =>
			isAdminOrSuperAdmin || RoleAccessPermissions.HasPage(designationRoleAccess, permission);

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.User;
			if (user?.Identity?.IsAuthenticated != true)
			{
				context.Result = new UnauthorizedObjectResult(new { Success = false, Message = "Authentication required." });
				return;
			}

			var isAdmin = user.IsInRole(nameof(AppRole.Admin)) || user.IsInRole(nameof(AppRole.SuperAdmin));
			if (isAdmin)
				return;

			var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
			var designation = await GuestHouseReceptionistScope.GetDesignationAsync(db, user);

			if (IsGranted(false, designation?.RoleAccess, Permission))
				return;

			context.Result = new ObjectResult(new { Success = false, Message = "You do not have permission to use this feature." })
			{
				StatusCode = StatusCodes.Status403Forbidden
			};
		}
	}
}
