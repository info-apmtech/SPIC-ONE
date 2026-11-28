namespace SPIC.Core.DTOs;

// Farmer Portal Task 5 contracts (docs/farmer-portal.md): SPIC AI for farmers. Farmer role only; every
// conversation belongs to the caller.
//   GET    api/Farmer/assistant/status               -> FarmerAssistantStatusDto (cap, language, starter questions)
//   GET    api/Farmer/assistant/conversations        -> List<FarmerChatConversationDto> (newest first)
//   GET    api/Farmer/assistant/conversations/{id}   -> FarmerChatDetailDto
//   DELETE api/Farmer/assistant/conversations/{id}   -> 204 (soft delete)
//   POST   api/Farmer/assistant/ask                  FarmerAskRequest -> FarmerAskResponse
//                                                    400 / 404 / 429 FarmerAssistantErrorDto
// Times are UTC; the screens show them in IST.

public class FarmerAssistantStatusDto
{
    public int DailyLimit { get; set; }
    public int UsedToday { get; set; }
    public int LeftToday { get; set; }
    /// <summary>The next IST midnight, in UTC: when the count starts again.</summary>
    public DateTime ResetsAtUtc { get; set; }
    public int MaxQuestionChars { get; set; }
    /// <summary>True when the full AI answers are switched on (an Anthropic key is configured). False: every
    /// answer is the basic library answer.</summary>
    public bool FullAnswers { get; set; }
    /// <summary>The answer language code (profile PreferredLanguage, "en" when none).</summary>
    public string Language { get; set; } = "en";
    /// <summary>The language name in English ("Tamil").</summary>
    public string LanguageName { get; set; } = "English";
    /// <summary>Starter questions for the farmer's crops (shown in English, answered in the language).</summary>
    public List<string> Starters { get; set; } = new();
}

public class FarmerChatConversationDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int QuestionCount { get; set; }
}

public class FarmerChatDetailDto : FarmerChatConversationDto
{
    public List<FarmerChatMessageDto> Messages { get; set; } = new();
}

public class FarmerChatMessageDto
{
    public long Id { get; set; }
    /// <summary>"user" | "assistant".</summary>
    public string Role { get; set; } = "user";
    public string Text { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>Digital Library items the answer used (link chips).</summary>
    public List<AssistantSourceDto> Sources { get; set; } = new();
    /// <summary>True when <see cref="Sources"/> are related library items the answer did not name ("Related in the SPIC library").</summary>
    public bool SourcesRelated { get; set; }
    /// <summary>Follow-up questions (chips).</summary>
    public List<string> Suggestions { get; set; } = new();
    /// <summary>"anthropic" | "keyword" for answers; null for questions. "keyword" = basic library answer.</summary>
    public string? Provider { get; set; }
}

public class FarmerAskRequest
{
    /// <summary>Null starts a new conversation.</summary>
    public long? ConversationId { get; set; }
    public string Question { get; set; } = "";
}

public class FarmerAskResponse
{
    public long ConversationId { get; set; }
    public string ConversationTitle { get; set; } = "";
    public FarmerChatMessageDto Question { get; set; } = new();
    public FarmerChatMessageDto Answer { get; set; } = new();
    public int LeftToday { get; set; }
    public DateTime ResetsAtUtc { get; set; }
    /// <summary>A gentle line when the daily AI budget or the kill switch chose the basic answer (fix round 1).</summary>
    public string? Notice { get; set; }
}

public class FarmerAssistantErrorDto
{
    public string Message { get; set; } = "";
    /// <summary>429 only: seconds until the next IST midnight.</summary>
    public int? RetryAfterSeconds { get; set; }
    /// <summary>429 only: the next IST midnight in UTC.</summary>
    public DateTime? ResetsAtUtc { get; set; }
    /// <summary>429 only: the daily limit.</summary>
    public int? DailyLimit { get; set; }
}
