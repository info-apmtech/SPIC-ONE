namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Working copy of a Digital Library entry (product information, AI video or brochure)
    /// while it is being created or edited in the three-step wizard: details, related
    /// content, detail-page layout. Saved through the content API once it exists.
    /// </summary>
    public sealed class ContentDraft
    {
        public int? Id { get; set; }
        public DigitalLibraryType Kind { get; set; } = DigitalLibraryType.Products;

        // Step 1: content and details (shared)
        public string Category { get; set; } = "";
        public string SubCategory { get; set; } = "";
        public string Format { get; set; } = "";
        public string Visibility { get; set; } = "";
        public string Title { get; set; } = "";
        public string Keywords { get; set; } = "";
        public List<string> Tags { get; } = new();
        public string ShortDescription { get; set; } = "";
        public string? ImageName { get; set; }
        public string? ImageDataUrl { get; set; }
        public string Overview { get; set; } = "";
        public string Features { get; set; } = "";
        public string Usage { get; set; } = "";
        public List<CustomSection> AdditionalSections { get; } = new();

        // Step 1: AI video only
        public string VideoUrl { get; set; } = "";
        public string? VideoFileName { get; set; }
        public string Duration { get; set; } = "00:00:00";

        // Step 1: brochure only
        public string? DocumentName { get; set; }
        public long DocumentSize { get; set; }

        // Step 2: related content
        public RelatedSelection Videos { get; } = new() { ViewMode = "Card View", MaxItems = 3 };
        public RelatedSelection Brochures { get; } = new() { ViewMode = "List View", MaxItems = 3 };

        // Step 3: detail page layout
        public List<LayoutSection> Layout { get; private set; } = LayoutSection.Defaults(DigitalLibraryType.Products);

        public bool IsDraft { get; set; } = true;

        public void SetKind(DigitalLibraryType kind)
        {
            if (Kind == kind && Layout.Count > 0) return;
            Kind = kind;
            Layout = LayoutSection.Defaults(kind);
        }
    }

    public sealed class CustomSection
    {
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
    }

    public sealed class RelatedSelection
    {
        /// <summary>Ids of the chosen items, in display order.</summary>
        public List<int> Ids { get; } = new();
        public string ViewMode { get; set; } = "Card View";
        public int MaxItems { get; set; } = 3;
    }

    public sealed class LayoutSection
    {
        public string Key { get; init; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool IsCustom { get; init; }
        public bool Expanded { get; set; }

        public static List<LayoutSection> Defaults(DigitalLibraryType kind)
        {
            var list = new List<LayoutSection>
            {
                new() { Key = "hero", Title = "Hero Banner", Description = "Image, title and short description at the top of the page." },
            };

            if (kind == DigitalLibraryType.Videos)
            {
                list.Add(new() { Key = "ai", Title = "SPIC AI", Description = "Ask questions about this video or the related product" });
                list.Add(new() { Key = "player", Title = "Overview & Video", Description = "Summary text with the video player" });
            }
            else if (kind == DigitalLibraryType.Brochures)
            {
                list.Add(new() { Key = "document", Title = "Brochure Viewer", Description = "The brochure document with download" });
                list.Add(new() { Key = "ai", Title = "SPIC AI", Description = "Ask questions about this brochure" });
            }

            list.Add(new() { Key = "videos", Title = "Related Videos", Description = "AI videos related to this content" });
            list.Add(new() { Key = "brochures", Title = "Related Brochures", Description = "Brochures and documents related to this content" });

            if (kind == DigitalLibraryType.Products)
            {
                list.Add(new() { Key = "ai", Title = "SPIC AI", Description = "Ask questions about this product" });
                list.Add(new() { Key = "overview", Title = "Product Overview", Description = "Detailed product overview and description" });
            }

            list.Add(new() { Key = "features", Title = "Key Features & Benefits", Description = "Highlight features and benefits" });
            list.Add(new() { Key = "usage", Title = "Usage & Application", Description = "How and where to use this product" });

            if (kind == DigitalLibraryType.Products)
            {
                list.Add(new() { Key = "safety", Title = "Storage & Safety", Description = "Storage instructions and safety guidelines" });
            }

            return list;
        }
    }
}
