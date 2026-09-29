using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Drains <see cref="TelemetryChannel"/> into the database: batches of up to 200 rows or whatever
/// arrived within 2 s, one DbContext scope per batch. A failed batch is logged at Warning (never
/// Error: that would feed the logger provider) and dropped. On shutdown it flushes what is queued
/// for at most 3 s and never holds the host.
/// </summary>
public sealed class TelemetryWriter : BackgroundService
{
    private const int BatchSize = 200;
    private static readonly TimeSpan BatchWindow = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ShutdownFlush = TimeSpan.FromSeconds(3);

    private readonly TelemetryChannel _channel;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<TelemetryWriter> _logger;

    public TelemetryWriter(TelemetryChannel channel, IServiceScopeFactory scopes, ILogger<TelemetryWriter> logger)
    {
        _channel = channel;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _channel.Reader;
        var batch = new List<object>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await reader.WaitToReadAsync(stoppingToken)) break;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var deadline = DateTime.UtcNow + BatchWindow;
            while (batch.Count < BatchSize)
            {
                while (batch.Count < BatchSize && reader.TryRead(out var row)) batch.Add(row);
                if (batch.Count >= BatchSize) break;

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                using var window = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                window.CancelAfter(remaining);
                try
                {
                    if (!await reader.WaitToReadAsync(window.Token)) break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            if (batch.Count > 0)
            {
                await FlushAsync(batch, stoppingToken.IsCancellationRequested ? CancellationToken.None : stoppingToken);
                batch.Clear();
            }
        }

        // Shutdown: one last bounded flush of whatever is still queued.
        try
        {
            using var cts = new CancellationTokenSource(ShutdownFlush);
            while (reader.TryRead(out var row))
            {
                batch.Add(row);
                if (batch.Count >= BatchSize)
                {
                    await FlushAsync(batch, cts.Token);
                    batch.Clear();
                    if (cts.IsCancellationRequested) break;
                }
            }
            if (batch.Count > 0 && !cts.IsCancellationRequested) await FlushAsync(batch, cts.Token);
        }
        catch
        {
            // shutting down
        }
    }

    private async Task FlushAsync(List<object> batch, CancellationToken ct)
    {
        using var _ = TelemetryScope.Suppress();
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.AddRange(batch);
            await db.SaveChangesAsync(ct);
            _channel.CountWritten(batch.Count);
        }
        catch (Exception ex)
        {
            _channel.CountDropped(batch.Count);
            try
            {
                _logger.LogWarning("Telemetry: dropped a batch of {Count} rows: {Message}", batch.Count, ex.GetBaseException().Message);
            }
            catch
            {
                // logging must not break the writer
            }
        }
    }
}
