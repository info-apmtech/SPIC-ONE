using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SPIC.Ifms.Automation.Portal
{
	/// <summary>
	/// Expands the {{token}} placeholders used in report-job step values, so that
	/// a filter such as "from date" is written once in configuration and resolves
	/// to the right date on every run.
	/// </summary>
	public sealed class RunTokens
	{
		private static readonly Regex TokenPattern = new(
			@"\{\{\s*(?<name>[A-Za-z]+)\s*(?::\s*(?<format>[^}]+?)\s*)?\}\}",
			RegexOptions.Compiled);

		private const string DefaultDateFormat = "dd/MM/yyyy";

		private readonly Dictionary<string, DateTime> _dates;
		private readonly Dictionary<string, string> _literals;

		public RunTokens(DateTime reportDate, DateTime runDateLocal, string userName)
		{
			_dates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase)
			{
				["reportDate"] = reportDate,
				["fromDate"] = reportDate,
				["toDate"] = reportDate,
				["today"] = runDateLocal.Date,
				["yesterday"] = runDateLocal.Date.AddDays(-1),
				["monthStart"] = new DateTime(reportDate.Year, reportDate.Month, 1),
				["monthEnd"] = new DateTime(reportDate.Year, reportDate.Month,
					DateTime.DaysInMonth(reportDate.Year, reportDate.Month))
			};

			_literals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["userName"] = userName,
				["financialYear"] = FinancialYear(reportDate)
			};
		}

		/// <summary>Overrides or adds a token, e.g. a per-job date range.</summary>
		public RunTokens WithDate(string name, DateTime value)
		{
			_dates[name] = value;
			return this;
		}

		public RunTokens WithLiteral(string name, string value)
		{
			_literals[name] = value;
			return this;
		}

		public string? Resolve(string? template)
		{
			if (string.IsNullOrEmpty(template))
				return template;

			return TokenPattern.Replace(template, match =>
			{
				var name = match.Groups["name"].Value;
				var format = match.Groups["format"].Success
					? match.Groups["format"].Value
					: null;

				if (_dates.TryGetValue(name, out var date))
					return date.ToString(format ?? DefaultDateFormat, CultureInfo.InvariantCulture);

				if (_literals.TryGetValue(name, out var literal))
					return literal;

				// Leave unknown tokens intact rather than silently emitting an empty
				// filter — an obviously wrong value in the log beats a wrong report.
				return match.Value;
			});
		}

		/// <summary>Indian financial year label for a date, e.g. "2026-2027".</summary>
		private static string FinancialYear(DateTime date) =>
			date.Month >= 4
				? $"{date.Year}-{date.Year + 1}"
				: $"{date.Year - 1}-{date.Year}";
	}
}
