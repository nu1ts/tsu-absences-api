using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Data;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Services;

public class FileService(AppDbContext context) : IFileService
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

    public async Task<FileStreamResult> GetFileAsync(Guid documentId, Guid userId, bool isDeanOffice)
    {
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new FileNotFoundException("File not found.");
        }

        var absence = await context.Absences
            .FirstOrDefaultAsync(a => a.Id == document.AbsenceId);

        if (absence == null)
        {
            throw new FileNotFoundException("Absence not found for the document.");
        }

        if (absence.UserId != userId && !isDeanOffice)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this file.");
        }

        var filePath = Path.Combine(_storagePath, document.FilePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found on disk.");
        }

        var fileExtension = Path.GetExtension(document.FileName).ToLower();
        var mimeType = fileExtension switch
        {
            ".pdf" => "application/pdf",
            ".jpeg" or ".jpg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(fileStream, mimeType)
        {
            FileDownloadName = document.FileName
        };
    }
    
    public async Task DeleteFileAsync(Guid documentId, Guid userId, bool isDeanOffice)
    {
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new FileNotFoundException("File not found.");
        }

        var absence = await context.Absences
            .FirstOrDefaultAsync(a => a.Id == document.AbsenceId);

        if (absence == null)
        {
            throw new FileNotFoundException("Absence not found for the document.");
        }
        
        if (absence.UserId != userId && !isDeanOffice)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this file.");
        }

        var filePath = Path.Combine(_storagePath, document.FilePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found on disk.");
        }
        
        File.Delete(filePath);

        context.Documents.Remove(document);
        await context.SaveChangesAsync();
    }
}