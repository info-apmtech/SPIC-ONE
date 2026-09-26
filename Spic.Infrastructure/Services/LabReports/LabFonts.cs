using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Drawing;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// Fonts for the lab report PDFs. Every .ttf / .otf / .ttc file in the fonts folder
/// (SpicAPI/Fonts, copied next to the published API; override with Sas:Lab:FontsPath) is
/// registered with QuestPDF's FontManager under a family name taken from the file name
/// ("NotoSansTamil-Bold.ttf" -> "NotoSansTamil"; the weight suffix is dropped so the Regular
/// and Bold files form one family).
///
/// A language is covered when a registered family name contains its script keyword:
///   ta -> "Tamil", te -> "Telugu", mr / hi -> "Devanagari", kn -> "Kannada", ml -> "Malayalam",
///   bn -> "Bengali", gu -> "Gujarati", pa -> "Gurmukhi", or -> "Oriya".
/// Sas:Lab:ReportFonts:{lang} overrides the keyword per language (e.g. "Nirmala" or "Latha").
/// English uses Lato (bundled with QuestPDF; Sas:Lab:LatinFont overrides it). A language
/// without a font is rendered in English by the caller, which adds the response header
/// X-Report-Language-Fallback. Adding fonts needs only a file copy and a restart.
/// </summary>
public sealed class LabFonts
{
    public const string DefaultLatinFamily = "Lato";

    private static readonly Dictionary<string, string> DefaultScriptKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ta"] = "Tamil",
        ["te"] = "Telugu",
        ["mr"] = "Devanagari",
        ["hi"] = "Devanagari",
        ["kn"] = "Kannada",
        ["ml"] = "Malayalam",
        ["bn"] = "Bengali",
        ["gu"] = "Gujarati",
        ["pa"] = "Gurmukhi",
        ["or"] = "Oriya"
    };

    private static readonly string[] StyleWords =
    {
        "Regular", "Bold", "SemiBold", "Semibold", "Medium", "Light", "ExtraLight", "Thin", "Black", "ExtraBold",
        "Italic", "BoldItalic", "Condensed", "SemiCondensed", "SemiLight", "Semilight", "Book", "Heavy", "UI", "Variable", "VF"
    };

    private readonly IConfiguration _config;
    private readonly IHostEnvironment? _env;
    private readonly ILogger<LabFonts> _logger;
    private readonly object _lock = new();
    private List<string>? _families;
    private string? _fontsFolder;

    public LabFonts(IConfiguration config, ILogger<LabFonts> logger, IHostEnvironment? env = null)
    {
        _config = config;
        _logger = logger;
        _env = env;
    }

    public string LatinFamily => _config["Sas:Lab:LatinFont"] is { Length: > 0 } f ? f : DefaultLatinFamily;

    /// <summary>Registered family names (after <see cref="EnsureLoaded"/>).</summary>
    public IReadOnlyList<string> Families
    {
        get { EnsureLoaded(); return _families!; }
    }

    public string? FontsFolder
    {
        get { EnsureLoaded(); return _fontsFolder; }
    }

    /// <summary>The font family for a language's script, or null when no registered font covers it.
    /// English (and any Latin-script language) returns the Latin family.</summary>
    public string? FamilyFor(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang) || lang.Equals("en", StringComparison.OrdinalIgnoreCase)) return LatinFamily;
        EnsureLoaded();

        var keyword = _config[$"Sas:Lab:ReportFonts:{lang}"];
        if (string.IsNullOrWhiteSpace(keyword) && !DefaultScriptKeywords.TryGetValue(lang, out keyword)) return null;

        return _families!.FirstOrDefault(f => f.Contains(keyword!, StringComparison.OrdinalIgnoreCase));
    }

    public bool Covers(string lang) => FamilyFor(lang) != null;

    public void EnsureLoaded()
    {
        if (_families != null) return;
        lock (_lock)
        {
            if (_families != null) return;
            var families = new List<string>();

            var folder = ResolveFolder();
            _fontsFolder = folder;
            if (folder == null)
            {
                _logger.LogWarning("LabFonts: no fonts folder found (SpicAPI/Fonts); Tamil / Telugu / Marathi reports fall back to English.");
                _families = families;
                return;
            }

            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var file in files)
            {
                var family = FamilyFromFileName(file);
                try
                {
                    using var stream = File.OpenRead(file);
                    FontManager.RegisterFontWithCustomName(family, stream);
                    if (!families.Contains(family, StringComparer.OrdinalIgnoreCase)) families.Add(family);
                    _logger.LogInformation("LabFonts: registered {File} as family {Family}.", Path.GetFileName(file), family);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LabFonts: could not register {File}.", file);
                }
            }

            var coverage = DefaultScriptKeywords.Keys
                .Select(l => $"{l}={(FamilyForLoaded(l, families) ?? "none (English fallback)")}");
            _logger.LogInformation("LabFonts: {Count} font file(s) from {Folder}; families [{Families}]; coverage: {Coverage}.",
                files.Count, folder, string.Join(", ", families), string.Join("; ", coverage));

            _families = families;
        }
    }

    private string? FamilyForLoaded(string lang, List<string> families)
    {
        var keyword = _config[$"Sas:Lab:ReportFonts:{lang}"];
        if (string.IsNullOrWhiteSpace(keyword) && !DefaultScriptKeywords.TryGetValue(lang, out keyword)) return null;
        return families.FirstOrDefault(f => f.Contains(keyword!, StringComparison.OrdinalIgnoreCase));
    }

    private string? ResolveFolder()
    {
        var candidates = new List<string>();
        var configured = _config["Sas:Lab:FontsPath"];
        if (!string.IsNullOrWhiteSpace(configured)) candidates.Add(configured);
        if (_env != null) candidates.Add(Path.Combine(_env.ContentRootPath, "Fonts"));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Fonts"));
        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "Fonts"));

        return candidates.FirstOrDefault(Directory.Exists);
    }

    /// <summary>"NotoSansTamil-Bold.ttf" -> "NotoSansTamil"; "NotoSansTelugu[wdth,wght].ttf" -> "NotoSansTelugu".</summary>
    public static string FamilyFromFileName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var bracket = name.IndexOf('[');
        if (bracket > 0) name = name[..bracket];
        name = name.Replace(' ', '-').Replace('_', '-').Trim('-');

        var parts = name.Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (parts.Count > 1 && StyleWords.Contains(parts[^1], StringComparer.OrdinalIgnoreCase))
            parts.RemoveAt(parts.Count - 1);

        return string.Join("", parts);
    }
}
