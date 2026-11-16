using System.Collections.Generic;

namespace SPIC.Core.Help
{
    /// <summary>Where a section's text came from: the English source, or a translation the
    /// back-translation check judged OK or LOW.</summary>
    public enum HelpConfidence { Source, Ok, Low }

    /// <summary>
    /// One screen or task inside a module's guide.
    /// <para><see cref="Routes"/> decide visibility and drive the "?" button; <see cref="For"/>
    /// ("Page.Action"), <see cref="Roles"/> and <see cref="Designations"/> only narrow it.</para>
    /// <para><see cref="SourceHash"/> is set on translated sections: the <see cref="HelpHash"/> of
    /// the English section the translation was made from.</para>
    /// </summary>
    public sealed record HelpSection(
        string Id,
        string Title,
        IReadOnlyList<string> Routes,
        IReadOnlyList<string> For,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Designations,
        string Body,
        string? SourceHash,
        HelpConfidence Confidence)
    {
        /// <summary>"screen" (a screen guide) or a block: overview, flow, statuses, roles, faq.</summary>
        public string Kind { get; init; } = "screen";

        /// <summary>Screenshots, hotspots, tour steps and reading time; set on screen sections.</summary>
        public HelpScreenMeta? Screen { get; init; }
    }

    /// <summary>A numbered marker on a screenshot, in % of the image (0–100).</summary>
    public sealed record HelpHotspot(int N, double X, double Y, double W, double H);

    /// <summary>One guided-tour step: the CSS selector of the element it highlights on the real page.</summary>
    public sealed record HelpTourStep(int N, string Target);

    /// <summary>Layout data of a screen section. Always taken from the English guide.</summary>
    public sealed record HelpScreenMeta(
        string? Image,
        IReadOnlyList<HelpHotspot> Hotspots,
        string? PhoneImage,
        IReadOnlyList<HelpHotspot> PhoneHotspots,
        IReadOnlyList<HelpTourStep> Tour,
        int Minutes)
    {
        public static readonly HelpScreenMeta Empty =
            new(null, System.Array.Empty<HelpHotspot>(), null, System.Array.Empty<HelpHotspot>(), System.Array.Empty<HelpTourStep>(), 0);
    }

    /// <summary>
    /// A module's guide in one language. <see cref="Problems"/> lists content mistakes found while
    /// parsing (malformed block, duplicate id); parsing never throws on content.
    /// </summary>
    public sealed record HelpGuide(
        string Module,
        string Language,
        string Title,
        string Icon,
        int Order,
        IReadOnlyList<HelpSection> Sections,
        IReadOnlyList<string> Problems)
    {
        /// <summary>Banner eyebrow and home grouping (translatable).</summary>
        public string Category { get; init; } = string.Empty;

        /// <summary>Module accent colour (layout; from English).</summary>
        public string Color { get; init; } = "#0b6b3a";

        /// <summary>Demo label on the banner's example journey (layout; from English).</summary>
        public string Example { get; init; } = string.Empty;

        /// <summary>Last guide update, shown on the banner and in What's new.</summary>
        public System.DateOnly? Updated { get; init; }

        /// <summary>What changed in that update (translatable).</summary>
        public string Note { get; init; } = string.Empty;

        /// <summary>In a translation: the hash of the English category and note it was made from.</summary>
        public string? MetaSourceHash { get; init; }
    }
}
