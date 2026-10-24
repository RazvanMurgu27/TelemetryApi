using Microsoft.EntityFrameworkCore;
using TelemetryApi.Models;

namespace TelemetryApi.Data;

/// <summary>
/// Contextul principal al bazei de date (Inima platformei in Entity Framework Core).
/// Gestioneaza conexiunea cu baza de date SQLite si mapeaza modelele orientate pe obiect (C#) in tabele relationale.
/// Implementeaza reguli stricte de integritate a datelor si optimizari de performanta via Fluent API.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();
    public DbSet<User> Users { get; set; }
    public DbSet<BmsModule> BmsModules { get; set; }
    public DbSet<BatterySettings> BatterySettings { get; set; }

    /// <summary>
    /// Constructorul clasei, primeste optiunile de configurare (ex: Connection String) injectate automat de ASP.NET Core la pornire.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Se executa o singura data la initializarea bazei de date.
    /// Aici utilizam Fluent API pentru a defini constrangeri la nivel de schema SQL pe care 
    /// Data Annotations (cum ar fi [Required] in modele) nu le pot acoperi complet.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Constrangere de unicitate (Unique Constraint) pentru adresele de email
        // Garanteaza la nivel de engine SQL ca nu pot exista doua conturi cu acelasi email,
        // acoperind chiar si scenariile de tip Race Condition din retea.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // 2. Constrangere de unicitate pentru identificatorul hardware (Device MAC/ID)
        // Previne inregistrarea accidentala sau abuziva a aceluiasi microcontroller de catre mai multi utilizatori.
        modelBuilder.Entity<BmsModule>()
            .HasIndex(b => b.DeviceId)
            .IsUnique();
            
        // 3. Optimizare de performanta (Database Indexing) pentru Telemetrie
        // Deoarece TelemetryController face frecvent interogari de tipul ".Where(t => t.DeviceId == deviceId)",
        // crearea unui index B-Tree pe aceasta coloana reduce timpul de cautare de la O(N) la O(log N).
        modelBuilder.Entity<TelemetryRecord>()
            .HasIndex(t => t.DeviceId);
            
        // 4. Optimizare de performanta pentru cautarea setarilor bateriei
        modelBuilder.Entity<BatterySettings>()
            .HasIndex(s => s.DeviceId)
            .IsUnique();
    }
}