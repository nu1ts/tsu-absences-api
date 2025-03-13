namespace tsu_absences_api.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<UserRole> Roles { get; set; } = [];
        public string? GroupId { get; set; }
    }
}