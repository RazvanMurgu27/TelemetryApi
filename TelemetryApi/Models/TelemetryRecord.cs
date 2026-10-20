namespace TelemetryApi.Models;

//Modelul tabelului din baza de date (adauga Id si TimeStamp)
public class TelemetryRecord
{
    public int Id{ get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Power {get; set; }
    public double Temperature { get; set;}
    public int Soc { get; set; }
    public double? HighestCellVoltage { get; set; }
    public double? LowestCellVoltage { get; set; }
    public DateTime Timestamp {get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public DateTime? ErrorTimeStamp { get; set; }
}
