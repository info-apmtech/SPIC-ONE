using Microsoft.AspNetCore.Http;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace SpicAPI.Services
{
    /// <summary>
    /// Server-side location-scope enforcement for the SpecialAdmin role only.
    /// All other roles are left completely untouched.
    ///
    /// The SpecialAdmin's assigned locations come from the database at login and are
    /// carried in the JWT as comma-separated claims:
    ///   spic:assigned_state_ids / spic:assigned_region_ids / spic:assigned_hq_ids.
    ///
    /// A SpecialAdmin can NEVER submit a filter that reaches data outside that scope:
    /// requested State/Region/HQ lists are intersected with the assigned scope, and an
    /// empty request list is defaulted to the full assigned scope. Client-supplied
    /// location ids are therefore never trusted in isolation.
    /// </summary>
    public static class SpecialAdminScope
    {
        public static bool IsSpecialAdmin(ClaimsPrincipal? user)
        {
            var role = user?.FindFirst(ClaimTypes.Role)?.Value
                ?? user?.FindFirst("Role")?.Value;
            return role != null
                && Enum.TryParse<AppRole>(role, out var parsed)
                && parsed == AppRole.SpecialAdmin;
        }

        /// <summary>
        /// Intersect a filter's location scope with the current user's assigned scope.
        /// No-op for every role other than SpecialAdmin.
        /// </summary>
        public static void ApplyScope(ClaimsPrincipal? user, ILocationScopeFilter? filter)
        {
            if (filter == null || !IsSpecialAdmin(user)) return;

            var assignedStates = ParseIds(user!.FindFirst("spic:assigned_state_ids")?.Value);
            var assignedRegions = ParseIds(user.FindFirst("spic:assigned_region_ids")?.Value);
            var assignedHqs = ParseIds(user.FindFirst("spic:assigned_hq_ids")?.Value);

            // State: always mandatory. If the client requested states, keep only the
            // intersection; if it sent none, default to the full assigned scope.
            filter.StateIds = Restrict(filter.StateIds, assignedStates);
            filter.RegionIds = Restrict(filter.RegionIds, assignedRegions);
            filter.HeadQuarterIds = Restrict(filter.HeadQuarterIds, assignedHqs);
        }

        private static List<int> Restrict(List<int> requested, List<int> assigned)
        {
            if (assigned.Count == 0) return new List<int>();
            if (requested == null || requested.Count == 0)
                return assigned.ToList();
            return requested.Where(assigned.Contains).Distinct().ToList();
        }

        /// <summary>Assigned state ids for the current user (empty unless SpecialAdmin).</summary>
        public static List<int> AssignedStateIds(ClaimsPrincipal? user) =>
            IsSpecialAdmin(user) ? ParseIds(user!.FindFirst("spic:assigned_state_ids")?.Value) : new List<int>();

        /// <summary>Assigned region ids for the current user (empty unless SpecialAdmin).</summary>
        public static List<int> AssignedRegionIds(ClaimsPrincipal? user) =>
            IsSpecialAdmin(user) ? ParseIds(user!.FindFirst("spic:assigned_region_ids")?.Value) : new List<int>();

        /// <summary>Assigned headquarter ids for the current user (empty unless SpecialAdmin).</summary>
        public static List<int> AssignedHeadquarterIds(ClaimsPrincipal? user) =>
            IsSpecialAdmin(user) ? ParseIds(user!.FindFirst("spic:assigned_hq_ids")?.Value) : new List<int>();

        private static List<int> ParseIds(string? csv)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(csv)) return result;
            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var id) && id > 0 && !result.Contains(id))
                    result.Add(id);
            }
            return result;
        }
    }
}
