using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Messaging;
using OrderManagement.Contracts.Messaging;
using RabbitMQ.Client;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class RabbitMqOrderCreatedPublisher(IOptions<RabbitMqOptions> options) : IOrderCreatedPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        var factory = RabbitMqConnectionFactory.Create(_options, "order-management-api");

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken: cancellationToken);

        await RabbitMqTopology.DeclareAsync(channel, _options, cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId.ToString(),
            Type = message.EventType
        };

        // Awaiting the publish call uses RabbitMQ publisher confirms before returning success.
        await channel.BasicPublishAsync(
            _options.Exchange,
            _options.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken);
    }
}
