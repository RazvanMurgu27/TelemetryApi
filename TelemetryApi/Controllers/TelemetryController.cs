using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelemetryApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using TelemetryApi.Models;
using TelemetryApi.Data;
using TelemetryApi.Services;

namespace TelemetryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RabbitMqService _rabbitMqService;
    private readonly IConfiguration _configuration;

    public TelemetryController(AppDbContext context, RabbitMqService rabbitMqService, IConfiguration configuration)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _configuration = configuration;
    }

    //1.POST: /api/telemetry
    [HttpPost]
    public async Task<IActionResult> PostTelemetry([FromBody] TelemetryData data)
    {
        await _rabbitMqService.SendMessageAsync(data);
        Console.WriteLine($"[API] Trimis in RabbitMq -> Baterie:{data.DeviceId}");
        return Ok(new {status = "Succes", message = "Datele de telemetrie au fost receptionate!"});
    }
    
    // //2.GET: /api/telemetry
    // [HttpGet]
    // public async Task<IActionResult> GetTelemetryHistory()
    // {
    //     var records = await _context.TelemetryRecords
    //         .OrderByDescending(t => t.Timestamp)
    //         .ToListAsync();
        
    //     return Ok(records);
    // }

    //3.GET /api/telemetry/battery/{batteryId}
    [Authorize]
    [HttpGet("battery/{deviceId}")]
    public async Task<IActionResult> GetTelemetryByBatteryId(string deviceId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value
                            ?? User.FindFirst("userId")?.Value;
        
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("email")?.Value;

        if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new {message = "Utilizator neautorizat sau token invalid!"});
        }

        string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;

        bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

        var bmsModule = await _context.BmsModules.FirstOrDefaultAsync(b => b.DeviceId == deviceId);

        if(bmsModule == null)
        {
            return NotFound(new {message = "Bateria nu a fost gasita in sistem!"});
        }

        if(!isAdmin && bmsModule.UserId != currentUserId)
        {
            return StatusCode(403, new {message = "Acces interzis! Nu poti vizualiza telemetria altui utilizator."});
        }
        var records = await _context.TelemetryRecords
                .Where(t => t.DeviceId == deviceId)
                .OrderByDescending(t => t.Timestamp)
                .Take(100)
                .ToListAsync();
        
        if (!records.Any())
        {
            return Ok(new List<TelemetryRecord>());
        }

        return Ok(records);
    }

    //4.DELETE: /api/telemetry/{id}
    // [Authorize]
    // [HttpDelete("{id}")]
    // public async Task<IActionResult> DeleteTelemetryById(int id)
    // {
    //     var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
    //                         ?? User.FindFirst("id")?.Value
    //                         ?? User.FindFirst("userId")?.Value;
        
    //     var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
    //                         ?? User.FindFirst("email")?.Value;

    //     if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
    //     {
    //         return Unauthorized(new {message = "Utilizator neautorizat sau token invalid!"});
    //     }

    //     var record = await _context.TelemetryRecords.FindAsync(id);
        
    //     if (record == null)
    //     {
    //         return NotFound(new {message = "Inregistrarea nu a fost gasita!"});
    //     }

    //     //Citire adresa admin
    //     string adminEmailConfig = _configuration["AdminSettings:AdminEmail"] ?? string.Empty;

    //     bool isAdmin = (userEmail != null && userEmail.Equals(adminEmailConfig, StringComparison.OrdinalIgnoreCase));

    //     _context.TelemetryRecords.Remove(record);
    //     await _context.SaveChangesAsync();

    //     return Ok(new {message = $"Inregistrarea cu ID {id} a fost stearsa."});
    // }

    // //5.DELETE: /api/telemetry/all
    // [Authorize]
    // [HttpDelete("all")]
    // public async Task<IActionResult> DeleteAllTelemetry()
    // {
    //     var allRecords = await _context.TelemetryRecords.ToListAsync();

    //     _context.TelemetryRecords.RemoveRange(allRecords);
    //     await _context.SaveChangesAsync();

    //     return Ok(new {message = "Toate datele de telemetrie au fost sterse."});
    // }
    
}