using System;
using System.Linq;

namespace SPIC.Core.Help
{
    /// <summary>
    /// Matches a guide section's route patterns (<c>/PublicityRequest/edit/{id}</c>) against the
    /// URL the user is on. Case, scheme/host, query string, fragment and a trailing slash are
    /// ignored; a <c>{name}</c> (or <c>{name:int}</c>) segment matches exactly one segment.
    /// </summary>
    public static class HelpRoutes
    {
        public static bool Matches(string pattern, string path)
        {
            var p = Segments(pattern);
            var u = Segments(path);
            if (p.Length != u.Length) return false;
            for (var i = 0; i < p.Length; i++)
            {
                if (IsParameter(p[i])) continue;
                if (!string.Equals(p[i], u[i], StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        /// <summary>Lower-case path with a leading slash, no host / query / fragment / trailing slash.</summary>
        public static string Normalize(string path)
        {
            var value = path ?? string.Empty;
            // Any absolute app URL: https://0.0.0.1/... (Android, Windows), app://0.0.0.1/... (iOS,
            // MacCatalyst), http(s)://host/... (web). A "file" URI is a relative path that .NET on
            // some platforms reads as absolute, so it is left alone.
            if (Uri.TryCreate(value, UriKind.Absolute, out var abs) && !abs.IsFile)
                value = abs.AbsolutePath;
            value = value.Split('?', '#')[0].Trim().TrimEnd('/');
            if (!value.StartsWith('/')) value = "/" + value;
            return value.ToLowerInvariant();
        }

        /// <summary>A concrete URL for a pattern (each parameter becomes "0"), used for access checks.</summary>
        public static string Sample(string pattern) =>
            "/" + string.Join("/", RawSegments(pattern).Select(s => IsParameter(s) ? "0" : s));

        /// <summary>The pattern with every parameter written as "{}", lower-case: "/lab/testentry/{}".</summary>
        public static string Shape(string pattern) =>
            "/" + string.Join("/", Segments(pattern).Select(s => IsParameter(s) ? "{}" : s));

        public static bool HasParameters(string pattern) => RawSegments(pattern).Any(IsParameter);

        /// <summary>Number of fixed (non-parameter) segments: the more, the more specific a match.</summary>
        public static int LiteralSegments(string pattern) => RawSegments(pattern).Count(s => !IsParameter(s));

        private static bool IsParameter(string segment) =>
            segment.StartsWith('{') && segment.EndsWith('}');

        private static string[] Segments(string path) =>
            Normalize(path).Split('/', StringSplitOptions.RemoveEmptyEntries);

        private static string[] RawSegments(string path) =>
            (path ?? string.Empty).Split('?', '#')[0].Trim().Split('/', StringSplitOptions.RemoveEmptyEntries);
    }
}
