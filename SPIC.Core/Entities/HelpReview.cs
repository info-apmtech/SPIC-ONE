using System;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.Entities
{
    /// <summary>What a native-speaker reviewer did on one translated Help Desk section.</summary>
    public enum HelpReviewKind
    {
        Approval = 0,
        Correction = 1,
        Pronunciation = 2,
    }

    /// <summary>
    /// One review action on a translated guide section (spec §6.9). Status is never stored: an
    /// approval counts only while <see cref="SourceHash"/> and <see cref="TranslationHash"/> still
    /// match the current English and translated text, and a correction counts as applied once the
    /// translation file contains the suggested text. The guide files stay the source of truth.
    /// </summary>
    public class HelpReviewItem
    {
        public int Id { get; set; }

        [MaxLength(80)] public string Module { get; set; } = string.Empty;
        [MaxLength(120)] public string SectionId { get; set; } = string.Empty;
        [MaxLength(8)] public string Language { get; set; } = string.Empty;

        public HelpReviewKind Kind { get; set; }

        /// <summary>HelpHash of the English section when the review was made.</summary>
        [MaxLength(32)] public string SourceHash { get; set; } = string.Empty;

        /// <summary>HelpHash of the translated section the reviewer saw.</summary>
        [MaxLength(32)] public string TranslationHash { get; set; } = string.Empty;

        [MaxLength(300)] public string? SuggestedTitle { get; set; }
        public string? SuggestedBody { get; set; }
        [MaxLength(1000)] public string? Comment { get; set; }

        /// <summary>Pronunciation report: the word read wrongly, and how it should be said.</summary>
        [MaxLength(120)] public string? Word { get; set; }
        [MaxLength(200)] public string? SayAs { get; set; }

        [MaxLength(450)] public string ReviewerUserId { get; set; } = string.Empty;
        [MaxLength(200)] public string ReviewerName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>The languages a Help Desk translation reviewer may review (CSV of codes; empty = all).</summary>
    public class HelpReviewer
    {
        [Key, MaxLength(450)] public string UserId { get; set; } = string.Empty;
        [MaxLength(100)] public string Languages { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
    }
}
