using System.Collections.Generic;

namespace SPIC.Core.Help
{
    /// <summary>A Help Desk language: ISO code, English and native names, and its Azure neural voice.</summary>
    public sealed record HelpLanguage(string Code, string Name, string Native, string Voice);

    /// <summary>help.config.json: the languages offered, the default one and the support contacts.</summary>
    public sealed record HelpConfig(string DefaultLanguage, IReadOnlyList<HelpLanguage> Languages, HelpSupport? Support = null);

    /// <summary>
    /// The "Still stuck?" box. Hidden while both numbers are empty, so the numbers can be added in
    /// help.config.json later without a code change. <see cref="Hours"/> is per language code.
    /// </summary>
    public sealed record HelpSupport(string Phone, string WhatsApp, IReadOnlyDictionary<string, string>? Hours)
    {
        public bool HasContact => !string.IsNullOrWhiteSpace(Phone) || !string.IsNullOrWhiteSpace(WhatsApp);
    }

    /// <summary>
    /// A glossary.json entry for one language. <see cref="Keep"/> leaves the term in Latin script;
    /// <see cref="Write"/> fixes its written form; <see cref="Say"/> is used only for the voice.
    /// </summary>
    public sealed record HelpGlossaryTerm(string Term, bool Keep, string? Write, string? Say);
}
