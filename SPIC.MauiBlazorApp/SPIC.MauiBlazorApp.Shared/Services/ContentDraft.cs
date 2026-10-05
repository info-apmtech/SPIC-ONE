using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Working copy of a Digital Library entry (product information, AI video or brochure)
    /// while it is being created or edited in the three-step wizard: details, related
    /// content, detail-page layout. <see cref="ToUpsertDto"/> is what the API is sent and
    /// <see cref="FromDto"/> loads an existing item back into the wizard.
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

        // ------------------------------------------------------------------ mapping

        /// <summary>The body of POST / PUT api/Library.</summary>
        public LibraryContentUpsertDto ToUpsertDto(bool publish) => new()
        {
            Kind = DigitalLibraryItem.ToKind(Kind),
            Title = Title.Trim(),
            Category = Empty(Category),
            SubCategory = Empty(SubCategory),
            Format = Empty(Format),
            Visibility = string.IsNullOrWhiteSpace(Visibility) ? "Everyone" : Visibility.Trim(),
            Keywords = Empty(Keywords),
            Tags = Tags.ToList(),
            ShortDescription = Empty(ShortDescription),
            Overview = Empty(Overview),
            Features = Empty(Features),
            Usage = Empty(Usage),
            AdditionalSections = AdditionalSections
                .Where(s => !string.IsNullOrWhiteSpace(s.Title) || !string.IsNullOrWhiteSpace(s.Body))
                .Select(s => new LibraryCustomSectionDto { Title = s.Title.Trim(), Body = s.Body })
                .ToList(),
            VideoUrl = Empty(VideoUrl),
            DurationSeconds = ParseDuration(Duration),
            RelatedVideos = Videos.ToDto(),
            RelatedBrochures = Brochures.ToDto(),
            Layout = Layout.Select((s, i) => new LibraryLayoutSectionDto
            {
                Key = s.Key,
                Title = s.Title,
                Enabled = s.Enabled,
                Order = i
            }).ToList(),
            Status = publish ? LibraryContentStatus.Published : LibraryContentStatus.Draft
        };

        /// <summary>
        /// Loads an existing item into the wizard. <paramref name="fileUrl"/> resolves the stored
        /// cover so the image box shows what is already saved (see DigitalLibraryApi.FileUrl).
        /// </summary>
        public void FromDto(LibraryContentDetailDto dto, Func<string?, string>? fileUrl = null)
        {
            Id = dto.Id;
            SetKind(DigitalLibraryItem.ToType(dto.Kind));

            Title = dto.Title;
            Category = dto.Category ?? "";
            SubCategory = dto.SubCategory ?? "";
            Format = dto.Format ?? "";
            Visibility = dto.Visibility;
            Keywords = dto.Keywords ?? "";
            ShortDescription = dto.ShortDescription ?? "";
            Overview = dto.Overview ?? "";
            Features = dto.Features ?? "";
            Usage = dto.Usage ?? "";

            Tags.Clear();
            Tags.AddRange(dto.Tags);

            AdditionalSections.Clear();
            AdditionalSections.AddRange(dto.AdditionalSections.Select(s => new CustomSection { Title = s.Title, Body = s.Body }));

            VideoUrl = dto.VideoUrl ?? "";
            VideoFileName = string.IsNullOrWhiteSpace(dto.VideoFilePath) ? null : FileNameOf(dto.VideoFilePath!);
            Duration = FormatDuration(dto.DurationSeconds);

            DocumentName = dto.DocumentName;
            DocumentSize = dto.DocumentSize ?? 0;

            if (!string.IsNullOrWhiteSpace(dto.CoverImagePath))
            {
                ImageDataUrl = fileUrl?.Invoke(dto.CoverImagePath) ?? dto.CoverImagePath;
                ImageName = FileNameOf(dto.CoverImagePath!);
            }

            Videos.FromDto(dto.RelatedVideos);
            Brochures.FromDto(dto.RelatedBrochures);

            if (dto.Layout.Count > 0)
            {
                // Descriptions are UI-only text, so they come back from the defaults by key.
                var defaults = LayoutSection.Defaults(Kind).ToDictionary(s => s.Key, s => s.Description);
                Layout = dto.Layout
                    .OrderBy(s => s.Order)
                    .Select(s => new LayoutSection
                    {
                        Key = s.Key,
                        Title = s.Title,
                        Description = defaults.TryGetValue(s.Key, out var d) ? d : "Custom section",
                        Enabled = s.Enabled,
                        IsCustom = s.Key.StartsWith("custom-", StringComparison.OrdinalIgnoreCase)
                    })
                    .ToList();
            }

            IsDraft = dto.Status != LibraryContentStatus.Published;
        }

        private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string FileNameOf(string path)
        {
            var slash = path.LastIndexOfAny(new[] { '/', '\\' });
            return slash < 0 ? path : path[(slash + 1)..];
        }

        /// <summary>"hh:mm:ss" (or "mm:ss") to seconds; null when nothing was entered.</summary>
        public static int? ParseDuration(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var parts = text.Split(':', StringSplitOptions.TrimEntries);
            var seconds = 0;
            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var value)) return null;
                seconds = seconds * 60 + value;
            }
            return seconds > 0 ? seconds : null;
        }

        public static string FormatDuration(int? seconds)
            => seconds is null or <= 0 ? "00:00:00" : TimeSpan.FromSeconds(seconds.Value).ToString(@"hh\:mm\:ss");

        /// <summary>Short form used on the detail page ("03:12", "1:02:30").</summary>
        public static string ShortDuration(int? seconds)
        {
            if (seconds is null or <= 0) return "00:00";
            var span = TimeSpan.FromSeconds(seconds.Value);
            return span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss") : span.ToString(@"mm\:ss");
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

        public LibraryRelatedDto ToDto() => new()
        {
            Ids = Ids.ToList(),
            ViewMode = ViewMode == "List View" ? "list" : "grid",
            MaxItems = MaxItems
        };

        public void FromDto(LibraryRelatedDto dto)
        {
            Ids.Clear();
            Ids.AddRange(dto.Ids);
            ViewMode = string.Equals(dto.ViewMode, "list", StringComparison.OrdinalIgnoreCase) ? "List View" : "Card View";
            if (dto.MaxItems is >= 3 and <= 6) MaxItems = dto.MaxItems;
        }
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
