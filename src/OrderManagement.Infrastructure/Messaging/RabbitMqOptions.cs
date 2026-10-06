namespace OrderManagement.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Queue { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string ErrorExchange { get; set; } = string.Empty;
    public string ErrorQueue { get; set; } = string.Empty;
    public string ErrorRoutingKey { get; set; } = string.Empty;
    public string RetryExchange { get; set; } = string.Empty;
    public string RetryQueue { get; set; } = string.Empty;
    public string RetryRoutingKey { get; set; } = string.Empty;
    public string StatusExchange { get; set; } = string.Empty;
    public string StatusQueue { get; set; } = string.Empty;
    public string StatusRoutingKey { get; set; } = string.Empty;
    public int RetryDelayMilliseconds { get; set; } = 5000;
    public int MaxDeliveryAttempts { get; set; } = 3;
}
