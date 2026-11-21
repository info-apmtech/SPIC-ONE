using System;
using System.Collections.Generic;
using System.Linq;

namespace SPIC.Core.Help
{
    /// <summary>One step of a role's starter path: a screen section of a module.</summary>
    public sealed record HelpPathStep(string Module, string Section, int Minutes, bool Tour);

    /// <summary>
    /// "Your path as …" on the help centre home: the screens a role or designation should learn first.
    /// Titles are per language code (English is the fallback).
    /// </summary>
    public sealed record HelpPath(
        string Id,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Designations,
        IReadOnlyDictionary<string, string> Title,
        IReadOnlyList<HelpPathStep> Steps)
    {
        public string TitleIn(string lang) =>
            Title.TryGetValue(lang, out var t) && !string.IsNullOrWhiteSpace(t) ? t
            : Title.TryGetValue("en", out var en) ? en : Id;

        /// <summary>Fills nulls left by JSON so callers never see them.</summary>
        internal static HelpPath Clean(HelpPath p) => p with
        {
            Id = p.Id ?? string.Empty,
            Roles = p.Roles ?? Array.Empty<string>(),
            Designations = p.Designations ?? Array.Empty<string>(),
            Title = p.Title ?? new Dictionary<string, string>(),
            Steps = (p.Steps ?? Array.Empty<HelpPathStep>()).Where(s => s is not null).ToList(),
        };
    }

    internal sealed record HelpPathsFile(IReadOnlyList<HelpPath>? Paths);

    public sealed record HelpWhatsNewItem(string Module, string Title, DateOnly Updated, string Note);

    /// <summary>What's new: built from each guide's "updated:" and "note:", newest first.</summary>
    public static class HelpWhatsNew
    {
        public static IReadOnlyList<HelpWhatsNewItem> From(IEnumerable<HelpGuide> guides, int max) =>
            guides.Where(g => g.Updated is not null)
                .OrderByDescending(g => g.Updated)
                .ThenBy(g => g.Order)
                .Take(max)
                .Select(g => new HelpWhatsNewItem(g.Module, g.Title, g.Updated!.Value, g.Note))
                .ToList();
    }
}
