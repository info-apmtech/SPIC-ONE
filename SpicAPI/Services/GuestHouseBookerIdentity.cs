using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
	public enum BookerCodeKind
	{
		/// <summary>The role has no fixed code: the guest form stays manual, exactly as before.</summary>
		NotBound = 0,
		Employee = 1,
		Dealer = 2
	}

	/// <summary>
	/// Result of resolving the signed-in booker's own Employee ID / Dealer Code.
	/// <see cref="Error"/> is set (and <see cref="Code"/> null) when the role requires a code but none
	/// could be found - the booking must then be refused rather than saved with a wrong or empty value.
	/// </summary>
	public sealed record BookerCode(BookerCodeKind Kind, string? Code, string? Error)
	{
		public bool IsBound => Kind != BookerCodeKind.NotBound;
		public static readonly BookerCode NotBound = new(BookerCodeKind.NotBound, null, null);
	}

	/// <summary>
	/// Single source of truth for the "Employee ID / Dealer Code" on a Guest House booking.
	/// The code is derived from the AUTHENTICATED user (the validated JWT's id and role) and the
	/// existing masters - never from anything the client sends:
	///
	///  - Dealer role           -> DealerRegistration.UserTableId == user id  -> DealerRegistration.DealerCode
	///                             (the same user-to-dealer link GET api/DealerRegistration/by-user uses);
	///  - Complimentary roles   -> the employee login resolved by the shared EmployeeloginSelection rule
	///    (GuestHouseBookingPolicy.IsEmployeeFreeBooking, the same test that grants free booking)
	///                             -> EmployeeInformation.EmployeeCode (the chain the Profile page uses);
	///  - every other role      -> NotBound: unchanged, the guest form value is used.
	/// </summary>
	public static class GuestHouseBookerIdentity
	{
		public static async Task<BookerCode> ResolveAsync(AppDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken = default)
		{
			if (user.Identity?.IsAuthenticated != true)
				return BookerCode.NotBound;

			if (!Enum.TryParse<AppRole>(user.FindFirstValue(ClaimTypes.Role), out var role))
				return BookerCode.NotBound;

			var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

			if (role == AppRole.Dealer)
			{
				if (string.IsNullOrWhiteSpace(userId))
					return new BookerCode(BookerCodeKind.Dealer, null, DealerMissing);

				// Newest registration that actually has a code (a new dealer has none until final approval).
				var dealerCode = await db.DealerRegistrations
					.AsNoTracking()
					.Where(d => d.UserTableId == userId && d.DealerCode != null && d.DealerCode != "")
					.OrderByDescending(d => d.Id)
					.Select(d => d.DealerCode)
					.FirstOrDefaultAsync(cancellationToken);

				return string.IsNullOrWhiteSpace(dealerCode)
					? new BookerCode(BookerCodeKind.Dealer, null, DealerMissing)
					: new BookerCode(BookerCodeKind.Dealer, dealerCode.Trim(), null);
			}

			if (GuestHouseBookingPolicy.IsEmployeeFreeBooking(role))
			{
				if (string.IsNullOrWhiteSpace(userId))
					return new BookerCode(BookerCodeKind.Employee, null, EmployeeMissing);

				var resolved = await db.Employeelogins.AsNoTracking()
					.ResolveAsync(userId, user.Identity?.Name, cancellationToken);
				var employeeInformationId = resolved.Row?.EmployeeInformationID ?? 0;
				if (employeeInformationId <= 0)
					return new BookerCode(BookerCodeKind.Employee, null, EmployeeMissing);

				var employeeCode = await db.EmployeeInformation
					.AsNoTracking()
					.Where(e => e.Id == employeeInformationId)
					.Select(e => e.EmployeeCode)
					.FirstOrDefaultAsync(cancellationToken);

				return string.IsNullOrWhiteSpace(employeeCode)
					? new BookerCode(BookerCodeKind.Employee, null, EmployeeMissing)
					: new BookerCode(BookerCodeKind.Employee, employeeCode.Trim(), null);
			}

			return BookerCode.NotBound;
		}

		private const string DealerMissing = "Your Dealer Code could not be found, so the booking cannot continue. Please contact support.";
		private const string EmployeeMissing = "Your Employee ID could not be found, so the booking cannot continue. Please contact support.";
	}
}
