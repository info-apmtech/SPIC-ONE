using System.Xml.Linq;
using SPIC.Core.Help;
using Spic.Infrastructure.Services.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// Help Desk voice: the speech request is valid XML whatever the guide text contains, glossary
/// "say it as" entries are applied, and each clip is generated once and then reused - by every
/// user, even when many tap Listen at the same moment - and regenerated only when the text changes.
/// </summary>
internal static class HelpSpeechTests
{
	private static readonly HelpLanguage Tamil = new("ta", "Tamil", "தமிழ்", "ta-IN-PallaviNeural");

	public static void Run()
	{
		Console.WriteLine("Help voice");

		SsmlEscapesAndAppliesSayAs();
		ConcurrentRequestsGenerateOnce().GetAwaiter().GetResult();
		SecondRequestUsesSavedFile().GetAwaiter().GetResult();
		ChangedTextMakesNewFile().GetAwaiter().GetResult();
		UnsafeLanguageIsRejected().GetAwaiter().GetResult();
		EmptyAudioIsNotSaved().GetAwaiter().GetResult();
	}

	private sealed class EmptySynth : ISpeechSynthesizer
	{
		public Task<byte[]> SynthesizeAsync(string ssml, CancellationToken ct) => Task.FromResult(Array.Empty<byte>());
	}

	// Final review (minor 6, promoted): an empty answer must not be saved and served for a year.
	private static async Task EmptyAudioIsNotSaved()
	{
		var root = TempRoot();
		try
		{
			var threw = false;
			try { await new HelpAudioCache(root, new EmptySynth()).GetOrCreateAsync("ta", "<speak>e</speak>", CancellationToken.None); }
			catch (InvalidOperationException) { threw = true; }
			Check.That("empty audio rejected", threw);
			Check.Equal("nothing saved for empty audio", 0,
				Directory.Exists(root) ? Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length : 0);
		}
		finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
	}

	private static void SsmlEscapesAndAppliesSayAs()
	{
		var ssml = HelpSpeech.BuildSsml("R&D <test> RMDO and RMDOS", Tamil,
			new[] { new HelpGlossaryTerm("RMDO", Keep: true, Write: null, Say: "ஆர் எம் டி ஓ") });
		Check.That("ampersand and angle brackets escaped", ssml.Contains("R&amp;D &lt;test&gt;"));
		Check.That("say-as applied to the whole word", ssml.Contains("<sub alias=\"ஆர் எம் டி ஓ\">RMDO</sub>"));
		Check.That("say-as not applied inside a longer word", ssml.Contains("RMDOS") && !ssml.Contains(">RMDO</sub>S"));
		Check.That("voice and locale set", ssml.Contains("xml:lang=\"ta-IN\"") && ssml.Contains("name=\"ta-IN-PallaviNeural\""));
		var parses = true;
		try { XDocument.Parse(ssml); } catch { parses = false; }
		Check.That("SSML is valid XML", parses);
	}

	private sealed class CountingSynth : ISpeechSynthesizer
	{
		public int Calls;
		public async Task<byte[]> SynthesizeAsync(string ssml, CancellationToken ct)
		{
			Interlocked.Increment(ref Calls);
			await Task.Delay(200, ct);
			return new byte[] { 1, 2, 3, 4 };
		}
	}

	private static string TempRoot() => Path.Combine(Path.GetTempPath(), "spic-help-audio-test-" + Guid.NewGuid().ToString("N"));

	private static async Task ConcurrentRequestsGenerateOnce()
	{
		var root = TempRoot();
		try
		{
			var synth = new CountingSynth();
			var cache = new HelpAudioCache(root, synth);
			var paths = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => cache.GetOrCreateAsync("ta", "<speak>a</speak>", CancellationToken.None)));
			Check.Equal("ten simultaneous requests synthesize once", 1, synth.Calls);
			Check.Equal("all get the same file", 1, paths.Distinct().Count());
			Check.That("file saved", File.Exists(paths[0]) && new FileInfo(paths[0]).Length == 4);
			Check.Equal("no temp file left", 0, Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories).Length);
		}
		finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
	}

	private static async Task SecondRequestUsesSavedFile()
	{
		var root = TempRoot();
		try
		{
			var synth = new CountingSynth();
			await new HelpAudioCache(root, synth).GetOrCreateAsync("ta", "<speak>b</speak>", CancellationToken.None);
			// A new cache instance (e.g. after an API restart) still reuses the saved file.
			await new HelpAudioCache(root, synth).GetOrCreateAsync("ta", "<speak>b</speak>", CancellationToken.None);
			Check.Equal("saved clip reused across restarts", 1, synth.Calls);
		}
		finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
	}

	private static async Task ChangedTextMakesNewFile()
	{
		var root = TempRoot();
		try
		{
			var synth = new CountingSynth();
			var cache = new HelpAudioCache(root, synth);
			var a = await cache.GetOrCreateAsync("ta", "<speak>c</speak>", CancellationToken.None);
			var b = await cache.GetOrCreateAsync("ta", "<speak>d</speak>", CancellationToken.None);
			Check.That("changed text gets a new file", a != b);
			Check.Equal("changed text synthesized again", 2, synth.Calls);
			Check.Equal("key matches the file name", HelpAudioCache.KeyFor("<speak>c</speak>") + ".mp3", Path.GetFileName(a));
		}
		finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
	}

	private static async Task UnsafeLanguageIsRejected()
	{
		var root = TempRoot();
		try
		{
			var threw = false;
			try { await new HelpAudioCache(root, new CountingSynth()).GetOrCreateAsync("../x", "<speak/>", CancellationToken.None); }
			catch (ArgumentException) { threw = true; }
			Check.That("path-like language code rejected", threw);
		}
		finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
	}
}
