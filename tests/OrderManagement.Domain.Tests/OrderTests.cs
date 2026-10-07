using OrderManagement.Domain.Orders;

namespace OrderManagement.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void NewOrder_StartsAsPending()
    {
        var order = CreateOrder();

        Assert.Equal(OrderStatus.Pendente, order.Status);
    }

    [Fact]
    public void Order_CanFollowTheValidStatusSequence()
    {
        var order = CreateOrder();

        order.StartProcessing(DateTimeOffset.UtcNow, Guid.NewGuid());
        order.Complete(DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Equal(OrderStatus.Finalizado, order.Status);
    }

    [Fact]
    public void Order_CannotBeCompletedWhilePending()
    {
        var order = CreateOrder();

        Assert.Throws<InvalidOperationException>(() => order.Complete(DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void Order_CannotStartProcessingTwice()
    {
        var order = CreateOrder();
        order.StartProcessing(DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => order.StartProcessing(DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void Order_CannotBeCompletedTwice()
    {
        var order = CreateOrder();
        order.StartProcessing(DateTimeOffset.UtcNow, Guid.NewGuid());
        order.Complete(DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => order.Complete(DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void Order_RecordsAnImmutableHistoryForEachValidTransition()
    {
        var createdAt = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        var order = new Order(Guid.NewGuid(), "Cliente", "Produto", 99.90m, createdAt);
        var messageId = Guid.NewGuid();

        order.StartProcessing(createdAt.AddMinutes(1), messageId);
        order.Complete(createdAt.AddMinutes(2), messageId);

        Assert.Collection(order.StatusHistory,
            pending =>
            {
                Assert.Equal(OrderStatus.Pendente, pending.Status);
                Assert.Equal(createdAt, pending.OccurredAt);
                Assert.Equal("api", pending.Source);
                Assert.Null(pending.MessageId);
            },
            processing =>
            {
                Assert.Equal(OrderStatus.Processando, processing.Status);
                Assert.Equal("worker", processing.Source);
                Assert.Equal(messageId, processing.MessageId);
            },
            completed =>
            {
                Assert.Equal(OrderStatus.Finalizado, completed.Status);
                Assert.Equal("worker", completed.Source);
                Assert.Equal(messageId, completed.MessageId);
            });
    }

    private static Order CreateOrder() =>
        new(Guid.NewGuid(), "Cliente", "Produto", 99.90m, DateTimeOffset.UtcNow);
}
