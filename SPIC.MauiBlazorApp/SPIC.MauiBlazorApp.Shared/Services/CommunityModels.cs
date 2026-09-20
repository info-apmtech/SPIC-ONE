namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// SPIC Knowledge Community (Community Connect) models + sample data.
/// Sample content only: the discussion API does not exist yet, so every page reads from
/// <see cref="CommunitySampleData"/> and "writes" stay in memory for the session.
/// </summary>
public enum DiscussionStatus
{
    Open,
    WaitingForReply,
    ExpertAnswer,
    Resolved
}

public sealed class CommunityMember
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    /// <summary>"Farmer", "SPIC Expert", "Dealer", "Marketing Officer".</summary>
    public string Role { get; init; } = "Farmer";
    public string Location { get; init; } = "";
    /// <summary>Optional avatar URL; null = render the initial in a tinted circle.</summary>
    public string? Avatar { get; init; }
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[..1].ToUpperInvariant();
    public bool IsExpert => Role == "SPIC Expert";
}

public sealed class CommunityDiscussion
{
    public int Id { get; init; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public CommunityMember Author { get; init; } = new();
    public string Category { get; set; } = "";
    public string Product { get; set; } = "";
    public string Crop { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public DiscussionStatus Status { get; set; } = DiscussionStatus.Open;
    public DateTime CreatedAt { get; init; }
    public int Views { get; set; }
    public int ReplyCount { get; set; }
    public int Likes { get; set; }
    /// <summary>Image URLs (app-relative, e.g. "_content/.../Images/Gallery/image1.jpg").</summary>
    public List<string> Attachments { get; set; } = new();
    public List<CommunityMember> Participants { get; set; } = new();
    public bool IsFollowing { get; set; }
    public bool IsSaved { get; set; }
    public bool IsMine { get; set; }
    public bool IsTrending { get; set; }

    public string StatusLabel => Status switch
    {
        DiscussionStatus.WaitingForReply => "Waiting For Reply",
        DiscussionStatus.ExpertAnswer => "Expert Answer",
        DiscussionStatus.Resolved => "Resolved",
        _ => "Open"
    };
}

public sealed class CommunityReply
{
    public int Id { get; init; }
    public int DiscussionId { get; init; }
    /// <summary>Null = top-level reply; otherwise the reply it is nested under (one level deep in the design).</summary>
    public int? ParentReplyId { get; init; }
    public CommunityMember Author { get; init; } = new();
    /// <summary>Name mentioned with "@" at the start of the reply, if any.</summary>
    public string? MentionName { get; init; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; init; }
    public int Likes { get; set; }
    public int ReplyCount { get; set; }
}

public sealed class CommunityProduct
{
    public string Name { get; init; } = "";
    public string Image { get; init; } = "";
    public int DiscussionCount { get; init; }
    public bool Joined { get; set; }
}

/// <summary>What the "Create a Discussion" form collects.</summary>
public sealed class DiscussionDraft
{
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Product { get; set; } = "";
    public string Crop { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = "";
    /// <summary>File names chosen in the attachments drop zone (no upload yet).</summary>
    public List<(string Name, long Size)> Files { get; set; } = new();
}

public static class CommunitySampleData
{
    private const string Img = "_content/SPIC.MauiBlazorApp.Shared/Images/Gallery/";

    public static readonly string[] Categories =
    {
        "Crop Fertilizer Knowledge", "Soil Health", "Pest & Disease", "Irrigation", "Seeds & Sowing", "Harvest & Storage", "Government Schemes"
    };

    public static readonly string[] ProductNames =
    {
        "SPIC Urea", "DAP Fertilizer", "SPIC Posh", "SPIC Prom", "SPIC Super", "SPIC Gypsum", "SPIC Potash"
    };

    public static readonly string[] Crops =
    {
        "Paddy", "Wheat", "Tomato", "Sugarcane", "Cotton", "Groundnut", "Banana", "Maize"
    };

    public static readonly string[] Statuses = { "Open", "Waiting For Reply", "Expert Answer", "Resolved" };

    public static readonly IReadOnlyList<CommunityMember> Members = new List<CommunityMember>
    {
        new() { Id = 1, Name = "Aravind Shankar", Role = "Farmer", Location = "Thanjavur, Tamil Nadu" },
        new() { Id = 2, Name = "Anitha Rajan", Role = "SPIC Expert", Location = "Chennai" },
        new() { Id = 3, Name = "Karthik", Role = "Farmer", Location = "Thanjavur" },
        new() { Id = 4, Name = "Meena Kumari", Role = "Farmer", Location = "Madurai" },
        new() { Id = 5, Name = "Ravi Prasad", Role = "Dealer", Location = "Trichy" },
        new() { Id = 6, Name = "Selvi Murugan", Role = "Farmer", Location = "Erode" },
        new() { Id = 7, Name = "Dr. Prakash Iyer", Role = "SPIC Expert", Location = "Tuticorin" },
        new() { Id = 8, Name = "Ganesh Babu", Role = "Marketing Officer", Location = "Salem" },
    };

    public static readonly IReadOnlyList<CommunityProduct> Products = new List<CommunityProduct>
    {
        new() { Name = "SPIC Urea", Image = Img + "Urea.png", DiscussionCount = 12400 },
        new() { Name = "SPIC Posh", Image = Img + "Urea.png", DiscussionCount = 8300 },
        new() { Name = "DAP Fertilizer", Image = Img + "Urea.png", DiscussionCount = 7600 },
        new() { Name = "SPIC Prom", Image = Img + "Urea.png", DiscussionCount = 4200 },
        new() { Name = "SPIC Gypsum", Image = Img + "Urea.png", DiscussionCount = 3900 },
        new() { Name = "SPIC Super", Image = Img + "Urea.png", DiscussionCount = 2800 },
    };

    private static readonly string[] Titles =
    {
        "Best time to apply SPIC Urea in Kharif season?",
        "Best way to apply DAP for paddy in Kharif season?",
        "Leaves turning yellow after DAP application, what could be the reason?",
        "Can I mix SPIC Posh with urea for tomato?",
        "Recommended dose of SPIC Prom for sugarcane ratoon crop",
        "Gypsum application timing for groundnut pegging stage",
        "Split application of urea in wheat: two or three doses?",
        "How to correct zinc deficiency in paddy nursery?",
        "Drip fertigation schedule with SPIC Super for banana",
        "Is potash needed for cotton square formation?",
    };

    private static readonly string[] Bodies =
    {
        "I applied SPIC DAP to my paddy field but the leaves are turning yellow. What could be the reason? Please suggest the correct method.",
        "I have 3 acres of paddy field and planning for kharif season. I've been applying DAP at transplanting stage but my cousin says applying at panicle initiation stage also helps. What is the recommended time and dose for DAP application in paddy? Also, can I split the application between two stages? Looking for experienced farmer inputs and expert guidance.",
        "My soil test shows low phosphorus. Which SPIC product should I use as the basal dose and how much per acre?",
        "The dealer suggested a top dressing after 25 days. Is that too early for a transplanted crop?",
    };

    private static readonly string[][] TagSets =
    {
        new[] { "Urea", "Paddy", "Mixing" },
        new[] { "DAP", "Fertilizer" },
        new[] { "Zinc", "Nursery" },
        new[] { "Drip", "Banana" },
        new[] { "Potash", "Cotton" },
    };

    private static List<CommunityDiscussion>? _discussions;
    private static readonly Dictionary<int, List<CommunityReply>> _replies = new();
    private static int _nextId = 1000;

    /// <summary>Session-wide list (new discussions posted from the form are appended here).</summary>
    public static List<CommunityDiscussion> Discussions => _discussions ??= Build(48);

    public static CommunityDiscussion? Find(int id) => Discussions.FirstOrDefault(d => d.Id == id);

    public static IReadOnlyList<CommunityReply> RepliesFor(int discussionId)
    {
        if (!_replies.TryGetValue(discussionId, out var list))
        {
            list = BuildReplies(discussionId);
            _replies[discussionId] = list;
        }
        return list;
    }

    /// <summary>Very small "similar discussions" heuristic: shared words in the title (3+ letters).</summary>
    public static IReadOnlyList<CommunityDiscussion> Similar(string title, int max = 3)
    {
        var words = (title ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 3).Select(w => w.ToLowerInvariant()).ToHashSet();
        if (words.Count == 0) return Array.Empty<CommunityDiscussion>();
        return Discussions
            .Select(d => (d, score: d.Title.ToLowerInvariant().Split(' ').Count(w => words.Contains(w.Trim('?', ',', '.')))))
            .Where(x => x.score >= 3)
            .OrderByDescending(x => x.score).ThenByDescending(x => x.d.ReplyCount)
            .Take(max).Select(x => x.d).ToList();
    }

    /// <summary>Adds a discussion from the form (in memory only) and returns it.</summary>
    public static CommunityDiscussion Post(DiscussionDraft draft, CommunityMember author)
    {
        var d = new CommunityDiscussion
        {
            Id = ++_nextId,
            Title = draft.Title.Trim(),
            Body = draft.Description.Trim(),
            Author = author,
            Category = draft.Category,
            Product = draft.Product,
            Crop = draft.Crop,
            Tags = draft.Tags.ToList(),
            Status = DiscussionStatus.WaitingForReply,
            CreatedAt = DateTime.Now,
            IsMine = true,
            IsFollowing = true,
        };
        Discussions.Insert(0, d);
        _replies[d.Id] = new List<CommunityReply>();
        return d;
    }

    public static CommunityMember CurrentUser => Members[0];

    private static List<CommunityDiscussion> Build(int count)
    {
        var rnd = new Random(7);
        var list = new List<CommunityDiscussion>(count);
        var start = new DateTime(2026, 9, 3, 9, 12, 0);
        for (int i = 0; i < count; i++)
        {
            var author = Members[i % Members.Count];
            var status = (DiscussionStatus)(i % 4);
            var attach = (i % 3) switch { 0 => 4, 1 => 2, _ => 0 };
            list.Add(new CommunityDiscussion
            {
                Id = i + 1,
                Title = Titles[i % Titles.Length],
                Body = Bodies[i % Bodies.Length],
                Author = author,
                Category = Categories[i % Categories.Length],
                Product = ProductNames[i % ProductNames.Length],
                Crop = Crops[i % Crops.Length],
                Tags = TagSets[i % TagSets.Length].ToList(),
                Status = status,
                CreatedAt = start.AddHours(-i * 7),
                Views = 18 + rnd.Next(0, 400),
                ReplyCount = status == DiscussionStatus.Open ? rnd.Next(0, 4) : 18 + rnd.Next(0, 30),
                Likes = rnd.Next(0, 60),
                Attachments = Enumerable.Range(1, attach).Select(n => $"{Img}image{((i + n) % 14) + 1}.jpg").ToList(),
                Participants = Members.Skip((i + 1) % 5).Take(3).ToList(),
                IsFollowing = i % 4 == 1,
                IsSaved = i % 6 == 0,
                IsMine = i % 8 == 0,
                IsTrending = i % 5 == 0,
            });
        }
        return list;
    }

    private static List<CommunityReply> BuildReplies(int discussionId)
    {
        const string text = "Based on my 15 years of paddy farming experience, the best time to apply DAP is at the time of transplanting, as a basal application. I mix it into the soil about 2 days before transplanting. The results have always been excellent for tillering.";
        var when = new DateTime(2026, 9, 3, 9, 12, 0);
        var list = new List<CommunityReply>
        {
            new() { Id = 1, DiscussionId = discussionId, Author = Members[1], Body = text, CreatedAt = when, Likes = 18, ReplyCount = 18 },
            new() { Id = 2, DiscussionId = discussionId, Author = Members[3], Body = text, CreatedAt = when.AddMinutes(20), Likes = 18, ReplyCount = 18 },
            new() { Id = 3, DiscussionId = discussionId, ParentReplyId = 2, Author = Members[5], Body = text, CreatedAt = when.AddMinutes(35), Likes = 18, ReplyCount = 18 },
            new() { Id = 4, DiscussionId = discussionId, ParentReplyId = 2, Author = Members[2], MentionName = "Anitha Rajan", Body = text, CreatedAt = when.AddMinutes(50), Likes = 18, ReplyCount = 18 },
            new() { Id = 5, DiscussionId = discussionId, Author = Members[1], Body = text, CreatedAt = when.AddHours(1), Likes = 18, ReplyCount = 18 },
            new() { Id = 6, DiscussionId = discussionId, ParentReplyId = 5, Author = Members[6], Body = text, CreatedAt = when.AddHours(1).AddMinutes(10), Likes = 18, ReplyCount = 18 },
            new() { Id = 7, DiscussionId = discussionId, ParentReplyId = 5, Author = Members[2], MentionName = "Anitha Rajan", Body = text, CreatedAt = when.AddHours(1).AddMinutes(25), Likes = 18, ReplyCount = 18 },
        };
        return list;
    }

    public static CommunityReply AddReply(int discussionId, string body, CommunityMember author, int? parentId = null)
    {
        var list = (List<CommunityReply>)RepliesFor(discussionId);
        var r = new CommunityReply
        {
            Id = list.Count == 0 ? 1 : list.Max(x => x.Id) + 1,
            DiscussionId = discussionId,
            ParentReplyId = parentId,
            Author = author,
            Body = body.Trim(),
            CreatedAt = DateTime.Now,
        };
        list.Add(r);
        var d = Find(discussionId);
        if (d is not null) d.ReplyCount++;
        return r;
    }
}
