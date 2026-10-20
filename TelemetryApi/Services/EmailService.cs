using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TelemetryApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using TelemetryApi.Models;
using TelemetryApi.Data;
using TelemetryApi.Controllers;

namespace TelemetryApi.Services;
public class EmailService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    public EmailService(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _config = config;
    }

    public async Task SendAlertEmailAsync(string email, string batteryId, string alertMessage, int soc)
    {
        if (string.IsNullOrEmpty(email))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[EMAIL] Lispa destinatie.");
            Console.ResetColor();
            return;
        }

        string appEmail = _config["EmailSettings:Email"] ?? string.Empty; 
        string appPassword = _config["EmailSettings:Password"] ?? string.Empty;
        string smtpServer = "smtp.gmail.com";
        int smtpPort = 587;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sistem Telemetrie LiFePO4", appEmail));
        message.To.Add(new MailboxAddress("Utilizator", email));
        message.Subject = $"[ALERTA CRITICA] Bateria {batteryId}";

        message.Body = new TextPart("plain")
        {
            Text = $"Atentie! Sistemul a detectat o anomalie.\n\n" +
                   $"Baterie: {batteryId}\n" +
                   $"Probleme: {alertMessage}\n\n" +
                   $"Nivel incarcare la momentul alertei:{soc}%\n\n"+
                   $"Mesaj generat automat de sistemul tau de telemetrie."
        };

        using var client = new SmtpClient();
        try
        {
            client.CheckCertificateRevocation = false;
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(appEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            Console.WriteLine($"\n[EMAIL] Alerta trimisa cu succes catre {email}!");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[EROARE EMAIL] Nu s-a putut trimite: {ex.Message}");
            Console.ResetColor();
        }
    }
}