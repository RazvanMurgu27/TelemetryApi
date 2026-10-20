using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace TelemetryApi.Models;

public class BatterySettings
{
    [JsonIgnore]
    public int Id { get; set; }  
    public string DeviceId { get; set; } = string.Empty;
    public string AlertEmail {get; set;} = string.Empty;
    
    //Praguri de tensiune per celula
    [Range(3.40, 3.85, ErrorMessage = "Tensiunea maximsa a celulei trebuie sa fie intre 3.40V și 3.85V.")]
    public double MaxVoltageThreshold { get; set; } = 3.65;

    [Range(2.00, 3.30, ErrorMessage = "Tensiunea minima a celulei trebuie sa fie intre 2.00V și 3.30V.")]
    public double MinVoltageThreshold { get; set;} = 2.50;

    [Range(2.50, 3.40, ErrorMessage = "Tensiunea de pre-avertizare trebuie sa fie intre 2.50V și 3.40V.")]
    public double WarningVoltageThreshold { get; set; } = 2.80;

    //Praguri de temperatura si curent
    [Range(30.0, 75.0, ErrorMessage = "Temperatura maxima trebuie sa fie intre 30°C și 75°C.")]
    public double MaxTemperatureThreshold { get; set; } = 55.0;

    [Range(-20.0, 15.0, ErrorMessage = "Temperatura minima trebuie sa fie intre -20°C și 15°C.")]
    public double MinTemperatureThreshold { get; set; } = 0.0;

    [Range(1.0, 500.0, ErrorMessage = "Curentul maxim admis trebuie sa fie intre 1A și 500A.")]
    public double MaxCurrentThreshold { get; set; } = 220.0;

    //prag pentru dezechilibru celule
    [Range(0.05, 1.00, ErrorMessage = "Dezechilibrul Delta V trebuie sa fie intre 0.05V și 1.00V.")]
    public double MaxDeltaVThreshold { get; set; } = 0.20;
}