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

        public List<UserRoleMapping> UserRoles { get; set; } = new List<UserRoleMapping>();
    }

    public class UserRoleMapping
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public UserRole Role { get; set; }
    }

    public enum UserRole
    {
        Student,
        Teacher,
        Admin,
        DeanOffice
    }
}