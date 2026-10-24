using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TelemetryApi.Models;
using TelemetryApi.Data;

namespace TelemetryApi.Controllers;

/// <summary>
/// Controller RESTful responsabil cu gestionarea ciclului de viata al echipamentelor BMS (Battery Management System).
/// Expune rute securizate care necesita un token JWT valid.
/// Implementeaza securitate la nivel de resursa (Resource-Based Authorization) pentru a preveni 
/// vulnerabilitatile de tip IDOR (Insecure Direct Object Reference).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BmsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializeaza o noua instanta a controller-ului, injectand dependentele necesare (IoC - Inversion of Control).
    /// </summary>
    /// <param name="context">Contextul bazei de date Entity Framework.</param>
    /// <param name="configuration">Furnizorul de configuratii pentru accesarea politicilor de acces (ex: AdminSettings).</param>
    public BmsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Inregistreaza un nou dispozitiv hardware BMS in platforma si il asociaza unui utilizator specific.
    /// </summary>
    /// <param name="request">Payload-ul continand specificatiile hardware si identificatorul proprietarului.</param>
    /// <returns>Obiectul BMS proaspat creat sau un cod de eroare HTTP corespunzator (401, 403, 404, 409).</returns>
    /// <remarks>
    /// Valideaza unicitatea hardware-ului si aplica restrictii de autorizare: un utilizator standard
    /// poate adauga dispozitive doar pentru propriul cont, in timp ce contul de Administrator are privilegii globale.
    /// </remarks>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddBmsModule([FromBody] CreateBmsRequest request)
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

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if (!isAdmin && request.UserId != currentUserId)
        {
            return StatusCode(403, new { message = "Acces interzis! Nu poti adauga baterii altui utilizator." });
        }

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return NotFound("Contul de utilizator nu a fost gasit!");
        }

        bool deviceExists = await _context.BmsModules.AnyAsync(b => b.DeviceId == request.DeviceId);
        if (deviceExists)
        {
            return Conflict(new { message = $"Dispozitivul hardware cu ID-ul {request.DeviceId} este deja inregistrat in sistem!" });
        }
        
        var newBms = new BmsModule
        {
            Name = request.Name,
            DeviceId = request.DeviceId,
            CellCount = request.CellCount,
            UserId = request.UserId
        };

        _context.BmsModules.Add(newBms);
        await _context.SaveChangesAsync();
        
        return Ok(newBms);
    }

    /// <summary>
    /// Returneaza lista completa a modulelor BMS detinute de un anumit utilizator.
    /// Utilizeaza filtrare contextuala pentru a asigura izolarea datelor intre chiriasi (Data Isolation).
    /// </summary>
    /// <param name="userId">Identificatorul unic al utilizatorului.</param>
    /// <returns>O lista de obiecte BmsModule in format JSON.</returns>
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserBmsModules(int userId)
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

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if (!isAdmin && userId != currentUserId)
        {
            return StatusCode(403, new { message = "Acces interzis! Nu poti vizualiza bateriile altui utilizator." });
        }

        var modules = await _context.BmsModules
            .Where(b => b.UserId == userId)
            .ToListAsync();

        return Ok(modules);
    }

    /// <summary>
    /// Elimina un dispozitiv BMS din sistem si declanseaza stergerea in cascada (Cascade Delete) 
    /// a tuturor inregistrarilor de telemetrie asociate la nivel de logica de business.
    /// </summary>
    /// <param name="deviceId">Identificatorul hardware unic al dispozitivului.</param>
    /// <returns>Mesaj de confirmare a stergerii.</returns>
    /// <remarks>
    /// Previne incalcarea constrangerilor Foreign Key (Referential Integrity) prin curatarea proactiva 
    /// a tabelelor copil inainte de stergerea inregistrarii parinte.
    /// </remarks>
    [Authorize]
    [HttpDelete("user/{deviceId}")]
    public async Task<IActionResult> DeleteBmsModule(string deviceId)
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
        
        var record = await _context.BmsModules.FirstOrDefaultAsync(b => b.DeviceId == deviceId);

        if (record == null)
        {
            return NotFound(new { message = "Bateria nu a fost gasita!" });
        }

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if (!isAdmin && record.UserId != currentUserId)
        {
            return StatusCode(403, new { message = "Acces interzis! Nu ai permisiunea sa stergi bateria altui utilizator." });
        }

        var associatedTelemetry = await _context.TelemetryRecords
            .Where(t => t.DeviceId == deviceId)
            .ToListAsync();

        if (associatedTelemetry.Any())
        {
            _context.TelemetryRecords.RemoveRange(associatedTelemetry);
        }
        
        _context.BmsModules.Remove(record);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Bateria cu ID {deviceId} a fost stearsa." });
    }
}

/// <summary>
/// Data Transfer Object (DTO) utilizat ca si contract pentru payload-ul JSON necesar crearii unui BMS.
/// Ofera framework-ului ASP.NET capabilitatea de a serializa doar datele expuse public, 
/// protejand modelul de domeniu intern.
/// </summary>
public class CreateBmsRequest
{
    public string Name { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int CellCount { get; set; }
    public int UserId { get; set; }
}