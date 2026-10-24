using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TelemetryApi.Services;

/// <summary>
/// Serviciu dedicat pentru expedierea notificarilor si alertelor critice prin email.
/// Utilizeaza protocolul SMTP prin intermediul librariei MailKit pentru o comunicare asincrona si securizata (TLS).
/// Decupleaza logica de notificare de logica de business a controllerelor.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructor cu injectare de dependente (DI) pentru accesarea setarilor securizate 
    /// (App Password si Email) din mediul de configurare (secrets.json / appsettings.json).
    /// </summary>
    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Expediaza un email de alerta critica catre proprietarul bateriei atunci cand 
    /// parametrii de telemetrie depasesc pragurile de siguranta (Thresholds).
    /// </summary>
    /// <param name="email">Adresa de destinatie a utilizatorului.</param>
    /// <param name="batteryId">Identificatorul hardware al echipamentului (MAC/ID).</param>
    /// <param name="alertMessage">Descrierea detaliata a anomaliei detectate.</param>
    /// <param name="soc">State of Charge (Nivelul bateriei la momentul avariei).</param>
    public async Task SendAlertEmailAsync(string email, string batteryId, string alertMessage, int soc)
    {
        // 1. Validarea datelor de intrare (Mecanism Fail-Fast)
        if (string.IsNullOrWhiteSpace(email))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[EMAIL Warning] Lipsa adresa de destinatie pentru alerta bateriei {batteryId}.");
            Console.ResetColor();
            return;
        }

        // 2. Extragerea credentialelor in mod securizat 
        string appEmail = _config["EmailSettings:Email"] ?? string.Empty; 
        string appPassword = _config["EmailSettings:Password"] ?? string.Empty;
        string smtpServer = "smtp.gmail.com";
        int smtpPort = 587;

        // 3. Construirea pachetului MIME (Multipurpose Internet Mail Extensions)
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sistem Telemetrie LiFePO4", appEmail));
        message.To.Add(new MailboxAddress("Proprietar BMS", email));
        message.Subject = $"[ALERTA CRITICA] Anomalie detectata la bateria {batteryId}";

        message.Body = new TextPart("plain")
        {
            Text = $"⚠️ ATENTIE! Sistemul a detectat o anomalie grava.\n\n" +
                   $"Detalii echipament: {batteryId}\n" +
                   $"Problema raportata: {alertMessage}\n" +
                   $"Nivel incarcare (SOC): {soc}%\n\n" +
                   $"Acesta este un mesaj generat automat de platforma de telemetrie. Va rugam sa verificati fizic echipamentul."
        };

        // 4. Conectarea si expedierea asincrona prin SMTP
        using var client = new SmtpClient();
        try
        {
            // Dezactivam verificarea de revocare pentru a preveni blocajele pe anumite retele corporate/universitare
            client.CheckCertificateRevocation = false;
            
            // Fortam conexiunea securizata (STARTTLS)
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(appEmail, appPassword);
            
            // Expediem si inchidem curat conexiunea
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[EMAIL Success] Alerta trimisa cu succes catre {email}!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            // 5. Izolarea erorilor (Fault Tolerance) - Nu lasam caderea email-ului sa prabuseasca intregul sistem
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[EROARE EMAIL] Nu s-a putut expedia mesajul: {ex.Message}");
            Console.ResetColor();
        }
    }
}