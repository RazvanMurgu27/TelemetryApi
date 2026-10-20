using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using RabbitMQ.Client;
using TelemetryApi.Models;
using TelemetryApi.Data;
using TelemetryApi.Controllers;

namespace TelemetryApi.Services;

public class RabbitMqService
{
    private readonly string _hostname = "localhost";
    private readonly string _queueName = "telemetry_queue";

    public async Task SendMessageAsync(TelemetryData data)
    {
        //1.configurare conexiune la RabbitMQ(care ruleaza pe portul 5672)
        var factory = new ConnectionFactory() { HostName = _hostname};

        using var connection  =  await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: false,
            exclusive:false,
            autoDelete: false,
            arguments: null
        );

        var message = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            body: body
        );
    }
}