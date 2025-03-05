namespace tsu_absences_api.Interfaces;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file);
}