using RabbitMQ.Client;

namespace TelemetryApi.Services;

public static class RabbitMqConnectionFactory
{
    public static ConnectionFactory Create(IConfiguration configuration)
    {
        var connectionString = configuration["RabbitMQ:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connectionString) &&
            !connectionString.StartsWith("INSEREAZA_", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("RabbitMQ:ConnectionString nu este un URI valid.");
            }

            return new ConnectionFactory { Uri = uri };
        }

        var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = configuration.GetValue("RabbitMQ:Port", 5672),
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
        };

        var userName = configuration["RabbitMQ:UserName"];
        var password = configuration["RabbitMQ:Password"];

        if (!string.IsNullOrWhiteSpace(userName))
        {
            factory.UserName = userName;
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            factory.Password = password;
        }

        if (configuration.GetValue("RabbitMQ:Ssl", false))
        {
            factory.Ssl.Enabled = true;
        }

        return factory;
    }
}
