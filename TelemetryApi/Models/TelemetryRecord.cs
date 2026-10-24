namespace TelemetryApi.Models;

/// <summary>
/// Reprezinta o inregistrare individuala a datelor de telemetrie preluate de la sistemul de management al bateriei (BMS).
/// Mapeaza tabelul "TelemetryRecords" din baza de date Entity Framework.
/// </summary>
public class TelemetryRecord
{
    /// <summary>
    /// Identificatorul unic (Primary Key) al inregistrarii in baza de date.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identificatorul unic al modulului BMS care a transmis datele (ex: adresa MAC sau ID-ul hardware "SIM-001").
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Tensiunea totala a pachetului de baterii, masurata in Volti (V).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Curentul instantaneu masurat pe circuit, exprimat in Amperi (A).
    /// </summary>
    public double Current { get; set; }

    /// <summary>
    /// Puterea curenta (P = V * I), calculata si raportata in Wati (W).
    /// </summary>
    public double Power { get; set; }

    /// <summary>
    /// Temperatura raportata de senzorii NTC ai pachetului de baterii, masurata in grade Celsius (°C).
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Starea de incarcare a bateriei (State of Charge), reprezentata procentual (0-100%).
    /// </summary>
    public int Soc { get; set; }

    /// <summary>
    /// Tensiunea celei mai incarcate celule din pachet (V). 
    /// Permite valori nule pentru cazurile in care BMS-ul nu citeste temporar senzorii individuali.
    /// </summary>
    public double? HighestCellVoltage { get; set; }

    /// <summary>
    /// Tensiunea celei mai descarcate celule din pachet (V).
    /// </summary>
    public double? LowestCellVoltage { get; set; }

    /// <summary>
    /// Data si ora exacta (in format UTC) la care a fost preluata si salvata masuratoarea.
    /// Functioneaza ca axa a timpului (X-Axis) pentru generarea graficelor in interfata.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Descrierea alertei raportate de hardware (ex: "Supracurent", "Supratemperatura"). 
    /// Valoarea este nula daca sistemul functioneaza in parametri normali.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Momentul exact (UTC) la care a fost declansata eroarea hardware.
    /// </summary>
    public DateTime? ErrorTimeStamp { get; set; }
}