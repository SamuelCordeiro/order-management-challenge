namespace OrderManagement.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderStatusHistory> _statusHistory = [];

    private Order() { }

    public Order(Guid id, string customer, string product, decimal amount, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(customer)) throw new ArgumentException("Customer is required.", nameof(customer));
        if (string.IsNullOrWhiteSpace(product)) throw new ArgumentException("Product is required.", nameof(product));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        Id = id;
        Customer = customer.Trim();
        Product = product.Trim();
        Amount = amount;
        Status = OrderStatus.Pendente;
        CreatedAt = createdAt;
        AddStatusHistory(createdAt, "api", messageId: null);
    }

    public Guid Id { get; private set; }
    public string Customer { get; private set; } = null!;
    public string Product { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public void StartProcessing(DateTimeOffset occurredAt, Guid messageId)
    {
        if (Status != OrderStatus.Pendente)
            throw new InvalidOperationException("Only pending orders can start processing.");

        Status = OrderStatus.Processando;
        AddStatusHistory(occurredAt, "worker", messageId);
    }

    public void Complete(DateTimeOffset occurredAt, Guid messageId)
    {
        if (Status != OrderStatus.Processando)
            throw new InvalidOperationException("Only processing orders can be completed.");

        Status = OrderStatus.Finalizado;
        AddStatusHistory(occurredAt, "worker", messageId);
    }

    private void AddStatusHistory(DateTimeOffset occurredAt, string source, Guid? messageId) =>
        _statusHistory.Add(new OrderStatusHistory(Id, Status, occurredAt, source, messageId));
}
