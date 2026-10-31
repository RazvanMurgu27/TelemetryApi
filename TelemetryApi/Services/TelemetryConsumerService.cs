using System.Text;
using System.Text.Json;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TelemetryApi.Models;
using TelemetryApi.Data;

namespace TelemetryApi.Services;

/// <summary>
/// Serviciu de fundal (Background Worker) care actioneaza ca un "Consumer" pentru RabbitMQ.
/// Proceseaza asincron pachetele de telemetrie, evalueaza regulile de siguranta (BMS Logic) 
/// si gestioneaza alertele si persistenta datelor (SQLite) in afara thread-ului principal HTTP.
/// </summary>
public class TelemetryConsumerService : BackgroundService
{
    private readonly string _queueName = "telemetry_queue";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailService _emailService;

    private IConnection _connection = null!;
    private IChannel _channel = null!;

    // Dictionar in-memory (Thread-Safe) pentru monitorizarea statusului Heartbeat al bateriilor (Watchdog)
    private readonly Dictionary<string, (DateTime LastSeen, int LastSoc)> _lastSeenBatteries = new();
    private readonly System.Timers.Timer _connectionTimer;

    public TelemetryConsumerService(IServiceScopeFactory scopeFactory, EmailService emailService)
    {
        _scopeFactory = scopeFactory;
        _emailService = emailService;

        // Configureaza un timer de tip Watchdog (60 sec) pentru a detecta pierderea conexiunii cu hardware-ul
        _connectionTimer = new System.Timers.Timer(60000);
        _connectionTimer.Elapsed += CheckOfflineBatteries;
    }

    /// <summary>
    /// Metoda principala de executie a serviciului de fundal. Ruleaza continuu cat timp aplicatia este pornita.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: _queueName, 
            durable: false, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        // Eveniment declansat la primirea unui mesaj nou in coada RabbitMQ
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var telemetry = JsonSerializer.Deserialize<TelemetryData>(message);

            if (telemetry != null)
            {
                Console.WriteLine($"[CONSUMER] Extras din coada -> Baterie: {telemetry.DeviceId} | {telemetry.Soc}% | {telemetry.Voltage}V | {telemetry.Current}A | {telemetry.Temperature}°C");
                
                // Actualizam statusul Heartbeat pentru watchdog
                _lastSeenBatteries[telemetry.DeviceId] = (DateTime.UtcNow, telemetry.Soc);

                // Crearea unui Scope izolat pentru accesul concurent si sigur la DbContext
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var bmsModule = await dbContext.BmsModules
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.DeviceId == telemetry.DeviceId, stoppingToken);

                // Mecanism de Fallback pentru bateriile noi (Default 4S LiFePO4)
                int cellCount = bmsModule?.CellCount ?? 4;

                // Cautam setarile personalizate din UserSettings pentru proprietarul bateriei
                var userSettings = await dbContext.BatterySettings
                    .FirstOrDefaultAsync(s => s.DeviceId == telemetry.DeviceId, stoppingToken);

                // Extragem pragurile (folosim valorile implicite daca userul nu si-a setat parametrii)
                double cellMaxVoltage = userSettings?.MaxVoltageThreshold ?? 3.65;
                double cellMinVoltage = userSettings?.MinVoltageThreshold ?? 2.50;
                double cellWarningVoltage = userSettings?.WarningVoltageThreshold ?? 2.80;
                double maxTemperature = userSettings?.MaxTemperatureThreshold ?? 55.0;
                double minTemperature = userSettings?.MinTemperatureThreshold ?? 0.0;
                double maxCurrent = userSettings?.MaxCurrentThreshold ?? 220.0;
                double maxDeltaV = userSettings?.MaxDeltaVThreshold ?? 0.20;

                // Evaluam regulile de siguranta
                var (dbError, emailAlert) = EvaluateSafetyRules(telemetry, cellCount, cellMaxVoltage, cellMinVoltage, cellWarningVoltage, maxTemperature, minTemperature, maxCurrent, maxDeltaV);
                
                await SaveToDatabaseAsync(dbContext, telemetry, dbError);

                if (emailAlert != null)
                {
                    if (bmsModule != null && bmsModule.User != null)
                    {
                        string ownerEmail = !string.IsNullOrWhiteSpace(userSettings?.AlertEmail) 
                            ? userSettings.AlertEmail 
                            : bmsModule.User.Email;

                        // Trimite alerta catre proprietar
                        await _emailService.SendAlertEmailAsync(ownerEmail, telemetry.DeviceId, emailAlert, telemetry.Soc);

                        Console.WriteLine($"[Email Trimis] Alerta trimisa catre: {ownerEmail}");
                    }
                    else
                    {
                        Console.WriteLine($"[AVERTIZARE] Modulul {telemetry.DeviceId} a generat o alerta dar nu are niciun User asignat in baza de date!");
                    }
                }
            }
        };

        // autoAck: true - mesajul este sters din coada imediat ce a fost preluat
        await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer);
        
        _connectionTimer.Start();
    }

    /// <summary>
    /// Metoda de tip Watchdog (Dead Man's Switch). Verifica periodic daca o baterie a incetat brusc transmisia.
    /// </summary>
    private async void CheckOfflineBatteries(object? sender, ElapsedEventArgs e)
    {
        var now = DateTime.UtcNow;
        var batteriesToRemove = new List<string>();

        foreach (var battery in _lastSeenBatteries)
        {
            string deviceId = battery.Key;
            var (lastseentime, lastSoc) = battery.Value;

            if ((now - lastseentime).TotalMinutes > 5)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"  [ALERTĂ CRITICA TIMEOUT] Bateria {deviceId} este OFFLINE! Niciun semnal de peste 5 minute. Ultimul SOC: {lastSoc}%");
                Console.ResetColor();

                batteriesToRemove.Add(deviceId);
                
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var bmsModule = await dbContext.BmsModules
                        .Include(b => b.User)
                        .FirstOrDefaultAsync(b => b.DeviceId == deviceId);

                    if (bmsModule != null && bmsModule.User != null)
                    {
                        var userSettings = await dbContext.BatterySettings
                             .FirstOrDefaultAsync(s => s.DeviceId == deviceId);

                        string ownerEmail = !string.IsNullOrWhiteSpace(userSettings?.AlertEmail)
                            ? userSettings.AlertEmail
                            : bmsModule.User.Email;

                        string messageOffline = "Dispozitivul este OFFLINE! Niciun semnal de peste 5 minute!";

                        await _emailService.SendAlertEmailAsync(ownerEmail, deviceId, messageOffline, lastSoc);
                        Console.WriteLine($"[Email Trimis] Alerta OFFLINE cu SOC {lastSoc}% a fost trimisa catre: {ownerEmail}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Eroare Email Offline] Nu s-a putut trimite notificarea: {ex.Message}");
                }
            }
        }

        // Sterge bateriile offline din memoria RAM pentru a preveni flood-ul de mesaje
        foreach (var key in batteriesToRemove)
        {
            _lastSeenBatteries.Remove(key);
        }
    }

    /// <summary>
    /// Persista inregistrarea de telemetrie impreuna cu statusul calculat in baza de date SQLite.
    /// </summary>
    private async Task SaveToDatabaseAsync(AppDbContext dbContext, TelemetryData telemetry, string? errorMessage)
    {
        var record = new TelemetryRecord
        {
            DeviceId = telemetry.DeviceId,
            Voltage = telemetry.Voltage,
            Current = telemetry.Current,
            Power = Math.Round(telemetry.Voltage * telemetry.Current, 2),
            Temperature = telemetry.Temperature,
            Soc = telemetry.Soc,
            HighestCellVoltage = telemetry.HighestCellVoltage,
            LowestCellVoltage = telemetry.LowestCellVoltage,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            ErrorTimeStamp = errorMessage != null ? DateTime.UtcNow : null
        };

        dbContext.TelemetryRecords.Add(record);
        await dbContext.SaveChangesAsync();

        var status = errorMessage != null ? $"[EROARE: {errorMessage}]" : "[OK]";
        Console.WriteLine($"[DATABASE] Salvat in baza de date (Id: {record.Id}) -> Status: {status} | Putere: {record.Power}W");
    }

    private (string? DbError, string? EmailAlert) EvaluateSafetyRules(TelemetryData telemetry, int cellCount, double cellMaxVoltage, double cellMinVoltage, double cellWarningVoltage, double maxTemperature, double minTemperature, double maxCurrent, double maxDeltaV)
    {
        var dbErrors = new List<string>();
        var emailAlerts = new List<string>();

        double PackMaxVoltage = cellMaxVoltage * cellCount;
        double PackMinVoltage = cellMinVoltage * cellCount;
        double PackWarningVoltage = cellWarningVoltage * cellCount;

        if (telemetry.Temperature > maxTemperature)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [ALERTA CRITICA] Bateria {telemetry.DeviceId} depaseste temperatura maxima! Real: {telemetry.Temperature}°C (Max: {maxTemperature}°C)");
            Console.ResetColor();
            var msg = $"Supra-temperatura ({telemetry.Temperature}°C > {maxTemperature}°C)";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }
        else if (telemetry.Temperature < minTemperature)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [ALERTA] Risc de înghet (Lithium Plating) pe {telemetry.DeviceId}! ({telemetry.Temperature}°C)");
            Console.ResetColor();
            var msg = $"Sub-temperatura ({telemetry.Temperature})°C";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }

        if (telemetry.Voltage > PackMaxVoltage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" [ALERTA] Supra-tensiune pe bateria {telemetry.DeviceId}! Real: {telemetry.Voltage}V (Max: {PackMaxVoltage}V)");
            Console.ResetColor();
            var msg = $"Supra-tensiune ({telemetry.Voltage}V > {PackMaxVoltage}V)";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }

        if (telemetry.Voltage < PackMinVoltage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" [AVERTISMENT] Sub-tensiune pe bateria {telemetry.DeviceId}! Real: {telemetry.Voltage}V (Min: {PackMinVoltage}V)");
            Console.ResetColor();
            var msg = $"Sub-tensiune ({telemetry.Voltage}V < {PackMinVoltage}V)";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }
        else if (telemetry.Voltage <= PackWarningVoltage)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($" [INFO] Pre-avertizare descarcare pe {telemetry.DeviceId}. ({telemetry.Voltage}V)");
            Console.ResetColor();
            var msg = $"Pre-avertizare descarcare (<={PackWarningVoltage}V)";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }

        if (telemetry.Current > maxCurrent)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[ALERTA] Supracurent pe {telemetry.DeviceId}! ({telemetry.Current}A)");
            Console.ResetColor();
            var msg = $"Supracurent ({telemetry.Current}A)";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }

        if (telemetry.HighestCellVoltage.HasValue && telemetry.LowestCellVoltage.HasValue)
        {
            double deltaV = telemetry.HighestCellVoltage.Value - telemetry.LowestCellVoltage.Value;
            if (deltaV > maxDeltaV)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[ALERTA CRITICA] Dezechilibru pe {telemetry.DeviceId}! Delta V: {Math.Round(deltaV, 3)}V (Max admis: {maxDeltaV}V)");
                Console.ResetColor();
                var msg = $"Dezechilibru sever (Delta v = {Math.Round(deltaV, 3)}V)";
                dbErrors.Add(msg);
                emailAlerts.Add(msg);
            }
        }

        // Verificare individuală a celei mai încărcate/descărcate celule
        if (telemetry.HighestCellVoltage.HasValue && telemetry.HighestCellVoltage.Value > cellMaxVoltage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" [ALERTA] Supra-tensiune individuala pe {telemetry.DeviceId}! O celulă are {telemetry.HighestCellVoltage.Value}V (Max admis: {cellMaxVoltage}V)");
            Console.ResetColor();
            var msg = $"Celula în supra-tensiune ({telemetry.HighestCellVoltage.Value}V > {cellMaxVoltage}V)";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }

        if (telemetry.LowestCellVoltage.HasValue && telemetry.LowestCellVoltage.Value < cellMinVoltage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" [AVERTISMENT] Sub-tensiune individuala pe {telemetry.DeviceId}! O celulă are {telemetry.LowestCellVoltage.Value}V (Min admis: {cellMinVoltage}V)");
            Console.ResetColor();
            var msg = $"Celula în sub-tensiune ({telemetry.LowestCellVoltage.Value}V < {cellMinVoltage}V)";
            dbErrors.Add(msg);
            emailAlerts.Add(msg);
        }
        
        if (telemetry.Soc >= 100)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[INFO] Bateria {telemetry.DeviceId} este complet incarcata (100%)!");
            Console.ResetColor();
            emailAlerts.Add($"Incarcare completa: (100%)");
        }
        
        if (telemetry.Soc <= 10)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"[ALERTA] Bateria {telemetry.DeviceId} necesita incarcare! SOC:{telemetry.Soc}%");
            Console.ResetColor();
            emailAlerts.Add($"Descarcare critica: (<=10%)");
        }

        string? finalDbError = dbErrors.Count > 0 ? string.Join(" | ", dbErrors) : null;
        string? finalEmailAlert = emailAlerts.Count > 0 ? string.Join(" | ", emailAlerts) : null;

        return (finalDbError, finalEmailAlert);
    }

    /// <summary>
    /// Se executa la inchiderea aplicatiei (Graceful Shutdown).
    /// Elibereaza conexiunile de retea catre RabbitMQ si resursele neadministrate.
    /// </summary>
    public override void Dispose()
    {
        _connectionTimer.Stop();
        _connectionTimer.Dispose();
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}