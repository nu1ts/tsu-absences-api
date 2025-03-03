namespace tsu_absences_api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Student;
        public string? GroupId { get; set; }
    }
    
    public enum UserRole
    {
        Admin,      
        DeanOffice, 
        Teacher,   
        Student 
    }
}