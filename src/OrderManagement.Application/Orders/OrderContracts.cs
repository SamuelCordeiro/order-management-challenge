using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public sealed record CreateOrderCommand(string Cliente, string Produto, decimal Valor);

public sealed record OrderResponse(
    Guid Id,
    string Cliente,
    string Produto,
    decimal Valor,
    OrderStatus Status,
    DateTimeOffset DataCriacao);

public sealed record OrderStatusHistoryResponse(
    OrderStatus Status,
    DateTimeOffset OcorridoEm,
    string Origem);

public sealed record PagedOrdersResponse(
    IReadOnlyList<OrderResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    OrderSummaryResponse Summary);

public sealed record OrderSummaryResponse(int Total, int Pendentes, int Processando, int Finalizados);
