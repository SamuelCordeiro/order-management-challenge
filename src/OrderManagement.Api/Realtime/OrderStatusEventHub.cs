using System.Collections.Concurrent;
using System.Threading.Channels;
using OrderManagement.Contracts.Messaging;

namespace OrderManagement.Api.Realtime;

public sealed class OrderStatusEventHub
{
    private readonly ConcurrentDictionary<Guid, Channel<OrderStatusChanged>> _subscribers = new();

    public Subscription Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<OrderStatusChanged>(new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        _subscribers.TryAdd(id, channel);
        return new Subscription(channel.Reader, () => _subscribers.TryRemove(id, out _));
    }

    public void Publish(OrderStatusChanged statusChanged)
    {
        foreach (var subscriber in _subscribers.Values)
            subscriber.Writer.TryWrite(statusChanged);
    }

    public sealed class Subscription(ChannelReader<OrderStatusChanged> reader, Action unsubscribe) : IDisposable
    {
        public ChannelReader<OrderStatusChanged> Reader { get; } = reader;

        public void Dispose() => unsubscribe();
    }
}
