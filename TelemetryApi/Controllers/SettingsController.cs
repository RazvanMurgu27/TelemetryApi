using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X509;
using System.Security.Claims;
using TelemetryApi.Models;
using TelemetryApi.Data;
using TelemetryApi.Services;

namespace TelemetryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Asigură că doar utilizatorii autentificați pot accesa rutele
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    // Injectăm contextul bazei de date și configurația (pentru a putea citi AdminEmail, dacă e nevoie)
    public SettingsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // GET: api/settings - Returnează setările utilizatorului curent
    [Authorize] // Asigură că doar utilizatorii autentificați pot accesa rutele
    [HttpGet("battery/{deviceId}")]
    public async Task<IActionResult> GetSettings(string deviceId)
    {
        // Preluăm ID-ul utilizatorului din token folosind mai multe variante posibile de claim (robustete maximă)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;

        // Validăm și convertim în siguranță ID-ul în integer
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentuserId))
        {
            return Unauthorized(new { message = "Utilizator neautorizat sau token invalid!" });
        }
        
        var bmsModule = await _context.BmsModules.FirstOrDefaultAsync(b => b.DeviceId == deviceId);
        if(bmsModule == null)
        {
            return NotFound(new {message = "Bateria nu a fost gasita in sistem!"});
        }

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty; 
        
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));
        
        if(!isAdmin && bmsModule.UserId != currentuserId)
        {
            return StatusCode(403, new {message = "Acces interzis! Nu poti accesa setarile altui utilizator!"});
        }

        //Cautam setarile personalizate 
        var settings = await _context.BatterySettings
            .FirstOrDefaultAsync(s => s.DeviceId == deviceId);

        // Dacă utilizatorul nu are încă setări salvate, îi returnăm valorile implicite
        if (settings == null)
        {
            var owner = await _context.Users.FindAsync(bmsModule.UserId);
            return Ok(new BatterySettings 
            { 
                DeviceId = deviceId, 
                AlertEmail = owner?.Email ?? string.Empty, // Setăm automat emailul contului ca adresă implicită de alertă
                MaxVoltageThreshold = 3.65,
                MinVoltageThreshold = 2.50,
                WarningVoltageThreshold = 2.80,
                MaxTemperatureThreshold = 55.0,
                MinTemperatureThreshold = 0.0,
                MaxCurrentThreshold = 220.0,
                MaxDeltaVThreshold = 0.20
            });
        }

        return Ok(settings);
    }

    // PUT: api/settings - Actualizează sau creează setările și pragurile personalizate ale utilizatorului
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] BatterySettings updatedSettings)
    {
        // Preluăm în siguranță ID-ul utilizatorului din token-ul JWT
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;

        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { message = "Utilizator neautorizat sau token invalid!" });
        }

        var bmsModule = await _context.BmsModules.FirstOrDefaultAsync(b => b.DeviceId == updatedSettings.DeviceId);
        if (bmsModule == null)
        {   
            return NotFound(new {message = $"Bateria {bmsModule} nu există în sistem!"});
        }
        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;

        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if(!isAdmin && bmsModule.UserId != currentUserId)
        {
            return StatusCode(403, new {message = "Acces interzis! Nu puteti modifica setarile altui utilizator!"});
        }

        var existingSettings = await _context.BatterySettings
            .FirstOrDefaultAsync(s => s.DeviceId == updatedSettings.DeviceId);

        if (existingSettings == null)
        {
            // Dacă nu există, creăm o înregistrare nouă legată de acest utilizator (INSERT)
            updatedSettings.Id = 0;

            //Daca campul AlertEmail a fost lasat gol, punem automat emailul contului din tabelul Users
            if (string.IsNullOrWhiteSpace(updatedSettings.AlertEmail))
            {
                var owner = await _context.Users.FindAsync(bmsModule.UserId);
                updatedSettings.AlertEmail = owner?.Email ?? string.Empty;
            }

            _context.BatterySettings.Add(updatedSettings);
        }
        else
        {
            // Dacă există, actualizăm toate câmpurile cu noile valori primite din formular (UPDATE)
            existingSettings.AlertEmail = updatedSettings.AlertEmail;
            existingSettings.MaxVoltageThreshold = updatedSettings.MaxVoltageThreshold;
            existingSettings.MinVoltageThreshold = updatedSettings.MinVoltageThreshold;
            existingSettings.WarningVoltageThreshold = updatedSettings.WarningVoltageThreshold;
            existingSettings.MaxTemperatureThreshold = updatedSettings.MaxTemperatureThreshold;
            existingSettings.MinTemperatureThreshold = updatedSettings.MinTemperatureThreshold;
            existingSettings.MaxCurrentThreshold = updatedSettings.MaxCurrentThreshold;
            existingSettings.MaxDeltaVThreshold = updatedSettings.MaxDeltaVThreshold;
        }

        // Salvăm modificările în baza de date SQLite
        await _context.SaveChangesAsync();
        Console.WriteLine($"[API] Setari actualizate -> Utilizator ID:{updatedSettings.DeviceId}");

        return Ok(new { message = "Toate setarile si pragurile de siguranta au fost salvate cu succes!" });
    }
}