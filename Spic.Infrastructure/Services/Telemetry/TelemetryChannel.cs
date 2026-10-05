using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Bounded in-memory queue between the capture points and <see cref="TelemetryWriter"/>. Rows are
/// telemetry entities (AppRequestLog / AppPageView / AppErrorLog). When full the OLDEST row is
/// dropped and counted (<see cref="DroppedSinceStart"/>, shown on the Metrics summary), so a slow
/// database never slows a request.
/// </summary>
public sealed class TelemetryChannel
{
    private readonly Channel<object> _channel;
    private readonly IOptionsMonitor<TelemetryOptions> _options;
    private long _dropped;
    private long _written;

    public TelemetryChannel(IOptionsMonitor<TelemetryOptions> options)
    {
        _options = options;
        var capacity = Math.Max(100, options.CurrentValue.ChannelCapacity);
        _channel = Channel.CreateBounded<object>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            },
            _ => Interlocked.Increment(ref _dropped));
    }

    public bool Enabled => _options.CurrentValue.Enabled;

    public ChannelReader<object> Reader => _channel.Reader;

    /// <summary>Rows dropped because the queue was full or a batch failed to save.</summary>
    public long DroppedSinceStart => Interlocked.Read(ref _dropped);

    /// <summary>Rows saved by the writer since start; lets the live "today" cache notice new data.</summary>
    public long WrittenSinceStart => Interlocked.Read(ref _written);

    /// <summary>Queues a row; never throws.</summary>
    public void Enqueue(object row)
    {
        try
        {
            if (!Enabled) return;
            _channel.Writer.TryWrite(row);
        }
        catch
        {
            // telemetry never fails a caller
        }
    }

    internal void CountDropped(int rows) => Interlocked.Add(ref _dropped, rows);

    internal void CountWritten(int rows) => Interlocked.Add(ref _written, rows);
}
