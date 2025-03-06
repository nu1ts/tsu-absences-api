using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Services;

public class FileService : IFileService
{
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "app_data", "uploads");

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File can't be empty.");
            }
            
            const long maxFileSize = 10485760;
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException("File is too large.");
            }

            var allowedFileTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
            if (!allowedFileTypes.Contains(file.ContentType))
            {
                throw new ArgumentException("Invalid file type.");
            }

            Directory.CreateDirectory(_storagePath);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(_storagePath, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return uniqueFileName;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while processing the file.", ex);
        }
    }
}