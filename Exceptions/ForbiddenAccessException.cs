namespace tsu_absences_api.Exceptions;

public class ForbiddenAccessException(string message = "You do not have permission to access this resource.") : Exception(message);