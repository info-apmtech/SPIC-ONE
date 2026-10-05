using System.Threading;
using System.Threading.Tasks;

namespace SPIC.Ifms.Automation.Portal.Challenges
{
	/// <summary>
	/// What the login page is showing us, lifted out of Playwright so that solvers
	/// stay testable and know nothing about browsers.
	/// </summary>
	public sealed class CaptchaChallenge
	{
		/// <summary>
		/// Text scraped from the CAPTCHA element, when the portal renders the
		/// challenge as readable text rather than an image.
		/// </summary>
		public string? Text { get; init; }

		/// <summary>PNG bytes of the CAPTCHA image, when there is one.</summary>
		public byte[]? ImagePng { get; init; }

		/// <summary>1-based login attempt this challenge belongs to.</summary>
		public int Attempt { get; init; } = 1;

		public int? RunId { get; init; }

		/// <summary>Which company's login is blocked, so the prompt can say so.</summary>
		public string? AccountKey { get; init; }
		public string? CompanyName { get; init; }

		/// <summary>
		/// What the automatic solvers read before giving up. Shown to whoever is
		/// asked to type it by hand, so an obvious near-miss is easy to correct.
		/// </summary>
		public string? FailedGuesses { get; init; }
	}

	public sealed class CaptchaAnswer
	{
		public required string Value { get; init; }

		/// <summary>Which solver produced it, recorded on the run for diagnostics.</summary>
		public required string Method { get; init; }

		/// <summary>0..1 where the solver can estimate it; OCR reports its own.</summary>
		public double Confidence { get; init; } = 1.0;

		/// <summary>
		/// Set when a person answered this from the app, so the login can report
		/// back whether the portal actually accepted their reading.
		/// </summary>
		public int? ChallengeRequestId { get; init; }
	}

	public interface ICaptchaSolver
	{
		/// <summary>HtmlText or Ocr — matched against Ifms:Captcha:Strategies.</summary>
		string Name { get; }

		/// <summary>Returns null when this solver cannot answer; the chain moves on.</summary>
		Task<CaptchaAnswer?> SolveAsync(CaptchaChallenge challenge, CancellationToken cancellationToken);
	}
}
