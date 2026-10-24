namespace TelemetryApi.Models;

/// <summary>
/// Reprezinta modelul de date (DTO - Data Transfer Object) proiectat pentru a receptiona pachetele JSON de la hardware (ex: ESP32).
/// Defineste structura datelor brute care urmeaza sa fie citite de senzori (via protocol RS485 de la JK BMS).
/// </summary>
public class TelemetryData
{
    /// <summary>
    /// Identificatorul unic al modulului hardware care transmite datele (ex: "SIM-001" sau adresa MAC).
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Tensiunea totala a pachetului de baterii la momentul citirii, masurata in Volti (V).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Curentul instantaneu citit pe circuit, exprimat in Amperi (A).
    /// </summary>
    public double Current { get; set; }

    /// <summary>
    /// Temperatura raportata de senzorii NTC ai BMS-ului, masurata in grade Celsius (°C).
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Starea estimata de incarcare a bateriei (State of Charge), in format procentual (0-100%).
    /// </summary>
    public int Soc { get; set; }

    /// <summary>
    /// Momentul preluarii pachetului de date. Este alocat automat de catre server in format UTC la momentul receptiei.
    /// </summary>
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow; 

    /// <summary>
    /// Tensiunea masurata pe cea mai incarcata celula din pachet (V).
    /// </summary>
    public double? HighestCellVoltage { get; set; }

    /// <summary>
    /// Tensiunea masurata pe cea mai descarcata celula din pachet (V).
    /// </summary>
    public double? LowestCellVoltage { get; set; }
}