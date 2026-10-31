using TelemetryApi.Models;

namespace TelemetryApi.Data;

/// <summary>
/// Clasa utilitara de Bootstrapping (Data Seeding).
/// Populeaza baza de date cu date de test realiste pentru mediul de dezvoltare si demonstratii (POC).
/// Simuleaza functionarea fizica a unui pachet LiFePO4 de 12V (4S), inclusiv scenarii de eroare (Spike-uri, Supra-incalzire).
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Initializeaza si populeaza baza de date daca aceasta este goala.
    /// Utilizeaza Scoped Dependency Injection pentru a asigura curatarea memoriei dupa rulare (Garbage Collection).
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<AppDbContext>();

        // 1. Validarea si crearea automata a schemei bazei de date (Code-First Approach)
        context.Database.EnsureCreated();

        // 2. Mecanism de tip Idempotency: Previne duplicarea datelor daca seeder-ul este rulat de mai multe ori
        if (context.Users.Any())
        {
            return; 
        }

        // 3. Crearea unui cont de inginer (Test User) securizat
        var testUser = new User 
        { 
            Email = "student@upt.ro", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        
        context.Users.Add(testUser);
        context.SaveChanges(); 

        // 4. Inregistrarea unui echipament hardware de test (BMS LiFePO4 4S)
        var testBms = new BmsModule 
        { 
            DeviceId = "SIM-001", 
            Name = "Baterie Demo (4S LiFePO4)", 
            CellCount = 4, 
            UserId = testUser.Id 
        };
        
        context.BmsModules.Add(testBms);
        context.SaveChanges();

        // 5. Generarea unui Set de Date de Telemetrie tip Time-Series
        var random = new Random();
        var telemetries = new List<TelemetryRecord>();

        double currentSoc = 99.0; // State of Charge initial

        // Generam date pentru ultimele ~4 ore (50 puncte cu interval de 5 minute)
        for (int i = 50; i >= 0; i--)
        {
            // Valori operationale nominale
            double voltage = Math.Round(13.2 + (random.NextDouble() * 0.2), 2); 
            double current = Math.Round(2.0 + (random.NextDouble() * 10.0), 2); 
            double temperature = Math.Round(22.0 + (random.NextDouble() * 2), 1);
            
            string? errorMessage = null;
            DateTime? errorTimeStamp = null;

            // --- INJECTAREA SCENARIILOR DE STRESS HARDWARE (Edge Cases) ---
            
            // Scenariul 1: Eroare de Supracurent (Overcurrent) si Voltage Sag
            if (i == 35)
            {
                current = 85.5; 
                voltage -= 0.4; 
                errorMessage = "Supracurent la descărcare (>80A)";
                errorTimeStamp = DateTime.UtcNow.AddMinutes(-i * 5);
            }
            
            // Scenariul 2: Thermal Runaway (Supra-incalzire periculoasa)
            if (i == 20)
            {
                temperature = 68.5; 
                errorMessage = "Temperatură critică (>65°C)";
                errorTimeStamp = DateTime.UtcNow.AddMinutes(-i * 5);
            }

            // Scenariul 3: Dezechilibru masiv intre celule (Cell Imbalance)
            double avgCellVoltage = voltage / 4.0;
            double highestCell = Math.Round(avgCellVoltage + 0.012, 3); 
            double lowestCell = Math.Round(avgCellVoltage - 0.008, 3);

            if (i == 5)
            {
                highestCell = Math.Round(avgCellVoltage + 0.25, 3); 
                lowestCell = Math.Round(avgCellVoltage - 0.30, 3); 
                errorMessage = "Dezechilibru masiv celule (ΔV > 0.5V)";
                errorTimeStamp = DateTime.UtcNow.AddMinutes(-i * 5);
            }

            // --- CALCULUL FIZIC AL PUTERII SI CAPACITATII ---
            
            double power = Math.Round(voltage * current, 2);
            // Simulare liniara a scaderii State of Charge (SOC) pe baza curentului
            currentSoc -= (current * 0.04); 
            if (currentSoc < 10) currentSoc = 10; // Capping la 10%

            telemetries.Add(new TelemetryRecord
            {
                DeviceId = "SIM-001",
                Voltage = voltage,
                Soc = (int)currentSoc,
                Current = current,
                Power = power,
                Temperature = temperature,
                HighestCellVoltage = highestCell,
                LowestCellVoltage = lowestCell,
                ErrorMessage = errorMessage,
                ErrorTimeStamp = errorTimeStamp,
                Timestamp = DateTime.UtcNow.AddMinutes(-i * 5)
            });
        }
        
        // 6. Batch Insert (Salvare in bloc) pentru optimizarea tranzactiei SQLite
        context.TelemetryRecords.AddRange(telemetries);
        context.SaveChanges();
    }
}