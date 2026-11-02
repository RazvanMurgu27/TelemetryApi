using RabbitMQ.Client;

namespace TelemetryApi.Services;

/// <summary>
/// Creează configurația de conectare utilizată de serviciile RabbitMQ.
/// </summary>
public static class RabbitMqConnectionFactory
{
    /// <summary>
    /// Construiește o fabrică RabbitMQ folosind fie URI-ul complet din configurație,
    /// fie setările individuale pentru host, port, virtual host și autentificare.
    /// </summary>
    /// <param name="configuration">Configurația aplicației care conține setările RabbitMQ.</param>
    /// <returns>O fabrică configurată pentru crearea conexiunilor RabbitMQ.</returns>
    /// <exception cref="InvalidOperationException">
    /// Este aruncată atunci când URI-ul RabbitMQ configurat nu are un format valid.
    /// </exception>
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
