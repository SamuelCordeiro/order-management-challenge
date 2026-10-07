using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class EfOrderRepository(OrdersDbContext dbContext) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        dbContext.Orders.AddAsync(order, cancellationToken).AsTask();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Order?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include(x => x.StatusHistory)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<OrderPage> GetPageAsync(OrderPageQuery query, CancellationToken cancellationToken)
    {
        var orders = dbContext.Orders.AsNoTracking();
        var totalCount = await orders.CountAsync(cancellationToken);
        var summary = await orders
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Pending = group.Count(order => order.Status == OrderStatus.Pendente),
                Processing = group.Count(order => order.Status == OrderStatus.Processando),
                Finalized = group.Count(order => order.Status == OrderStatus.Finalizado)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var sortedOrders = (query.SortField, query.SortDirection) switch
        {
            (OrderSortField.Customer, SortDirection.Asc) => orders.OrderBy(order => order.Customer).ThenBy(order => order.Id),
            (OrderSortField.Customer, SortDirection.Desc) => orders.OrderByDescending(order => order.Customer).ThenByDescending(order => order.Id),
            (OrderSortField.Product, SortDirection.Asc) => orders.OrderBy(order => order.Product).ThenBy(order => order.Id),
            (OrderSortField.Product, SortDirection.Desc) => orders.OrderByDescending(order => order.Product).ThenByDescending(order => order.Id),
            (OrderSortField.Amount, SortDirection.Asc) => orders.OrderBy(order => order.Amount).ThenBy(order => order.Id),
            (OrderSortField.Amount, SortDirection.Desc) => orders.OrderByDescending(order => order.Amount).ThenByDescending(order => order.Id),
            (OrderSortField.Status, SortDirection.Asc) => orders.OrderBy(order => order.Status).ThenBy(order => order.Id),
            (OrderSortField.Status, SortDirection.Desc) => orders.OrderByDescending(order => order.Status).ThenByDescending(order => order.Id),
            (OrderSortField.CreatedAt, SortDirection.Asc) => orders.OrderBy(order => order.CreatedAt).ThenBy(order => order.Id),
            _ => orders.OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id)
        };

        var items = await sortedOrders
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new OrderPage(items, totalCount, summary?.Pending ?? 0, summary?.Processing ?? 0, summary?.Finalized ?? 0);
    }

    public async Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken) =>
        await dbContext.OrderStatusHistories
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
