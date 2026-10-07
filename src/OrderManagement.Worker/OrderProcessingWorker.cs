using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Messaging;
using OrderManagement.Contracts.Messaging;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderManagement.Worker;

public sealed class OrderProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    TimeProvider timeProvider,
    ILogger<OrderProcessingWorker> logger) : BackgroundService
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
                logger.LogError(
                    exception,
                    "Order worker could not connect to RabbitMQ. Retrying in {DelaySeconds} seconds.",
                    ReconnectDelay.TotalSeconds);

                try
                {
                    await Task.Delay(ReconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqConnectionFactory.Create(_options, "order-management-worker");
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);

        await RabbitMqTopology.DeclareAsync(channel, _options, stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) => await ProcessDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(_options.Queue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Order worker is consuming queue {Queue}.", _options.Queue);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task ProcessDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        OrderCreated? message;

        try
        {
            message = JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span, SerializerOptions);
            if (message is null || message.EventType != OrderCreated.EventTypeName || message.CorrelationId != message.OrderId)
                throw new InvalidDataException("The message does not match the OrderCreated contract.");
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            logger.LogError(exception, "Invalid message was sent to the order consumer and will be dead-lettered.");
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, stoppingToken);
            return;
        }

        try
        {
            await ProcessOrderAsync(message, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            await RetryOrDeadLetterAsync(channel, delivery, message, exception, stoppingToken);
        }
    }

    private async Task ProcessOrderAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var statusOutbox = scope.ServiceProvider.GetRequiredService<IOrderEventOutbox>();
        var order = await repository.GetByIdWithHistoryAsync(message.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {message.OrderId} was not found.");

        if (order.Status == OrderStatus.Finalizado)
            return;

        if (order.Status == OrderStatus.Pendente)
        {
            var occurredAt = timeProvider.GetUtcNow();
            order.StartProcessing(occurredAt, message.MessageId);
            await EnqueueStatusChangedAsync(statusOutbox, order, message, occurredAt, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        if (order.Status == OrderStatus.Processando)
        {
            var occurredAt = timeProvider.GetUtcNow();
            order.Complete(occurredAt, message.MessageId);
            await EnqueueStatusChangedAsync(statusOutbox, order, message, occurredAt, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
    }

    private static Task EnqueueStatusChangedAsync(
        IOrderEventOutbox outbox,
        Order order,
        OrderCreated sourceMessage,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        outbox.EnqueueAsync(
            new OrderStatusChanged(
                MessageId: Guid.NewGuid(),
                OrderId: order.Id,
                CorrelationId: sourceMessage.CorrelationId,
                EventType: OrderStatusChanged.EventTypeName,
                Status: order.Status.ToString().ToLowerInvariant(),
                OccurredAt: occurredAt),
            cancellationToken);

    private async Task RetryOrDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        OrderCreated message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var attempts = GetRetryCount(delivery.BasicProperties) + 1;
        if (attempts >= _options.MaxDeliveryAttempts)
        {
            logger.LogError(exception, "Order event {MessageId} exhausted {Attempts} delivery attempts and will be dead-lettered.", message.MessageId, attempts);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken);
            return;
        }

        var headers = delivery.BasicProperties.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(delivery.BasicProperties.Headers);
        headers["x-retry-count"] = attempts;

        var retryProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId.ToString(),
            Type = message.EventType,
            Headers = headers
        };

        try
        {
            logger.LogWarning(exception, "Order event {MessageId} failed; scheduling retry {Attempt} of {MaxAttempts}.", message.MessageId, attempts, _options.MaxDeliveryAttempts - 1);
            await channel.BasicPublishAsync(
                _options.RetryExchange,
                _options.RetryRoutingKey,
                mandatory: true,
                basicProperties: retryProperties,
                body: delivery.Body,
                cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception retryException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(retryException, "Retry scheduling failed for order event {MessageId}; returning the original delivery to RabbitMQ.", message.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties) =>
        properties.Headers?.TryGetValue("x-retry-count", out var value) == true && value is not null
            ? Convert.ToInt32(value)
            : 0;
}
