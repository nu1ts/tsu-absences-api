using Microsoft.AspNetCore.Mvc;

namespace tsu_absences_api.Interfaces;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file);
    Task<FileStreamResult> GetFileAsync(Guid documentId, Guid userId, bool isDeanOffice);
    Task DeleteFileAsync(Guid documentId, Guid userId, bool isDeanOffice);
}