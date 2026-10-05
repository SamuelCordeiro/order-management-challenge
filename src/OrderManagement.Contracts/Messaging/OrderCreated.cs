namespace OrderManagement.Contracts.Messaging;

public sealed record OrderCreated(
    Guid MessageId,
    Guid OrderId,
    Guid CorrelationId,
    string EventType,
    DateTimeOffset OccurredAt)
{
    public const string EventTypeName = "OrderCreated";
}
