using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TelemetryApi.Models;

/// <summary>
/// Reprezinta un modul hardware BMS (Battery Management System) inregistrat in sistem.
/// Defineste legatura dintre echipamentul fizic si utilizatorul (proprietarul) acestuia in baza de date.
/// </summary>
public class BmsModule
{
    /// <summary>
    /// Identificatorul unic (Primary Key) al modulului BMS in baza de date.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Numele personalizat atribuit de utilizator echipamentului (ex: "Pachet Solar 8S", "Baterie Backup").
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Identificatorul hardware unic transmis de dispozitiv (ex: "SIM-001" sau adresa MAC).
    /// Este folosit de controller pentru a asocia fluxul de telemetrie cu acest modul specific.
    /// </summary>
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Numarul de celule inseriate in pachetul de baterii monitorizat (ex: 4, 8 sau 16 pentru pachete LiFePO4).
    /// </summary>
    [Required]
    public int CellCount { get; set; }

    /// <summary>
    /// Identificatorul utilizatorului detinator (Foreign Key catre tabelul Users).
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Proprietatea de navigare Entity Framework catre entitatea User.
    /// Atributul [JsonIgnore] este utilizat pentru a preveni o bucla infinita de serializare (circular reference)
    /// in momentul in care API-ul impacheteaza datele pentru a le trimite catre aplicatia React.
    /// </summary>
    [JsonIgnore]
    public User? User { get; set; }
}