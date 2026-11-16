using System.Text;
using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Builds a signed-in <see cref="LoginState"/> for a role and a designation's RoleAccess CSV,
/// the same two inputs PageGuard and the menus decide from. The token is an unsigned JWT:
/// LoginState only reads the payload's role claim, it never validates a signature.
/// </summary>
internal static class TestLogin
{
	public static LoginState As(AppRole role, string roleAccessCsv, string? designationName = null)
	{
		var login = new LoginState();
		var payload = Base64Url($"{{\"role\":\"{role}\"}}");
		login.Token = $"x.{payload}.x";
		login.SetAllowedPages(roleAccessCsv);
		login.SetDesignationName(designationName);
		return login;
	}

	public static LoginState SignedOut() => new();

	private static string Base64Url(string json) =>
		Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
}
