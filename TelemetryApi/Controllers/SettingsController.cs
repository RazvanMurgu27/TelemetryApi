using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TelemetryApi.Models;
using TelemetryApi.Data;

namespace TelemetryApi.Controllers;

/// <summary>
/// Controller RESTful dedicat configurarii parametrilor de siguranta (Thresholds) pentru pachetele de baterii LiFePO4.
/// Toate rutele sunt protejate si necesita un token JWT valid.
/// Implementeaza mecanisme de Fallback pentru configuratii implicite si operatiuni de tip UPSERT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Injecteaza dependentele necesare pentru accesul la date si setarile globale ale serverului.
    /// </summary>
    public SettingsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Recupereaza setarile curente ale unui dispozitiv BMS.
    /// Daca dispozitivul nu a fost configurat anterior, API-ul returneaza automat un set de 
    /// parametri impliciti, optimizati pentru chimia LiFePO4, fara a persista inca in baza de date.
    /// </summary>
    /// <param name="deviceId">Identificatorul hardware al dispozitivului.</param>
    /// <returns>Obiectul de tip BatterySettings cu parametrii de functionare.</returns>
    [HttpGet("battery/{deviceId}")]
    public async Task<IActionResult> GetSettings(string deviceId)
    {
        // 1. Extragere claim-uri de securitate
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;

        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { message = "Utilizator neautorizat sau token invalid!" });
        }
        
        // 2. Validarea existentei resursei hardware
        var bmsModule = await _context.BmsModules.FirstOrDefaultAsync(b => b.DeviceId == deviceId);
        if (bmsModule == null)
        {
            return NotFound(new { message = "Bateria nu a fost gasita in sistem!" });
        }

        // 3. Verificare IDOR si privilegii
        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty; 
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));
        
        if (!isAdmin && bmsModule.UserId != currentUserId)
        {
            return StatusCode(403, new { message = "Acces interzis! Nu poti accesa setarile altui utilizator!" });
        }

        var settings = await _context.BatterySettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);

        // 4. Mecanismul de Fallback (Graceful Degradation)
        if (settings == null)
        {
            var owner = await _context.Users.FindAsync(bmsModule.UserId);
            
            return Ok(new BatterySettings 
            { 
                DeviceId = deviceId, 
                AlertEmail = owner?.Email ?? string.Empty, // Autocompletare inteligenta
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

    /// <summary>
    /// Aplica logica de tip UPSERT (Update or Insert) pentru a salva noile setari de siguranta.
    /// Valideaza integritatea cererii si aplica mecanisme de auto-completare pentru datele omise.
    /// </summary>
    /// <param name="updatedSettings">Payload-ul JSON reprezentand noile limite de operare.</param>
    /// <returns>Confirmare standard (200 OK) pentru a semnala finalizarea tranzactiei.</returns>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] BatterySettings updatedSettings)
    {
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
            return NotFound(new { message = $"Dispozitivul cu ID-ul {updatedSettings.DeviceId} nu exista in sistem!" });
        }
        
        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if (!isAdmin && bmsModule.UserId != currentUserId)
        {
            return StatusCode(403, new { message = "Acces interzis! Nu puteti modifica setarile altui utilizator!" });
        }

        // 1. Verificam existenta inregistrarii pentru a determina actiunea DML necesara (Insert vs Update)
        var existingSettings = await _context.BatterySettings.FirstOrDefaultAsync(s => s.DeviceId == updatedSettings.DeviceId);

        if (existingSettings == null)
        {
            // Executare INSERT: Resetam ID-ul pentru a evita conflictele de PK (Primary Key)
            updatedSettings.Id = 0;

            if (string.IsNullOrWhiteSpace(updatedSettings.AlertEmail))
            {
                var owner = await _context.Users.FindAsync(bmsModule.UserId);
                updatedSettings.AlertEmail = owner?.Email ?? string.Empty;
            }

            _context.BatterySettings.Add(updatedSettings);
        }
        else
        {
            // Executare UPDATE: Actualizam entitatea urmarita (tracked entity) in memorie
            existingSettings.AlertEmail = updatedSettings.AlertEmail;
            existingSettings.MaxVoltageThreshold = updatedSettings.MaxVoltageThreshold;
            existingSettings.MinVoltageThreshold = updatedSettings.MinVoltageThreshold;
            existingSettings.WarningVoltageThreshold = updatedSettings.WarningVoltageThreshold;
            existingSettings.MaxTemperatureThreshold = updatedSettings.MaxTemperatureThreshold;
            existingSettings.MinTemperatureThreshold = updatedSettings.MinTemperatureThreshold;
            existingSettings.MaxCurrentThreshold = updatedSettings.MaxCurrentThreshold;
            existingSettings.MaxDeltaVThreshold = updatedSettings.MaxDeltaVThreshold;
        }

        // 2. Commit al tranzactiei si salvare persistenta
        await _context.SaveChangesAsync();
        
        Console.WriteLine($"[Audit Log] Setari actualizate pentru hardware ID: {updatedSettings.DeviceId} de catre User ID: {currentUserId}");

        return Ok(new { message = "Toate setarile si pragurile de siguranta au fost salvate cu succes!" });
    }
}