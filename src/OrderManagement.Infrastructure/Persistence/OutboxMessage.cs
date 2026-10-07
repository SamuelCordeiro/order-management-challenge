using System.Text.Json;
using OrderManagement.Contracts.Messaging;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private OutboxMessage() { }

    private OutboxMessage(Guid id, string type, DateTimeOffset occurredAt, object payload)
    {
        Id = id;
        Type = type;
        Payload = JsonSerializer.Serialize(payload, SerializerOptions);
        OccurredAt = occurredAt;
        NextAttemptAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage From(OrderCreated message) =>
        new(message.MessageId, message.EventType, message.OccurredAt, message);

    public static OutboxMessage From(OrderStatusChanged message) =>
        new(message.MessageId, message.EventType, message.OccurredAt, message);

    public TMessage Deserialize<TMessage>() =>
        JsonSerializer.Deserialize<TMessage>(Payload, SerializerOptions)
        ?? throw new InvalidOperationException($"Outbox message {Id} has an invalid payload.");

    public void MarkPublished(DateTimeOffset publishedAt)
    {
        PublishedAt = publishedAt;
        LastError = null;
    }

    public void RegisterFailure(DateTimeOffset occurredAt, Exception exception)
    {
        AttemptCount++;
        NextAttemptAt = occurredAt.AddSeconds(Math.Min(60, Math.Pow(2, AttemptCount)));
        var error = $"{exception.GetType().Name}: {exception.Message}";
        LastError = error[..Math.Min(error.Length, 2000)];
    }
}
