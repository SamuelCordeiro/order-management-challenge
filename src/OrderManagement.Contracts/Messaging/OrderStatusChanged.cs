namespace OrderManagement.Contracts.Messaging;

public sealed record OrderStatusChanged(
    Guid MessageId,
    Guid OrderId,
    Guid CorrelationId,
    string EventType,
    string Status,
    DateTimeOffset OccurredAt)
{
    public const string EventTypeName = "OrderStatusChanged";
}
