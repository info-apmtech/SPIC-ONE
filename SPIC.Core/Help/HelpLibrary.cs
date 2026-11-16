using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SPIC.Core.Help
{
    /// <summary>
    /// Every Help Desk guide, the language list and the glossary. Built once from the files
    /// embedded in SPIC.Core (<see cref="FromEmbedded"/>), or from in-memory files in tests and tools.
    /// <para>
    /// English is the source of truth. <see cref="Localized"/> builds a guide in another language
    /// section by section: a translated section is used only when it exists and its source-hash
    /// still matches the English section; otherwise that section stays English. Routes and
    /// visibility filters always come from English, so a translation can never change who sees what.
    /// </para>
    /// </summary>
    public sealed class HelpLibrary
    {
        private const string Prefix = "help/";
        private static readonly Lazy<HelpLibrary> Embedded = new(LoadEmbedded);
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        // module -> language -> guide
        private readonly Dictionary<string, Dictionary<string, HelpGuide>> _guides = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IReadOnlyList<HelpGlossaryTerm>> _glossary = new(StringComparer.OrdinalIgnoreCase);

        public HelpLibrary(IEnumerable<(string Path, string Text)> files)
        {
            Config = new HelpConfig("en", new[] { new HelpLanguage("en", "English", "English", "en-IN-NeerjaNeural") });

            foreach (var (rawPath, text) in files)
            {
                var path = rawPath.Replace('\\', '/');
                if (path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) path = path.Substring(Prefix.Length);

                if (path.Equals("help.config.json", StringComparison.OrdinalIgnoreCase))
                {
                    Config = JsonSerializer.Deserialize<HelpConfig>(text, Json) ?? Config;
                    continue;
                }
                if (path.Equals("glossary.json", StringComparison.OrdinalIgnoreCase))
                {
                    LoadGlossary(text);
                    continue;
                }
                if (path.Equals("paths.json", StringComparison.OrdinalIgnoreCase))
                {
                    Paths = JsonSerializer.Deserialize<HelpPathsFile>(text, Json)?.Paths?.Select(HelpPath.Clean).ToList() ?? new List<HelpPath>();
                    continue;
                }

                // <module>/guide.<lang>.md
                var parts = path.Split('/');
                if (parts.Length != 2) continue;
                var name = parts[1];
                if (!name.StartsWith("guide.", StringComparison.OrdinalIgnoreCase) || !name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;
                var lang = name.Substring("guide.".Length, name.Length - "guide.".Length - ".md".Length);
                var module = parts[0];

                if (!_guides.TryGetValue(module, out var byLang))
                    _guides[module] = byLang = new Dictionary<string, HelpGuide>(StringComparer.OrdinalIgnoreCase);
                byLang[lang] = HelpGuideParser.Parse(module, lang, text);
            }

            Modules = _guides
                .Where(g => g.Value.ContainsKey("en"))
                .Select(g => g.Key)
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static HelpLibrary FromEmbedded() => Embedded.Value;

        public HelpConfig Config { get; }

        /// <summary>The "Still stuck?" contacts, or null when not configured.</summary>
        public HelpSupport? Support => Config.Support;

        /// <summary>"Your path" per role or designation (paths.json).</summary>
        public IReadOnlyList<HelpPath> Paths { get; } = Array.Empty<HelpPath>();

        /// <summary>Modules that have an English guide.</summary>
        public IReadOnlyList<string> Modules { get; }

        public HelpGuide? English(string module) => Raw(module, "en");

        /// <summary>The guide file as written for a language (no English merge), or null.</summary>
        public HelpGuide? Raw(string module, string lang) =>
            _guides.TryGetValue(module, out var byLang) && byLang.TryGetValue(lang, out var g) ? g : null;

        public IReadOnlyList<HelpGlossaryTerm> GlossaryFor(string lang) =>
            _glossary.TryGetValue(lang, out var terms) ? terms : Array.Empty<HelpGlossaryTerm>();

        /// <summary>The guide in <paramref name="lang"/>, falling back to English per section; null for an unknown module.</summary>
        public HelpGuide? Localized(string module, string lang) =>
            // The library never changes after loading, so each (module, language) is merged once.
            _localized.GetOrAdd((module.ToLowerInvariant(), (lang ?? "en").ToLowerInvariant()), key => Localize(key.Module, key.Lang));

        private readonly System.Collections.Concurrent.ConcurrentDictionary<(string Module, string Lang), HelpGuide?> _localized = new();

        private HelpGuide? Localize(string module, string lang)
        {
            var en = English(module);
            if (en is null) return null;
            if (string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase)) return en;

            var translated = Raw(module, lang);
            if (translated is null) return en;

            var byId = translated.Sections.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
            var sections = en.Sections.Select(source =>
            {
                if (!byId.TryGetValue(source.Id, out var t)) return source;
                if (!string.Equals(t.SourceHash, HelpHash.Of(source.Title, source.Body), StringComparison.OrdinalIgnoreCase)) return source;
                // Kind and screen meta stay English (source with ...); blocks also keep English layout lines.
                return source with
                {
                    Title = string.IsNullOrWhiteSpace(t.Title) ? source.Title : t.Title,
                    Body = source.Kind == "screen" ? t.Body : HelpBlocks.MergeLayout(source.Body, t.Body),
                    SourceHash = t.SourceHash,
                    Confidence = t.Confidence,
                };
            }).ToList();

            var translatedTitle = translated.Title != module ? translated.Title : en.Title;
            var metaCurrent = string.Equals(translated.MetaSourceHash, HelpHash.Of(en.Category, en.Note), StringComparison.OrdinalIgnoreCase);
            return en with
            {
                Language = lang,
                Title = translatedTitle,
                Sections = sections,
                Category = metaCurrent && !string.IsNullOrWhiteSpace(translated.Category) ? translated.Category : en.Category,
                Note = metaCurrent && !string.IsNullOrWhiteSpace(translated.Note) ? translated.Note : en.Note,
            };
        }

        private void LoadGlossary(string text)
        {
            using var doc = JsonDocument.Parse(text);
            foreach (var lang in doc.RootElement.EnumerateObject())
            {
                var terms = new List<HelpGlossaryTerm>();
                if (lang.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var term in lang.Value.EnumerateObject())
                    {
                        var v = term.Value;
                        terms.Add(new HelpGlossaryTerm(
                            term.Name,
                            v.TryGetProperty("keep", out var keep) && keep.ValueKind == JsonValueKind.True,
                            v.TryGetProperty("write", out var write) ? write.GetString() : null,
                            v.TryGetProperty("say", out var say) ? say.GetString() : null));
                    }
                }
                _glossary[lang.Name] = terms;
            }
        }

        private static HelpLibrary LoadEmbedded()
        {
            var assembly = typeof(HelpLibrary).Assembly;
            var files = new List<(string, string)>();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.Replace('\\', '/').StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) continue;
                using var stream = assembly.GetManifestResourceStream(name);
                if (stream is null) continue;
                using var reader = new StreamReader(stream);
                files.Add((name, reader.ReadToEnd()));
            }
            return new HelpLibrary(files);
        }
    }
}
