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
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string Author { get; init; } = "SPIC Agri Team";
        public DateTime Date { get; init; } = new(2026, 1, 15);
        public string Image { get; init; } = "";

        public string TypeLabel => Type switch
        {
            DigitalLibraryType.Videos => "AI Video",
            DigitalLibraryType.Products => "Product",
            _ => "Brochure"
        };
    }

    /// <summary>
    /// Sample library content used by the landing and list pages until the content API
    /// exists. Kept in one place so both pages show the same items and counts.
    /// </summary>
    public static class DigitalLibrarySampleData
    {
        private const string Img = "_content/SPIC.MauiBlazorApp.Shared/Images/Gallery/";

        private static readonly (string Title, string Description)[] Texts =
        {
            ("DAP Fertilizer Application Guide for Rabi Crops", "A comprehensive guide to applying DAP fertilizer effectively across different soil types for optimal Rabi crop yields."),
            ("NPK Complexes 20-20: Maximum yield in cotton farming", "Expert insights on using SPIC NPK Complex fertilizers to boost cotton crop."),
            ("Urea: Best practices for paddy cultivation", "Learn how to apply SPIC Urea fertilizer at the right growth stages to achieve maximum nitrogen utilization in paddy fields."),
            ("Soil sampling done right", "Step-by-step field procedure for collecting representative soil samples before fertilizer planning."),
            ("Micronutrient mixtures for groundnut", "When and how to apply SPIC micronutrient mixtures for higher pod fill in groundnut."),
            ("Water-soluble fertilizers through drip", "Dosage schedules for fertigation in banana, sugarcane and vegetables."),
        };

        public static List<DigitalLibraryItem> Create(int count = 16)
        {
            var list = new List<DigitalLibraryItem>(count);
            for (var i = 1; i <= count; i++)
            {
                var text = Texts[(i - 1) % Texts.Length];
                list.Add(new DigitalLibraryItem
                {
                    Id = i,
                    Type = (i % 5) switch { 0 => DigitalLibraryType.Brochures, 3 => DigitalLibraryType.Products, _ => DigitalLibraryType.Videos },
                    Published = i % 4 != 0,
                    Title = text.Title,
                    Description = text.Description,
                    Image = Img + $"image{((i - 1) % 21) + 1}.jpg",
                    Date = new DateTime(2026, 1, 15).AddDays(-i * 3),
                });
            }
            return list;
        }
    }
}
