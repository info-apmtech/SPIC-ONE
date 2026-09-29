using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// Digital Library API contracts (shared by SpicAPI and the Blazor client).
/// Routes (all [Authorize], controller "Library"):
///   GET    api/Library/stats
///   GET    api/Library?kind=&status=&q=&page=&pageSize=          -> PageResult&lt;LibraryContentSummaryDto&gt;
///   GET    api/Library/{id}                                        -> LibraryContentDetailDto (Views += 1)
///   POST   api/Library                                             -> LibraryContentDetailDto (body: LibraryContentUpsertDto)
///   PUT    api/Library/{id}                                        -> LibraryContentDetailDto
///   PATCH  api/Library/{id}/status?status=Published|Draft|Archived -> LibraryContentSummaryDto
///   DELETE api/Library/{id}                                        (soft delete)
///   POST   api/Library/{id}/cover | /video | /document             (multipart "file") -> LibraryFileDto
///   GET    api/Library/file/{*path}                                (also accepts ?access_token= for &lt;img&gt;/&lt;video&gt;)
///   GET    api/Library/lookups                                     -> LibraryLookupsDto
///   -- category master (V5r; reads for every signed-in user, writes need the DigitalLibrary page) --
///   GET    api/Library/categories?includeInactive=                 -> List&lt;LibraryCategoryDto&gt; (categories, each with SubCategories)
///   POST   api/Library/categories                                  -> LibraryCategoryDto (body: LibraryCategoryUpsertDto)
///   PUT    api/Library/categories/{id}                             -> LibraryCategoryDto (a rename is carried into the content rows)
///   PATCH  api/Library/categories/{id}/active?value=true|false     -> LibraryCategoryDto
///   DELETE api/Library/categories/{id}                             (409 while content uses the name or it has sub-categories)
///   -- assistant --
///   GET    api/Library/assistant/conversations                     -> List&lt;AssistantConversationDto&gt; (mine)
///   GET    api/Library/assistant/conversations/{id}                -> AssistantConversationDetailDto
///   POST   api/Library/assistant/ask                               -> AssistantAskResponse
///   DELETE api/Library/assistant/conversations/{id}
/// Write endpoints require LoginState.Can("DigitalLibrary", "Entry"/"Update"/"Delete") semantics server side:
/// Admin/CorporateAdmin always; other roles when their Designation.RoleAccess grants the DigitalLibrary page.
/// Reads of PUBLISHED content and the assistant are open to every signed-in user.
/// </summary>
public class LibraryContentSummaryDto
{
    public int Id { get; set; }
    public LibraryContentKind Kind { get; set; }
    public LibraryContentStatus Status { get; set; }
    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    /// <summary>Relative path under Uploads/Library, served by GET api/Library/file/{path}. Null = no cover.</summary>
    public string? CoverImagePath { get; set; }
    public int Views { get; set; }
    public List<string> Tags { get; set; } = new();
    /// <summary>Videos only, seconds.</summary>
    public int? DurationSeconds { get; set; }
}

public class LibraryContentDetailDto : LibraryContentSummaryDto
{
    public string? Format { get; set; }
    public string Visibility { get; set; } = "Everyone";
    public string? Keywords { get; set; }
    public string? Overview { get; set; }
    public string? Features { get; set; }
    public string? Usage { get; set; }
    public List<LibraryCustomSectionDto> AdditionalSections { get; set; } = new();
    public string? VideoUrl { get; set; }
    public string? VideoFilePath { get; set; }
    public string? DocumentPath { get; set; }
    public string? DocumentName { get; set; }
    public long? DocumentSize { get; set; }
    public LibraryRelatedDto RelatedVideos { get; set; } = new();
    public LibraryRelatedDto RelatedBrochures { get; set; } = new();
    /// <summary>Resolved related items (published only), in the order of RelatedVideos.Ids.</summary>
    public List<LibraryContentSummaryDto> RelatedVideoItems { get; set; } = new();
    public List<LibraryContentSummaryDto> RelatedBrochureItems { get; set; } = new();
    public List<LibraryLayoutSectionDto> Layout { get; set; } = new();
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LibraryCustomSectionDto
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
}

/// <summary>Related-content selection (step 2 of the wizard).</summary>
public class LibraryRelatedDto
{
    public List<int> Ids { get; set; } = new();
    /// <summary>"grid" | "list" | "carousel".</summary>
    public string ViewMode { get; set; } = "grid";
    public int MaxItems { get; set; } = 6;
}

/// <summary>Detail-page layout (step 3 of the wizard).</summary>
public class LibraryLayoutSectionDto
{
    /// <summary>overview | media | relatedVideos | relatedBrochures | features | usage | custom:{index}</summary>
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
}

public class LibraryContentUpsertDto
{
    public LibraryContentKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Format { get; set; }
    public string Visibility { get; set; } = "Everyone";
    public string? Keywords { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ShortDescription { get; set; }
    public string? Overview { get; set; }
    public string? Features { get; set; }
    public string? Usage { get; set; }
    public List<LibraryCustomSectionDto> AdditionalSections { get; set; } = new();
    public string? VideoUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public LibraryRelatedDto RelatedVideos { get; set; } = new();
    public LibraryRelatedDto RelatedBrochures { get; set; } = new();
    public List<LibraryLayoutSectionDto> Layout { get; set; } = new();
    /// <summary>Draft or Published ("Save as Draft" vs "Publish").</summary>
    public LibraryContentStatus Status { get; set; } = LibraryContentStatus.Draft;
}

public class LibraryFileDto
{
    /// <summary>Relative path to pass back in the detail (CoverImagePath / VideoFilePath / DocumentPath).</summary>
    public string Path { get; set; } = "";
    public string FileName { get; set; } = "";
    public long Size { get; set; }
    public string ContentType { get; set; } = "";
}

public class LibraryStatsDto
{
    public int Total { get; set; }
    public int Published { get; set; }
    public int Drafts { get; set; }
    public int Archived { get; set; }
    public int Videos { get; set; }
    public int Products { get; set; }
    public int Brochures { get; set; }
    public int TotalViews { get; set; }
}

public class LibraryLookupsDto
{
    public List<string> Categories { get; set; } = new();
    public List<string> SubCategories { get; set; } = new();
    public List<string> Formats { get; set; } = new();
    public List<string> Visibilities { get; set; } = new();
    /// <summary>
    /// ACTIVE categories (SortOrder, then Name) with their ACTIVE sub-categories, so the form can
    /// narrow the sub-category list to the chosen category. The flat Categories / SubCategories
    /// lists above stay (master names merged with names already used by content).
    /// </summary>
    public List<LibraryCategoryDto> CategoryTree { get; set; } = new();
    /// <summary>Tags already used by non-deleted content: distinct ignoring case, most used first, then by name.</summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>Generic page envelope used by the new list endpoints.</summary>
public class PageResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool HasMore => Page * PageSize < Total;
}

// ---------------------------------------------------------------- assistant

public class AssistantConversationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AssistantConversationDetailDto : AssistantConversationDto
{
    public List<AssistantMessageDto> Messages { get; set; } = new();
}

public class AssistantMessageDto
{
    public int Id { get; set; }
    /// <summary>"user" | "assistant".</summary>
    public string Role { get; set; } = "user";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<AssistantSourceDto> Sources { get; set; } = new();
    /// <summary>Follow-up questions the client can show as chips.</summary>
    public List<string> Suggestions { get; set; } = new();
    /// <summary>"anthropic" | "keyword" for assistant messages; null for user messages.</summary>
    public string? Provider { get; set; }
}

public class AssistantSourceDto
{
    public int ContentId { get; set; }
    public string Title { get; set; } = "";
    public LibraryContentKind Kind { get; set; }
}

public class AssistantAskRequest
{
    /// <summary>Null starts a new conversation.</summary>
    public int? ConversationId { get; set; }
    public string Question { get; set; } = "";
    /// <summary>Optional library item the question is about (from ?about=).</summary>
    public int? AboutContentId { get; set; }
    /// <summary>Optional source filter (the composer's "Select Source"): restrict retrieval to one kind.</summary>
    public LibraryContentKind? Kind { get; set; }
}

public class AssistantAskResponse
{
    public int ConversationId { get; set; }
    public string ConversationTitle { get; set; } = "";
    public AssistantMessageDto UserMessage { get; set; } = new();
    public AssistantMessageDto Reply { get; set; } = new();
    /// <summary>"anthropic" | "keyword" — which provider answered (for the UI disclaimer).</summary>
    public string Provider { get; set; } = "keyword";
}

// ---------------------------------------------------------------- category master (V5r)

/// <summary>One category or sub-category of the Digital Library category master.</summary>
public class LibraryCategoryDto
{
    public int Id { get; set; }
    public LibraryCategoryKind Kind { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Sub-categories: the category they belong to. Null for categories.</summary>
    public int? ParentCategoryId { get; set; }
    public string? ParentName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Non-deleted content items whose Category / SubCategory is this name (0 in the lookups tree).</summary>
    public int UsageCount { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    /// <summary>Categories only: their sub-categories (SortOrder, then Name).</summary>
    public List<LibraryCategoryDto> SubCategories { get; set; } = new();
}

/// <summary>Body of POST / PUT api/Library/categories. Kind cannot change on PUT.</summary>
public class LibraryCategoryUpsertDto
{
    public LibraryCategoryKind Kind { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Required for a sub-category (must be a category); ignored for a category.</summary>
    public int? ParentCategoryId { get; set; }
    /// <summary>Null = after the last one at the same level.</summary>
    public int? SortOrder { get; set; }
}
