using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Assistant;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// SPIC AI, the Digital Library assistant. Every signed-in user may ask; answers are
    /// composed only from PUBLISHED library content retrieved for the question, and the
    /// items used come back as sources so the UI can link them.
    ///
    /// Conversations belong to the caller (JWT NameIdentifier): the list, the detail and
    /// the delete never reach another user's thread. Delete is a soft delete.
    ///
    /// Routes: api/Library/assistant/{conversations|conversations/{id}|ask}
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/Library/assistant")]
    public class LibraryAssistantController : ControllerBase
    {
        private const int HistoryTurns = 10;
        private const int TitleLength = 60;

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly AppDbContext _db;
        private readonly LibraryRetriever _retriever;
        private readonly IAssistantProvider _provider;
        private readonly ILogger<LibraryAssistantController> _logger;

        public LibraryAssistantController(
            AppDbContext db,
            LibraryRetriever retriever,
            IAssistantProvider provider,
            ILogger<LibraryAssistantController> logger)
        {
            _db = db;
            _retriever = retriever;
            _provider = provider;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // =====================================================================
        //  Conversations
        // =====================================================================

        // GET /api/Library/assistant/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { Success = false, Message = "Could not resolve the signed-in user." });

            var conversations = await _db.LibraryConversations
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.UpdatedAt)
                .ThenByDescending(c => c.Id)
                .Select(c => new AssistantConversationDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(conversations);
        }

        // GET /api/Library/assistant/conversations/{id}
        [HttpGet("conversations/{id:int}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { Success = false, Message = "Could not resolve the signed-in user." });

            var conversation = await _db.LibraryConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);

            if (conversation == null)
                return NotFound(new { Success = false, Message = "Conversation not found." });

            var messages = await _db.LibraryMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.Id)
                .ToListAsync();

            return Ok(new AssistantConversationDetailDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                Messages = messages.Select(MapMessage).ToList()
            });
        }

        // DELETE /api/Library/assistant/conversations/{id}  (soft delete)
        [HttpDelete("conversations/{id:int}")]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { Success = false, Message = "Could not resolve the signed-in user." });

            var conversation = await _db.LibraryConversations
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);

            if (conversation == null)
                return NotFound(new { Success = false, Message = "Conversation not found." });

            conversation.IsDeleted = true;
            conversation.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Conversation deleted." });
        }

        // =====================================================================
        //  Ask
        // =====================================================================

        // POST /api/Library/assistant/ask
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AssistantAskRequest? request)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { Success = false, Message = "Could not resolve the signed-in user." });

            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { Success = false, Message = "Question is required." });

            var question = request.Question.Trim();
            var now = DateTime.Now;

            LibraryConversation conversation;
            var history = new List<AssistantTurn>();

            if (request.ConversationId.HasValue)
            {
                var existing = await _db.LibraryConversations
                    .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.UserId == userId && !c.IsDeleted);

                if (existing == null)
                    return NotFound(new { Success = false, Message = "Conversation not found." });

                conversation = existing;

                history = (await _db.LibraryMessages
                        .AsNoTracking()
                        .Where(m => m.ConversationId == conversation.Id)
                        .OrderByDescending(m => m.Id)
                        .Take(HistoryTurns)
                        .ToListAsync())
                    .OrderBy(m => m.Id)
                    .Select(m => new AssistantTurn { Role = m.Role, Text = m.Text })
                    .ToList();
            }
            else
            {
                conversation = new LibraryConversation
                {
                    UserId = userId,
                    Title = MakeTitle(question),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.LibraryConversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            var (items, about) = await _retriever.RetrieveAsync(question, request.AboutContentId, kind: request.Kind);

            var answer = await _provider.AnswerAsync(new AssistantContext
            {
                Question = question,
                History = history,
                Items = items,
                About = about
            }, HttpContext.RequestAborted);

            var userMessage = new LibraryMessage
            {
                ConversationId = conversation.Id,
                Role = "user",
                Text = question,
                CreatedAt = now
            };

            var replyMessage = new LibraryMessage
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Text = answer.Text,
                SourcesJson = answer.Sources.Count == 0 ? null : JsonSerializer.Serialize(answer.Sources, Json),
                Provider = answer.Provider,
                CreatedAt = DateTime.Now
            };

            _db.LibraryMessages.AddRange(userMessage, replyMessage);

            conversation.UpdatedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(conversation.Title))
                conversation.Title = MakeTitle(question);

            await _db.SaveChangesAsync();

            var reply = MapMessage(replyMessage);
            reply.Suggestions = answer.Suggestions;

            return Ok(new AssistantAskResponse
            {
                ConversationId = conversation.Id,
                ConversationTitle = conversation.Title,
                UserMessage = MapMessage(userMessage),
                Reply = reply,
                Provider = answer.Provider
            });
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        /// <summary>Conversation title: the first 60 characters of the first question.</summary>
        private static string MakeTitle(string question)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(question.Trim(), @"\s+", " ");
            if (cleaned.Length <= TitleLength) return cleaned;
            return cleaned.Substring(0, TitleLength).TrimEnd() + "…";
        }

        private static AssistantMessageDto MapMessage(LibraryMessage m) => new()
        {
            Id = m.Id,
            Role = m.Role,
            Text = m.Text,
            CreatedAt = m.CreatedAt,
            Sources = DeserializeSources(m.SourcesJson),
            Provider = m.Provider
        };

        private static List<AssistantSourceDto> DeserializeSources(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<AssistantSourceDto>();
            try
            {
                return JsonSerializer.Deserialize<List<AssistantSourceDto>>(json, Json) ?? new List<AssistantSourceDto>();
            }
            catch (JsonException)
            {
                return new List<AssistantSourceDto>();
            }
        }
    }
}
