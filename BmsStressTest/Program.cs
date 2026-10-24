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

        await channel.QueueDeclareAsync(
            queue: "telemetry_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        Console.WriteLine("=== SIMULATOR & STRESS TEST BMS LiFePO4 ===");
        Console.Write("Introdu un DeviceId specific (ex: SIM-001) sau apasa ENTER pentru 50 baterii random (SIM-001..SIM-050): ");
        string? customDeviceId = Console.ReadLine()?.Trim();

        Console.Write("Tip sistem [1 = 12V (4S), 2 = 24V (8S), 3 = 48V (16S)] (Default: 1): ");
        string? systemChoice = Console.ReadLine()?.Trim();
        int cellCount = systemChoice switch
        {
            "2" => 8,
            "3" => 16,
            _ => 4
        };

        Console.Write("Interval intre mesaje in milisecunde (ex: 1000 pt 1 secunda, 10 pt Stress Test) (Default: 500): ");
        if (!int.TryParse(Console.ReadLine(), out int delayMs) || delayMs < 1)
        {
            delayMs = 500;
        }

        var random = new Random();
        int messageCount = 0;

        Console.WriteLine($"\n🔥 Începem transmisia ({cellCount} celule, interval {delayMs}ms)! Apasă CTRL+C pentru a opri.\n");
        await Task.Delay(1000);

        while (true)
        {
            string deviceId = string.IsNullOrEmpty(customDeviceId)
                ? $"SIM-{random.Next(1, 51):D3}"
                : customDeviceId;

            // În 85% din cazuri generăm date normale, în 15% din cazuri generăm câte o anomalie pentru testarea alertelor
            bool triggerAnomaly = random.Next(1, 101) <= 15;

            double lowCell = triggerAnomaly && random.Next(2) == 0
                ? Math.Round(2.35 + random.NextDouble() * 0.15, 3)   // Sub-tensiune celulă
                : Math.Round(3.15 + random.NextDouble() * 0.15, 3);  // Normal (3.15V - 3.30V)

            double highCell = triggerAnomaly && random.Next(2) == 1
                ? Math.Round(3.66 + random.NextDouble() * 0.12, 3)   // Supra-tensiune / Delta V mare
                : Math.Round(lowCell + 0.01 + random.NextDouble() * 0.05, 3); // Echilibrat (Delta V <= 0.06V)

            double avgCellVoltage = (lowCell + highCell) / 2.0;
            double packVoltage = Math.Round(avgCellVoltage * cellCount, 2);

            double current = triggerAnomaly && random.Next(4) == 0
                ? Math.Round(230.0 + random.NextDouble() * 40.0, 2)  // Supracurent (> 220A)
                : Math.Round(random.NextDouble() * 80.0, 2);         // Normal (0A - 80A)

            double temperature = triggerAnomaly && random.Next(3) == 0
                ? Math.Round(56.0 + random.NextDouble() * 10.0, 1)   // Supra-temperatură (> 55°C)
                : Math.Round(20.0 + random.NextDouble() * 18.0, 1);  // Normal (20°C - 38°C)

            int soc = random.Next(15, 100);

            var telemetry = new
            {
                DeviceId = deviceId,
                Voltage = packVoltage,
                Current = current,
                Temperature = temperature,
                Soc = soc,
                TimeStamp = DateTime.UtcNow,
                HighestCellVoltage = highCell,
                LowestCellVoltage = lowCell
            };

            string message = JsonSerializer.Serialize(telemetry);
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "telemetry_queue", body: body);

            messageCount++;
            Console.WriteLine($"[TRIMIS #{messageCount}] -> {deviceId} | {telemetry.Voltage}V | {telemetry.Current}A | {telemetry.Temperature}°C | ΔV: {Math.Round(highCell - lowCell, 3)}V | SOC: {telemetry.Soc}%");

            await Task.Delay(delayMs);
        }
    }
}
