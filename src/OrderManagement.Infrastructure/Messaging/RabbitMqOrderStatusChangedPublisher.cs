using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Messaging;
using OrderManagement.Contracts.Messaging;
using RabbitMQ.Client;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class RabbitMqOrderStatusChangedPublisher(IOptions<RabbitMqOptions> options) : IOrderStatusChangedPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(OrderStatusChanged message, CancellationToken cancellationToken)
    {
        var factory = RabbitMqConnectionFactory.Create(_options, "order-management-worker");

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken: cancellationToken);

        await RabbitMqTopology.DeclareAsync(channel, _options, cancellationToken);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId.ToString(),
            Type = message.EventType
        };

        await channel.BasicPublishAsync(
            _options.StatusExchange,
            _options.StatusRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions),
            cancellationToken);
    }
}
