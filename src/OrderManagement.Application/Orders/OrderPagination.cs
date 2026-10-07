using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public enum OrderSortField
{
    Customer,
    Product,
    Amount,
    Status,
    CreatedAt
}

public enum SortDirection
{
    Asc,
    Desc
}

public sealed record OrderPageQuery(int Page, int PageSize, OrderSortField SortField, SortDirection SortDirection);

public sealed record OrderPage(
    IReadOnlyList<Order> Items,
    int TotalCount,
    int PendingCount,
    int ProcessingCount,
    int FinalizedCount);
