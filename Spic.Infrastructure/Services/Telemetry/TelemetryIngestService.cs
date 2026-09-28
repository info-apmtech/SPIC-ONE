using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// POST api/Telemetry/batch (plan section 4). Who sent it decides what is kept:
///   bearer user     : page views + errors, attributed to the token's claims (body user fields ignored)
///   X-Telemetry-Key : server source (web host); body user fields and Source are trusted
///   anonymous       : errors only, at most 5 per batch, 60 batches / minute / IP
/// Every string is capped to the entity length, a batch to 200 page views + 50 errors, and an At more
/// than 1 day ahead or 7 days back is replaced by the server time. Bad rows are dropped, never thrown.
/// </summary>
public sealed class TelemetryIngestService
{
    public const int MaxPageViews = 200;
    public const int MaxErrors = 50;
    public const int MaxAnonymousErrors = 5;
    public const int AnonymousBatchesPerMinute = 60;

    public enum CallerKind { User, Trusted, Anonymous }

    public sealed record Result(int Accepted, int Dropped, bool RateLimited);

    private readonly TelemetryChannel _channel;
    private readonly IOptionsMonitor<TelemetryOptions> _options;
    private readonly ConcurrentDictionary<string, (long Minute, int Count)> _anonymous = new();

    public TelemetryIngestService(TelemetryChannel channel, IOptionsMonitor<TelemetryOptions> options)
    {
        _channel = channel;
        _options = options;
    }

    public CallerKind Classify(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true) return CallerKind.User;
        var key = _options.CurrentValue.IngestKey;
        var sent = context.Request.Headers[TelemetryHttp.KeyHeader].ToString();
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrEmpty(sent) && KeyEquals(sent, key)) return CallerKind.Trusted;
        return CallerKind.Anonymous;
    }

    /// <summary>Constant-time compare (both sides hashed first so the length does not leak).</summary>
    private static bool KeyEquals(string sent, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(sent.Trim())),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected.Trim())));

    public Result Ingest(HttpContext context, ClientTelemetryBatchDto? batch)
    {
        var pageViews = batch?.PageViews ?? new List<ClientPageViewDto>();
        var errors = batch?.Errors ?? new List<ClientErrorReportDto>();
        var offered = pageViews.Count + errors.Count;
        if (batch is null || offered == 0) return new Result(0, 0, false);

        var caller = Classify(context);
        if (caller == CallerKind.Anonymous && !AllowAnonymous(TelemetryHttp.IpOf(context) ?? "unknown"))
        {
            return new Result(0, offered, true);
        }

        var now = DateTime.UtcNow;
        var app = Enum.IsDefined(batch.App) ? batch.App : TelemetryApp.Unknown;
        if (app == TelemetryApp.Unknown) app = TelemetryHttp.AppOf(context);
        var version = TelemetryHttp.Trim(batch.AppVersion, 40) is { Length: > 0 } v ? v : TelemetryHttp.VersionOf(context);
        var (claimId, claimName, claimRole) = TelemetryHttp.UserOf(context.User);

        var accepted = 0;

        if (caller != CallerKind.Anonymous)
        {
            foreach (var pv in pageViews.Take(MaxPageViews))
            {
                try
                {
                    if (pv is null || string.IsNullOrWhiteSpace(pv.Route)) continue;
                    // A trusted source has no user of its own; its page views stay unattributed.
                    _channel.Enqueue(new AppPageView
                    {
                        At = SafeAt(pv.At, now),
                        Route = TelemetryHttp.TrimRequired(TelemetryHttp.NormalizePath(pv.Route, keepLeadingSlash: true), 300, "/"),
                        UserId = claimId,
                        UserName = claimName,
                        Role = claimRole,
                        App = app,
                        AppVersion = version,
                        SessionId = TelemetryHttp.Trim(pv.SessionId, 128)
                    });
                    accepted++;
                }
                catch
                {
                    // bad row: dropped
                }
            }
        }

        var errorCap = caller == CallerKind.Anonymous ? MaxAnonymousErrors : MaxErrors;
        foreach (var er in errors.Take(errorCap))
        {
            try
            {
                if (er is null || (string.IsNullOrWhiteSpace(er.Message) && string.IsNullOrWhiteSpace(er.ExceptionType))) continue;

                var source = caller switch
                {
                    CallerKind.Trusted => Enum.IsDefined(er.Source) && er.Source != TelemetrySource.Api ? er.Source : TelemetrySource.WebHost,
                    _ => er.Source is TelemetrySource.Client or TelemetrySource.Js or TelemetrySource.WebHost ? er.Source : TelemetrySource.Client
                };
                if (caller == CallerKind.Anonymous && source == TelemetrySource.WebHost) source = TelemetrySource.Client;

                var type = TelemetryHttp.TrimRequired(er.ExceptionType, 200, "Error");
                var message = TelemetryHttp.TrimRequired(er.Message, 2000, "(no message)");
                var stack = TelemetryHttp.Trim(er.StackTrace, 8000);
                var route = string.IsNullOrWhiteSpace(er.Route)
                    ? null
                    : TelemetryHttp.Trim(TelemetryHttp.NormalizePath(er.Route, keepLeadingSlash: er.Route.TrimStart().StartsWith('/')), 300);

                string? userId = null, userName = null, role = null;
                if (caller == CallerKind.User) (userId, userName, role) = (claimId, claimName, claimRole);
                else if (caller == CallerKind.Trusted)
                    (userId, userName, role) = (TelemetryHttp.Trim(er.UserId, 450), TelemetryHttp.Trim(er.UserName, 256), TelemetryHttp.Trim(er.Role, 64));

                _channel.Enqueue(new AppErrorLog
                {
                    At = SafeAt(er.At, now),
                    Source = source,
                    App = app,
                    AppVersion = version,
                    Fingerprint = TelemetryFingerprint.Compute(source, type, message, TelemetryFingerprint.TopFrame(stack) ?? route),
                    ExceptionType = type,
                    Message = message,
                    StackTrace = stack,
                    Route = route,
                    Category = TelemetryHttp.Trim(er.Category, 300),
                    UserId = userId,
                    UserName = userName,
                    Role = role,
                    SessionId = TelemetryHttp.Trim(er.SessionId, 128)
                });
                accepted++;
            }
            catch
            {
                // bad row: dropped
            }
        }

        return new Result(accepted, offered - accepted, false);
    }

    /// <summary>UTC; values more than 1 day ahead or 7 days back (or unset) become the server time.</summary>
    private static DateTime SafeAt(DateTime at, DateTime now)
    {
        if (at == default) return now;
        var utc = at.Kind switch
        {
            DateTimeKind.Local => at.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(at, DateTimeKind.Utc),
            _ => at
        };
        return utc > now.AddDays(1) || utc < now.AddDays(-7) ? now : utc;
    }

    private bool AllowAnonymous(string ip)
    {
        var minute = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
        var allowed = false;
        _anonymous.AddOrUpdate(ip,
            _ =>
            {
                allowed = true;
                return (minute, 1);
            },
            (_, current) =>
            {
                if (current.Minute != minute)
                {
                    allowed = true;
                    return (minute, 1);
                }
                allowed = current.Count < AnonymousBatchesPerMinute;
                return (minute, current.Count + 1);
            });

        if (_anonymous.Count > 10000)
        {
            foreach (var entry in _anonymous)
            {
                if (entry.Value.Minute != minute) _anonymous.TryRemove(entry.Key, out _);
            }
        }
        return allowed;
    }
}
