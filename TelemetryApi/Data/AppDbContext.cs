using System;
using Microsoft.EntityFrameworkCore;
using TelemetryApi.Models;

namespace TelemetryApi.Data;

//Clasa de conexiune/management cu SQLite
public class AppDbContext : DbContext
{
    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();

    public DbSet<User> Users {get; set; }
    public DbSet<BmsModule> BmsModules{get; set;}
    public DbSet<BatterySettings> BatterySettings {get; set; }
    // public DbSet<EmailSettingsRecord> EmailSettings { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

}