using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
    /// <summary>A "Was this guide helpful?" vote. <see cref="SectionId"/> null = the whole guide.</summary>
    public sealed record HelpFeedbackDto(string Module, string? SectionId, string Language, bool Helpful, string? Comment);

    /// <summary>Totals per guide or screen for the reviewers' Feedback tab.</summary>
    public sealed record HelpFeedbackSummaryDto(string Module, string? SectionId, int Yes, int No, IReadOnlyList<string> RecentComments);
}
