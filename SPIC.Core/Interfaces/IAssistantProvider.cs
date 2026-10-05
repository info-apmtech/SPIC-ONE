using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.Core.Interfaces;

/// <summary>
/// One retrieved Digital Library item handed to the assistant as context.
/// The text fields are already truncated by the retriever (~1500 chars each)
/// so a provider can concatenate them without blowing a token budget.
/// </summary>
public class AssistantLibraryItem
{
    public int ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public LibraryContentKind Kind { get; set; }
    public string? ShortDescription { get; set; }
    public string? Overview { get; set; }
    public string? Features { get; set; }
    public string? Usage { get; set; }
    public string? Tags { get; set; }

    public AssistantSourceDto ToSource() => new()
    {
        ContentId = ContentId,
        Title = Title,
        Kind = Kind
    };
}

/// <summary>One earlier turn of the conversation ("user" | "assistant").</summary>
public class AssistantTurn
{
    public string Role { get; set; } = "user";
    public string Text { get; set; } = string.Empty;
}

/// <summary>Everything a provider needs to answer one question.</summary>
public class AssistantContext
{
    public string Question { get; set; } = string.Empty;

    /// <summary>The last ~10 messages of the conversation, oldest first, excluding the current question.</summary>
    public List<AssistantTurn> History { get; set; } = new();

    /// <summary>Published library items retrieved for this question, most relevant first.</summary>
    public List<AssistantLibraryItem> Items { get; set; } = new();

    /// <summary>The library item the user opened the assistant from (?about=), when any.</summary>
    public AssistantLibraryItem? About { get; set; }
}

/// <summary>What a provider returns for one question.</summary>
public class AssistantAnswer
{
    public string Text { get; set; } = string.Empty;
    public List<AssistantSourceDto> Sources { get; set; } = new();
    /// <summary>Up to three follow-up questions the client shows as chips.</summary>
    public List<string> Suggestions { get; set; } = new();
    /// <summary>"anthropic" | "keyword" — which provider actually produced the answer.</summary>
    public string Provider { get; set; } = "keyword";
}

/// <summary>
/// SPIC AI assistant back end. Two implementations exist: a no-network keyword
/// provider (always available) and the Anthropic Messages API provider, used
/// when Assistant:AnthropicApiKey is configured. Anthropic falls back to the
/// keyword provider on any error, so AnswerAsync never throws for the caller.
/// </summary>
public interface IAssistantProvider
{
    /// <summary>"anthropic" | "keyword".</summary>
    string Name { get; }

    Task<AssistantAnswer> AnswerAsync(AssistantContext ctx, CancellationToken cancellationToken = default);
}
