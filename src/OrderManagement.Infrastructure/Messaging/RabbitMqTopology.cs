using RabbitMQ.Client;

namespace OrderManagement.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public static async Task DeclareAsync(IChannel channel, RabbitMqOptions options, CancellationToken cancellationToken)
    {
        await DeclareExchangeAsync(channel, options.Exchange, cancellationToken);
        await DeclareExchangeAsync(channel, options.ErrorExchange, cancellationToken);
        await DeclareExchangeAsync(channel, options.RetryExchange, cancellationToken);

        var mainQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = options.ErrorExchange,
            ["x-dead-letter-routing-key"] = options.ErrorRoutingKey
        };

        var retryQueueArguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = options.RetryDelayMilliseconds,
            ["x-dead-letter-exchange"] = options.Exchange,
            ["x-dead-letter-routing-key"] = options.RoutingKey
        };

        await DeclareQueueAsync(channel, options.Queue, mainQueueArguments, cancellationToken);
        await DeclareQueueAsync(channel, options.ErrorQueue, null, cancellationToken);
        await DeclareQueueAsync(channel, options.RetryQueue, retryQueueArguments, cancellationToken);

        await BindQueueAsync(channel, options.Queue, options.Exchange, options.RoutingKey, cancellationToken);
        await BindQueueAsync(channel, options.ErrorQueue, options.ErrorExchange, options.ErrorRoutingKey, cancellationToken);
        await BindQueueAsync(channel, options.RetryQueue, options.RetryExchange, options.RetryRoutingKey, cancellationToken);
    }

    private static Task DeclareExchangeAsync(IChannel channel, string exchange, CancellationToken cancellationToken) =>
        channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken);

    private static Task DeclareQueueAsync(
        IChannel channel,
        string queue,
        IDictionary<string, object?>? arguments,
        CancellationToken cancellationToken) =>
        channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments, passive: false, noWait: false, cancellationToken);

    private static Task BindQueueAsync(
        IChannel channel,
        string queue,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken) =>
        channel.QueueBindAsync(queue, exchange, routingKey, arguments: null, noWait: false, cancellationToken);
}
