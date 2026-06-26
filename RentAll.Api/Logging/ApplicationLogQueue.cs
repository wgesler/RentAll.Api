using System.Threading.Channels;
using Microsoft.Extensions.Options;
using RentAll.Domain.Models;

namespace RentAll.Api.Logging;

public class ApplicationLogQueue : IApplicationLogQueue
{
    private readonly Channel<ApplicationLog> _channel;

    public ApplicationLogQueue(IOptions<ApplicationLoggingSettings> settings)
    {
        var capacity = Math.Max(100, settings.Value.QueueCapacity);
        _channel = Channel.CreateBounded<ApplicationLog>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryEnqueue(ApplicationLog log)
    {
        return _channel.Writer.TryWrite(log);
    }

    public IAsyncEnumerable<ApplicationLog> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
