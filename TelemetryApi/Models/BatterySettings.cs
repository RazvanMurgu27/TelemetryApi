using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace TelemetryApi.Models;

/// <summary>
/// Reprezinta configuratia parametrilor de siguranta (Thresholds) pentru un pachet de baterii LiFePO4.
/// Aceste setari dicteaza cand sistemul backend (alerte email) sau frontend-ul trebuie sa semnalizeze o avarie.
/// Utilizeaza atribute de validare (Data Annotations) pentru a preveni introducerea unor parametri periculosi fizic.
/// </summary>
public class BatterySettings
{
    /// <summary>
    /// Identificatorul unic intern din baza de date. 
    /// Atributul [JsonIgnore] previne expunerea structurii interne a bazei de date catre client (React),
    /// identificarile facandu-se strict prin DeviceId.
    /// </summary>
    [JsonIgnore]
    public int Id { get; set; }  
    
    /// <summary>
    /// Identificatorul echipamentului hardware (ex: "SIM-001") caruia ii apartin aceste setari.
    /// </summary>
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Adresa de email la care sistemul va expedia notificarile in cazul in care senzorii depasesc pragurile setate.
    /// </summary>
    [EmailAddress]
    public string AlertEmail { get; set; } = string.Empty;
    
    // --- Praguri de Tensiune (Voltage) ---

    /// <summary>
    /// Tensiunea maxima permisa pe o singura celula. Pentru chimia LiFePO4, depasirea valorii de 3.65V duce la degradare.
    /// </summary>
    [Range(3.40, 3.85, ErrorMessage = "Tensiunea maxima a celulei trebuie sa fie intre 3.40V si 3.85V.")]
    public double MaxVoltageThreshold { get; set; } = 3.65;

    /// <summary>
    /// Tensiunea minima admisa (Cut-off voltage). Descarcarea sub acest prag distruge ireversibil celula.
    /// </summary>
    [Range(2.00, 3.30, ErrorMessage = "Tensiunea minima a celulei trebuie sa fie intre 2.00V si 3.30V.")]
    public double MinVoltageThreshold { get; set; } = 2.50;

    /// <summary>
    /// Pragul de tensiune la care sistemul trimite un avertisment inainte de oprirea fortata a descarcarii (Low Battery Warning).
    /// </summary>
    [Range(2.50, 3.40, ErrorMessage = "Tensiunea de pre-avertizare trebuie sa fie intre 2.50V si 3.40V.")]
    public double WarningVoltageThreshold { get; set; } = 2.80;

    // --- Praguri de Temperatura si Curent ---

    /// <summary>
    /// Limita maxima termica de operare. Previne fenomenul de Thermal Runaway.
    /// </summary>
    [Range(30.0, 75.0, ErrorMessage = "Temperatura maxima trebuie sa fie intre 30°C si 75°C.")]
    public double MaxTemperatureThreshold { get; set; } = 55.0;

    /// <summary>
    /// Limita minima termica. Este critica pentru celulele LiFePO4, deoarece incarcarea sub 0°C provoaca placarea cu litiu (Lithium Plating).
    /// </summary>
    [Range(-20.0, 15.0, ErrorMessage = "Temperatura minima trebuie sa fie intre -20°C si 15°C.")]
    public double MinTemperatureThreshold { get; set; } = 0.0;

    /// <summary>
    /// Curentul maxim de descarcare/incarcare permis prin cabluri si invertor, raportat in Amperi.
    /// </summary>
    [Range(1.0, 500.0, ErrorMessage = "Curentul maxim admis trebuie sa fie intre 1A si 500A.")]
    public double MaxCurrentThreshold { get; set; } = 220.0;

    // --- Calitatea si Balansarea Pachetului ---

    /// <summary>
    /// Dezechilibrul maxim admis intre cea mai incarcata si cea mai descarcata celula (Delta V).
    /// O valoare ridicata indica o celula defecta sau un dezechilibru major pe care balancerul activ nu il mai poate compensa.
    /// </summary>
    [Range(0.05, 1.00, ErrorMessage = "Dezechilibrul Delta V trebuie sa fie intre 0.05V si 1.00V.")]
    public double MaxDeltaVThreshold { get; set; } = 0.20;
}