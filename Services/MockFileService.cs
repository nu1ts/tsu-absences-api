using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Services;

public class MockFileService : IFileService
{
    public Task<string> SaveFileAsync(IFormFile file)
    {
        return Task.FromResult("mockFilePath");
    }
}
