namespace tsu_absences_api.Models
{
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Role Role { get; set; }
    }

    public enum Role
    {
        Admin,
        DeanOffice,
        Teacher,
        Student
    }
}