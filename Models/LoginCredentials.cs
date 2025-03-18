using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.Models;

public class LoginCredentials
{
    [Required]
    [EmailAddress]
    [MinLength(1)]
    public string Email { get; set; }
    
    [Required]
    [MinLength(1)]
    public string Password { get; set; }
}