namespace SPIC.Core.Entities;

// Farmer Portal Task 5 (docs/farmer-portal.md, 2026-11-28): SPIC AI for farmers (/farmer/assistant).
// Own tables instead of the Digital Library LibraryConversations / LibraryMessages: those store local
// DateTime.Now timestamps and int ids and are read by the live library chat with no scope filter, so a
// discriminator would have meant editing every library query. These rows are UTC, belong to one farmer
// and carry what the daily cap and the language need. No foreign key to the user (like the other farmer
// tables); the messages cascade with their conversation.

/// <summary>One SPIC AI conversation of a farmer. Delete is a soft delete (the questions still count
/// against the daily cap).</summary>
public class FarmerChatConversation
{
    public long Id { get; set; }
    public string FarmerUserId { get; set; } = string.Empty;
    /// <summary>The first 60 characters of the first question.</summary>
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<FarmerChatMessage> Messages { get; set; } = new List<FarmerChatMessage>();
}

/// <summary>One question ("user") or answer ("assistant"). <see cref="FarmerUserId"/> is copied from
/// the conversation so the daily cap is one indexed count.</summary>
public class FarmerChatMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public FarmerChatConversation? Conversation { get; set; }
    public string FarmerUserId { get; set; } = string.Empty;
    /// <summary>"user" | "assistant".</summary>
    public string Role { get; set; } = "user";
    public string Text { get; set; } = string.Empty;
    /// <summary>List&lt;AssistantSourceDto&gt; as JSON (assistant messages only).</summary>
    public string? SourcesJson { get; set; }
    /// <summary>Up to three follow-up questions as JSON (assistant messages only).</summary>
    public string? SuggestionsJson { get; set; }
    /// <summary>True when the sources are related library items the answer did not name (fix round 1, V39).</summary>
    public bool SourcesRelated { get; set; }
    /// <summary>"anthropic" | "keyword" (assistant messages only).</summary>
    public string? Provider { get; set; }
    /// <summary>The answer language asked for (the profile's PreferredLanguage at the time).</summary>
    public string? Language { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// One AI call made for a farmer (Task 5 fix round 1, V39): the shared daily AI budget across all farmers
/// (SpicAPI/Services/FarmerAiBudget.cs) counts these rows on the IST day. Kind is the FarmerAiKind name
/// ("Chat", "LeafCheck"). Rows are written before the call, under an advisory lock.
/// </summary>
public class FarmerAiUsage
{
    public long Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string FarmerUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
