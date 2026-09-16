using System;

namespace SPIC.Core.Entities
{
    // Permission System Phase 1 (ADDITIVE ONLY - not used by any runtime code yet).
    //
    // ApplicationPage is a database-backed catalog mirror of the existing
    // PagePermission enum. It is seeded once from the enum + PageModuleAttribute
    // so every existing permission key is preserved verbatim. No current flow
    // (login, LoginState, NavMenu, PageGuard, Designation.razor, server checks)
    // reads this table yet; the running application continues to work entirely
    // from Designation.RoleAccess exactly as before.
    public class ApplicationPage
    {
        public int Id { get; set; }

        // Exact PagePermission enum name (e.g. "Register", "SDWADashboard").
        // Unique. Never rename existing keys.
        public required string Key { get; set; }

        // Human-friendly display name (matches the existing Designation UI's
        // "AnnualSales" -> "Annual Sales" prettification).
        public required string Name { get; set; }

        // PageModuleAttribute.Module grouping (e.g. "DealerRegistration").
        // Null = standalone page (not part of any wizard module).
        public string? Module { get; set; }

        // Enum declaration order, which is the order the existing Designation
        // UI uses to build and persist the RoleAccess CSV.
        public int SortOrder { get; set; }

        // Whether the page participates in View/Entry/Update/Delete actions.
        // All catalog entries today have actions (the existing grid always shows
        // the four action columns for every page).
        public bool HasActions { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}