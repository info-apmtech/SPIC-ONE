namespace SPIC.Core.Entities;

public enum CommunityDiscussionStatus
{
    Open = 0,
    WaitingForReply = 1,
    ExpertAnswer = 2,
    Resolved = 3
}

public enum CommunityReactionTarget
{
    Post = 0,
    Reply = 1
}

public enum CommunityReactionKind
{
    Like = 0,
    Save = 1,
    Follow = 2
}

/// <summary>
/// A Knowledge Community discussion (question / topic). Named CommunityPost so it does not clash
/// with the client's CommunityDiscussion view model. Author details are denormalised so the
/// list renders without joining Identity.
/// </summary>
public class CommunityPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Product { get; set; }
    public string? Crop { get; set; }
    public string? Tags { get; set; }                   // comma separated
    public CommunityDiscussionStatus Status { get; set; } = CommunityDiscussionStatus.WaitingForReply;

    public string AuthorUserId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = "Farmer";  // Farmer | Dealer | SPIC Expert
    public string? AuthorLocation { get; set; }

    public int Views { get; set; }
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }

    public ICollection<CommunityPostReply> Replies { get; set; } = new List<CommunityPostReply>();
    public ICollection<CommunityPostAttachment> Attachments { get; set; } = new List<CommunityPostAttachment>();
}

public class CommunityPostReply
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public CommunityPost? Post { get; set; }
    public int? ParentReplyId { get; set; }             // one level of nesting in the UI
    public string AuthorUserId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = "Farmer";
    public string? AuthorLocation { get; set; }
    public string? MentionName { get; set; }
    public string Body { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }
}

public class CommunityPostAttachment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public CommunityPost? Post { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty; // Community/{postId}/....
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Like / Save / Follow by one user on one post or reply (unique per user+target+kind).</summary>
public class CommunityReaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public CommunityReactionTarget TargetType { get; set; }
    public int TargetId { get; set; }
    public CommunityReactionKind Kind { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>"Join" on a Popular Product card.</summary>
public class CommunityProductMember
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
