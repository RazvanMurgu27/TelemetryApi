using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace BmsStressTest;

class Program
{
    static async Task Main(string[] args)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(queue: "telemetry_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var random = new Random();
        int messageCount = 0;

        Console.WriteLine("🔥 Începem bombardamentul! Apasă CTRL+C pentru a opri.");
        await Task.Delay(2000); 
        while (true)
        {
            string batteryId = $"SIM-{random.Next(1, 51):D3}";
            
            var telemetry = new 
            {
                BatteryId = batteryId,
                Voltage = Math.Round(random.NextDouble() * (60.0 - 40.0) + 40.0, 2),
                Current = Math.Round(random.NextDouble() * 120.0, 2),
                Temperature = Math.Round(random.NextDouble() * 60.0 - 10.0, 1), 
                Soc = random.Next(5, 101),
                HighestCellVoltage = Math.Round(random.NextDouble() * 0.1 + 3.6, 3), 
                LowestCellVoltage = Math.Round(random.NextDouble() * 0.1 + 3.5, 3)   
            };

            string message = JsonSerializer.Serialize(telemetry);
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: "", routingKey: "telemetry_queue", body: body);

            messageCount++;
            Console.WriteLine($"[TRIMIS {messageCount}] -> {batteryId} | {telemetry.Voltage}V | SOC: {telemetry.Soc}%");

            await Task.Delay(10); 
        }
    }
}