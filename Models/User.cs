using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.Models
{
    public class User
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
        
        [Required]
        [EmailAddress]
        [MinLength(1)]
        public string Email { get; set; } = string.Empty;
        public string? GroupId { get; set; }

        public List<UserRole> Roles { get; set; } = [UserRole.Student];
    }

    public enum UserRole
    {
        Admin,
        DeanOffice,
        Teacher,
        Student
    }
}