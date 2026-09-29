using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// Report labels per language from the LabTranslation table (Key + Lang), cached in memory
/// (reloaded every few minutes so edits in the table show up without a restart). Missing keys
/// fall back to English (the en row, then the English text the caller passes).
///
/// Seeding (<see cref="EnsureSeededAsync"/>) runs once at startup from
/// <see cref="LabReportsStartup"/> and lazily on first use: it inserts every seed row
/// (<see cref="LabTranslationSeed"/>) and an en row for every LabParameter code the seed does not
/// know, with INSERT ... ON CONFLICT DO NOTHING (several API instances share the database), and
/// fills rows whose text is empty. Existing non-empty rows are left alone so corrections made
/// in the table survive restarts.
/// </summary>
public sealed class LabTranslations
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<LabTranslations> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, Dictionary<string, string>>? _cache;
    private DateTime _loadedAt;
    private bool _seeded;

    public LabTranslations(IServiceScopeFactory scopes, ILogger<LabTranslations> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task<LabTranslator> ForAsync(string lang, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        all.TryGetValue(lang, out var own);
        all.TryGetValue("en", out var en);
        return new LabTranslator(lang, own ?? new(), en ?? new());
    }

    public void Invalidate() => _cache = null;

    private async Task<Dictionary<string, Dictionary<string, string>>> LoadAsync(CancellationToken ct)
    {
        var cache = _cache;
        if (cache != null && DateTime.UtcNow - _loadedAt < CacheLifetime) return cache;

        await _gate.WaitAsync(ct);
        try
        {
            if (_cache != null && DateTime.UtcNow - _loadedAt < CacheLifetime) return _cache;

            if (!_seeded) await EnsureSeededCoreAsync(ct);

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.LabTranslations.AsNoTracking()
                .Select(t => new { t.Key, t.Lang, t.Text })
                .ToListAsync(ct);

            var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Text)) continue;
                if (!map.TryGetValue(row.Lang, out var lang))
                    map[row.Lang] = lang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                lang[row.Key] = row.Text;
            }

            _cache = map;
            _loadedAt = DateTime.UtcNow;
            return map;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The report still renders (English fallback from code) when the table cannot be read.
            _logger.LogWarning(ex, "LabTranslations: could not load the LabTranslation table; using the built-in English text.");
            return _cache ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!_seeded) await EnsureSeededCoreAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureSeededCoreAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rows = LabTranslationSeed.Rows().ToList();

        // Parameters added to the master later still get an English name row.
        var known = rows.Select(r => r.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var parameters = await db.LabParameters.AsNoTracking()
            .Select(p => new { p.Code, p.Name })
            .ToListAsync(ct);
        foreach (var p in parameters)
        {
            var key = $"param.{p.Code}.name";
            if (!known.Contains(key)) rows.Add(new LabTranslation { Key = key, Lang = "en", Text = p.Name });
        }

        var inserted = 0;
        foreach (var chunk in rows.Chunk(100))
        {
            var sql = new System.Text.StringBuilder(
                "INSERT INTO \"LabTranslations\" (\"Key\", \"Lang\", \"Text\") VALUES ");
            var args = new List<object>();
            for (var i = 0; i < chunk.Length; i++)
            {
                if (i > 0) sql.Append(", ");
                sql.Append($"({{{args.Count}}}, {{{args.Count + 1}}}, {{{args.Count + 2}}})");
                args.Add(chunk[i].Key);
                args.Add(chunk[i].Lang);
                args.Add(chunk[i].Text);
            }
            sql.Append(" ON CONFLICT (\"Key\", \"Lang\") DO UPDATE SET \"Text\" = EXCLUDED.\"Text\" WHERE COALESCE(TRIM(\"LabTranslations\".\"Text\"), '') = ''");
            inserted += await db.Database.ExecuteSqlRawAsync(sql.ToString(), args.ToArray(), ct);
        }

        _seeded = true;
        _logger.LogInformation("LabTranslations: seed checked ({Rows} rows for {Langs}); {Changed} inserted or filled.",
            rows.Count, string.Join(", ", LabTranslationSeed.Languages), inserted);
    }
}

/// <summary>Label lookup for one language with English fallback.</summary>
public sealed class LabTranslator
{
    private readonly Dictionary<string, string> _own;
    private readonly Dictionary<string, string> _en;

    public LabTranslator(string lang, Dictionary<string, string> own, Dictionary<string, string> en)
    {
        Lang = lang;
        _own = own;
        _en = en;
    }

    public string Lang { get; }
    public bool IsEnglish => string.Equals(Lang, "en", StringComparison.OrdinalIgnoreCase);

    /// <summary>Text of <paramref name="key"/> in this language, else the en row, else <paramref name="english"/>.</summary>
    public string T(string key, string english)
    {
        if (!IsEnglish && _own.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text)) return text;
        if (_en.TryGetValue(key, out var en) && !string.IsNullOrWhiteSpace(en)) return en;
        return english;
    }

    /// <summary>Only the translated text (no English fallback): null when the language has no row.</summary>
    public string? TryOwn(string key) =>
        !IsEnglish && _own.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text) ? text : null;

    public string Param(string? code, string englishName) =>
        string.IsNullOrWhiteSpace(code) ? englishName : T($"param.{code}.name", englishName);

    public string Response(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "";
        return T($"response.{label.Trim()}", label.Trim());
    }

    public string Hint(string line) => T($"hint.{line.Trim()}", line.Trim());
    public string Group(string group) => T($"group.{group}", group + " Recommendation");
    public string Product(string product) => T($"product.{product}", product);
    public string Crop(string crop) => T($"crop.{crop}", crop);
    public string Texture(string? value) => string.IsNullOrWhiteSpace(value) ? "" : T($"texture.{value.Trim()}", value.Trim());

    /// <summary>A recommendation line of the report. English prints the engine's line as it is;
    /// another language prints the translated hint of that line (hint.{hint}), else the English line.</summary>
    public string Line(LabReportModel model, string line)
    {
        if (IsEnglish) return line;
        var part = model.RecommendationLines.FirstOrDefault(l => l.Text == line);
        return (part == null ? null : TryOwn($"hint.{part.Hint}")) ?? TryOwn($"hint.{line.Trim()}") ?? line;
    }

    /// <summary>The crop suitability note. English prints the engine's sentence as it is; another
    /// language fills its note.* template with the crop and the translated parameter names the
    /// engine's note lists.</summary>
    public string CropNote(LabReportModel model)
    {
        if (IsEnglish && !string.IsNullOrWhiteSpace(model.CropSuitabilityNote)) return model.CropSuitabilityNote;

        var (kind, problems) = LabReportRules.CropNote(model.Layout, model.Suitability);
        var template = T(LabTranslationSeed.CropNoteKey(kind), LabTranslationSeed.CropNoteTemplate(kind));
        var crop = string.IsNullOrWhiteSpace(model.Crop1) ? T("text.proposedCrop", "the proposed crop") : Crop(model.Crop1!);
        var list = string.Join(", ", problems.Select(b => Param(b.Code, b.Name)));
        return template.Replace("{crop}", crop).Replace("{params}", list);
    }
}
