using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.Assistant;

/// <summary>
/// Retrieval for the SPIC AI assistant: ranks PUBLISHED library content by keyword
/// overlap between the question (plus the title of the "about" item, when the user
/// opened the assistant from a content page) and the item's
/// Title (3) / Keywords (3) / Tags (2) / ShortDescription (1) / Overview (1).
/// The top 4 items become the assistant's context. Nothing that is a draft,
/// archived or soft-deleted is ever retrieved.
/// </summary>
public class LibraryRetriever
{
    private const int MaxFieldChars = 1500;

    private readonly AppDbContext _db;

    public LibraryRetriever(AppDbContext db) => _db = db;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","and","for","are","but","not","you","your","with","this","that","from","have","has","had",
        "what","when","where","which","how","why","who","can","could","should","would","will","shall",
        "about","into","over","under","per","use","used","using","any","all","was","were","been","being",
        "does","did","doing","them","they","there","their","then","than","also","much","many","more",
        "tell","give","need","want","please","spic","help","know","some","its","it's","one","two"
    };

    public static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var current = new System.Text.StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                if (current.Length >= 3) yield return current.ToString();
                current.Clear();
            }
        }
        if (current.Length >= 3) yield return current.ToString();
    }

    private static HashSet<string> QueryTerms(string? question, string? aboutTitle)
    {
        var terms = Tokenize(question)
            .Concat(Tokenize(aboutTitle))
            .Where(t => !StopWords.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return terms;
    }

    private static int FieldScore(string? field, HashSet<string> terms, int weight)
    {
        if (string.IsNullOrWhiteSpace(field) || terms.Count == 0) return 0;
        var words = Tokenize(field).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (words.Count == 0) return 0;
        return terms.Count(t => words.Contains(t)) * weight;
    }

    /// <summary>Ranks published content for the question; the "about" item is always included first.</summary>
    public async Task<(List<AssistantLibraryItem> Items, AssistantLibraryItem? About)> RetrieveAsync(
        string question,
        int? aboutContentId,
        int take = 4,
        CancellationToken cancellationToken = default,
        LibraryContentKind? kind = null)
    {
        var published = await _db.LibraryContents
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Status == LibraryContentStatus.Published)
            .Where(c => kind == null || c.Kind == kind || c.Id == aboutContentId)
            .Select(c => new Candidate
            {
                Id = c.Id,
                Kind = c.Kind,
                Title = c.Title,
                Keywords = c.Keywords,
                Tags = c.Tags,
                ShortDescription = c.ShortDescription,
                Overview = c.Overview,
                Features = c.Features,
                Usage = c.Usage,
                Views = c.Views
            })
            .ToListAsync(cancellationToken);

        var aboutRow = aboutContentId.HasValue
            ? published.FirstOrDefault(c => c.Id == aboutContentId.Value)
            : null;

        var terms = QueryTerms(question, aboutRow?.Title);

        var ranked = published
            .Select(c => new
            {
                Row = c,
                Score = FieldScore(c.Title, terms, 3)
                      + FieldScore(c.Keywords, terms, 3)
                      + FieldScore(c.Tags, terms, 2)
                      + FieldScore(c.ShortDescription, terms, 1)
                      + FieldScore(c.Overview, terms, 1)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Row.Views)
            .ThenBy(x => x.Row.Title)
            .Take(Math.Max(1, take))
            .ToList();

        var items = ranked.Select(x => Map(x.Row)).ToList();
        AssistantLibraryItem? about = aboutRow != null ? Map(aboutRow) : null;

        // The item the question is about is always part of the context, first.
        if (about != null)
        {
            items.RemoveAll(i => i.ContentId == about.ContentId);
            items.Insert(0, about);
            if (items.Count > take) items = items.Take(take).ToList();
        }

        return (items, about);
    }

    /// <summary>The most viewed published items, used when nothing matched the question.</summary>
    public async Task<List<AssistantLibraryItem>> MostViewedAsync(int take = 3, CancellationToken cancellationToken = default)
    {
        var rows = await _db.LibraryContents
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Status == LibraryContentStatus.Published)
            .OrderByDescending(c => c.Views)
            .ThenByDescending(c => c.PublishedAt)
            .Take(Math.Max(1, take))
            .Select(c => new AssistantLibraryItem
            {
                ContentId = c.Id,
                Kind = c.Kind,
                Title = c.Title,
                ShortDescription = c.ShortDescription,
                Overview = c.Overview,
                Features = c.Features,
                Usage = c.Usage,
                Tags = c.Tags
            })
            .ToListAsync(cancellationToken);

        foreach (var r in rows)
        {
            r.ShortDescription = Truncate(r.ShortDescription);
            r.Overview = Truncate(r.Overview);
            r.Features = Truncate(r.Features);
            r.Usage = Truncate(r.Usage);
        }

        return rows;
    }

    private sealed class Candidate
    {
        public int Id { get; set; }
        public LibraryContentKind Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Keywords { get; set; }
        public string? Tags { get; set; }
        public string? ShortDescription { get; set; }
        public string? Overview { get; set; }
        public string? Features { get; set; }
        public string? Usage { get; set; }
        public int Views { get; set; }
    }

    private static AssistantLibraryItem Map(Candidate c) => new()
    {
        ContentId = c.Id,
        Kind = c.Kind,
        Title = c.Title ?? string.Empty,
        ShortDescription = Truncate(c.ShortDescription),
        Overview = Truncate(c.Overview),
        Features = Truncate(c.Features),
        Usage = Truncate(c.Usage),
        Tags = c.Tags
    };

    private static string? Truncate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        s = s.Trim();
        return s.Length <= MaxFieldChars ? s : s.Substring(0, MaxFieldChars) + "…";
    }
}
