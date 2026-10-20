using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TelemetryApi.Models;

public class BmsModule
{
    public int Id {get; set; }//Cheia Primara
    
    [Required]
    public string Name {get; set;} = string.Empty;
    
    [Required]
    public string DeviceId {get; set;} = string.Empty;

    [Required]
    public int CellCount {get; set; }

    //RElatia cu Userul
    [Required]
    public int UserId {get; set;} //Foreign Key (cheia secundara care face legatura cu contul)

    [JsonIgnore]
    public User? User {get; set;}
}