using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OrderManagement.Api.Models;
using OrderManagement.Api.Realtime;
using OrderManagement.Application.Orders;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderService orderService, OrderStatusEventHub statusEventHub) : ControllerBase
{
    private static readonly JsonSerializerOptions SseSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await orderService.CreateAsync(
            new CreateOrderCommand(request.Cliente, request.Produto, request.Valor), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    [ProducesResponseType<PagedOrdersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedOrdersResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] string sortBy = "data_criacao",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 5 or > 100)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(page)] = ["Page must be greater than zero."],
                [nameof(pageSize)] = ["PageSize must be between 5 and 100."]
            }));

        if (!TryParseSort(sortBy, sortDirection, out var sortField, out var direction))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(sortBy)] = ["SortBy must be cliente, produto, valor, status or data_criacao."],
                [nameof(sortDirection)] = ["SortDirection must be asc or desc."]
            }));

        return Ok(await orderService.GetPageAsync(new OrderPageQuery(page, pageSize, sortField, direction), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType<IReadOnlyList<OrderStatusHistoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusHistoryResponse>>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await orderService.GetStatusHistoryAsync(id, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }

    [HttpGet("events")]
    [Produces("text/event-stream")]
    public async Task StreamStatusEvents(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");
        Response.ContentType = "text/event-stream";

        using var subscription = statusEventHub.Subscribe();
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var statusChanged in subscription.Reader.ReadAllAsync(cancellationToken))
        {
            await Response.WriteAsync("event: order.status.changed\n", cancellationToken);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(statusChanged, SseSerializerOptions)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static bool TryParseSort(string sortBy, string sortDirection, out OrderSortField sortField, out SortDirection direction)
    {
        sortField = sortBy switch
        {
            "cliente" => OrderSortField.Customer,
            "produto" => OrderSortField.Product,
            "valor" => OrderSortField.Amount,
            "status" => OrderSortField.Status,
            "data_criacao" => OrderSortField.CreatedAt,
            _ => default
        };

        direction = sortDirection switch
        {
            "asc" => SortDirection.Asc,
            "desc" => SortDirection.Desc,
            _ => default
        };

        return new[] { "cliente", "produto", "valor", "status", "data_criacao" }.Contains(sortBy)
            && (sortDirection is "asc" or "desc");
    }
}
