using System.Security.Cryptography;
using System.Text;

namespace SPIC.Core.FarmerPortal;

public enum OtpSendOutcome
{
    Allowed,
    /// <summary>The last code for this number went out less than <see cref="FarmerOtpRules.ResendCooldown"/> ago.</summary>
    Cooldown,
    /// <summary><see cref="FarmerOtpRules.MaxSendsPerPhonePerHour"/> codes already went to this number in the rolling hour.</summary>
    PhoneHourLimit,
    /// <summary><see cref="FarmerOtpRules.MaxSendsPerIpPerHour"/> codes already went out from this client IP in the rolling hour.</summary>
    IpHourLimit
}

public sealed record OtpSendDecision(OtpSendOutcome Outcome, int RetryAfterSeconds)
{
    public bool Allowed => Outcome == OtpSendOutcome.Allowed;
}

public enum OtpVerifyOutcome
{
    Ok,
    /// <summary>No live challenge (never requested, expired or already used).</summary>
    NoChallenge,
    TooManyAttempts,
    WrongCode
}

/// <summary>
/// Every OTP rule of the farmer portal as pure functions, so the harness can pin them without a
/// database: send limits, code generation, hashing, constant-time compare and the attempt/expiry
/// rules. FarmerAuthController only loads the rows and applies the decisions.
/// </summary>
public static class FarmerOtpRules
{
    public const int CodeLength = 6;
    public const int MaxAttempts = 5;
    public const int MaxSendsPerPhonePerHour = 5;
    public const int MaxSendsPerIpPerHour = 20;

    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan LimitWindow = TimeSpan.FromHours(1);
    /// <summary>Challenge rows older than this are deleted (they no longer count for any limit).</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(1);

    /// <summary>
    /// May another code be sent? <paramref name="phoneSendsUtc"/> are the creation times of this
    /// number's challenges (any order, older ones are ignored); <paramref name="ipSendsInWindow"/>
    /// counts the challenges created from the client IP in the last hour. The cooldown is checked
    /// first so the farmer sees the shortest wait.
    /// </summary>
    public static OtpSendDecision DecideSend(DateTime nowUtc, IEnumerable<DateTime> phoneSendsUtc, int ipSendsInWindow)
    {
        var windowStart = nowUtc - LimitWindow;
        var recent = phoneSendsUtc.Where(t => t > windowStart).OrderBy(t => t).ToList();

        if (recent.Count > 0)
        {
            var sinceLast = nowUtc - recent[^1];
            if (sinceLast < ResendCooldown)
                return new(OtpSendOutcome.Cooldown, SecondsUntil(ResendCooldown - sinceLast));
        }

        if (recent.Count >= MaxSendsPerPhonePerHour)
        {
            // The oldest send in the window leaves it first; then one more send is allowed.
            var freeAt = recent[recent.Count - MaxSendsPerPhonePerHour] + LimitWindow;
            return new(OtpSendOutcome.PhoneHourLimit, SecondsUntil(freeAt - nowUtc));
        }

        if (ipSendsInWindow >= MaxSendsPerIpPerHour)
            return new(OtpSendOutcome.IpHourLimit, (int)LimitWindow.TotalSeconds);

        return new(OtpSendOutcome.Allowed, 0);
    }

    /// <summary>
    /// The decision for an entered code against the live challenge (null when there is none).
    /// Expired and consumed challenges count as none.
    /// </summary>
    public static OtpVerifyOutcome DecideVerify(DateTime nowUtc, DateTime? expiresAtUtc, DateTime? consumedAtUtc, int attempts,
        string? storedHash, string enteredHash)
    {
        if (expiresAtUtc is null || storedHash is null || consumedAtUtc is not null || expiresAtUtc <= nowUtc)
            return OtpVerifyOutcome.NoChallenge;
        if (attempts >= MaxAttempts)
            return OtpVerifyOutcome.TooManyAttempts;
        return HashesMatch(storedHash, enteredHash) ? OtpVerifyOutcome.Ok : OtpVerifyOutcome.WrongCode;
    }

    /// <summary>Tries left after one more wrong code (0 = the challenge is now dead).</summary>
    public static int TriesLeftAfterWrong(int attemptsBefore) => Math.Max(0, MaxAttempts - (attemptsBefore + 1));

    /// <summary>A uniformly random 6-digit code, leading zeros kept.</summary>
    public static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);

    public static bool IsWellFormedCode(string? code) =>
        code is { Length: CodeLength } && code.All(char.IsAsciiDigit);

    /// <summary>Hex SHA-256 of <c>code:phone:pepper</c>. The phone ties a code to its number.</summary>
    public static string HashCode(string code, string phone, string pepper) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{code}:{phone}:{pepper}")));

    /// <summary>Constant-time comparison of two hex hashes.</summary>
    public static bool HashesMatch(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a ?? string.Empty), Encoding.ASCII.GetBytes(b ?? string.Empty));

    /// <summary>
    /// The client IP used for the per-IP limit: the LAST X-Forwarded-For entry (the one the
    /// nearest proxy appended - a client can prepend anything it likes, but cannot change what
    /// the ingress adds), else the socket address.
    /// </summary>
    public static string? ClientIp(string? forwardedFor, string? remoteAddress)
    {
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var last = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
            if (!string.IsNullOrEmpty(last)) return last.Length > 64 ? last[..64] : last;
        }
        return string.IsNullOrWhiteSpace(remoteAddress) ? null : remoteAddress;
    }

    private static int SecondsUntil(TimeSpan span) => Math.Max(1, (int)Math.Ceiling(span.TotalSeconds));
}
