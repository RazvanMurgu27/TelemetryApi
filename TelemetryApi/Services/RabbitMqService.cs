using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TelemetryApi.Models;

namespace TelemetryApi.Services;

/// <summary>
/// Serviciu de tip "Producer" (Message Publisher) pentru RabbitMQ.
/// Decupleaza ingestia datelor de telemetrie de procesarea lor, scriind pachetele intr-o coada (Queue)
/// de unde vor fi preluate ulterior de un "Consumer" (Worker asincron).
/// </summary>
public class RabbitMqService
{
    private readonly string _hostname;
    private readonly string _queueName = "telemetry_queue";

    /// <summary>
    /// Constructor cu injectare de dependente (DI).
    /// Preia setarile de conexiune din configuratiile aplicatiei, evitand hardcodarea.
    /// </summary>
    public RabbitMqService(IConfiguration configuration)
    {
        // Cauta host-ul in appsettings.json, iar daca nu il gaseste, foloseste "localhost" ca fallback
        _hostname = configuration["RabbitMQ:HostName"] ?? "localhost";
    }

    /// <summary>
    /// Publica asincron un pachet de date de telemetrie in message broker.
    /// </summary>
    /// <param name="data">Obiectul de telemetrie brut primit de la hardware.</param>
    public async Task SendMessageAsync(TelemetryData data)
    {
        // 1. Configurarea si deschiderea conexiunii TCP catre broker-ul RabbitMQ (port standard: 5672)
        var factory = new ConnectionFactory() { HostName = _hostname };

        // Folosim 'using' pentru a ne asigura ca eliberam resursele de retea la finalul metodei (Dispose)
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // 2. Declararea cozii (Queue) pentru a garanta existenta acesteia inainte de publicare.
        // Parametri: Nu este durabila pe disc, nu este exclusiva acestei conexiuni si nu se sterge automat.
        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        // 3. Serializarea payload-ului din obiect C# in format JSON, urmat de encodarea in UTF-8
        var message = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(message);

        // 4. Publicarea mesajului in coada specificata (Fire-and-Forget)
        await channel.BasicPublishAsync(
            exchange: string.Empty, // Folosim exchange-ul implicit (Direct routing dupa numele cozii)
            routingKey: _queueName,
            body: body
        );
    }
}