using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TelemetryApi.Data;
using TelemetryApi.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Configurarea serviciilor pentru expunerea endpoint-urilor REST
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); 

// Initializarea Swagger UI cu suport integrat pentru testarea rutelor securizate (JWT Bearer Token)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Introduceti token-ul JWT in formatul: Bearer {token_aici}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Introduceti cheia secreta pentru telemetrie (HardwareApiKey).",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } },
            Array.Empty<string>()
        }
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Inregistrarea contextului Entity Framework Core folosind PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Inregistrarea serviciilor Singleton pentru comunicarea asincrona cu RabbitMQ si trimiterea de email-uri
builder.Services.AddSingleton<RabbitMqService>(); 
builder.Services.AddSingleton<EmailService>();

// Inregistrarea Consumer-ului ca Background Service pentru procesarea datelor in afara thread-ului principal HTTP
builder.Services.AddHostedService<TelemetryConsumerService>();

// Definirea politicilor de securitate si validare a token-ului JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Configurarea mecanismului CORS pentru a permite cererile de tip cross-origin (ex: din frontend-ul React)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Crearea unui Scope izolat la pornirea aplicatiei pentru initializarea si popularea bazei de date (Data Seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        
        dbContext.Database.EnsureCreated();
        DataSeeder.Initialize(services);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[CRITICAL] Eroare la initializarea bazei de date: {ex.Message}");
        Console.ResetColor();
    }
}

// Expunerea panoului Swagger exclusiv in mediul de dezvoltare
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Aplicarea politicilor CORS la nivel global pe intreaga aplicatie
app.UseCors("AllowAll");

// Middleware-urile de securitate trebuie executate strict in aceasta ordine: validarea identitatii, apoi verificarea drepturilor
app.UseAuthentication();
app.UseAuthorization();

// Rutarea cererilor HTTP catre controllerele specifice
app.MapControllers();

app.Run();