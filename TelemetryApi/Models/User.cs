using System;
using System.ComponentModel.DataAnnotations;

namespace TelemetryApi.Models;

public class User
{
    [Key] //Id unic [PK]
    public int Id{ get; set; }

    [Required] 
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash {get; set;} = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

    public ICollection<BmsModule> BmsModules { get; set; } = new List<BmsModule>();
}