namespace TelemetryApi.Models;

public class TelemetryData
{
    public string DeviceId {get; set; } = string.Empty;
    public double Voltage {get; set; }
    public double Current {get; set; }
    public double Temperature { get; set; }
    public int Soc { get; set; }
    public DateTime TimeStamp {get; set; } = DateTime.UtcNow; 
    public double? HighestCellVoltage { get; set; }
    public double? LowestCellVoltage{ get; set; }
}