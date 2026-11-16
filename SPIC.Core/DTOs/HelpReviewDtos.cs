using System;
using System.Collections.Generic;
using SPIC.Core.Entities;
using SPIC.Core.Help;

namespace SPIC.Core.DTOs
{
    /// <summary>One translated section in a reviewer's list.</summary>
    public sealed record HelpReviewRowDto(string Module, string SectionId, string Title, HelpReviewState State, string Confidence);

    /// <summary>A reviewer's action. <see cref="ShownSourceHash"/> / <see cref="ShownTranslationHash"/>
    /// are the hashes of the text the reviewer was shown; the server rejects the review (409) when
    /// they differ from its own files.</summary>
    public sealed record HelpReviewSubmitDto(
        string Module,
        string SectionId,
        string Language,
        HelpReviewKind Kind,
        string? SuggestedTitle,
        string? SuggestedBody,
        string? Comment,
        string? Word,
        string? SayAs,
        string? ShownSourceHash,
        string? ShownTranslationHash);

    /// <summary>A correction not yet in the translation file (download for help-tools apply-reviews).</summary>
    public sealed record HelpCorrectionExportDto(
        string Module,
        string SectionId,
        string Language,
        string SourceHash,
        string SuggestedTitle,
        string SuggestedBody,
        string ReviewerName,
        DateTime CreatedAtUtc);

    public sealed record HelpPronunciationExportDto(string Language, string Word, string SayAs, string? Comment, string ReviewerName);

    public sealed record HelpReviewExportDto(
        IReadOnlyList<HelpCorrectionExportDto> Corrections,
        IReadOnlyList<HelpPronunciationExportDto> Pronunciations);

    public sealed record HelpReviewerDto(string UserId, string Name, string? DesignationName, IReadOnlyList<string> Languages);

    public sealed record HelpReviewerUpdateDto(IReadOnlyList<string> Languages);

    /// <summary>What the review screen needs about the current user.</summary>
    public sealed record HelpReviewAccessDto(bool IsReviewer, bool IsAdmin, IReadOnlyList<string> Languages);
}
