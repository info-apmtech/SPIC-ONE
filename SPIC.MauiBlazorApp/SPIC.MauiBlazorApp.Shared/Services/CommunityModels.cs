using Microsoft.AspNetCore.Components.Forms;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// SPIC Knowledge Community (Community Connect) view models.
///
/// These are the shapes the pages and components render. <see cref="CommunityApi"/> fetches the
/// API DTOs (<c>SPIC.Core.DTOs</c>) and maps them here through the <c>FromDto</c> helpers, so the
/// markup never has to change when a DTO grows a field. Nothing in this file talks to the network.
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
    /// <summary>Identity user id (the API's <c>CommunityMemberDto.UserId</c>).</summary>
    public string UserId { get; init; } = "";
    public string Name { get; init; } = "";
    /// <summary>"Farmer", "Dealer" or "SPIC Expert" (every staff role).</summary>
    public string Role { get; init; } = "Farmer";
    public string Location { get; init; } = "";
    /// <summary>Optional avatar URL (already resolved); null = render the initial in a tinted circle.</summary>
    public string? Avatar { get; init; }
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[..1].ToUpperInvariant();
    public bool IsExpert => Role == "SPIC Expert";

    public static CommunityMember FromDto(CommunityMemberDto? dto, Func<string, string>? fileUrl = null) =>
        dto is null
            ? new CommunityMember()
            : new CommunityMember
            {
                UserId = dto.UserId ?? "",
                Name = dto.Name ?? "",
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "Farmer" : dto.Role,
                Location = dto.Location ?? "",
                Avatar = string.IsNullOrWhiteSpace(dto.AvatarPath) ? null : fileUrl?.Invoke(dto.AvatarPath!)
            };
}

/// <summary>One file posted with a discussion, with its URL already resolved for the browser.</summary>
public sealed class CommunityAttachment
{
    public int Id { get; init; }
    public string FileName { get; init; } = "";
    /// <summary>Absolute URL served by <c>GET api/Community/file/{path}</c>.</summary>
    public string Url { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long Size { get; init; }
    public bool IsImage { get; init; }

    public static CommunityAttachment FromDto(AttachmentDto dto, Func<string, string> fileUrl) => new()
    {
        Id = dto.Id,
        FileName = dto.FileName ?? "",
        Url = fileUrl(dto.Path ?? ""),
        ContentType = dto.ContentType ?? "",
        Size = dto.Size,
        IsImage = dto.IsImage
    };
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
    public DateTime LastActivityAt { get; set; }
    public int Views { get; set; }
    public int ReplyCount { get; set; }
    public int Likes { get; set; }
    public List<CommunityAttachment> Attachments { get; set; } = new();
    public List<CommunityReply> Replies { get; set; } = new();
    public List<CommunityMember> Participants { get; set; } = new();
    public bool IsFollowing { get; set; }
    public bool IsSaved { get; set; }
    public bool IsLiked { get; set; }
    public bool IsMine { get; set; }
    public bool IsTrending { get; set; }

    public IEnumerable<CommunityAttachment> Images => Attachments.Where(a => a.IsImage);
    public IEnumerable<CommunityAttachment> Files => Attachments.Where(a => !a.IsImage);

    public string StatusLabel => Status switch
    {
        DiscussionStatus.WaitingForReply => "Waiting For Reply",
        DiscussionStatus.ExpertAnswer => "Expert Answer",
        DiscussionStatus.Resolved => "Resolved",
        _ => "Open"
    };

    public static CommunityDiscussion FromDto(DiscussionSummaryDto dto, Func<string, string> fileUrl)
    {
        var d = new CommunityDiscussion
        {
            Id = dto.Id,
            Title = dto.Title ?? "",
            Body = dto.Excerpt ?? "",
            Author = CommunityMember.FromDto(dto.Author, fileUrl),
            Category = dto.Category ?? "",
            Product = dto.Product ?? "",
            Crop = dto.Crop ?? "",
            Tags = dto.Tags ?? new List<string>(),
            Status = FromStatus(dto.Status),
            CreatedAt = dto.CreatedAt,
            LastActivityAt = dto.LastActivityAt,
            Views = dto.Views,
            ReplyCount = dto.ReplyCount,
            Likes = dto.LikeCount,
            Participants = (dto.Participants ?? new List<CommunityMemberDto>())
                .Select(p => CommunityMember.FromDto(p, fileUrl)).ToList(),
            IsFollowing = dto.IsFollowing,
            IsSaved = dto.IsSaved,
            IsLiked = dto.IsLiked,
            IsMine = dto.IsMine,
            IsTrending = dto.IsTrending
        };

        if (dto is DiscussionDetailDto detail)
        {
            d.Body = detail.Body ?? "";
            d.Attachments = (detail.Attachments ?? new List<AttachmentDto>())
                .Select(a => CommunityAttachment.FromDto(a, fileUrl)).ToList();
            d.Replies = (detail.Replies ?? new List<ReplyDto>())
                .Select(r => CommunityReply.FromDto(r, fileUrl)).ToList();
        }

        return d;
    }

    public static DiscussionStatus FromStatus(CommunityDiscussionStatus status) => status switch
    {
        CommunityDiscussionStatus.WaitingForReply => DiscussionStatus.WaitingForReply,
        CommunityDiscussionStatus.ExpertAnswer => DiscussionStatus.ExpertAnswer,
        CommunityDiscussionStatus.Resolved => DiscussionStatus.Resolved,
        _ => DiscussionStatus.Open
    };

    public static CommunityDiscussionStatus ToStatus(DiscussionStatus status) => status switch
    {
        DiscussionStatus.WaitingForReply => CommunityDiscussionStatus.WaitingForReply,
        DiscussionStatus.ExpertAnswer => CommunityDiscussionStatus.ExpertAnswer,
        DiscussionStatus.Resolved => CommunityDiscussionStatus.Resolved,
        _ => CommunityDiscussionStatus.Open
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
    public bool IsLiked { get; set; }
    public int ReplyCount { get; set; }

    public static CommunityReply FromDto(ReplyDto dto, Func<string, string>? fileUrl = null) => new()
    {
        Id = dto.Id,
        DiscussionId = dto.DiscussionId,
        ParentReplyId = dto.ParentReplyId,
        Author = CommunityMember.FromDto(dto.Author, fileUrl),
        MentionName = dto.MentionName,
        Body = dto.Body ?? "",
        CreatedAt = dto.CreatedAt,
        Likes = dto.LikeCount,
        IsLiked = dto.IsLiked,
        ReplyCount = dto.ReplyCount
    };
}

public sealed class CommunityProduct
{
    public string Name { get; init; } = "";
    public string Image { get; init; } = "";
    public int DiscussionCount { get; init; }
    public int MemberCount { get; init; }
    public bool Joined { get; set; }

    /// <summary>Placeholder artwork for products the API has no image for.</summary>
    public const string FallbackImage = "_content/SPIC.MauiBlazorApp.Shared/Images/Gallery/Urea.png";

    public static CommunityProduct FromDto(CommunityProductDto dto, Func<string, string> fileUrl) => new()
    {
        Name = dto.Name ?? "",
        Image = string.IsNullOrWhiteSpace(dto.ImagePath) ? FallbackImage : fileUrl(dto.ImagePath!),
        DiscussionCount = dto.DiscussionCount,
        MemberCount = dto.MemberCount,
        Joined = dto.Joined
    };
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
    /// <summary>Files chosen in the drop zone; uploaded after the discussion is created.</summary>
    public List<IBrowserFile> Files { get; set; } = new();

    public DiscussionUpsertDto ToDto() => new()
    {
        Title = Title.Trim(),
        Body = Description.Trim(),
        Category = string.IsNullOrWhiteSpace(Category) ? null : Category,
        Product = string.IsNullOrWhiteSpace(Product) ? null : Product,
        Crop = string.IsNullOrWhiteSpace(Crop) ? null : Crop,
        Tags = Tags.ToList()
    };
}

/// <summary>Everything the Discussions list puts in the URL, passed straight through to the API.</summary>
public sealed class DiscussionQuery
{
    /// <summary>all | mine | trending | unanswered | following | expert.</summary>
    public string Tab { get; set; } = "all";
    public string Category { get; set; } = "";
    public string Product { get; set; } = "";
    public string Crop { get; set; } = "";
    /// <summary>A <see cref="CommunityDiscussionStatus"/> name, or "" for every status.</summary>
    public string Status { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Search { get; set; } = "";
    /// <summary>latest | replies | views | oldest.</summary>
    public string Sort { get; set; } = "latest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Community stats strip / tiles.</summary>
public sealed class CommunityStats
{
    public int CommunityMembers { get; init; }
    public int TotalDiscussions { get; init; }
    public int ActiveMembers { get; init; }
    public int ExpertAnswers { get; init; }
    public int NewThisMonth { get; init; }

    public static CommunityStats FromDto(CommunityStatsDto dto) => new()
    {
        CommunityMembers = dto.CommunityMembers,
        TotalDiscussions = dto.TotalDiscussions,
        ActiveMembers = dto.ActiveMembers,
        ExpertAnswers = dto.ExpertAnswers,
        NewThisMonth = dto.NewThisMonth
    };
}

/// <summary>Category / product / crop lists for the selects.</summary>
public sealed class CommunityLookups
{
    public List<string> Categories { get; init; } = new();
    public List<string> Products { get; init; } = new();
    public List<string> Crops { get; init; } = new();

    /// <summary>Status filter options: the enum name goes in the URL, the label on screen.</summary>
    public static readonly (string Value, string Label)[] Statuses =
    {
        (nameof(CommunityDiscussionStatus.Open), "Open"),
        (nameof(CommunityDiscussionStatus.WaitingForReply), "Waiting For Reply"),
        (nameof(CommunityDiscussionStatus.ExpertAnswer), "Expert Answer"),
        (nameof(CommunityDiscussionStatus.Resolved), "Resolved"),
    };

    public static CommunityLookups FromDto(CommunityLookupsDto dto) => new()
    {
        Categories = dto.Categories ?? new List<string>(),
        Products = dto.Products ?? new List<string>(),
        Crops = dto.Crops ?? new List<string>()
    };
}

/// <summary>One page of discussions plus the total the "Showing x - y of n" line needs.</summary>
public sealed class DiscussionPage
{
    public List<CommunityDiscussion> Items { get; init; } = new();
    public int Total { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
