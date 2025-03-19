using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.Models;

public class UserDto
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    [MinLength(1)]
    public string FullName { get; set; }
    
    [Required]
    [EmailAddress]
    [MinLength(1)]
    public string Email { get; set; }
    public List<UserRole> Roles { get; set; }
}