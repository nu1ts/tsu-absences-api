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

    public async Task UpdateAbsenceAsync(Guid id, Guid userId, bool isDeanOffice, UpdateAbsenceDto dto)
    {
        var absence = await context.Absences.FindAsync(id);

        if (absence == null)
        {
            throw new KeyNotFoundException("Absence not found.");
        }

        if (absence.Status != AbsenceStatus.Pending && !(isDeanOffice && absence.Type == AbsenceType.Academic))
        {
            throw new ForbiddenAccessException("You do not have permission to edit this absence.");
        }

        if (absence.UserId != userId && !isDeanOffice)
        {
            throw new ForbiddenAccessException("You do not have permission to edit this absence.");
        }

        ValidateAbsenceDto(absence.Type, dto.StartDate, dto.EndDate, dto.Documents, dto.DeclarationToDean);

        absence.StartDate = dto.StartDate;
        absence.EndDate = dto.EndDate;

        if (dto.DeclarationToDean.HasValue)
            absence.DeclarationToDean = dto.DeclarationToDean.Value;
        
        if (dto.RemovedDocuments?.Count > 0)
        {
            foreach (var documentId in dto.RemovedDocuments)
            {
                await fileService.DeleteFileAsync(documentId, userId, isDeanOffice);
                absence.Documents.Remove(documentId);
            }
        }
        
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
                    throw new ArgumentException("Start date is required for sick absences.");
                }
                break;

            case AbsenceType.Academic:
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    throw new ArgumentException("Start and end dates are required for academic absences.");
                }
                if (documents == null || documents.Count == 0)
                {
                    throw new ArgumentException("Academic absences require at least one document.");
                }
                break;

            case AbsenceType.Family:
                if ((documents == null || documents.Count == 0) && !(declarationToDean.HasValue && declarationToDean.Value))
                {
                    throw new ArgumentException("For family absences, 'DeclarationToDean' must be true if no documents are provided.");
                }
                break;

            default:
                throw new ArgumentException("Invalid absence type.");
        }
    }
}