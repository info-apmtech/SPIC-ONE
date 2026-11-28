using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Assistant;
using SpicAPI.Services;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Farmer portal Task 5 (docs/farmer-portal.md): SPIC AI for farmers, mirroring
    /// <see cref="LibraryAssistantController"/>. Farmer role only; every conversation is the caller's own.
    ///
    ///   GET    api/Farmer/assistant/status               cap, answer language, starter questions
    ///   GET    api/Farmer/assistant/conversations        the caller's conversations, newest first
    ///   GET    api/Farmer/assistant/conversations/{id}   one conversation with its messages
    ///   DELETE api/Farmer/assistant/conversations/{id}   soft delete (its questions still count today)
    ///   POST   api/Farmer/assistant/ask                  ask (new conversation when ConversationId is null)
    ///
    /// Fix round 1: every AI answer first takes one unit of the shared daily AI budget (<see cref="IFarmerAiBudget"/>,
    /// FarmerPortal:AssistantGlobalDailyLimit, kill switch FarmerPortal:AssistantEnabled); when it says no, the labelled
    /// basic answer is given with a gentle notice.
    ///
    /// Answers come from the configured <see cref="IAssistantProvider"/> in <see cref="AssistantMode.Farmer"/>:
    /// the farmer system prompt in the profile's language, the farmer's context and the Digital Library items
    /// retrieved for the question. With no Anthropic key (or when the call fails) the keyword provider answers,
    /// labelled "Basic answer from the SPIC library". At most 30 questions per farmer per IST day
    /// (FarmerPortal:AssistantDailyLimit), counted and stored under a PostgreSQL advisory lock per farmer before
    /// the answer is made, so parallel taps and both API replicas cannot pass the cap together.
    /// </summary>
    [Authorize(Roles = nameof(AppRole.Farmer))]
    [ApiController]
    [Route("api/Farmer/assistant")]
    public class FarmerAssistantController : ControllerBase
    {
        private const int MaxConversationsListed = 50;

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly AppDbContext _db;
        private readonly LibraryRetriever _retriever;
        private readonly IAssistantProvider _provider;
        private readonly KeywordAssistantProvider _basic;
        private readonly IFarmerAiBudget _budget;
        private readonly FarmerAssistantContextLoader _context;
        private readonly AssistantOptions _options;
        private readonly IConfiguration _config;
        private readonly ILogger<FarmerAssistantController> _log;

        public FarmerAssistantController(
            AppDbContext db,
            LibraryRetriever retriever,
            IAssistantProvider provider,
            KeywordAssistantProvider basic,
            IFarmerAiBudget budget,
            FarmerAssistantContextLoader context,
            IOptions<AssistantOptions> options,
            IConfiguration config,
            ILogger<FarmerAssistantController> log)
        {
            _db = db;
            _retriever = retriever;
            _provider = provider;
            _basic = basic;
            _budget = budget;
            _context = context;
            _options = options.Value;
            _config = config;
            _log = log;
        }

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private int DailyLimit => FarmerAssistantRules.Limit(_config["FarmerPortal:AssistantDailyLimit"]);

        /// <summary>Questions the farmer asked since IST midnight (deleted conversations included).</summary>
        private Task<int> UsedTodayAsync(string userId, DateTime utcNow, CancellationToken ct)
        {
            var start = FarmerAssistantRules.IstDayStartUtc(utcNow);
            return _db.FarmerChatMessages.CountAsync(m => m.FarmerUserId == userId && m.Role == "user" && m.CreatedAtUtc >= start, ct);
        }

        // ------------------------------------------------------------------ status

        [HttpGet("status")]
        public async Task<IActionResult> Status(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var now = DateTime.UtcNow;
            var used = await UsedTodayAsync(userId, now, ct);
            var profile = await _db.FarmerProfiles.AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new { p.PrimaryCrops, p.PreferredLanguage })
                .FirstOrDefaultAsync(ct);
            var language = FarmerAssistantRules.LanguageCode(profile?.PreferredLanguage);
            var crops = (profile?.PrimaryCrops ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var limit = DailyLimit;
            return Ok(new FarmerAssistantStatusDto
            {
                DailyLimit = limit,
                UsedToday = used,
                LeftToday = FarmerAssistantRules.Left(used, limit),
                ResetsAtUtc = FarmerAssistantRules.NextResetUtc(now),
                MaxQuestionChars = FarmerAssistantRules.MaxQuestionChars,
                // Key set, not switched off and today's shared AI budget not used up.
                FullAnswers = await _budget.AvailableAsync(FarmerAiKind.Chat, ct),
                Language = language,
                LanguageName = FarmerAssistantRules.LanguageName(language),
                Starters = FarmerAssistantRules.Starters(crops)
            });
        }

        // ------------------------------------------------------------------ conversations

        [HttpGet("conversations")]
        public async Task<IActionResult> Conversations(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var list = await _db.FarmerChatConversations.AsNoTracking()
                .Where(c => c.FarmerUserId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.UpdatedAtUtc).ThenByDescending(c => c.Id)
                .Take(MaxConversationsListed)
                .Select(c => new FarmerChatConversationDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAtUtc = c.CreatedAtUtc,
                    UpdatedAtUtc = c.UpdatedAtUtc,
                    QuestionCount = c.Messages.Count(m => m.Role == "user")
                })
                .ToListAsync(ct);
            foreach (var row in list)
            {
                row.CreatedAtUtc = Utc(row.CreatedAtUtc);
                row.UpdatedAtUtc = Utc(row.UpdatedAtUtc);
            }
            return Ok(list);
        }

        [HttpGet("conversations/{id:long}")]
        public async Task<IActionResult> Conversation(long id, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var c = await _db.FarmerChatConversations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmerUserId == userId && !x.IsDeleted, ct);
            if (c is null) return NotFound(new FarmerAssistantErrorDto { Message = "This chat was not found. It may have been deleted." });

            var messages = await _db.FarmerChatMessages.AsNoTracking()
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.Id)
                .ToListAsync(ct);
            return Ok(new FarmerChatDetailDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAtUtc = Utc(c.CreatedAtUtc),
                UpdatedAtUtc = Utc(c.UpdatedAtUtc),
                QuestionCount = messages.Count(m => m.Role == "user"),
                Messages = messages.Select(Map).ToList()
            });
        }

        [HttpDelete("conversations/{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var c = await _db.FarmerChatConversations
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmerUserId == userId && !x.IsDeleted, ct);
            if (c is null) return NotFound(new FarmerAssistantErrorDto { Message = "This chat was not found. It may have been deleted." });
            c.IsDeleted = true;
            c.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // ------------------------------------------------------------------ ask

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] FarmerAskRequest? request, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var problem = FarmerAssistantRules.Validate(request?.Question);
            if (problem is not null) return BadRequest(new FarmerAssistantErrorDto { Message = problem });
            var question = request!.Question.Trim();

            FarmerChatConversation? conversation = null;
            if (request.ConversationId is { } cid)
            {
                conversation = await _db.FarmerChatConversations
                    .FirstOrDefaultAsync(x => x.Id == cid && x.FarmerUserId == userId && !x.IsDeleted, ct);
                if (conversation is null)
                    return NotFound(new FarmerAssistantErrorDto { Message = "This chat was not found. Please start a new chat." });
            }

            var limit = DailyLimit;
            var now = DateTime.UtcNow;

            // A capped farmer gets the 429 before any context is loaded (fix round 1, M3); the locked count below
            // stays the authority.
            if (FarmerAssistantRules.IsCapped(await UsedTodayAsync(userId, now, ct), limit))
                return Capped(limit, now);

            var loaded = await _context.LoadAsync(userId, ct);

            // Earlier messages for the model, read before this question is stored.
            var earlier = conversation is null
                ? new List<AssistantTurn>()
                : (await _db.FarmerChatMessages.AsNoTracking()
                        .Where(m => m.ConversationId == conversation.Id)
                        .OrderByDescending(m => m.Id)
                        .Take(FarmerAssistantRules.HistoryMessages + 2)
                        .Select(m => new { m.Id, m.Role, m.Text })
                        .ToListAsync(ct))
                    .OrderBy(m => m.Id)
                    .Select(m => new AssistantTurn { Role = m.Role, Text = m.Text })
                    .ToList();

            // Count and store the question under one advisory lock per farmer: the cap holds for parallel taps
            // and both API replicas. The lock is released before the (slow) answer is made.
            FarmerChatMessage userMessage;
            int usedAfter;
            var npgsql = _db.Database.IsNpgsql();
            await using (var tx = npgsql ? await _db.Database.BeginTransactionAsync(ct) : null)
            {
                if (npgsql)
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT pg_advisory_xact_lock(hashtextextended({"fai:" + userId}, 0))", ct);

                var used = await UsedTodayAsync(userId, now, ct);
                if (FarmerAssistantRules.IsCapped(used, limit))
                {
                    _log.LogInformation("Farmer assistant daily cap reached ({Used}/{Limit}).", used, limit);
                    return Capped(limit, now);
                }

                if (conversation is null)
                {
                    conversation = new FarmerChatConversation
                    {
                        FarmerUserId = userId,
                        Title = FarmerAssistantRules.MakeTitle(question),
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    _db.FarmerChatConversations.Add(conversation);
                    await _db.SaveChangesAsync(ct);
                }

                userMessage = new FarmerChatMessage
                {
                    ConversationId = conversation.Id,
                    FarmerUserId = userId,
                    Role = "user",
                    Text = question,
                    Language = loaded.Language,
                    CreatedAtUtc = now
                };
                _db.FarmerChatMessages.Add(userMessage);
                conversation.UpdatedAtUtc = now;
                await _db.SaveChangesAsync(ct);
                if (tx is not null) await tx.CommitAsync(ct);
                usedAfter = used + 1;
            }

            // From here on the question is stored and counted: the answer is made and saved even if the farmer leaves
            // the page (fix round 1, M2), so CancellationToken.None; the 60 s Anthropic timeout still bounds it.
            // The shared daily AI budget (or the kill switch) may choose the labelled basic answer instead of the AI.
            var decision = await _budget.TryConsumeAsync(FarmerAiKind.Chat, userId, CancellationToken.None);
            var provider = decision == FarmerAiDecision.Allowed ? _provider : _basic;

            // The answer (never throws: Anthropic falls back to the keyword provider on any failure).
            var (items, _) = await _retriever.RetrieveAsync(question, null, cancellationToken: CancellationToken.None);
            var answer = await provider.AnswerAsync(new AssistantContext
            {
                Mode = AssistantMode.Farmer,
                Question = question,
                History = FarmerAssistantRules.History(earlier),
                Items = items,
                SystemPrompt = FarmerAssistantRules.SystemPrompt(loaded.Language),
                ContextBlock = FarmerAssistantRules.ContextBlock(loaded.Facts),
                Language = loaded.Language,
                Crops = loaded.Facts.Crops,
                MaxTokens = _options.FarmerMaxTokens > 0 ? _options.FarmerMaxTokens : FarmerAssistantRules.DefaultMaxTokens,
                Effort = string.IsNullOrWhiteSpace(_options.FarmerEffort) ? null : _options.FarmerEffort
            }, CancellationToken.None);

            var reply = new FarmerChatMessage
            {
                ConversationId = conversation.Id,
                FarmerUserId = userId,
                Role = "assistant",
                Text = answer.Text,
                SourcesJson = answer.Sources.Count == 0 ? null : JsonSerializer.Serialize(answer.Sources, Json),
                SuggestionsJson = answer.Suggestions.Count == 0 ? null : JsonSerializer.Serialize(answer.Suggestions, Json),
                SourcesRelated = answer.SourcesRelated,
                Provider = answer.Provider,
                Language = loaded.Language,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.FarmerChatMessages.Add(reply);
            conversation.UpdatedAtUtc = reply.CreatedAtUtc;
            // The answer is saved even when the farmer has left the page (they see it when they come back).
            await _db.SaveChangesAsync(CancellationToken.None);

            _log.LogInformation("Farmer assistant answered with {Provider} ({Items} library items, language {Language}).",
                answer.Provider, items.Count, loaded.Language);

            return Ok(new FarmerAskResponse
            {
                ConversationId = conversation.Id,
                ConversationTitle = conversation.Title,
                Question = Map(userMessage),
                Answer = Map(reply),
                LeftToday = FarmerAssistantRules.Left(usedAfter, limit),
                ResetsAtUtc = FarmerAssistantRules.NextResetUtc(now),
                Notice = answer.Provider == "keyword" ? FarmerAiBudgetRules.Notice(decision) : null
            });
        }

        // ------------------------------------------------------------------ helpers

        private static FarmerChatMessageDto Map(FarmerChatMessage m) => new()
        {
            Id = m.Id,
            Role = m.Role,
            Text = m.Text,
            CreatedAtUtc = Utc(m.CreatedAtUtc),
            Sources = Read<List<AssistantSourceDto>>(m.SourcesJson) ?? new(),
            SourcesRelated = m.SourcesRelated,
            Suggestions = Read<List<string>>(m.SuggestionsJson) ?? new(),
            Provider = m.Provider
        };

        private IActionResult Capped(int limit, DateTime now)
        {
            Response.Headers.RetryAfter = FarmerAssistantRules.SecondsUntilReset(now).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new FarmerAssistantErrorDto
            {
                Message = FarmerAssistantRules.CapMessage(limit),
                RetryAfterSeconds = FarmerAssistantRules.SecondsUntilReset(now),
                ResetsAtUtc = FarmerAssistantRules.NextResetUtc(now),
                DailyLimit = limit
            });
        }

        /// <summary>Timestamps are stored as UTC without a kind (timestamp without time zone): send them with "Z".</summary>
        private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private static T? Read<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<T>(json, Json); }
            catch (JsonException) { return null; }
        }
    }
}
