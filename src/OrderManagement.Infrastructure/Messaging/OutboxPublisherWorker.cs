using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Messaging;
using OrderManagement.Contracts.Messaging;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The outbox publisher iteration failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task PublishNextAsync(CancellationToken cancellationToken)
    {
        Guid? messageId = null;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var orderCreatedPublisher = scope.ServiceProvider.GetRequiredService<IOrderCreatedPublisher>();
            var statusChangedPublisher = scope.ServiceProvider.GetRequiredService<IOrderStatusChangedPublisher>();
            var now = timeProvider.GetUtcNow();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var message = await dbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT * FROM outbox_messages
                    WHERE published_at IS NULL AND next_attempt_at <= {now}
                    ORDER BY occurred_at
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (message is null)
                return;

            messageId = message.Id;
            await PublishAsync(message, orderCreatedPublisher, statusChangedPublisher, cancellationToken);
            message.MarkPublished(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Outbox message {MessageId} was published.", message.Id);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && messageId is not null)
        {
            await RegisterFailureAsync(messageId.Value, exception, cancellationToken);
        }
    }

    private static Task PublishAsync(
        OutboxMessage message,
        IOrderCreatedPublisher orderCreatedPublisher,
        IOrderStatusChangedPublisher statusChangedPublisher,
        CancellationToken cancellationToken) =>
        message.Type switch
        {
            OrderCreated.EventTypeName => orderCreatedPublisher.PublishAsync(message.Deserialize<OrderCreated>(), cancellationToken),
            OrderStatusChanged.EventTypeName => statusChangedPublisher.PublishAsync(message.Deserialize<OrderStatusChanged>(), cancellationToken),
            _ => throw new InvalidOperationException($"Outbox message {message.Id} has an unsupported type '{message.Type}'.")
        };

    private async Task RegisterFailureAsync(Guid messageId, Exception exception, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var message = await dbContext.OutboxMessages.SingleOrDefaultAsync(item => item.Id == messageId, cancellationToken);
        if (message is null || message.PublishedAt is not null)
            return;

        message.RegisterFailure(timeProvider.GetUtcNow(), exception);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning(exception, "Outbox message {MessageId} failed and was scheduled for retry.", messageId);
    }
}
