using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelemetryApi;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using TelemetryApi.Models;
using TelemetryApi.Data;
using TelemetryApi.Services;

namespace TelemetryApi.Controllers;

[ApiController]
[Route("api/[controller]")] // ruta va deveni automat /api/bms
public class BmsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    //Conectare baza de date la acest controller
    public BmsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    //1.Adaugare Baterie Noua(POST)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddBmsModule([FromBody] CreateBmsRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;
        
         if(userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { message = "Utilizator neautorizat sau token invalid!"});
        }

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if(!isAdmin && request.UserId != currentUserId)
        {
            return StatusCode(403, new{ message = "Acces interzis! Nu poti adauga baterii altui utilizator."});
        }

        //1.Verificam daca Id-ul contului exista in baza de date
        var user = await _context.Users.FindAsync(request.UserId);
        if(user == null)
        {
            return NotFound("Contul de utilizator nu a fost gasit!");
        }

        bool deviceExists = await _context.BmsModules.AnyAsync(b => b.DeviceId == request.DeviceId);
        if (deviceExists)
        {
            return Conflict(new {message = $"Dispozitivul hardware cu Id-ul {request.DeviceId} este deja inregistrat in sistem!"});
        }
        
        //2.Construire obiectul baterie
        var newBms = new BmsModule
        {
            Name = request.Name,
            DeviceId = request.DeviceId,
            CellCount = request.CellCount,
            UserId = request.UserId
        };

        //3.Salvare in baza de date
        _context.BmsModules.Add(newBms);
        await _context.SaveChangesAsync();
        
        return Ok(newBms);// Raspuns cu succes si aratam bateria creata
    }

    //2.Citirea Bateriilor pentru un cont
    [Authorize]
    [HttpGet("user/{userId}")] // Ruta va fi de tipul /api/bms/user/1
    public async Task<IActionResult> GetUserBmsModules(int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;
        
        if(userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { message = "Utilizator neautorizat sau token invalid!"});
        }

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;
        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if(!isAdmin && userId != currentUserId)
        {
            return StatusCode(403, new{ message = "Acces interzis! Nu poti vizualiza bateriile altui utilizator."});
        }

        //Cautam toate bateriile care au UserId-ul specificat
        var modules = await _context.BmsModules
            .Where(b =>b.UserId == userId)
            .ToListAsync();

        return Ok(modules);
    }

    [Authorize]
    [HttpDelete("user/{deviceId}")]
    public async Task<IActionResult> DeleteBmsModule(string deviceId)
    {
        //Extragere id utilizator din token-ul JWT
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;
        
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new {message = "Utilizator neautorizat sau token invalid!"});
        }
        var record = await _context.BmsModules.FirstOrDefaultAsync(b => b.DeviceId == deviceId);

        if(record == null)
        {
            return NotFound(new {message ="Bateria nu a fost gasita!"});
        }

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;

        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        if(!isAdmin && record.UserId != currentUserId)
        {
            return StatusCode(403, new {message = "Acces interzis! Nu ai premisiunea sa stergi bateria altui utilizator."});
        }

        //in momentul stergerii unei baterii se sterg automat si toate inregistrarile de telemetrie ale acesteia 
        var associatedTelemetry = await _context.TelemetryRecords
            .Where(t => t.DeviceId == deviceId)
            .ToListAsync();

        if(associatedTelemetry.Any())
        {
            _context.TelemetryRecords.RemoveRange(associatedTelemetry);
        }
        
        _context.BmsModules.Remove(record);
        await _context.SaveChangesAsync();

        return Ok(new {message = $"Bateria cu ID {deviceId} a fost stearsa."});
    }
}

//Aceaasta este o matrita "DTO" care ii spune Swagger-ului si REact-ului ce date trebuie sa trimita pentru a crea o baterie
public class CreateBmsRequest
{
    public string Name{ get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int CellCount{ get; set; }
    public int UserId { get; set; }
}