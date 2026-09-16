using System;

namespace SPIC.Core.Entities
{
    // Permission System Phase 1 (ADDITIVE ONLY - not used by any runtime code yet).
    //
    // DesignationPermission is a normalized, compatibility copy of the existing
    // Designation.RoleAccess CSV, one row per (Designation, Page) with the four
    // action flags. Backfilled once from RoleAccess using the exact same semantics
    // the running application uses (bare "Page" = full access to that page;
    // "Page.Action" = that single action). No runtime flow reads this table yet.
    public class DesignationPermission
    {
        // Composite primary key: one row per (Designation, Page).
        public int DesignationId { get; set; }
        public int PageId { get; set; }

        public bool View { get; set; }
        public bool Entry { get; set; }
        public bool Update { get; set; }
        public bool Delete { get; set; }

        public Designation? Designation { get; set; }
        public ApplicationPage? Page { get; set; }
    }
}