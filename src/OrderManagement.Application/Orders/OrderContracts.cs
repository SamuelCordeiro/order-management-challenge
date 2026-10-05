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
