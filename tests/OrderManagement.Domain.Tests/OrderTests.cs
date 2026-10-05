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

        order.StartProcessing();
        order.Complete();

        Assert.Equal(OrderStatus.Finalizado, order.Status);
    }

    [Fact]
    public void Order_CannotBeCompletedWhilePending()
    {
        var order = CreateOrder();

        Assert.Throws<InvalidOperationException>(order.Complete);
    }

    [Fact]
    public void Order_CannotStartProcessingTwice()
    {
        var order = CreateOrder();
        order.StartProcessing();

        Assert.Throws<InvalidOperationException>(order.StartProcessing);
    }

    [Fact]
    public void Order_CannotBeCompletedTwice()
    {
        var order = CreateOrder();
        order.StartProcessing();
        order.Complete();

        Assert.Throws<InvalidOperationException>(order.Complete);
    }

    private static Order CreateOrder() =>
        new(Guid.NewGuid(), "Cliente", "Produto", 99.90m, DateTimeOffset.UtcNow);
}
