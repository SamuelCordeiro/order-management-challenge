namespace OrderManagement.Domain.Orders;

public sealed class OrderStatusHistory
{
    private OrderStatusHistory() { }

    internal OrderStatusHistory(
        Guid orderId,
        OrderStatus status,
        DateTimeOffset occurredAt,
        string source,
        Guid? messageId)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required.", nameof(source));

        OrderId = orderId;
        Status = status;
        OccurredAt = occurredAt;
        Source = source;
        MessageId = messageId;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Source { get; private set; } = null!;
    public Guid? MessageId { get; private set; }
}
