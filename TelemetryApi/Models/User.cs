using System.ComponentModel.DataAnnotations;

namespace TelemetryApi.Models;

/// <summary>
/// Reprezinta entitatea utilizatorului in sistem (contul de client/administrator).
/// Gestioneaza datele de autentificare si relatia cu echipamentele hardware detinute.
/// </summary>
public class User
{
    /// <summary>
    /// Identificatorul unic al utilizatorului in baza de date (Primary Key).
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Adresa de email folosita atat ca username pentru autentificare, cat si pentru trimiterea alertelor.
    /// Atributul [EmailAddress] asigura validarea automata a formatului la nivel de API.
    /// </summary>
    [Required] 
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Stocheaza parola in format criptat (ex: folosind BCrypt sau HMAC). 
    /// Din motive critice de securitate, platforma nu stocheaza niciodata parolele in clar (plain-text).
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Momentul exact al crearii contului. Generat automat la nivel de server in format UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

    /// <summary>
    /// Proprietatea de navigare Entity Framework care defineste relatia One-to-Many (Unu-la-Mai-Multi).
    /// Reprezinta lista tuturor modulelor BMS inregistrate pe acest cont.
    /// Este initializata cu o lista goala pentru a preveni erorile de tip NullReferenceException la accesare.
    /// </summary>
    public ICollection<BmsModule> BmsModules { get; set; } = new List<BmsModule>();
}