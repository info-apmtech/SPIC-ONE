using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using SpicAPI.Controllers;

namespace SpicAPI.Services
{
    /// <summary>What one account deletion removed or changed (counts only; logged without any personal data).</summary>
    public sealed record FarmerDeletionResult(
        int ChatMessages, int Conversations, int AiUsagesAnonymised, int LeafChecks, int FertilizerRuns, int YieldForecasts,
        int OpenRequestsDeleted, int RequestsKept, int CommunityPosts, int CommunityReplies,
        int Reactions, int SasRecordsUnlinked, int RefreshTokens);

    /// <summary>
    /// "Delete my account" (final fix wave, review I4; DELETE api/Farmer/profile). One transaction for the database:
    /// the farmer profile, chats and messages, leaf checks, fertilizer runs, yield forecasts, open call-back requests (handled ones stay for the officer's history: the staff view
    /// reads the name and phone from the profile, which is gone, so it shows "Farmer" with no number), Community
    /// discussions and replies taken down (soft delete, author name and place cleared), reactions and product
    /// memberships, the SAS farmer record UNLINKED (lab data stays with the lab), refresh tokens deleted, the request
    /// log rows anonymised, and finally the Identity user deleted so the number can sign up fresh later. Files (leaf
    /// photos, the Community photo) are deleted only after the commit.
    /// <para>
    /// Residual fix R2: two kinds of rows are deliberately NOT deleted, because the global limits count them and a
    /// sign-up / use / delete loop must not reset those limits. The AI usage rows (the 2000-a-day SPIC AI ceiling) are
    /// kept with the farmer id replaced by <see cref="FarmerPrivacyRules.DeletedUserTombstone"/> (kind and time only).
    /// The OTP challenges (the per-number / per-IP / global SMS caps) are left to the existing one-day purge in
    /// FarmerAuthController (the privacy notice says sign-in codes are removed after one day).
    /// </para>
    /// </summary>
    public sealed class FarmerAccountDeletion
    {
        private readonly AppDbContext _db;
        private readonly UserManager<UserInfo> _users;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<FarmerAccountDeletion> _log;

        public FarmerAccountDeletion(AppDbContext db, UserManager<UserInfo> users, IWebHostEnvironment env, IConfiguration config,
            ILogger<FarmerAccountDeletion> log)
        {
            _db = db;
            _users = users;
            _env = env;
            _config = config;
            _log = log;
        }

        /// <summary>Deletes the Farmer account <paramref name="userId"/>; null when there is no such Farmer account.</summary>
        public async Task<FarmerDeletionResult?> DeleteAsync(string userId, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is null || user.Role != AppRole.Farmer) return null;

            var profile = await _db.FarmerProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

            var npgsql = _db.Database.IsNpgsql();
            await using var tx = npgsql ? await _db.Database.BeginTransactionAsync(ct) : null;

            var messages = await _db.FarmerChatMessages.Where(m => m.FarmerUserId == userId).ToListAsync(ct);
            _db.FarmerChatMessages.RemoveRange(messages);
            var conversations = await _db.FarmerChatConversations.Where(c => c.FarmerUserId == userId).ToListAsync(ct);
            _db.FarmerChatConversations.RemoveRange(conversations);
            // Kept for the global AI ceiling, without who it was (R2).
            var usages = await _db.FarmerAiUsages.Where(u => u.FarmerUserId == userId).ToListAsync(ct);
            foreach (var u in usages) u.FarmerUserId = FarmerPrivacyRules.DeletedUserTombstone;
            var leaves = await _db.LeafDiagnoses.Where(d => d.FarmerUserId == userId).ToListAsync(ct);
            _db.LeafDiagnoses.RemoveRange(leaves);
            var runs = await _db.FertilizerRecommendationRuns.Where(r => r.FarmerUserId == userId).ToListAsync(ct);
            _db.FertilizerRecommendationRuns.RemoveRange(runs);
            var forecasts = await _db.YieldPredictions.Where(p => p.FarmerUserId == userId).ToListAsync(ct);
            _db.YieldPredictions.RemoveRange(forecasts);

            // Call-backs: unanswered ones go; handled ones stay for the officer (closed, no name or phone any more).
            var requests = await _db.FarmerContactRequests.Where(r => r.FarmerUserId == userId).ToListAsync(ct);
            var open = requests.Where(r => r.Status == FarmerContactRequestStatus.Open).ToList();
            _db.FarmerContactRequests.RemoveRange(open);
            var now = DateTime.UtcNow;
            foreach (var r in requests.Except(open))
            {
                r.Status = FarmerContactRequestStatus.Closed;
                r.PreferredTime = null;
                r.UpdatedAtUtc = now;
            }

            // Community: take down the farmer's discussions and replies (soft delete, as the Community does).
            var posts = await _db.CommunityPosts.Where(p => p.AuthorUserId == userId).ToListAsync(ct);
            foreach (var p in posts)
            {
                p.IsDeleted = true;
                p.AuthorName = FarmerPrivacyRules.DeletedAuthorName;
                p.AuthorLocation = null;
                p.UpdatedAt = DateTime.Now;
            }
            var replies = await _db.CommunityPostReplies.Where(r => r.AuthorUserId == userId).ToListAsync(ct);
            var liveReplyPostIds = replies.Where(r => !r.IsDeleted).Select(r => r.PostId).ToList();
            foreach (var r in replies)
            {
                r.IsDeleted = true;
                r.AuthorName = FarmerPrivacyRules.DeletedAuthorName;
                r.AuthorLocation = null;
                r.MentionName = null;
            }
            var reactions = await _db.CommunityReactions.Where(x => x.UserId == userId).ToListAsync(ct);
            var likedPostIds = reactions.Where(x => x.Kind == CommunityReactionKind.Like && x.TargetType == CommunityReactionTarget.Post).Select(x => x.TargetId).ToList();
            var likedReplyIds = reactions.Where(x => x.Kind == CommunityReactionKind.Like && x.TargetType == CommunityReactionTarget.Reply).Select(x => x.TargetId).ToList();
            _db.CommunityReactions.RemoveRange(reactions);
            var touchedPostIds = liveReplyPostIds.Concat(likedPostIds).Distinct().ToList();
            var touchedPosts = touchedPostIds.Count == 0 ? new List<CommunityPost>()
                : await _db.CommunityPosts.Where(p => touchedPostIds.Contains(p.Id)).ToListAsync(ct);
            foreach (var p in touchedPosts)
            {
                p.ReplyCount = Math.Max(0, p.ReplyCount - liveReplyPostIds.Count(id => id == p.Id));
                p.LikeCount = Math.Max(0, p.LikeCount - likedPostIds.Count(id => id == p.Id));
            }
            var likedReplies = likedReplyIds.Count == 0 ? new List<CommunityPostReply>()
                : await _db.CommunityPostReplies.Where(r => likedReplyIds.Contains(r.Id)).ToListAsync(ct);
            foreach (var r in likedReplies)
                r.LikeCount = Math.Max(0, r.LikeCount - likedReplyIds.Count(id => id == r.Id));
            _db.CommunityProductMembers.RemoveRange(await _db.CommunityProductMembers.Where(m => m.UserId == userId).ToListAsync(ct));

            // SAS: unlink, never delete (the lab's samples and reports are the lab's records).
            var sas = await _db.SasFarmers.Where(f => f.UserId == userId).ToListAsync(ct);
            foreach (var f in sas)
            {
                f.UserId = null;
                f.UpdatedBy = "farmer account deleted";
                f.UpdatedAt = DateTime.Now;
            }

            var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(ct);
            _db.RefreshTokens.RemoveRange(tokens);
            if (profile is not null) _db.FarmerProfiles.Remove(profile);
            await _db.SaveChangesAsync(ct);

            // The request / error logs carry the user id and the "f<phone>" user name: keep the rows, drop who it was.
            if (npgsql)
            {
                await _db.AppRequestLogs.Where(l => l.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.UserId, (string?)null).SetProperty(l => l.UserName, (string?)null), ct);
                await _db.AppErrorLogs.Where(l => l.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.UserId, (string?)null).SetProperty(l => l.UserName, (string?)null), ct);
            }

            var deleted = await _users.DeleteAsync(user);
            if (!deleted.Succeeded)
                throw new InvalidOperationException("The account could not be deleted: " + string.Join("; ", deleted.Errors.Select(e => e.Code)));

            if (tx is not null) await tx.CommitAsync(ct);

            // Files after the commit: a failed delete leaves an orphan file, never a row pointing at nothing.
            DeleteFolder(Path.Combine(FarmerLeafCheckController.SafePrivateRoot(_env.ContentRootPath, _env.WebRootPath,
                _config["FarmerPortal:LeafPhotoRoot"], out _), FarmerLeafCheckController.UploadFolder, FarmerLeafCheckController.SafeSegment(userId)));
            DeleteAvatar(userId);

            var result = new FarmerDeletionResult(messages.Count, conversations.Count, usages.Count, leaves.Count, runs.Count,
                forecasts.Count, open.Count, requests.Count - open.Count, posts.Count, replies.Count, reactions.Count,
                sas.Count, tokens.Count);
            _log.LogInformation("Farmer account deleted: {Counts}", result);
            return result;
        }

        private void DeleteFolder(string folder)
        {
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex)
            {
                _log.LogWarning("Could not delete a deleted farmer's leaf photos ({Error})", ex.GetType().Name);
            }
        }

        private void DeleteAvatar(string userId)
        {
            try
            {
                var folder = Path.Combine(_env.ContentRootPath, "Uploads", "Community", "avatars");
                if (!Directory.Exists(folder) || string.IsNullOrWhiteSpace(userId)) return;
                foreach (var file in Directory.EnumerateFiles(folder))
                    if (string.Equals(Path.GetFileNameWithoutExtension(file), userId, StringComparison.OrdinalIgnoreCase))
                        File.Delete(file);
            }
            catch (Exception ex)
            {
                _log.LogWarning("Could not delete a deleted farmer's Community photo ({Error})", ex.GetType().Name);
            }
        }
    }
}
