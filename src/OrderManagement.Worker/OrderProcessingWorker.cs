namespace OrderManagement.Worker;

public sealed class OrderProcessingWorker(ILogger<OrderProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consumption is introduced with the event contract in the next increment.
        logger.LogInformation("Order worker started and is awaiting RabbitMQ consumer configuration.");
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
