using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.DTOs;
public class UserUpdateDto
{
    [Required]
    [MinLength(1)]
    public string FullName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string? GroupId { get; set; }
}
