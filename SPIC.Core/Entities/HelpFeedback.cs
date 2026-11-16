using System;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.Entities
{
    /// <summary>
    /// "Was this guide helpful?" — one vote per user, guide (and screen) and day; a second vote on the
    /// same day replaces the first. Reviewers see the totals in /Help/Review → Feedback.
    /// </summary>
    public class HelpFeedback
    {
        public int Id { get; set; }

        [MaxLength(80)] public string Module { get; set; } = string.Empty;

        /// <summary>The screen section voted on; empty for a vote on the whole guide.</summary>
        [MaxLength(120)] public string SectionId { get; set; } = string.Empty;

        [MaxLength(8)] public string Language { get; set; } = string.Empty;
        public bool Helpful { get; set; }
        [MaxLength(500)] public string? Comment { get; set; }

        [MaxLength(450)] public string UserId { get; set; } = string.Empty;
        public DateOnly Day { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
