namespace tsu_absences_api.Exceptions;

public class UserException : Exception
{
    public UserException() 
        : base("User not found") { }
}