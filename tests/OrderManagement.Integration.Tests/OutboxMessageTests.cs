using OrderManagement.Contracts.Messaging;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Integration.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void RegisterFailure_IncrementsAttemptsAndSchedulesExponentialRetry()
    {
        var occurredAt = new DateTimeOffset(2026, 10, 6, 20, 30, 0, TimeSpan.Zero);
        var message = OutboxMessage.From(new OrderCreated(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderCreated.EventTypeName,
            occurredAt));

        message.RegisterFailure(occurredAt, new InvalidOperationException("RabbitMQ unavailable."));

        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(occurredAt.AddSeconds(2), message.NextAttemptAt);
        Assert.Equal("InvalidOperationException: RabbitMQ unavailable.", message.LastError);
        Assert.Null(message.PublishedAt);
    }
}
