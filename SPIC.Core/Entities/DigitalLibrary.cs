namespace SPIC.Core.Entities;

public enum LibraryContentKind
{
    Video = 0,
    Product = 1,
    Brochure = 2
}

public enum LibraryContentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

/// <summary>
/// One Digital Library item: an AI video, a product information page or a product brochure.
/// The three kinds share one table; kind-specific columns stay null for the other kinds.
/// Files live under Uploads/Library/{Id}/ and the relative path is stored here.
/// </summary>
public class LibraryContent
{
    public int Id { get; set; }
    public LibraryContentKind Kind { get; set; }
    public LibraryContentStatus Status { get; set; } = LibraryContentStatus.Draft;

    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Format { get; set; }                 // e.g. "Video", "PDF", "Article"
    public string Visibility { get; set; } = "Everyone"; // Everyone | Staff | Dealers
    public string? Keywords { get; set; }
    public string? Tags { get; set; }                   // comma separated
    public string? ShortDescription { get; set; }
    public string? CoverImagePath { get; set; }         // Library/{id}/cover_....jpg

    // Product information / brochure text
    public string? Overview { get; set; }
    public string? Features { get; set; }
    public string? Usage { get; set; }
    public string? AdditionalSectionsJson { get; set; } // List<LibraryCustomSectionDto>

    // Video
    public string? VideoUrl { get; set; }               // external (YouTube etc.)
    public string? VideoFilePath { get; set; }          // uploaded file
    public int? DurationSeconds { get; set; }

    // Brochure
    public string? DocumentPath { get; set; }
    public string? DocumentName { get; set; }
    public long? DocumentSize { get; set; }

    // Related content + detail layout (wizard steps 2 and 3), stored as JSON
    public string? RelatedVideosJson { get; set; }      // LibraryRelatedDto
    public string? RelatedBrochuresJson { get; set; }   // LibraryRelatedDto
    public string? LayoutJson { get; set; }             // List<LibraryLayoutSectionDto>

    public int Views { get; set; }
    public string? AuthorUserId { get; set; }
    public string? AuthorName { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? PublishedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>A SPIC AI assistant conversation belonging to one user.</summary>
public class LibraryConversation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }
    public ICollection<LibraryMessage> Messages { get; set; } = new List<LibraryMessage>();
}

public class LibraryMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public LibraryConversation? Conversation { get; set; }
    public string Role { get; set; } = "user";          // user | assistant
    public string Text { get; set; } = string.Empty;
    public string? SourcesJson { get; set; }            // List<AssistantSourceDto>
    public string? Provider { get; set; }               // anthropic | keyword
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
