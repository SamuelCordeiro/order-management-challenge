using RabbitMQ.Client;

namespace OrderManagement.Infrastructure.Messaging;

public static class RabbitMqConnectionFactory
{
    public static ConnectionFactory Create(RabbitMqOptions options, string clientName) => new()
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.Username,
        Password = options.Password,
        ClientProvidedName = clientName
    };
}
