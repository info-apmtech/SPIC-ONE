using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    public enum DigitalLibraryType
    {
        Videos,
        Products,
        Brochures
    }

    /// <summary>One entry of the Digital Library (AI video, product information or brochure).</summary>
    public sealed class DigitalLibraryItem
    {
        public int Id { get; init; }
        public DigitalLibraryType Type { get; init; } = DigitalLibraryType.Videos;
        public bool Published { get; set; } = true;
        public LibraryContentStatus Status { get; set; } = LibraryContentStatus.Published;
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string Author { get; init; } = "SPIC Agri Team";
        public DateTime Date { get; init; } = DateTime.Today;
        /// <summary>Ready-to-use cover URL (API file endpoint, or a gallery placeholder).</summary>
        public string Image { get; set; } = "";
        /// <summary>Stored cover path as the API returns it; null when the item has no cover.</summary>
        public string? CoverImagePath { get; init; }
        public string? Category { get; init; }
        public int Views { get; init; }
        public int? DurationSeconds { get; init; }
        public List<string> Tags { get; init; } = new();

        public string TypeLabel => Type switch
        {
            DigitalLibraryType.Videos => "AI Video",
            DigitalLibraryType.Products => "Product",
            _ => "Brochure"
        };

        // ------------------------------------------------------------------ mapping

        public static DigitalLibraryType ToType(LibraryContentKind kind) => kind switch
        {
            LibraryContentKind.Product => DigitalLibraryType.Products,
            LibraryContentKind.Brochure => DigitalLibraryType.Brochures,
            _ => DigitalLibraryType.Videos
        };

        public static LibraryContentKind ToKind(DigitalLibraryType type) => type switch
        {
            DigitalLibraryType.Products => LibraryContentKind.Product,
            DigitalLibraryType.Brochures => LibraryContentKind.Brochure,
            _ => LibraryContentKind.Video
        };

        /// <summary>Route segment used by /DigitalLibrary/content?type= and /DigitalLibrary/add/{kind}.</summary>
        public static string RouteOf(DigitalLibraryType type) => type.ToString().ToLowerInvariant();

        public static DigitalLibraryType? ParseType(string? route) => route?.Trim().ToLowerInvariant() switch
        {
            "videos" or "video" => DigitalLibraryType.Videos,
            "products" or "product" => DigitalLibraryType.Products,
            "brochures" or "brochure" => DigitalLibraryType.Brochures,
            _ => null
        };

        /// <summary>
        /// Summary DTO from the API to the card view model. <paramref name="fileUrl"/> turns the
        /// stored cover path into a URL the browser can load (see DigitalLibraryApi.FileUrl);
        /// items without a cover fall back to a gallery image chosen from the id, so the grids
        /// keep the design's look instead of showing a broken image.
        /// </summary>
        public static DigitalLibraryItem FromDto(LibraryContentSummaryDto dto, Func<string?, string>? fileUrl = null)
        {
            var cover = string.IsNullOrWhiteSpace(dto.CoverImagePath)
                ? PlaceholderImage(dto.Id)
                : (fileUrl?.Invoke(dto.CoverImagePath) ?? dto.CoverImagePath!);

            return new DigitalLibraryItem
            {
                Id = dto.Id,
                Type = ToType(dto.Kind),
                Published = dto.Status == LibraryContentStatus.Published,
                Status = dto.Status,
                Title = dto.Title,
                Description = dto.ShortDescription ?? "",
                Author = string.IsNullOrWhiteSpace(dto.Author) ? "SPIC Agri Team" : dto.Author!,
                Date = dto.PublishedAt ?? dto.CreatedAt,
                Image = string.IsNullOrEmpty(cover) ? PlaceholderImage(dto.Id) : cover,
                CoverImagePath = dto.CoverImagePath,
                Category = dto.Category,
                Views = dto.Views,
                DurationSeconds = dto.DurationSeconds,
                Tags = dto.Tags ?? new List<string>(),
            };
        }

        private const string GalleryRoot = "_content/SPIC.MauiBlazorApp.Shared/Images/Gallery/";

        /// <summary>Stable stand-in cover for content that has no image yet.</summary>
        public static string PlaceholderImage(int id) => GalleryRoot + $"image{(Math.Abs(id) % 21) + 1}.jpg";
    }
}
