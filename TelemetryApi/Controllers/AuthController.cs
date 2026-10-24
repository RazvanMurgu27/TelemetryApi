using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net; 
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using TelemetryApi.Models;
using TelemetryApi.Data;

namespace TelemetryApi.Controllers;

/// <summary>
/// Controller responsabil cu gestionarea identitatii utilizatorilor (Identity & Access Management).
/// Expune rutele publice pentru inregistrare si autentificare, utilizand criptarea BCrypt si token-uri JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor cu Dependency Injection. 
    /// ASP.NET Core injecteaza automat contextul bazei de date si configuratia aplicatiei.
    /// </summary>
    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Endpoint pentru crearea unui cont nou. 
    /// Securitate: Previne inregistrarea duplicatelor si aplica hashing criptografic (BCrypt) pe parola inainte de salvare.
    /// </summary>
    /// <param name="request">Datele trimise de client (Email si Parola in clar).</param>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        // 1. Verificare existenta cont
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email); 

        if(existingUser != null)
        {
            // Oprim executia pentru a preveni incalcarile de constrangeri in baza de date
            return BadRequest("Exista deja un cont cu aceasta adresa de email."); 
        }

        // 2. Criptarea parolei (BCrypt genereaza si aplica automat un "Salt" criptografic)
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Maparea datelor in modelul Entity Framework
        var newUser = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash
        };

        // 4. Salvarea tranzactiei in baza de date SQLite
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        
        return Ok("Cont creat cu succes!");
    }

    /// <summary>
    /// Endpoint pentru autentificarea utilizatorilor.
    /// Emite un JSON Web Token (JWT) Stateless daca credentialele sunt valide.
    /// </summary>
    /// <param name="request">Credentialele trimise de client.</param>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        // 1. Cautarea utilizatorului dupa email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            // Returnam un mesaj generic din motive de securitate (enumerarea conturilor)
            return BadRequest("Credentiale invalide!");
        }

        // 2. Verificarea semnaturii criptografice a parolei
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if(!isPasswordValid)
        {
            return BadRequest("Credentiale invalide!");
        }

        // 3. Generarea Token-ului JWT pentru mentinerea sesiunii (Stateless Authentication)
        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Citirea cheii secrete din User Secrets / appsettings.json
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!); 

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Payload-ul (Sarcina utila) contine identificatorii necesari pentru deciziile de autorizare ulterioare
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(2), // Previne abuzurile in caz de interceptare (Token hijacking)
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);

        // Returnam token-ul impreuna cu UserId-ul pentru a facilita logica din frontend-ul React
        return Ok(new { message = "Logare cu succes!", userId = user.Id, token = jwtString });
    }
}

/// <summary>
/// Data Transfer Object (DTO) utilizat strict pentru incapsularea datelor de autentificare primite de la client.
/// Decupleaza modelul intern al bazei de date (User) de contractul API-ului.
/// </summary>
public class AuthRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}