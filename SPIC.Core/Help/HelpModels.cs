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
        HelpConfidence Confidence);

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
        IReadOnlyList<string> Problems);
}
