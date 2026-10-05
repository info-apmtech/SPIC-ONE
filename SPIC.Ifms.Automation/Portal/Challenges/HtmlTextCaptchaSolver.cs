using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SPIC.Ifms.Automation.Portal.Challenges
{
	/// <summary>
	/// Handles the case where the portal writes the challenge into the DOM as text
	/// instead of an image. Two shapes are common on Indian government portals:
	/// a literal code ("7K2P9") and an arithmetic question ("7 + 3 = ?").
	/// When this solver applies there is no OCR and no guessing involved.
	/// </summary>
	public sealed class HtmlTextCaptchaSolver : ICaptchaSolver
	{
		public string Name => "HtmlText";

		private readonly ILogger<HtmlTextCaptchaSolver> _logger;

		private static readonly Regex ArithmeticPattern = new(
			@"(-?\d+)\s*(\+|-|–|—|\*|x|×|plus|minus|into|times)\s*(-?\d+)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex WordyNoise = new(
			@"(what\s+is|enter|type|the\s+result|answer|captcha|code|please)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public HtmlTextCaptchaSolver(ILogger<HtmlTextCaptchaSolver> logger) => _logger = logger;

		public Task<CaptchaAnswer?> SolveAsync(CaptchaChallenge challenge, CancellationToken cancellationToken)
		{
			var raw = challenge.Text;
			if (string.IsNullOrWhiteSpace(raw))
				return Task.FromResult<CaptchaAnswer?>(null);

			var text = raw.Replace(" ", " ").Trim();

			var arithmetic = ArithmeticPattern.Match(text);
			if (arithmetic.Success)
			{
				var result = Evaluate(arithmetic);
				if (result.HasValue)
				{
					_logger.LogInformation(
						"CAPTCHA is arithmetic ({Question}) and resolves to {Answer}.",
						arithmetic.Value, result.Value);

					return Task.FromResult<CaptchaAnswer?>(new CaptchaAnswer
					{
						Value = result.Value.ToString(CultureInfo.InvariantCulture),
						Method = Name
					});
				}
			}

			// Not arithmetic: strip the instruction words and keep the code itself.
			var stripped = WordyNoise.Replace(text, " ");
			var code = new string(stripped.Where(char.IsLetterOrDigit).ToArray());

			if (code.Length is < 3 or > 12)
			{
				_logger.LogDebug(
					"CAPTCHA text {Text} did not reduce to a plausible code; deferring to the next solver.",
					text);
				return Task.FromResult<CaptchaAnswer?>(null);
			}

			_logger.LogInformation("CAPTCHA was readable from the page as {Answer}.", code);
			return Task.FromResult<CaptchaAnswer?>(new CaptchaAnswer { Value = code, Method = Name });
		}

		private static long? Evaluate(Match match)
		{
			if (!long.TryParse(match.Groups[1].Value, out var left) ||
				!long.TryParse(match.Groups[3].Value, out var right))
			{
				return null;
			}

			return match.Groups[2].Value.ToLowerInvariant() switch
			{
				"+" or "plus" => left + right,
				"-" or "–" or "—" or "minus" => left - right,
				"*" or "x" or "×" or "into" or "times" => left * right,
				_ => null
			};
		}
	}
}
