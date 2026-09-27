namespace SPIC.Core.Entities;

public enum LibraryCategoryKind
{
    Category = 0,
    SubCategory = 1
}

/// <summary>
/// Digital Library category master: the "Main Category / Type" and "Associated Program /
/// Sub-Category" options on the content forms. Content stores the NAME (LibraryContent.Category /
/// SubCategory are plain strings), so a rename here is carried into the content rows by the API,
/// and a name that content still uses cannot be deleted (deactivate it instead).
///
/// Names are unique per Kind, ignoring case: a stored generated column "NormalizedName"
/// (lower(Name)) carries the unique index, and the API compares in code first for a friendly 409.
/// A sub-category belongs to exactly one category (ParentCategoryId); categories have no parent.
/// </summary>
public class LibraryCategory
{
    public int Id { get; set; }
    public LibraryCategoryKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Sub-categories only: the category they belong to. Null for categories.</summary>
    public int? ParentCategoryId { get; set; }
    public LibraryCategory? ParentCategory { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
