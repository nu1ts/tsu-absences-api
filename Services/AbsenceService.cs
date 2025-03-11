using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Data;
using tsu_absences_api.DTOs;
using tsu_absences_api.Enums;
using tsu_absences_api.Exceptions;
using tsu_absences_api.Interfaces;
using tsu_absences_api.Models;

namespace tsu_absences_api.Services;

public class AbsenceService(AppDbContext context, IFileService fileService) : IAbsenceService
{
    public async Task<Absence> CreateAbsenceAsync(Guid userId, CreateAbsenceDto dto)
    {
        ValidateAbsenceDto(dto.Type, dto.StartDate, dto.EndDate, dto.Documents, dto.DeclarationToDean);

        var absence = new Absence
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = dto.Type,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = AbsenceStatus.Pending,
            DeclarationToDean = dto.DeclarationToDean,
            Documents = []
        };

        context.Absences.Add(absence);
        await context.SaveChangesAsync();

        if (dto.Documents?.Count > 0)
        {
            foreach (var file in dto.Documents)
            {
                var filePath = await fileService.SaveFileAsync(file);
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    AbsenceId = absence.Id,
                    FileName = file.FileName,
                    FilePath = filePath
                };
                context.Documents.Add(document);
                absence.Documents.Add(document.Id);
            }
            await context.SaveChangesAsync();
        }

        return absence;
    }

    public async Task UpdateAbsenceAsync(Guid userId, UpdateAbsenceDto dto, Guid id, bool isDeanOffice)
    {
        var absence = await context.Absences.FindAsync(id);
        if (absence == null)
            throw new KeyNotFoundException("Absence not found.");

        if (absence.Status == AbsenceStatus.Rejected)
            throw new ForbiddenAccessException("You can't edit a rejected absence.");

        if (absence.Status != AbsenceStatus.Pending && !(isDeanOffice && absence.Type == AbsenceType.Academic))
            throw new ForbiddenAccessException("You don't have permission to edit this absence.");

        if (absence.UserId != userId && !isDeanOffice)
            throw new ForbiddenAccessException("You don't have permission to edit this absence.");

        ValidateAbsenceDto(absence.Type, dto.StartDate, dto.EndDate, dto.Documents, dto.DeclarationToDean);

        absence.StartDate = dto.StartDate;
        absence.EndDate = dto.EndDate;

        if (dto.DeclarationToDean.HasValue)
            absence.DeclarationToDean = dto.DeclarationToDean.Value;
        
        var existingDocuments = await context.Documents
            .Where(d => d.AbsenceId == absence.Id)
            .ToListAsync();

        var existingFileNames = existingDocuments.Select(d => d.FileName).ToHashSet();
        var newFileNames = dto.Documents?.Select(f => f.FileName).ToHashSet() ?? [];
        
        var filesToDelete = existingDocuments.Where(d => !newFileNames.Contains(d.FileName)).ToList();
        foreach (var file in filesToDelete)
        {
            await fileService.DeleteFileAsync(file.Id, userId, isDeanOffice);
            absence.Documents.Remove(file.Id);
        }
        
        if (dto.Documents?.Count > 0)
        {
            foreach (var file in dto.Documents)
            {
                if (!existingFileNames.Contains(file.FileName))
                {
                    var filePath = await fileService.SaveFileAsync(file);
                    var document = new Document
                    {
                        Id = Guid.NewGuid(),
                        AbsenceId = absence.Id,
                        FileName = file.FileName,
                        FilePath = filePath
                    };
                    context.Documents.Add(document);
                    absence.Documents.Add(document.Id);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static void ValidateAbsenceDto(AbsenceType type, DateTime? startDate, DateTime? endDate, List<IFormFile>? documents, bool? declarationToDean)
    {
        switch (type)
        {
            case AbsenceType.Sick:
                if (!startDate.HasValue)
                {
                    throw new ArgumentException("Field 'StartDate' is required for 'Sick' absence type.");
                }
                if (declarationToDean == true)
                {
                    throw new ArgumentException("Field 'DeclarationToDean' is not allowed for 'Sick' absence type.");
                }
                break;

            case AbsenceType.Academic:
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    throw new ArgumentException("Field 'StartDate' and 'EndDate' are required for 'Academic' absence type.");
                }
                if (documents == null || documents.Count == 0)
                {
                    throw new ArgumentException("'Academic' absence type requires at least one document.");
                }
                if (declarationToDean == true)
                {
                    throw new ArgumentException("Field 'DeclarationToDean' is not allowed for 'Academic' absence type.");
                }
                break;

            case AbsenceType.Family:
                if ((documents == null || documents.Count == 0) && !(declarationToDean.HasValue && declarationToDean.Value))
                {
                    throw new ArgumentException("Field 'DeclarationToDean' must be true for 'Family' absence type if no documents are provided.");
                }
                break;

            default:
                throw new ArgumentException("Invalid absence type.");
        }
    }
}