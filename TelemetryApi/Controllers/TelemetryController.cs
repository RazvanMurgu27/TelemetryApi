using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TelemetryApi.Models;
using TelemetryApi.Data;
using TelemetryApi.Services;

namespace TelemetryApi.Controllers;

/// <summary>
/// Controller de inalta performanta responsabil cu ingestia si interogarea datelor de telemetrie.
/// Utilizeaza un model arhitectural asincron bazat pe Message Brokering (RabbitMQ) pentru a asigura 
/// scalabilitatea si decuplarea hardware-ului de baza de date relationala.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RabbitMqService _rabbitMqService;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Injecteaza dependentele necesare: Contextul bazei de date, serviciul RabbitMQ si configuratiile globale.
    /// </summary>
    public TelemetryController(AppDbContext context, RabbitMqService rabbitMqService, IConfiguration configuration)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _configuration = configuration;
    }

    /// <summary>
    /// Endpoint public de ingestie (Fire-and-Forget). Apelat de microcontroller-ul ESP32.
    /// In loc sa blocheze thread-ul asteptand scrierea pe disc (SQLite), 
    /// publica mesajul in coada RabbitMQ si elibereaza imediat conexiunea HTTP.
    /// </summary>
    /// <param name="data">Pachetul de telemetrie brut primit de la hardware (JSON).</param>
    /// <returns>Confirmare instanta (200 OK) pentru a minimiza latenta pe dispozitivul IoT.</returns>
    [HttpPost]
    public async Task<IActionResult> PostTelemetry([FromBody] TelemetryData data)
    {
        // 1. Serializare si publicare pe Message Broker (RabbitMQ)
        await _rabbitMqService.SendMessageAsync(data);
        
        Console.WriteLine($"[Ingestion API] Pachet trimis in RabbitMQ -> Hardware ID: {data.DeviceId}");
        
        // 2. Returnare imediata catre clientul IoT pentru a elibera resursele hardware-ului
        return Ok(new { status = "Succes", message = "Datele de telemetrie au fost receptionate si puse in coada!" });
    }
    
    /// <summary>
    /// Endpoint securizat utilizat de frontend-ul React pentru a extrage istoricul recent de telemetrie.
    /// </summary>
    /// <param name="deviceId">Identificatorul hardware al dispozitivului.</param>
    /// <returns>Ultimele 100 de inregistrari, ordonate cronologic descrescator.</returns>
    [Authorize]
    [HttpGet("battery/{deviceId}")]
    public async Task<IActionResult> GetTelemetryByBatteryId(string deviceId)
    {
        // 1. Extragere date de securitate (JWT Claims)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;
        
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;

        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { message = "Utilizator neautorizat sau token invalid!" });
        }

        // 2. Validarea resursei hardware solicitate
        var bmsModule = await _context.BmsModules.FirstOrDefaultAsync(b => b.DeviceId == deviceId);

        if (bmsModule == null)
        {
            return NotFound(new { message = "Bateria nu a fost gasita in sistem!" });
        }

        // 3. Verificare IDOR (Insecure Direct Object Reference)
        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if (!isAdmin && bmsModule.UserId != currentUserId)
        {
            return StatusCode(403, new { message = "Acces interzis! Nu poti vizualiza telemetria altui utilizator." });
        }
        
        // 4. Interogare optimizata (Limitata la ultimele 100 de inregistrari pentru a preveni blocarea memoriei - Pagination / Limiting)
        var records = await _context.TelemetryRecords
                .Where(t => t.DeviceId == deviceId)
                .OrderByDescending(t => t.Timestamp)
                .Take(100)
                .ToListAsync();
        
        // 5. Tratarea cazului in care bateria exista, dar nu are inca date transmise
        if (!records.Any())
        {
            return Ok(new List<TelemetryRecord>());
        }

        return Ok(records);
    }
}