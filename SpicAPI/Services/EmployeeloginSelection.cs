using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Services
{
    /// <summary>
    /// The single, canonical rule for deciding which <see cref="Employeelogin"/>
    /// row represents a logged-in user.
    /// </summary>
    /// <remarks>
    /// <para><b>Why one rule is needed.</b> The intended data model is one login
    /// row per user, but nothing enforces it: there is no unique index on
    /// Employeelogins.UserId, and Employeelogin carries no timestamp and no
    /// "current" flag. EmployeeBulkUploadController and EmployeeLoginSetupController
    /// both add rows, and historical imports can leave duplicates or legacy rows
    /// whose UserId holds a UserName instead of the Identity id. When duplicates
    /// exist, every "first" query is free to return a different row, which is how
    /// the Profile card, the Edit modal and the update could each act on a
    /// different record.</para>
    ///
    /// <para><b>The rule.</b> Candidates are every row whose UserId matches the
    /// user's Identity id, or (legacy rows only) their UserName. Of those, the
    /// current row is the first by:</para>
    /// <list type="number">
    ///   <item><description><see cref="Employeelogin.IsActive"/> descending — an
    ///   active row always wins over a deactivated one.</description></item>
    ///   <item><description><see cref="Employeelogin.Id"/> descending — the
    ///   entity has no CreatedAt/UpdatedAt, so the integer identity key is the
    ///   only recency signal available, and the highest Id is the most recently
    ///   inserted row.</description></item>
    /// </list>
    /// <para>This reproduces the previous "active row, else any row" behaviour
    /// exactly, but with a total order, so the result can never depend on
    /// database row order.</para>
    ///
    /// <para>AuthenticationController, ProfileController (GET and PUT) and the
    /// Profile page all resolve through here, so the JWT location claims, the
    /// displayed profile and the updated row cannot diverge.</para>
    /// </remarks>
    public static class EmployeeloginSelection
    {
        /// <summary>Applies the ordering half of the rule. Exposed separately so a
        /// caller that already holds the candidate rows can sort them identically
        /// (used by the read-only verification path).</summary>
        public static IOrderedQueryable<Employeelogin> InCurrentRowOrder(
            this IQueryable<Employeelogin> source) =>
            source
                .OrderByDescending(l => l.IsActive)
                .ThenByDescending(l => l.Id);

        /// <summary>Resolves the current row and reports how ambiguous the data
        /// was, so callers can surface a data-integrity problem instead of
        /// silently using an arbitrary row.</summary>
        public static async Task<CurrentEmployeeloginRow> ResolveAsync(
            this IQueryable<Employeelogin> source,
            string userId,
            string? userName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return CurrentEmployeeloginRow.Empty;

            // The UserName arm matches legacy rows written before UserId was
            // standardised to the Identity id. It is preserved deliberately, with
            // the same exact comparison AuthenticationController already used:
            // dropping it would break location claims for any account still on
            // that older format.
            var candidates = await source
                .Where(l => l.UserId == userId
                            || (userName != null && l.UserId == userName))
                .InCurrentRowOrder()
                .ToListAsync(cancellationToken);

            return new CurrentEmployeeloginRow
            {
                Row = candidates.FirstOrDefault(),
                TotalCandidates = candidates.Count,
                ActiveCandidates = candidates.Count(l => l.IsActive)
            };
        }
    }

    /// <summary>Outcome of resolving the current <see cref="Employeelogin"/> row.</summary>
    public sealed class CurrentEmployeeloginRow
    {
        public static readonly CurrentEmployeeloginRow Empty = new();

        public Employeelogin? Row { get; init; }

        /// <summary>How many rows matched the user. More than one means the
        /// one-row-per-user assumption does not hold for this account.</summary>
        public int TotalCandidates { get; init; }

        /// <summary>How many of those were active. More than one active row is a
        /// data-integrity fault: the system cannot tell which assignment is
        /// current, so the highest Id wins and the condition is logged.</summary>
        public int ActiveCandidates { get; init; }

        /// <summary>True when two or more active rows compete for the same user.</summary>
        public bool HasMultipleActiveRows => ActiveCandidates > 1;
    }
}
