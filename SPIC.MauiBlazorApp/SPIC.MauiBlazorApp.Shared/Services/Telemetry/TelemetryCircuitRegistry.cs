using System.Collections.Concurrent;

namespace SPIC.MauiBlazorApp.Shared.Services.Telemetry;

/// <summary>
/// Who is on which circuit (web host) or app run (MAUI), so the host logger provider can put a
/// user and a route on an error it only knows by circuit id. Kept current by ClientTelemetry and
/// the web host's TelemetryCircuitHandler. Singleton in both hosts.
/// </summary>
public sealed class TelemetryCircuitRegistry
{
    public readonly record struct Entry(string? UserId, string? UserName, string? Role, string? Route);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public int Count => _entries.Count;

    /// <summary>
    /// MAUI only: the signed-in user's token, so the logger provider can post as that user.
    /// Never set on the web host (many users share the process).
    /// </summary>
    public string? AppToken { get; set; }

    public void Set(string sessionId, Entry entry)
    {
        if (string.IsNullOrEmpty(sessionId)) return;
        _entries[sessionId] = entry;
    }

    /// <summary>Adds the session with no user yet unless it is already known.</summary>
    public void Touch(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return;
        _entries.TryAdd(sessionId, default);
    }

    public void Remove(string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId)) _entries.TryRemove(sessionId, out _);
    }

    public bool TryGet(string sessionId, out Entry entry) => _entries.TryGetValue(sessionId, out entry);

    /// <summary>The entry whose session id appears in <paramref name="text"/> (best effort).</summary>
    public bool TryFindIn(string? text, out Entry entry)
    {
        entry = default;
        if (string.IsNullOrEmpty(text) || _entries.IsEmpty) return false;

        foreach (var pair in _entries)
        {
            if (text.Contains(pair.Key, StringComparison.Ordinal))
            {
                entry = pair.Value;
                return true;
            }
        }

        return false;
    }

    /// <summary>The only entry, when there is exactly one (the MAUI app run).</summary>
    public bool TryGetSingle(out Entry entry)
    {
        entry = default;
        if (_entries.Count != 1) return false;
        foreach (var pair in _entries)
        {
            entry = pair.Value;
            return true;
        }
        return false;
    }
}
