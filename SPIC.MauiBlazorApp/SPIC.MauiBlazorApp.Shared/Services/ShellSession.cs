using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// The small amount of shell state that MainLayout used to keep in private fields and
    /// hand to its markup directly: the signed-in user's display name / role / photo, and
    /// the logout action.
    ///
    /// It exists so that the shell components split out of MainLayout
    /// (<c>Components/Shell/TopbarUserMenu.razor</c>) can take NO parameters. A component
    /// with an empty parameter view is skipped by the renderer when its parent re-renders,
    /// while any parameter - an <c>EventCallback</c> in particular, which never compares
    /// equal - would drag the child into every single parent render.
    ///
    /// Registered as scoped (per circuit on the web host, per app on MAUI) in both
    /// SPIC.MauiBlazorApp.Web/Program.cs and SPIC.MauiBlazorApp/MauiProgram.cs.
    /// </summary>
    public sealed class ShellSession
    {
        /// <summary>Raised when the current user's name / role / photo changes.</summary>
        public event Action? OnChange;

        public string UserName { get; private set; } = "User";

        public string UserRole { get; private set; } = "";

        /// <summary>Optional profile photo from the JWT. If unavailable, the initial is shown.</summary>
        public string? ProfileImageUrl { get; private set; }

        /// <summary>First letter of <see cref="UserName"/>, uppercased; "U" when unknown.</summary>
        public string UserInitial =>
            string.IsNullOrWhiteSpace(UserName)
                ? "U"
                : UserName.Trim().Substring(0, 1).ToUpper();


        /* =========================================================
           LOGOUT BRIDGE

           MainLayout still owns the logout flow, because only it can hide the whole
           authenticated shell (menu + profile icon + content) as one state before any
           authentication state is cleared. The shell components call through here
           instead of receiving an EventCallback parameter.
           ========================================================= */

        public Func<Task>? LogoutHandler { get; set; }

        public Task LogoutAsync() =>
            LogoutHandler is null ? Task.CompletedTask : LogoutHandler();


        /* =========================================================
           LOAD USER NAME + ROLE FROM JWT
           (moved verbatim out of MainLayout)
           ========================================================= */

        public void LoadFromToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var userName = "User";
            var userRole = "";
            string? profileImageUrl = null;

            try
            {
                /*
                 * JWT Structure:
                 *
                 * Header.Payload.Signature
                 */

                var tokenParts = token.Split('.');

                if (tokenParts.Length < 2)
                {
                    return;
                }


                var payload = tokenParts[1];


                /*
                 * Convert Base64 URL to normal Base64
                 */

                payload = payload
                    .Replace('-', '+')
                    .Replace('_', '/');


                /*
                 * Add Base64 padding
                 */

                switch (payload.Length % 4)
                {
                    case 2:
                        payload += "==";
                        break;

                    case 3:
                        payload += "=";
                        break;
                }


                var jsonBytes =
                    Convert.FromBase64String(payload);

                var json =
                    Encoding.UTF8.GetString(jsonBytes);


                using var document =
                    JsonDocument.Parse(json);

                var root =
                    document.RootElement;


                /*
                 * USER NAME
                 *
                 * Supports common JWT claim names.
                 */

                userName =
                    GetJwtClaim(
                        root,

                        "name",
                        "Name",

                        "unique_name",

                        "username",
                        "Username",
                        "UserName",

                        "preferred_username",

                        "given_name",

                        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
                    )
                    ?? "User";


                /*
                 * ROLE
                 *
                 * Supports normal role claim
                 * and ASP.NET Identity role claim.
                 */

                userRole =
                    GetJwtClaim(
                        root,

                        "role",
                        "Role",
                        "roles",
                        "Roles",

                        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                    )
                    ?? "";


                profileImageUrl =
                    GetJwtClaim(
                        root,
                        "picture",
                        "profile_image",
                        "profileImage",
                        "ProfileImage",
                        "ProfileImageUrl",
                        "profile_image_url"
                    );
            }
            catch
            {
                /*
                 * If JWT parsing fails,
                 * don't break the shell.
                 */

                userName = "User";

                userRole = "";

                profileImageUrl = null;
            }

            SetUser(userName, userRole, profileImageUrl);
        }


        /// <summary>Resets to the signed-out values (used by manual and automatic logout).</summary>
        public void ClearUser() => SetUser("User", "", null);


        private void SetUser(string userName, string userRole, string? profileImageUrl)
        {
            var changed =
                UserName != userName
                || UserRole != userRole
                || ProfileImageUrl != profileImageUrl;

            UserName = userName;
            UserRole = userRole;
            ProfileImageUrl = profileImageUrl;

            if (changed)
            {
                OnChange?.Invoke();
            }
        }


        /* =========================================================
           GET JWT CLAIM
           ========================================================= */

        private static string? GetJwtClaim(
            JsonElement root,
            params string[] claimNames)
        {
            foreach (var claimName in claimNames)
            {
                if (!root.TryGetProperty(
                        claimName,
                        out var claim))
                {
                    continue;
                }


                /*
                 * Normal string claim
                 */

                if (claim.ValueKind ==
                    JsonValueKind.String)
                {
                    var value =
                        claim.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }


                /*
                 * Role can sometimes be array
                 *
                 * Example:
                 *
                 * "roles": [
                 *     "Admin",
                 *     "Manager"
                 * ]
                 */

                if (claim.ValueKind ==
                    JsonValueKind.Array)
                {
                    var values =
                        new List<string>();

                    foreach (var item
                             in claim.EnumerateArray())
                    {
                        if (item.ValueKind ==
                            JsonValueKind.String)
                        {
                            var value =
                                item.GetString();

                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                values.Add(value);
                            }
                        }
                    }


                    if (values.Count > 0)
                    {
                        return string.Join(", ", values);
                    }
                }
            }


            return null;
        }
    }
}
