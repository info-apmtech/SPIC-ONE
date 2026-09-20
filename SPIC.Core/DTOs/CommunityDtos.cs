using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// Knowledge Community API contracts (shared by SpicAPI and the Blazor client).
/// Routes (all [Authorize], controller "Community"; every signed-in user may read and post):
///   GET    api/Community/stats                                   -> CommunityStatsDto
///   GET    api/Community/recent?take=6                           -> List&lt;DiscussionSummaryDto&gt;
///   GET    api/Community/discussions?tab=&category=&product=&crop=&status=&tags=&from=&to=&q=&sort=&page=&pageSize=
///                                                                -> PageResult&lt;DiscussionSummaryDto&gt;
///          tab: all|mine|trending|unanswered|following|expert   sort: latest|replies|views|oldest
///   GET    api/Community/discussions/{id}                        -> DiscussionDetailDto (Views += 1)
///   POST   api/Community/discussions                             -> DiscussionDetailDto (body: DiscussionUpsertDto)
///   PUT    api/Community/discussions/{id}                        (author or Admin/CorporateAdmin)
///   DELETE api/Community/discussions/{id}                        (author or Admin/CorporateAdmin, soft)
///   PATCH  api/Community/discussions/{id}/status?status=Resolved|Open (author or Admin)
///   POST   api/Community/discussions/similar                     -> List&lt;DiscussionSummaryDto&gt; (body: SimilarRequest)
///   POST   api/Community/discussions/{id}/replies                -> ReplyDto (body: ReplyCreateDto)
///   DELETE api/Community/replies/{id}                            (author or Admin, soft)
///   POST   api/Community/discussions/{id}/like|save|follow       -> ReactionResultDto (toggle)
///   POST   api/Community/replies/{id}/like                       -> ReactionResultDto (toggle)
///   POST   api/Community/discussions/{id}/attachments            (multipart "files") -> List&lt;AttachmentDto&gt;
///   DELETE api/Community/attachments/{id}
///   GET    api/Community/file/{*path}                            (also accepts ?access_token=)
///   GET    api/Community/products                                -> List&lt;CommunityProductDto&gt;
///   POST   api/Community/products/{name}/join                    -> ReactionResultDto (toggle)
///   GET    api/Community/lookups                                 -> CommunityLookupsDto
/// Status rules: new = WaitingForReply; a reply by SPIC staff (any role except Dealer/Farmer) => ExpertAnswer;
/// a reply by anyone else while WaitingForReply => Open; author/admin may set Resolved or reopen.
/// Trending = LikeCount + ReplyCount*2 + Views/10 within the last 30 days, top 20%.
/// </summary>
public class CommunityMemberDto
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>"Farmer" | "Dealer" | "SPIC Expert" (all staff roles).</summary>
    public string Role { get; set; } = "Farmer";
    public string Location { get; set; } = "";
    public string? AvatarPath { get; set; }
}

public class DiscussionSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    /// <summary>First ~200 characters of the body.</summary>
    public string Excerpt { get; set; } = "";
    public string? Category { get; set; }
    public string? Product { get; set; }
    public string? Crop { get; set; }
    public List<string> Tags { get; set; } = new();
    public CommunityDiscussionStatus Status { get; set; }
    public CommunityMemberDto Author { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int Views { get; set; }
    public int ReplyCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsFollowing { get; set; }
    public bool IsSaved { get; set; }
    public bool IsLiked { get; set; }
    public bool IsMine { get; set; }
    public bool IsTrending { get; set; }
    /// <summary>Up to 3 distinct reply authors (for the avatar stack).</summary>
    public List<CommunityMemberDto> Participants { get; set; } = new();
    public string? CoverAttachmentPath { get; set; }
}

public class DiscussionDetailDto : DiscussionSummaryDto
{
    public string Body { get; set; } = "";
    public List<AttachmentDto> Attachments { get; set; } = new();
    /// <summary>Flat list; nest client-side by ParentReplyId (one level in the UI).</summary>
    public List<ReplyDto> Replies { get; set; } = new();
}

public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    /// <summary>Relative path under Uploads/Community, served by GET api/Community/file/{path}.</summary>
    public string Path { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public bool IsImage { get; set; }
}

public class ReplyDto
{
    public int Id { get; set; }
    public int DiscussionId { get; set; }
    public int? ParentReplyId { get; set; }
    public CommunityMemberDto Author { get; set; } = new();
    public string? MentionName { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool IsLiked { get; set; }
    /// <summary>Number of nested replies under this one.</summary>
    public int ReplyCount { get; set; }
}

public class DiscussionUpsertDto
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Category { get; set; }
    public string? Product { get; set; }
    public string? Crop { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ReplyCreateDto
{
    public string Body { get; set; } = "";
    public int? ParentReplyId { get; set; }
    public string? MentionName { get; set; }
}

public class SimilarRequest
{
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public int Max { get; set; } = 3;
}

public class ReactionResultDto
{
    public bool Active { get; set; }
    public int Count { get; set; }
}

public class CommunityStatsDto
{
    public int CommunityMembers { get; set; }
    public int TotalDiscussions { get; set; }
    public int ActiveMembers { get; set; }
    public int ExpertAnswers { get; set; }
    public int NewThisMonth { get; set; }
}

public class CommunityProductDto
{
    public string Name { get; set; } = "";
    public int DiscussionCount { get; set; }
    public int MemberCount { get; set; }
    public bool Joined { get; set; }
    public string? ImagePath { get; set; }
}

public class CommunityLookupsDto
{
    public List<string> Categories { get; set; } = new();
    public List<string> Products { get; set; } = new();
    public List<string> Crops { get; set; } = new();
}
