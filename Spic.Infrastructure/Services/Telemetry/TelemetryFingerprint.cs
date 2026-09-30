using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Error grouping key: SHA-256 (hex, 64) of source + exception type + normalised message (guids and
/// digits replaced by '#', whitespace collapsed, lower-case) + the first stack frame line. Ids and
/// counts in a message therefore do not split one bug into many groups.
/// </summary>
public static partial class TelemetryFingerprint
{
    public static string Compute(TelemetrySource source, string? exceptionType, string? message, string? topFrame)
    {
        var text = $"{(int)source}|{(exceptionType ?? "").Trim()}|{Normalize(message)}|{Normalize(topFrame)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    /// <summary>First "at ..." line of a stack trace (or its first line), without the file / line suffix.</summary>
    public static string? TopFrame(string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace)) return null;
        string? first = null;
        foreach (var raw in stackTrace.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            first ??= line;
            if (line.StartsWith("at ", StringComparison.Ordinal))
            {
                first = line;
                break;
            }
        }
        if (first is null) return null;
        var inFile = first.IndexOf(" in ", StringComparison.Ordinal);
        return inFile > 0 ? first[..inFile] : first;
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var v = Guids().Replace(value, "#");
        v = Digits().Replace(v, "#");
        v = Spaces().Replace(v, " ");
        return v.Trim().ToLowerInvariant();
    }

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex Guids();

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
