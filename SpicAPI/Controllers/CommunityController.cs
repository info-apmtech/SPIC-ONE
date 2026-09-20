using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// SPIC Knowledge Community — discussions, replies, reactions, attachments and the
	/// dashboard panels (stats, popular products, lookups).
	///
	/// Every signed-in user may read and post; editing/deleting is limited to the author
	/// or Admin/CorporateAdmin. Contracts are fixed in SPIC.Core/DTOs/CommunityDtos.cs.
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class CommunityController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly IWebHostEnvironment _env;

		public CommunityController(AppDbContext db, IWebHostEnvironment env)
		{
			_db = db;
			_env = env;
		}

		// ---------------------------------------------------------------- constants

		private const int MaxAttachmentsPerPost = 6;
		private const long MaxAttachmentBytes = 10L * 1024 * 1024;
		private const int TrendingWindowDays = 30;

		private static readonly string[] AllowedExtensions =
			{ ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".doc", ".docx" };

		private static readonly string[] DefaultCategories =
		{
			"Crop Fertilizer Knowledge", "Soil Health", "Pest & Disease", "Irrigation",
			"Seeds & Sowing", "Harvest & Storage", "Government Schemes"
		};

		private static readonly string[] DefaultProducts =
		{
			"SPIC Urea", "DAP Fertilizer", "SPIC Posh", "SPIC Prom",
			"SPIC Super", "SPIC Gypsum", "SPIC Potash"
		};

		private static readonly string[] DefaultCrops =
		{
			"Paddy", "Wheat", "Tomato", "Sugarcane", "Cotton", "Groundnut", "Banana", "Maize"
		};

		// ---------------------------------------------------------------- stats

		// GET /api/Community/stats
		[HttpGet("stats")]
		public async Task<IActionResult> GetStats()
		{
			var since = DateTime.Now.AddDays(-TrendingWindowDays);
			var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

			var postAuthors = await _db.CommunityPosts.AsNoTracking()
				.Where(p => !p.IsDeleted)
				.Select(p => new { p.AuthorUserId, p.CreatedAt })
				.ToListAsync();

			var replyAuthors = await _db.CommunityPostReplies.AsNoTracking()
				.Where(r => !r.IsDeleted)
				.Select(r => new { r.AuthorUserId, r.CreatedAt, r.AuthorRole })
				.ToListAsync();

			var members = postAuthors.Select(p => p.AuthorUserId)
				.Concat(replyAuthors.Select(r => r.AuthorUserId))
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Count();

			var active = postAuthors.Where(p => p.CreatedAt >= since).Select(p => p.AuthorUserId)
				.Concat(replyAuthors.Where(r => r.CreatedAt >= since).Select(r => r.AuthorUserId))
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Count();

			var dto = new CommunityStatsDto
			{
				CommunityMembers = members,
				TotalDiscussions = postAuthors.Count,
				ActiveMembers = active,
				ExpertAnswers = replyAuthors.Count(r => r.AuthorRole == "SPIC Expert"),
				NewThisMonth = postAuthors.Count(p => p.CreatedAt >= monthStart)
			};

			return Ok(dto);
		}

		// ---------------------------------------------------------------- lists

		// GET /api/Community/recent?take=6
		[HttpGet("recent")]
		public async Task<IActionResult> GetRecent([FromQuery] int take = 6)
		{
			if (take <= 0) take = 6;
			if (take > 12) take = 12;

			var posts = await _db.CommunityPosts.AsNoTracking()
				.Where(p => !p.IsDeleted)
				.OrderByDescending(p => p.CreatedAt)
				.Take(take)
				.ToListAsync();

			var items = await BuildSummariesAsync(posts);
			return Ok(items);
		}

		// GET /api/Community/discussions?tab=&category=&product=&crop=&status=&tags=&from=&to=&q=&sort=&page=&pageSize=
		[HttpGet("discussions")]
		public async Task<IActionResult> GetDiscussions(
			[FromQuery] string? tab,
			[FromQuery] string? category,
			[FromQuery] string? product,
			[FromQuery] string? crop,
			[FromQuery] string? status,
			[FromQuery] string? tags,
			[FromQuery] DateTime? from,
			[FromQuery] DateTime? to,
			[FromQuery] string? q,
			[FromQuery] string? sort,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = 10;
			if (pageSize > 50) pageSize = 50;

			var userId = CurrentUserId();
			var query = _db.CommunityPosts.AsNoTracking().Where(p => !p.IsDeleted);

			if (!string.IsNullOrWhiteSpace(category))
				query = query.Where(p => p.Category == category);
			if (!string.IsNullOrWhiteSpace(product))
				query = query.Where(p => p.Product == product);
			if (!string.IsNullOrWhiteSpace(crop))
				query = query.Where(p => p.Crop == crop);

			if (!string.IsNullOrWhiteSpace(status) &&
				Enum.TryParse<CommunityDiscussionStatus>(status, true, out var statusValue))
			{
				query = query.Where(p => p.Status == statusValue);
			}

			var tagList = SplitCsv(tags).Select(t => t.ToLowerInvariant()).Distinct().ToList();
			if (tagList.Count > 0)
				query = query.Where(BuildTagPredicate(tagList));

			if (from.HasValue)
				query = query.Where(p => p.CreatedAt >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(p => p.CreatedAt < end);
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(p =>
					p.Title.ToLower().Contains(term) ||
					p.Body.ToLower().Contains(term) ||
					(p.Tags != null && p.Tags.ToLower().Contains(term)));
			}

			// Tabs. "trending" and "following" need ids resolved first.
			HashSet<int>? trending = null;
			switch ((tab ?? "all").ToLowerInvariant())
			{
				case "mine":
					query = query.Where(p => p.AuthorUserId == userId);
					break;
				case "trending":
					trending = await GetTrendingIdsAsync();
					var trendingIds = trending.ToList();
					query = query.Where(p => trendingIds.Contains(p.Id));
					break;
				case "unanswered":
					query = query.Where(p => p.ReplyCount == 0);
					break;
				case "following":
					var followed = await _db.CommunityReactions.AsNoTracking()
						.Where(r => r.UserId == userId &&
									r.TargetType == CommunityReactionTarget.Post &&
									r.Kind == CommunityReactionKind.Follow)
						.Select(r => r.TargetId)
						.ToListAsync();
					query = query.Where(p => followed.Contains(p.Id));
					break;
				case "expert":
					query = query.Where(p => p.Status == CommunityDiscussionStatus.ExpertAnswer);
					break;
			}

			query = (sort ?? "latest").ToLowerInvariant() switch
			{
				"replies" => query.OrderByDescending(p => p.ReplyCount).ThenByDescending(p => p.LastActivityAt),
				"views" => query.OrderByDescending(p => p.Views).ThenByDescending(p => p.LastActivityAt),
				"oldest" => query.OrderBy(p => p.CreatedAt),
				_ => query.OrderByDescending(p => p.LastActivityAt).ThenByDescending(p => p.Id)
			};

			var total = await query.CountAsync();
			var posts = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

			var result = new PageResult<DiscussionSummaryDto>
			{
				Items = await BuildSummariesAsync(posts, trending),
				Total = total,
				Page = page,
				PageSize = pageSize
			};

			return Ok(result);
		}

		// GET /api/Community/discussions/{id}   (Views += 1)
		// GET /api/Community/discussions/{id}?countView=false  (edit form / posted page: no view counted)
		[HttpGet("discussions/{id:int}")]
		public async Task<IActionResult> GetDiscussion(int id, [FromQuery] bool countView = true)
		{
			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
			if (post == null)
				return NotFound(new { Success = false, Message = "Discussion not found." });

			if (countView)
			{
				post.Views += 1;
				await _db.SaveChangesAsync();
			}

			return Ok(await BuildDetailAsync(post));
		}

		// POST /api/Community/discussions/similar
		[HttpPost("discussions/similar")]
		public async Task<IActionResult> GetSimilar([FromBody] SimilarRequest request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Title))
				return Ok(new List<DiscussionSummaryDto>());

			var max = request.Max <= 0 ? 3 : Math.Min(request.Max, 10);
			var words = Tokenize(request.Title);
			if (words.Count == 0)
				return Ok(new List<DiscussionSummaryDto>());

			// Small data set: score in memory over the most recently active discussions.
			var candidates = await _db.CommunityPosts.AsNoTracking()
				.Where(p => !p.IsDeleted)
				.OrderByDescending(p => p.LastActivityAt)
				.Take(500)
				.ToListAsync();

			var scored = new List<(CommunityPost Post, int Score)>();
			foreach (var post in candidates)
			{
				var titleWords = Tokenize(post.Title);
				var bodyWords = Tokenize(post.Body);

				var sharedTitle = words.Count(w => titleWords.Contains(w));
				var sharedBody = words.Count(w => bodyWords.Contains(w));
				var score = sharedTitle * 2 + sharedBody;

				// At least two shared content words (stop-words are already dropped by Tokenize).
				if (score >= 3 && sharedTitle + sharedBody >= 2)
					scored.Add((post, score));
			}

			var top = scored
				.OrderByDescending(s => s.Score)
				.ThenByDescending(s => s.Post.LastActivityAt)
				.Take(max)
				.Select(s => s.Post)
				.ToList();

			return Ok(await BuildSummariesAsync(top));
		}

		// ---------------------------------------------------------------- write

		// POST /api/Community/discussions
		[HttpPost("discussions")]
		public async Task<IActionResult> CreateDiscussion([FromBody] DiscussionUpsertDto request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Title))
				return BadRequest(new { Success = false, Message = "Title is required." });
			if (string.IsNullOrWhiteSpace(request.Body))
				return BadRequest(new { Success = false, Message = "Description is required." });

			var author = await ResolveAuthorAsync();

			var post = new CommunityPost
			{
				Title = request.Title.Trim(),
				Body = request.Body.Trim(),
				Category = Clean(request.Category),
				Product = Clean(request.Product),
				Crop = Clean(request.Crop),
				Tags = JoinTags(request.Tags),
				Status = CommunityDiscussionStatus.WaitingForReply,
				AuthorUserId = author.UserId,
				AuthorName = author.Name,
				AuthorRole = author.Role,
				AuthorLocation = author.Location,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now,
				LastActivityAt = DateTime.Now
			};

			_db.CommunityPosts.Add(post);
			await _db.SaveChangesAsync();

			return Ok(await BuildDetailAsync(post));
		}

		// PUT /api/Community/discussions/{id}
		[HttpPut("discussions/{id:int}")]
		public async Task<IActionResult> UpdateDiscussion(int id, [FromBody] DiscussionUpsertDto request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Title))
				return BadRequest(new { Success = false, Message = "Title is required." });
			if (string.IsNullOrWhiteSpace(request.Body))
				return BadRequest(new { Success = false, Message = "Description is required." });

			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
			if (post == null)
				return NotFound(new { Success = false, Message = "Discussion not found." });

			if (!CanModify(post.AuthorUserId))
				return StatusCode(403, new { Success = false, Message = "You can only edit your own discussion." });

			post.Title = request.Title.Trim();
			post.Body = request.Body.Trim();
			post.Category = Clean(request.Category);
			post.Product = Clean(request.Product);
			post.Crop = Clean(request.Crop);
			post.Tags = JoinTags(request.Tags);
			post.UpdatedAt = DateTime.Now;

			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Discussion updated." });
		}

		// DELETE /api/Community/discussions/{id}  (soft)
		[HttpDelete("discussions/{id:int}")]
		public async Task<IActionResult> DeleteDiscussion(int id)
		{
			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
			if (post == null)
				return NotFound(new { Success = false, Message = "Discussion not found." });

			if (!CanModify(post.AuthorUserId))
				return StatusCode(403, new { Success = false, Message = "You can only delete your own discussion." });

			post.IsDeleted = true;
			post.UpdatedAt = DateTime.Now;
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Discussion deleted." });
		}

		// PATCH /api/Community/discussions/{id}/status?status=Resolved|Open
		[HttpPatch("discussions/{id:int}/status")]
		public async Task<IActionResult> SetStatus(int id, [FromQuery] string status)
		{
			if (!Enum.TryParse<CommunityDiscussionStatus>(status, true, out var value))
				return BadRequest(new { Success = false, Message = "Unknown status." });

			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
			if (post == null)
				return NotFound(new { Success = false, Message = "Discussion not found." });

			if (!CanModify(post.AuthorUserId))
				return StatusCode(403, new { Success = false, Message = "Only the author or an administrator can change the status." });

			post.Status = value;
			post.UpdatedAt = DateTime.Now;
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Status updated." });
		}

		// ---------------------------------------------------------------- replies

		// POST /api/Community/discussions/{id}/replies
		[HttpPost("discussions/{id:int}/replies")]
		public async Task<IActionResult> CreateReply(int id, [FromBody] ReplyCreateDto request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Body))
				return BadRequest(new { Success = false, Message = "Reply cannot be empty." });

			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
			if (post == null)
				return NotFound(new { Success = false, Message = "Discussion not found." });

			if (request.ParentReplyId.HasValue)
			{
				var parentExists = await _db.CommunityPostReplies
					.AnyAsync(r => r.Id == request.ParentReplyId.Value && r.PostId == id && !r.IsDeleted);
				if (!parentExists)
					return BadRequest(new { Success = false, Message = "The reply you are answering no longer exists." });
			}

			var author = await ResolveAuthorAsync();

			var reply = new CommunityPostReply
			{
				PostId = id,
				ParentReplyId = request.ParentReplyId,
				AuthorUserId = author.UserId,
				AuthorName = author.Name,
				AuthorRole = author.Role,
				AuthorLocation = author.Location,
				MentionName = Clean(request.MentionName),
				Body = request.Body.Trim(),
				CreatedAt = DateTime.Now
			};

			_db.CommunityPostReplies.Add(reply);

			post.ReplyCount += 1;
			post.LastActivityAt = DateTime.Now;
			post.UpdatedAt = DateTime.Now;

			// Status rules: a SPIC staff reply marks the thread as answered by an expert;
			// any other reply moves a still-waiting thread to Open. A Resolved thread stays
			// resolved until the author or an administrator reopens it.
			if (post.Status != CommunityDiscussionStatus.Resolved)
			{
				if (author.Role == "SPIC Expert")
					post.Status = CommunityDiscussionStatus.ExpertAnswer;
				else if (post.Status == CommunityDiscussionStatus.WaitingForReply)
					post.Status = CommunityDiscussionStatus.Open;
			}

			await _db.SaveChangesAsync();

			return Ok(MapReply(reply, 0, false));
		}

		// DELETE /api/Community/replies/{id}  (soft)
		[HttpDelete("replies/{id:int}")]
		public async Task<IActionResult> DeleteReply(int id)
		{
			var reply = await _db.CommunityPostReplies.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
			if (reply == null)
				return NotFound(new { Success = false, Message = "Reply not found." });

			if (!CanModify(reply.AuthorUserId))
				return StatusCode(403, new { Success = false, Message = "You can only delete your own reply." });

			reply.IsDeleted = true;

			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == reply.PostId);
			if (post != null && post.ReplyCount > 0)
			{
				post.ReplyCount -= 1;
				post.UpdatedAt = DateTime.Now;
			}

			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Reply deleted." });
		}

		// ---------------------------------------------------------------- reactions

		// POST /api/Community/discussions/{id}/like
		[HttpPost("discussions/{id:int}/like")]
		public Task<IActionResult> LikePost(int id) => TogglePostReaction(id, CommunityReactionKind.Like);

		// POST /api/Community/discussions/{id}/save
		[HttpPost("discussions/{id:int}/save")]
		public Task<IActionResult> SavePost(int id) => TogglePostReaction(id, CommunityReactionKind.Save);

		// POST /api/Community/discussions/{id}/follow
		[HttpPost("discussions/{id:int}/follow")]
		public Task<IActionResult> FollowPost(int id) => TogglePostReaction(id, CommunityReactionKind.Follow);

		private async Task<IActionResult> TogglePostReaction(int id, CommunityReactionKind kind)
		{
			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
			if (post == null)
				return NotFound(new { Success = false, Message = "Discussion not found." });

			var active = await ToggleReactionAsync(CommunityReactionTarget.Post, id, kind);

			var count = await _db.CommunityReactions.CountAsync(r =>
				r.TargetType == CommunityReactionTarget.Post && r.TargetId == id && r.Kind == kind);

			if (kind == CommunityReactionKind.Like)
			{
				post.LikeCount = count;
				await _db.SaveChangesAsync();
			}

			return Ok(new ReactionResultDto { Active = active, Count = count });
		}

		// POST /api/Community/replies/{id}/like
		[HttpPost("replies/{id:int}/like")]
		public async Task<IActionResult> LikeReply(int id)
		{
			var reply = await _db.CommunityPostReplies.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
			if (reply == null)
				return NotFound(new { Success = false, Message = "Reply not found." });

			var active = await ToggleReactionAsync(CommunityReactionTarget.Reply, id, CommunityReactionKind.Like);

			var count = await _db.CommunityReactions.CountAsync(r =>
				r.TargetType == CommunityReactionTarget.Reply &&
				r.TargetId == id &&
				r.Kind == CommunityReactionKind.Like);

			reply.LikeCount = count;
			await _db.SaveChangesAsync();

			return Ok(new ReactionResultDto { Active = active, Count = count });
		}

		private async Task<bool> ToggleReactionAsync(CommunityReactionTarget target, int targetId, CommunityReactionKind kind)
		{
			var userId = CurrentUserId();

			var existing = await _db.CommunityReactions.FirstOrDefaultAsync(r =>
				r.UserId == userId && r.TargetType == target && r.TargetId == targetId && r.Kind == kind);

			if (existing != null)
			{
				_db.CommunityReactions.Remove(existing);
				await _db.SaveChangesAsync();
				return false;
			}

			_db.CommunityReactions.Add(new CommunityReaction
			{
				UserId = userId,
				TargetType = target,
				TargetId = targetId,
				Kind = kind,
				CreatedAt = DateTime.Now
			});
			await _db.SaveChangesAsync();
			return true;
		}

		// ---------------------------------------------------------------- attachments

		// POST /api/Community/discussions/{id}/attachments   (multipart, field name "files")
		[HttpPost("discussions/{id:int}/attachments")]
		[RequestSizeLimit(64 * 1024 * 1024)]
		public async Task<IActionResult> UploadAttachments(int id, [FromForm] List<IFormFile> files)
		{
			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
			if (post == null)
				return NotFound(new { Success = false, Message = "Discussion not found." });

			if (!CanModify(post.AuthorUserId))
				return StatusCode(403, new { Success = false, Message = "You can only attach files to your own discussion." });

			if (files == null || files.Count == 0)
				return BadRequest(new { Success = false, Message = "No files uploaded." });

			var existingCount = await _db.CommunityPostAttachments.CountAsync(a => a.PostId == id);
			if (existingCount + files.Count > MaxAttachmentsPerPost)
				return BadRequest(new { Success = false, Message = $"A discussion can have at most {MaxAttachmentsPerPost} attachments." });

			foreach (var file in files)
			{
				if (file.Length == 0)
					return BadRequest(new { Success = false, Message = $"\"{file.FileName}\" is empty." });
				if (file.Length > MaxAttachmentBytes)
					return BadRequest(new { Success = false, Message = $"\"{file.FileName}\" is larger than 10 MB." });

				var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
				if (!AllowedExtensions.Contains(ext))
					return BadRequest(new { Success = false, Message = "Only JPG, PNG, WEBP, PDF, DOC and DOCX files are allowed." });
			}

			var folder = Path.Combine(GetUploadsRoot(), "Community", id.ToString());
			Directory.CreateDirectory(folder);

			var saved = new List<CommunityPostAttachment>();

			try
			{
				foreach (var file in files)
				{
					var safeName = MakeSafeFileName(file.FileName);
					var storedName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{safeName}";
					var physicalPath = Path.Combine(folder, storedName);

					await using (var stream = new FileStream(physicalPath, FileMode.Create))
					{
						await file.CopyToAsync(stream);
					}

					var attachment = new CommunityPostAttachment
					{
						PostId = id,
						FileName = Path.GetFileName(file.FileName),
						StoredPath = $"Community/{id}/{storedName}",
						ContentType = ResolveContentType(Path.GetExtension(storedName).ToLowerInvariant()),
						Size = file.Length,
						CreatedAt = DateTime.Now
					};

					_db.CommunityPostAttachments.Add(attachment);
					saved.Add(attachment);
				}

				post.UpdatedAt = DateTime.Now;
				await _db.SaveChangesAsync();
			}
			catch (Exception)
			{
				return StatusCode(500, new { Success = false, Message = "The files could not be uploaded. Please try again." });
			}

			return Ok(saved.Select(MapAttachment).ToList());
		}

		// DELETE /api/Community/attachments/{id}
		[HttpDelete("attachments/{id:int}")]
		public async Task<IActionResult> DeleteAttachment(int id)
		{
			var attachment = await _db.CommunityPostAttachments.FirstOrDefaultAsync(a => a.Id == id);
			if (attachment == null)
				return NotFound(new { Success = false, Message = "Attachment not found." });

			var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == attachment.PostId);
			if (post == null || !CanModify(post.AuthorUserId))
				return StatusCode(403, new { Success = false, Message = "You can only remove attachments from your own discussion." });

			var root = GetUploadsRoot();
			var normalized = attachment.StoredPath.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
			var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

			if (fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
			{
				try { System.IO.File.Delete(fullPath); } catch (Exception) { /* best effort */ }
			}

			_db.CommunityPostAttachments.Remove(attachment);
			post.UpdatedAt = DateTime.Now;
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Attachment removed." });
		}

		// GET /api/Community/file/{*path}   (also accepts ?access_token=, see Program.cs)
		[HttpGet("file/{*path}")]
		public IActionResult ViewFile(string path)
		{
			if (string.IsNullOrWhiteSpace(path) ||
				path.Contains("..", StringComparison.Ordinal) ||
				path.IndexOf(':') >= 0)
			{
				return NotFound(new { Success = false, Message = "File not found." });
			}

			var root = GetUploadsRoot();
			var normalized = path.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
			var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

			if (!fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
				return NotFound(new { Success = false, Message = "File not found." });

			return PhysicalFile(fullPath, ResolveContentType(Path.GetExtension(fullPath).ToLowerInvariant()));
		}

		// ---------------------------------------------------------------- products and lookups

		// GET /api/Community/products
		[HttpGet("products")]
		public async Task<IActionResult> GetProducts()
		{
			var userId = CurrentUserId();

			var counts = await _db.CommunityPosts.AsNoTracking()
				.Where(p => !p.IsDeleted && p.Product != null)
				.GroupBy(p => p.Product!)
				.Select(g => new { Name = g.Key, Count = g.Count() })
				.ToListAsync();

			var memberCounts = await _db.CommunityProductMembers.AsNoTracking()
				.GroupBy(m => m.ProductName)
				.Select(g => new { Name = g.Key, Count = g.Count() })
				.ToListAsync();

			var mine = await _db.CommunityProductMembers.AsNoTracking()
				.Where(m => m.UserId == userId)
				.Select(m => m.ProductName)
				.ToListAsync();

			var items = DefaultProducts.Select(name => new CommunityProductDto
			{
				Name = name,
				DiscussionCount = counts.FirstOrDefault(c => c.Name == name)?.Count ?? 0,
				MemberCount = memberCounts.FirstOrDefault(c => c.Name == name)?.Count ?? 0,
				Joined = mine.Contains(name, StringComparer.OrdinalIgnoreCase)
			}).ToList();

			return Ok(items);
		}

		// POST /api/Community/products/{name}/join   (toggle)
		[HttpPost("products/{name}/join")]
		public async Task<IActionResult> ToggleProductMembership(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return BadRequest(new { Success = false, Message = "Product is required." });

			name = name.Trim();
			var userId = CurrentUserId();

			var existing = await _db.CommunityProductMembers
				.FirstOrDefaultAsync(m => m.UserId == userId && m.ProductName == name);

			bool active;
			if (existing != null)
			{
				_db.CommunityProductMembers.Remove(existing);
				active = false;
			}
			else
			{
				_db.CommunityProductMembers.Add(new CommunityProductMember
				{
					UserId = userId,
					ProductName = name,
					CreatedAt = DateTime.Now
				});
				active = true;
			}

			await _db.SaveChangesAsync();

			var count = await _db.CommunityProductMembers.CountAsync(m => m.ProductName == name);
			return Ok(new ReactionResultDto { Active = active, Count = count });
		}

		// GET /api/Community/lookups
		[HttpGet("lookups")]
		public async Task<IActionResult> GetLookups()
		{
			var used = await _db.CommunityPosts.AsNoTracking()
				.Where(p => !p.IsDeleted)
				.Select(p => new { p.Category, p.Product, p.Crop })
				.ToListAsync();

			var dto = new CommunityLookupsDto
			{
				Categories = Merge(DefaultCategories, used.Select(u => u.Category)),
				Products = Merge(DefaultProducts, used.Select(u => u.Product)),
				Crops = Merge(DefaultCrops, used.Select(u => u.Crop))
			};

			return Ok(dto);
		}

		private static List<string> Merge(IEnumerable<string> defaults, IEnumerable<string?> used)
		{
			var list = defaults.ToList();
			foreach (var value in used)
			{
				if (string.IsNullOrWhiteSpace(value)) continue;
				var trimmed = value.Trim();
				if (!list.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
					list.Add(trimmed);
			}
			return list;
		}

		// ---------------------------------------------------------------- mapping

		private async Task<List<DiscussionSummaryDto>> BuildSummariesAsync(
			List<CommunityPost> posts, HashSet<int>? trendingIds = null)
		{
			var items = new List<DiscussionSummaryDto>();
			if (posts.Count == 0)
				return items;

			var ids = posts.Select(p => p.Id).ToList();
			var userId = CurrentUserId();

			var myReactions = await _db.CommunityReactions.AsNoTracking()
				.Where(r => r.UserId == userId &&
							r.TargetType == CommunityReactionTarget.Post &&
							ids.Contains(r.TargetId))
				.Select(r => new { r.TargetId, r.Kind })
				.ToListAsync();

			var replyAuthors = await _db.CommunityPostReplies.AsNoTracking()
				.Where(r => ids.Contains(r.PostId) && !r.IsDeleted)
				.OrderBy(r => r.CreatedAt)
				.Select(r => new { r.PostId, r.AuthorUserId, r.AuthorName, r.AuthorRole, r.AuthorLocation })
				.ToListAsync();

			var attachments = await _db.CommunityPostAttachments.AsNoTracking()
				.Where(a => ids.Contains(a.PostId))
				.OrderBy(a => a.Id)
				.Select(a => new { a.PostId, a.StoredPath, a.ContentType })
				.ToListAsync();

			trendingIds ??= await GetTrendingIdsAsync();

			foreach (var post in posts)
			{
				var reactions = myReactions.Where(r => r.TargetId == post.Id).Select(r => r.Kind).ToList();

				var participants = replyAuthors
					.Where(r => r.PostId == post.Id)
					.GroupBy(r => r.AuthorUserId)
					.Take(3)
					.Select(g => new CommunityMemberDto
					{
						UserId = g.Key,
						Name = g.First().AuthorName,
						Role = g.First().AuthorRole,
						Location = g.First().AuthorLocation ?? ""
					})
					.ToList();

				var cover = attachments
					.FirstOrDefault(a => a.PostId == post.Id && a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

				items.Add(new DiscussionSummaryDto
				{
					Id = post.Id,
					Title = post.Title,
					Excerpt = Excerpt(post.Body),
					Category = post.Category,
					Product = post.Product,
					Crop = post.Crop,
					Tags = SplitCsv(post.Tags),
					Status = post.Status,
					Author = new CommunityMemberDto
					{
						UserId = post.AuthorUserId,
						Name = post.AuthorName,
						Role = post.AuthorRole,
						Location = post.AuthorLocation ?? ""
					},
					CreatedAt = post.CreatedAt,
					LastActivityAt = post.LastActivityAt,
					Views = post.Views,
					ReplyCount = post.ReplyCount,
					LikeCount = post.LikeCount,
					IsLiked = reactions.Contains(CommunityReactionKind.Like),
					IsSaved = reactions.Contains(CommunityReactionKind.Save),
					IsFollowing = reactions.Contains(CommunityReactionKind.Follow),
					IsMine = string.Equals(post.AuthorUserId, userId, StringComparison.OrdinalIgnoreCase),
					IsTrending = trendingIds.Contains(post.Id),
					Participants = participants,
					CoverAttachmentPath = cover?.StoredPath
				});
			}

			return items;
		}

		private async Task<DiscussionDetailDto> BuildDetailAsync(CommunityPost post)
		{
			var summary = (await BuildSummariesAsync(new List<CommunityPost> { post })).First();
			var userId = CurrentUserId();

			var replies = await _db.CommunityPostReplies.AsNoTracking()
				.Where(r => r.PostId == post.Id && !r.IsDeleted)
				.OrderBy(r => r.CreatedAt)
				.ThenBy(r => r.Id)
				.ToListAsync();

			var replyIds = replies.Select(r => r.Id).ToList();

			var likedReplyIds = await _db.CommunityReactions.AsNoTracking()
				.Where(r => r.UserId == userId &&
							r.TargetType == CommunityReactionTarget.Reply &&
							r.Kind == CommunityReactionKind.Like &&
							replyIds.Contains(r.TargetId))
				.Select(r => r.TargetId)
				.ToListAsync();

			var attachments = await _db.CommunityPostAttachments.AsNoTracking()
				.Where(a => a.PostId == post.Id)
				.OrderBy(a => a.Id)
				.ToListAsync();

			var detail = new DiscussionDetailDto
			{
				Id = summary.Id,
				Title = summary.Title,
				Excerpt = summary.Excerpt,
				Category = summary.Category,
				Product = summary.Product,
				Crop = summary.Crop,
				Tags = summary.Tags,
				Status = summary.Status,
				Author = summary.Author,
				CreatedAt = summary.CreatedAt,
				LastActivityAt = summary.LastActivityAt,
				Views = summary.Views,
				ReplyCount = summary.ReplyCount,
				LikeCount = summary.LikeCount,
				IsFollowing = summary.IsFollowing,
				IsSaved = summary.IsSaved,
				IsLiked = summary.IsLiked,
				IsMine = summary.IsMine,
				IsTrending = summary.IsTrending,
				Participants = summary.Participants,
				CoverAttachmentPath = summary.CoverAttachmentPath,
				Body = post.Body,
				Attachments = attachments.Select(MapAttachment).ToList(),
				Replies = replies
					.Select(r => MapReply(
						r,
						replies.Count(child => child.ParentReplyId == r.Id),
						likedReplyIds.Contains(r.Id)))
					.ToList()
			};

			return detail;
		}

		private static ReplyDto MapReply(CommunityPostReply reply, int childCount, bool isLiked) => new()
		{
			Id = reply.Id,
			DiscussionId = reply.PostId,
			ParentReplyId = reply.ParentReplyId,
			Author = new CommunityMemberDto
			{
				UserId = reply.AuthorUserId,
				Name = reply.AuthorName,
				Role = reply.AuthorRole,
				Location = reply.AuthorLocation ?? ""
			},
			MentionName = reply.MentionName,
			Body = reply.Body,
			CreatedAt = reply.CreatedAt,
			LikeCount = reply.LikeCount,
			IsLiked = isLiked,
			ReplyCount = childCount
		};

		private static AttachmentDto MapAttachment(CommunityPostAttachment a) => new()
		{
			Id = a.Id,
			FileName = a.FileName,
			Path = a.StoredPath,
			ContentType = a.ContentType,
			Size = a.Size,
			IsImage = a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
		};

		// ---------------------------------------------------------------- trending

		// Trending = LikeCount + ReplyCount*2 + Views/10 within the last 30 days, top 20%.
		// Computed in memory: the community is small enough that this is one cheap query.
		private async Task<HashSet<int>> GetTrendingIdsAsync()
		{
			var since = DateTime.Now.AddDays(-TrendingWindowDays);

			var recent = await _db.CommunityPosts.AsNoTracking()
				.Where(p => !p.IsDeleted && p.CreatedAt >= since)
				.Select(p => new { p.Id, p.LikeCount, p.ReplyCount, p.Views })
				.ToListAsync();

			if (recent.Count == 0)
				return new HashSet<int>();

			var take = (int)Math.Ceiling(recent.Count * 0.2);
			if (take < 1) take = 1;

			return recent
				.Select(p => new { p.Id, Score = p.LikeCount + p.ReplyCount * 2 + p.Views / 10 })
				.Where(p => p.Score > 0)
				.OrderByDescending(p => p.Score)
				.Take(take)
				.Select(p => p.Id)
				.ToHashSet();
		}

		// ---------------------------------------------------------------- author identity

		private sealed record AuthorInfo(string UserId, string Name, string Role, string? Location);

		private async Task<AuthorInfo> ResolveAuthorAsync()
		{
			var userId = CurrentUserId();
			var userName = User.Identity?.Name ?? "";

			var profile = await _db.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => new { u.Name, u.UserName })
				.FirstOrDefaultAsync();

			var name = profile?.Name;
			if (string.IsNullOrWhiteSpace(name)) name = profile?.UserName;
			if (string.IsNullOrWhiteSpace(name)) name = userName;

			return new AuthorInfo(userId, name ?? "", DisplayRole(), await ResolveLocationAsync());
		}

		// "Headquarter, State" from the location claims the JWT carries; empty when the
		// user has no headquarters/state mapping (dealers, farmers, corporate accounts).
		private async Task<string?> ResolveLocationAsync()
		{
			var hqId = ClaimInt("spic:hq_id");
			var stateId = ClaimInt("spic:state_id");

			string? hqName = null;
			string? stateName = null;

			if (hqId > 0)
			{
				hqName = await _db.Headquarters.AsNoTracking()
					.Where(h => h.Id == hqId)
					.Select(h => h.HeadquarterName)
					.FirstOrDefaultAsync();
			}

			if (stateId > 0)
			{
				stateName = await _db.States.AsNoTracking()
					.Where(s => s.Id == stateId)
					.Select(s => s.StateName)
					.FirstOrDefaultAsync();
			}

			var parts = new[] { hqName, stateName }
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.ToList();

			return parts.Count == 0 ? null : string.Join(", ", parts);
		}

		private int ClaimInt(string type)
		{
			var raw = User.FindFirst(type)?.Value;
			return int.TryParse(raw, out var value) ? value : 0;
		}

		private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

		private AppRole? CurrentRole()
		{
			var raw = User.FindFirst(ClaimTypes.Role)?.Value;
			return Enum.TryParse<AppRole>(raw, true, out var role) ? role : null;
		}

		// Dealer -> "Dealer", Farmer -> "Farmer", every SPIC staff role -> "SPIC Expert".
		private string DisplayRole() => CurrentRole() switch
		{
			AppRole.Dealer => "Dealer",
			AppRole.Farmer => "Farmer",
			null => "Farmer",
			_ => "SPIC Expert"
		};

		private bool IsAdmin()
		{
			var role = CurrentRole();
			return role == AppRole.Admin || role == AppRole.CorporateAdmin;
		}

		private bool CanModify(string authorUserId) =>
			IsAdmin() || string.Equals(authorUserId, CurrentUserId(), StringComparison.OrdinalIgnoreCase);

		// ---------------------------------------------------------------- small helpers

		private string GetUploadsRoot() => Path.Combine(_env.ContentRootPath, "Uploads");

		private static string? Clean(string? value) =>
			string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		private static string Excerpt(string body)
		{
			if (string.IsNullOrWhiteSpace(body)) return "";
			var text = body.Trim();
			return text.Length <= 200 ? text : text.Substring(0, 200);
		}

		private static string? JoinTags(List<string>? tags)
		{
			if (tags == null) return null;
			var clean = tags.Where(t => !string.IsNullOrWhiteSpace(t))
				.Select(t => t.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			return clean.Count == 0 ? null : string.Join(",", clean);
		}

		private static List<string> SplitCsv(string? csv) =>
			string.IsNullOrWhiteSpace(csv)
				? new List<string>()
				: csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

		private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
		{
			"the", "for", "and", "with", "can", "how", "what", "why", "are", "you", "your", "from", "this", "that",
			"which", "when", "where", "who", "will", "should", "could", "does", "did", "have", "has", "not", "but",
			"any", "all", "our", "its", "into", "than", "then", "there", "here", "about", "after", "before", "also",
			"best", "please", "suggest", "question", "help", "need", "use", "using", "used", "get", "some", "more"
		};

		// Shared words of 3+ letters, punctuation stripped, lower-cased, stop-words removed.
		private static HashSet<string> Tokenize(string? text)
		{
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(text)) return set;

			var word = new StringBuilder();
			foreach (var ch in text)
			{
				if (char.IsLetterOrDigit(ch))
				{
					word.Append(char.ToLowerInvariant(ch));
				}
				else
				{
					if (word.Length >= 3) set.Add(word.ToString());
					word.Clear();
				}
			}
			if (word.Length >= 3) set.Add(word.ToString());

			set.RemoveWhere(StopWords.Contains);
			return set;
		}

		// tags=a,b matches a discussion tagged with ANY of them. Built as an expression tree
		// so the OR chain stays translatable to SQL.
		private static Expression<Func<CommunityPost, bool>> BuildTagPredicate(List<string> tags)
		{
			var parameter = Expression.Parameter(typeof(CommunityPost), "p");
			var tagsProperty = Expression.Property(parameter, nameof(CommunityPost.Tags));
			var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
			var contains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

			Expression? body = null;
			foreach (var tag in tags)
			{
				var test = Expression.Call(
					Expression.Call(tagsProperty, toLower),
					contains,
					Expression.Constant(tag));

				body = body == null ? test : Expression.OrElse(body, test);
			}

			var notNull = Expression.NotEqual(tagsProperty, Expression.Constant(null, typeof(string)));
			return Expression.Lambda<Func<CommunityPost, bool>>(Expression.AndAlso(notNull, body!), parameter);
		}

		private static string MakeSafeFileName(string fileName)
		{
			var name = Path.GetFileName(fileName);
			var invalid = Path.GetInvalidFileNameChars();
			var sb = new StringBuilder();

			foreach (var ch in name)
				sb.Append(invalid.Contains(ch) || ch == ' ' ? '_' : ch);

			var safe = sb.ToString();
			return safe.Length > 80 ? safe[^80..] : safe;
		}

		private static string ResolveContentType(string extension) => extension switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".webp" => "image/webp",
			".pdf" => "application/pdf",
			".doc" => "application/msword",
			".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
			_ => "application/octet-stream"
		};
	}
}
