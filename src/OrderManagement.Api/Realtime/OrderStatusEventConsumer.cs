using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderManagement.Contracts.Messaging;
using OrderManagement.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderManagement.Api.Realtime;

public sealed class OrderStatusEventConsumer(
    OrderStatusEventHub eventHub,
    IOptions<RabbitMqOptions> options,
    ILogger<OrderStatusEventConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Status event consumer disconnected from RabbitMQ. Retrying in {DelaySeconds} seconds.", ReconnectDelay.TotalSeconds);
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqConnectionFactory.Create(_options, "order-management-api-realtime");
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareAsync(channel, _options, stoppingToken);
        await channel.BasicQosAsync(0, 20, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) => await ProcessDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(_options.StatusQueue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Status event consumer is consuming queue {Queue}.", _options.StatusQueue);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task ProcessDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<OrderStatusChanged>(delivery.Body.Span, SerializerOptions);
            if (message is null ||
                message.EventType != OrderStatusChanged.EventTypeName ||
                message.CorrelationId != message.OrderId ||
                !IsKnownStatus(message.Status))
            {
                throw new InvalidDataException("The message does not match the OrderStatusChanged contract.");
            }

            eventHub.Publish(message);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            logger.LogWarning(exception, "Invalid status event was discarded.");
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, stoppingToken);
        }
    }

    private static bool IsKnownStatus(string status) =>
        status is "pendente" or "processando" or "finalizado";
}
