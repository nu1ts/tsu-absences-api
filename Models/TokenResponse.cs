using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.Models;

public class TokenResponse
{
    [Required]
    [MinLength(1)]
    public string Token { get; set; }
}