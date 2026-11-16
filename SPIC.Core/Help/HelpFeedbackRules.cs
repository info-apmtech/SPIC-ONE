using System;
using System.Linq;
using SPIC.Core.DTOs;

namespace SPIC.Core.Help
{
    /// <summary>Checks a feedback vote before it is stored. Returns the message to show, or null when valid.</summary>
    public static class HelpFeedbackRules
    {
        public const int MaxComment = 500;

        public static string? Validate(HelpFeedbackDto? dto, HelpLibrary library)
        {
            if (dto is null) return "Send a vote.";
            var guide = string.IsNullOrWhiteSpace(dto.Module) ? null : library.English(dto.Module);
            if (guide is null) return "That guide does not exist.";
            if (!string.IsNullOrWhiteSpace(dto.SectionId)
                && !guide.Sections.Any(s => string.Equals(s.Id, dto.SectionId, StringComparison.OrdinalIgnoreCase)))
                return "That part of the guide does not exist.";
            if (!library.Config.Languages.Any(l => string.Equals(l.Code, dto.Language, StringComparison.OrdinalIgnoreCase)))
                return "That language is not offered.";
            if ((dto.Comment ?? string.Empty).Trim().Length > MaxComment)
                return $"Keep the comment to {MaxComment} characters.";
            return null;
        }
    }
}
