using System.Collections.Generic;

namespace SPIC.Core.Help
{
    /// <summary>A Help Desk language: ISO code, English and native names, and its Azure neural voice.</summary>
    public sealed record HelpLanguage(string Code, string Name, string Native, string Voice);

    /// <summary>help.config.json: the languages offered and the default one.</summary>
    public sealed record HelpConfig(string DefaultLanguage, IReadOnlyList<HelpLanguage> Languages);

    /// <summary>
    /// A glossary.json entry for one language. <see cref="Keep"/> leaves the term in Latin script;
    /// <see cref="Write"/> fixes its written form; <see cref="Say"/> is used only for the voice.
    /// </summary>
    public sealed record HelpGlossaryTerm(string Term, bool Keep, string? Write, string? Say);
}
