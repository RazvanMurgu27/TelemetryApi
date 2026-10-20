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
using TelemetryApi.Services;

namespace TelemetryApi.Controllers;

//definire caii de acces in Swagger: /api/auth
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    //Constructorul: ASP.NET injecteaza autimat baza de date cand porneste Controllerul
    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    //1.Ruta pentru creare cont (POST api/auth/register)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email); //verifica daca exista deja un utilizator cu acest email

        if(existingUser != null)
        {
            return BadRequest("Exista deja un cont cu aceasta adresa de email."); //daca gaseste un cont, opreste functia si returneaza eroare
        }

        //2.Cripatre parola primita
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        //3.Noul rand pentru tabelul users
        var newUser = new User
        {
            Email = request.Email,
            PasswordHash= passwordHash
        };

        //4.Adaugarea utilizatorului si salvarea modificarilor pe hard diskul bazei de date
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        
        return Ok("Cont creat cu succes!");
    }

    //2.Ruta pentru logare (POST /api/auth/login)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        //1.Cautare cont in baza de date
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        //2.Daca nu s-a gasit niciun user, eroare
        if (user == null)
        {
            return BadRequest("Contul nu exista!");
        }

        //3.Verificare parola
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if(!isPasswordValid)
        {
            return BadRequest("Parola este incorecta!");
        }

        //Generare JWT
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!); // ! este null forgiving operator

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);

        return Ok(new {message = "Logare cu succes!", userId = user.Id, token = jwtString});
    }
}

public class AuthRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password {get; set; } = string.Empty;
}