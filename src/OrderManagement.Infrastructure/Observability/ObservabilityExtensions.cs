using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OrderManagement.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddOrderManagementObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        services.AddOpenTelemetry().WithTracing(builder =>
        {
            builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                .AddSource(MessagingActivity.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

            if (configuration.GetValue("Observability:ConsoleExporterEnabled", false))
                builder.AddConsoleExporter();
        });

        return services;
    }
}
