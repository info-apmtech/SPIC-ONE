using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SPIC.Core.Help
{
    /// <summary>
    /// A short, stable fingerprint of a section's text. It ties a translation (and its review
    /// approval, and its cached voice clip) to the exact English it was made from: change the
    /// English and the hash changes, so stale translations and approvals are detected.
    /// Line endings and trailing spaces are ignored so editors cannot change it by accident.
    /// </summary>
    public static class HelpHash
    {
        public static string Of(string title, string body)
        {
            var normalized = Normalize(title) + "\n" + Normalize(body);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(bytes).ToLowerInvariant().Substring(0, 16);
        }

        private static string Normalize(string? text) =>
            string.Join("\n",
                (text ?? string.Empty)
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Split('\n')
                    .Select(l => l.TrimEnd()))
                .Trim();
    }
}
