using System.ComponentModel.DataAnnotations;
using tsu_absences_api.Validation;

namespace tsu_absences_api.Models;

public class UserRegisterModel
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string FullName { get; set; }
    
    [Required]
    [MinLength(6)]
    [PasswordValidation]
    public string Password { get; set; }
    
    [Required]
    [EmailAddress]
    [MinLength(1)]
    public string Email { get; set; }

    public string? GroupId { get; set; }
}