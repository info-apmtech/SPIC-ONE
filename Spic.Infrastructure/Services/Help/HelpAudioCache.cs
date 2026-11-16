using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SPIC.Core.Help;

namespace Spic.Infrastructure.Services.Help;

/// <summary>Turns SSML into MP3 audio (Azure AI Speech in production).</summary>
public interface ISpeechSynthesizer
{
    Task<byte[]> SynthesizeAsync(string ssml, CancellationToken ct);
}

/// <summary>
/// Help Desk voice clips, generated ONCE and saved: <c>{root}/{lang}/{key}.mp3</c>, where the key
/// is a hash of the exact SSML (text + voice + pronunciation glossary). Every later request, from
/// any user and after restarts, streams the saved file; a changed guide section has a new key and
/// so gets a new clip. Concurrent first requests for the same clip are serialized so the synthesizer
/// is called once, and the file is written to a temp name and moved into place so nobody reads a
/// half-written clip.
/// </summary>
public sealed class HelpAudioCache
{
    private static readonly Regex SafeLanguage = new("^[a-z]{2,3}$", RegexOptions.Compiled);

    private readonly string _root;
    private readonly ISpeechSynthesizer _synthesizer;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public HelpAudioCache(string root, ISpeechSynthesizer synthesizer)
    {
        _root = root;
        _synthesizer = synthesizer;
    }

    public static string KeyFor(string ssml) => HelpHash.Of("ssml", ssml);

    /// <summary>Path of the saved clip if it exists, else null (no synthesis).</summary>
    public string? Existing(string lang, string ssml)
    {
        var path = PathFor(lang, ssml);
        return File.Exists(path) ? path : null;
    }

    public async Task<string> GetOrCreateAsync(string lang, string ssml, CancellationToken ct)
    {
        var path = PathFor(lang, ssml);
        if (File.Exists(path)) return path;

        var gate = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (File.Exists(path)) return path;

            var audio = await _synthesizer.SynthesizeAsync(ssml, ct);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllBytesAsync(temp, audio, ct);
            try
            {
                File.Move(temp, path, overwrite: false);
            }
            catch (IOException) when (File.Exists(path))
            {
                // Another API instance (shared Azure Files mount) saved it first.
                File.Delete(temp);
            }
            return path;
        }
        finally
        {
            gate.Release();
        }
    }

    private string PathFor(string lang, string ssml)
    {
        if (string.IsNullOrEmpty(lang) || !SafeLanguage.IsMatch(lang))
            throw new ArgumentException("Language must be a 2-3 letter code.", nameof(lang));
        return Path.Combine(_root, lang, KeyFor(ssml) + ".mp3");
    }
}
