using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Assistant;

namespace SpicAPI.Services
{
    /// <summary>
    /// The daily AI budget shared by every farmer AI feature (farmer portal Task 5 fix round 1; Task 6's leaf check
    /// uses it with <see cref="FarmerAiKind.LeafCheck"/>). Call <see cref="TryConsumeAsync"/> right before an Anthropic
    /// call: anything but <see cref="FarmerAiDecision.Allowed"/> means "give the basic answer, do not call the AI".
    /// </summary>
    public interface IFarmerAiBudget
    {
        Task<FarmerAiDecision> TryConsumeAsync(FarmerAiKind kind, string farmerUserId, CancellationToken ct = default);

        /// <summary>Whether a full AI answer is possible now (key, switch, today's budget); a read without consuming.</summary>
        Task<bool> AvailableAsync(FarmerAiKind kind, CancellationToken ct = default);
    }

    /// <summary>
    /// Config: FarmerPortal:AssistantEnabled (kill switch, default on), FarmerPortal:AssistantGlobalDailyLimit (AI calls
    /// per IST day across all farmers and kinds, default 2000), FarmerPortal:AiKindDailyLimit:{Chat|LeafCheck} (optional
    /// sub-limit). Race-safe: today's <see cref="FarmerAiUsage"/> rows are counted and the new row inserted in one
    /// transaction under a GLOBAL PostgreSQL advisory lock, so parallel requests and both API replicas cannot pass the
    /// limit together. The lock is held only for the count + insert (milliseconds), never during the AI call.
    /// </summary>
    public class FarmerAiBudget : IFarmerAiBudget
    {
        public const string LockKey = "farmer-ai-budget";

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly AssistantOptions _options;
        private readonly ILogger<FarmerAiBudget> _log;

        public FarmerAiBudget(AppDbContext db, IConfiguration config, IOptions<AssistantOptions> options, ILogger<FarmerAiBudget> log)
        {
            _db = db;
            _config = config;
            _options = options.Value;
            _log = log;
        }

        private static int _unreadableLogged;

        /// <summary>The kill switch; an unreadable value counts as OFF and is logged once per replica (#35).</summary>
        private bool Enabled
        {
            get
            {
                var raw = _config["FarmerPortal:AssistantEnabled"];
                if (FarmerAiBudgetRules.IsUnreadable(raw) && Interlocked.Exchange(ref _unreadableLogged, 1) == 0)
                    _log.LogWarning("FarmerPortal:AssistantEnabled has an unreadable value; farmer AI answers are OFF until it is true or false");
                return FarmerAiBudgetRules.Enabled(raw);
            }
        }
        private int GlobalLimit => FarmerAiBudgetRules.GlobalLimit(_config["FarmerPortal:AssistantGlobalDailyLimit"]);
        private int? KindLimit(FarmerAiKind kind) => FarmerAiBudgetRules.KindLimit(_config[$"FarmerPortal:AiKindDailyLimit:{kind}"]);

        private async Task<(int All, int Kind)> UsedTodayAsync(FarmerAiKind kind, DateTime now, CancellationToken ct)
        {
            var start = FarmerAssistantRules.IstDayStartUtc(now);
            var name = kind.ToString();
            var all = await _db.FarmerAiUsages.CountAsync(u => u.CreatedAtUtc >= start, ct);
            var mine = KindLimit(kind) is null ? 0 : await _db.FarmerAiUsages.CountAsync(u => u.CreatedAtUtc >= start && u.Kind == name, ct);
            return (all, mine);
        }

        public async Task<bool> AvailableAsync(FarmerAiKind kind, CancellationToken ct = default)
        {
            if (!_options.HasAnthropicKey || !Enabled) return false;
            var (all, mine) = await UsedTodayAsync(kind, DateTime.UtcNow, ct);
            return FarmerAiBudgetRules.Decide(true, true, all, GlobalLimit, mine, KindLimit(kind)) == FarmerAiDecision.Allowed;
        }

        public async Task<FarmerAiDecision> TryConsumeAsync(FarmerAiKind kind, string farmerUserId, CancellationToken ct = default)
        {
            // No key / switched off: decided without touching the database.
            var quick = FarmerAiBudgetRules.Decide(Enabled, _options.HasAnthropicKey, 0, int.MaxValue, 0, null);
            if (quick != FarmerAiDecision.Allowed) return quick;

            var now = DateTime.UtcNow;
            var npgsql = _db.Database.IsNpgsql();
            await using var tx = npgsql ? await _db.Database.BeginTransactionAsync(ct) : null;
            if (npgsql)
                await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({LockKey}, 0))", ct);

            var (all, mine) = await UsedTodayAsync(kind, now, ct);
            var limit = GlobalLimit;
            var decision = FarmerAiBudgetRules.Decide(true, true, all, limit, mine, KindLimit(kind));
            if (decision != FarmerAiDecision.Allowed)
            {
                // Logged once per IST day per replica (the first refusal), not for every farmer after it.
                if (Interlocked.Exchange(ref _lastLoggedDay, FarmerAssistantRules.IstDayStartUtc(now).Ticks) != FarmerAssistantRules.IstDayStartUtc(now).Ticks)
                    _log.LogWarning("Farmer AI daily budget reached ({Decision}: {Used}/{Limit} today); basic answers until IST midnight.", decision, all, limit);
                return decision;
            }

            _db.FarmerAiUsages.Add(new FarmerAiUsage { Kind = kind.ToString(), FarmerUserId = farmerUserId, CreatedAtUtc = now });
            await _db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
            return FarmerAiDecision.Allowed;
        }

        private static long _lastLoggedDay;
    }
}
