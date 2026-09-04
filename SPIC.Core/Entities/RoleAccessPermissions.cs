using System;
using System.Collections.Generic;
using System.Linq;

namespace SPIC.Core.Entities
{
    // Shared, reusable parsing of a Designation.RoleAccess CSV ("Page.Action" tokens,
    // e.g. "SDWA.View,SDWA.Entry,SchemeApproval.View"). This is the SAME algorithm
    // SPIC.MauiBlazorApp.Shared.Services.LoginState.CanAccess/Can already implement
    // client-side (parsed from AllowedPages there); this is the server-side
    // equivalent so backend code (which has no LoginState/JWT-parsed AllowedPages)
    // can ask the same question directly against a Designation's RoleAccess without
    // a second, divergent permission mechanism.
    public static class RoleAccessPermissions
    {
        // Returns the page part of a token: "Register.View" -> "Register".
        // A bare legacy token ("Register") returns itself.
        public static string PagePart(string token)
        {
            var dot = token.IndexOf('.');
            return dot < 0 ? token : token.Substring(0, dot);
        }

        public static HashSet<string> ParseTokens(string? roleAccessCsv) =>
            string.IsNullOrWhiteSpace(roleAccessCsv)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : roleAccessCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Page-level: does this RoleAccess CSV grant ANY permission token for the
        // given page (any action, or a legacy bare page token)?
        public static bool HasPage(string? roleAccessCsv, string pageKey)
        {
            var tokens = ParseTokens(roleAccessCsv);
            return tokens.Count > 0 &&
                   tokens.Any(t => string.Equals(PagePart(t), pageKey, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasPage(string? roleAccessCsv, PagePermission page) =>
            HasPage(roleAccessCsv, page.ToString());
    }
}
