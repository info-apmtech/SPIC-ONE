using System;
using System.Collections.Generic;
using System.Linq;
using SPIC.Core.DTOs;

namespace SPIC.Core.Help
{
    /// <summary>
    /// Writes reviewer corrections (downloaded from /Help/Review) into a translated guide.
    /// Per section the NEWEST correction made against the CURRENT English is applied and marked
    /// confidence ok; a correction made against older English is skipped (it could bring back a
    /// removed step). Everything else in the translation file is kept as it is.
    /// </summary>
    public static class HelpReviewApplier
    {
        public static (string NewText, IReadOnlyList<string> Skipped) Apply(
            HelpGuide english,
            HelpGuide translated,
            IEnumerable<HelpCorrectionExportDto> corrections)
        {
            var skipped = new List<string>();
            var result = new Dictionary<string, (string Title, string Body, HelpConfidence Confidence)>(StringComparer.OrdinalIgnoreCase);
            // Untouched sections keep the source-hash they had, so a stale one stays stale.
            var keptHashes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(translated.Title) && translated.Title != translated.Module)
                result[HelpGuideWriter.GuideTitleKey] = (translated.Title, string.Empty, HelpConfidence.Ok);
            // Keep the translated category and note with the hash they were made from.
            if (!string.IsNullOrWhiteSpace(translated.Category))
                result[HelpGuideWriter.CategoryKey] = (translated.Category, string.Empty, HelpConfidence.Ok);
            if (!string.IsNullOrWhiteSpace(translated.Note))
                result[HelpGuideWriter.NoteKey] = (translated.Note, string.Empty, HelpConfidence.Ok);
            keptHashes[HelpGuideWriter.MetaKey] = translated.MetaSourceHash ?? "missing";
            foreach (var t in translated.Sections)
            {
                result[t.Id] = (t.Title, t.Body, t.Confidence);
                keptHashes[t.Id] = t.SourceHash ?? "missing";
            }

            var newest = corrections
                .Where(c => string.Equals(c.Module, english.Module, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(c.Language, translated.Language, StringComparison.OrdinalIgnoreCase))
                .GroupBy(c => c.SectionId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(c => c.CreatedAtUtc).First());

            foreach (var c in newest)
            {
                var source = english.Sections.FirstOrDefault(s => string.Equals(s.Id, c.SectionId, StringComparison.OrdinalIgnoreCase));
                if (source is null)
                {
                    skipped.Add($"{c.Module}/{c.Language}/{c.SectionId}: section no longer exists in English");
                    continue;
                }
                if (!string.Equals(c.SourceHash, HelpHash.Of(source.Title, source.Body), StringComparison.OrdinalIgnoreCase))
                {
                    skipped.Add($"{c.Module}/{c.Language}/{c.SectionId}: English changed after this correction (by {c.ReviewerName})");
                    continue;
                }
                var body = c.SuggestedBody;
                if (source.Kind != "screen")
                {
                    // Blocks: keep English layout lines; refuse a correction whose text lines can't be read.
                    var problems = new List<string>();
                    HelpBlocks.Items(body, problems);
                    if (problems.Count > 0)
                    {
                        skipped.Add($"{c.Module}/{c.Language}/{c.SectionId}: correction could not be read ({string.Join("; ", problems)})");
                        continue;
                    }
                    body = HelpBlocks.MergeLayout(source.Body, body);
                }
                result[source.Id] = (string.IsNullOrWhiteSpace(c.SuggestedTitle) ? source.Title : c.SuggestedTitle, body, HelpConfidence.Ok);
                keptHashes.Remove(source.Id); // corrected against the current English: stamp the current hash
            }

            return (HelpGuideWriter.Write(english, translated.Language, result, keptHashes), skipped);
        }
    }
}
