using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    public class LoginState
    {
        public bool IsBusy { get; set; }
        public string? ErrorMessage { get; set; }

        private string? _token;
        public DateTime Expiration { get; set; }
        public bool IsTokenExpired => Expiration != default && DateTime.UtcNow >= Expiration.ToUniversalTime();
        public event Action? OnChange;

        public string? Token
        {
            get => _token;
            set
            {
                _token = value;
                ParseClaims(value);
                OnChange?.Invoke();
            }
        }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

        public AppRole? UserRole { get; private set; }
        public int StateId { get; private set; }
        public int RegionId { get; private set; }
        public int HQId { get; private set; }

        // Current authenticated user id (from token claims)
        public string? UserId { get; private set; }

        public bool IsAdmin => UserRole is AppRole.Admin or AppRole.CorporateAdmin or AppRole.Director or AppRole.AVP;
        public bool IsStateRole => UserRole is AppRole.SMD or AppRole.SMM;
        public bool IsRegionRole => UserRole is AppRole.RM or AppRole.RMD;
        public bool IsHQRole => UserRole is AppRole.MO or AppRole.MDO or AppRole.JMDO;

        // Specific role group helpers
        public bool IsHQCreatorRole => UserRole is AppRole.MO or AppRole.MDO or AppRole.JMDO;
        public bool IsRMGroup => UserRole is AppRole.RM or AppRole.RMD;
        public bool IsSMGroup => UserRole is AppRole.SMD or AppRole.SMM;
        public bool IsDirectorOrAVP => UserRole is AppRole.Director or AppRole.AVP;
        public bool IsReviewerRole => UserRole is AppRole.Admin or AppRole.CorporateAdmin or AppRole.Director or AppRole.AVP or AppRole.SMD or AppRole.SMM or AppRole.RM or AppRole.RMD;

        // ---------------- Page-level permissions (from Designation.RoleAccess) ----------------

        // Empty set => no restriction (treated as full access).
        // Preserves behaviour for users without an assigned designation.
        public HashSet<string> AllowedPages { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public void SetAllowedPages(string? roleAccessCsv)
        {
            AllowedPages = string.IsNullOrWhiteSpace(roleAccessCsv)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : roleAccessCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);
            OnChange?.Invoke();
        }

        public void ClearAllowedPages()
        {
            AllowedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            OnChange?.Invoke();
        }

        public bool CanAccess(PagePermission page) => CanAccess(page.ToString());

        public bool CanAccess(string pageKey)
        {
            // Admin group bypasses everything
            if (IsAdmin) return true;
            // No designation assigned => no restriction
            if (AllowedPages.Count == 0) return true;
            return AllowedPages.Contains(pageKey);
        }

        // -------------------------------------------------------------------------------------

        private void ParseClaims(string? token)
        {
            UserRole = null;
            StateId = 0;
            RegionId = 0;
            HQId = 0;

            if (string.IsNullOrWhiteSpace(token)) return;

            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return;

                var payload = parts[1];
                // Fix base64url padding
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                payload = payload.Replace('-', '+').Replace('_', '/');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Role claim (may be "role" or the long ClaimTypes.Role URI)
                string? roleClaim = null;
                if (root.TryGetProperty("role", out var rp)) roleClaim = rp.GetString();
                else if (root.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var rp2)) roleClaim = rp2.GetString();

                if (roleClaim != null && Enum.TryParse<AppRole>(roleClaim, out var role))
                    UserRole = role;

                if (root.TryGetProperty("spic:state_id", out var sp) && int.TryParse(sp.GetString(), out var sid))
                    StateId = sid;
                if (root.TryGetProperty("spic:region_id", out var rip) && int.TryParse(rip.GetString(), out var rid))
                    RegionId = rid;
                if (root.TryGetProperty("spic:hq_id", out var hp) && int.TryParse(hp.GetString(), out var hid))
                    HQId = hid;

                // Try to parse user identifier from common claim names
                string? uid = null;
                if (root.TryGetProperty("sub", out var sub)) uid = sub.GetString();
                if (string.IsNullOrEmpty(uid) && root.TryGetProperty("nameid", out var nameid)) uid = nameid.GetString();
                if (string.IsNullOrEmpty(uid) && root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out var nid)) uid = nid.GetString();
                if (string.IsNullOrEmpty(uid) && root.TryGetProperty("spic:user_id", out var sup)) uid = sup.GetString();
                UserId = uid;
            }
            catch { }
        }
    }
}