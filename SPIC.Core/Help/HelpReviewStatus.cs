using System;
using System.Collections.Generic;
using System.Linq;
using SPIC.Core.Entities;

namespace SPIC.Core.Help
{
    public enum HelpReviewState { NotReviewed = 0, NeedsCorrection = 1, Approved = 2 }

    /// <summary>
    /// Review status of one translated section, computed from review records and the CURRENT text
    /// (spec §6.9). Approved: an approval whose source and translation hashes both match the
    /// current text. NeedsCorrection: a correction made against the current English that the
    /// translation file does not contain yet. Otherwise NotReviewed - including right after a
    /// correction is applied, because the corrected text still needs a final approval.
    /// </summary>
    public static class HelpReviewStatus
    {
        public static HelpReviewState StateOf(HelpSection english, HelpSection translated, IEnumerable<HelpReviewItem> items)
        {
            var source = HelpHash.Of(english.Title, english.Body);
            var translation = HelpHash.Of(translated.Title, translated.Body);
            var list = items as IList<HelpReviewItem> ?? items.ToList();

            if (list.Any(i => i.Kind == HelpReviewKind.Approval && Same(i.SourceHash, source) && Same(i.TranslationHash, translation)))
                return HelpReviewState.Approved;

            if (list.Any(i => i.Kind == HelpReviewKind.Correction && Same(i.SourceHash, source) && !IsApplied(i, translated)))
                return HelpReviewState.NeedsCorrection;

            return HelpReviewState.NotReviewed;
        }

        /// <summary>True when the translation file already contains this correction's text.</summary>
        public static bool IsApplied(HelpReviewItem correction, HelpSection translated) =>
            Same(HelpHash.Of(correction.SuggestedTitle ?? translated.Title, correction.SuggestedBody ?? string.Empty),
                 HelpHash.Of(translated.Title, translated.Body));

        private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
