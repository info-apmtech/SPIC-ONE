using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.MauiBlazorApp.Shared.Services
{
	public class LoginState
	{
		public bool IsBusy { get; set; }
		public string? ErrorMessage { get; set; }
		public string? Token { get; set; }
		public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);
	}
}
