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

        var now = DateTime.UtcNow;

        var absence = new Absence
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
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
    
    public async Task<AbsenceDetailsDto> GetAbsenceAsync(Guid userId, Guid id, bool isDeanOffice)
    {
        var absence = await context.Absences
            .Where(a => a.Id == id)
            .Select(a => new AbsenceDetailsDto
            {
                UserId = a.UserId,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                Type = a.Type,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Status = a.Status,
                DeclarationToDean = a.DeclarationToDean,
                Documents = context.Documents
                    .Where(d => d.AbsenceId == a.Id)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (absence == null)
        {
            throw new KeyNotFoundException("Absence not found.");
        }

        if (absence.UserId != userId && !isDeanOffice)
        {
            throw new ForbiddenAccessException("You do not have permission to view this absence.");
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

        absence.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }
    
    public async Task<AbsenceListResponse> GetAbsencesAsync(AbsenceFilterDto filterDto, int page, int size, IQueryable<Absence>? query = null)
    {
        query ??= context.Absences.AsQueryable();

        // TODO: Cделать подгрузку из таблицы юзеров
        /*if (!string.IsNullOrEmpty(filterDto.Group))
            query = query.Include(a => a.User).Where(a => a.User.Group.Contains(filterDto.Group));

        if (!string.IsNullOrEmpty(filterDto.StudentName))
            query = query.Include(a => a.User).Where(a => a.User.Name.Contains(filterDto.StudentName));*/

        if (filterDto.Status != null)
            query = query.Where(a => a.Status == filterDto.Status);

        if (filterDto.Type != null)
            query = query.Where(a => a.Type == filterDto.Type);

        query = filterDto.Sorting switch
        {
            AbsenceSorting.CreateDesc => query.OrderByDescending(a => a.CreatedAt),
            AbsenceSorting.CreateAsc => query.OrderBy(a => a.CreatedAt),
            AbsenceSorting.UpdateDesc => query.OrderByDescending(a => a.UpdatedAt),
            AbsenceSorting.UpdateAsc => query.OrderBy(a => a.UpdatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };
        
        var totalAbsences = await query.CountAsync();
        var absences = await query.Skip((page - 1) * size)
                                  .Take(size)
                                  .Select(a => new AbsenceDto
                                  {
                                      Id = a.Id,
                                      UserId = a.UserId,
                                      StudentName = "Иванов Иван Иванович",
                                      //StudentName = a.Student.Name, // TODO: Cделать подгрузку из таблицы юзеров
                                      //Group = a.Student.Group,
                                      CreatedAt = a.CreatedAt,
                                      UpdatedAt = a.UpdatedAt,
                                      Type = a.Type,
                                      Status = a.Status
                                  })
                                  .ToListAsync();

        return new AbsenceListResponse
        {
            Size = size,
            Count = totalAbsences,
            Current = page,
            Absences = absences
        };
    }
    public async Task<AbsenceListResponse> GetAbsencesForStudentAsync(Guid studentId, AbsenceFilterDto filterDto, int page, int size)
    {
        var query = context.Absences.Where(a => a.UserId == studentId);
        
        return await GetAbsencesAsync(filterDto, page, size, query);
    }
    public async Task<AbsenceListResponse> GetAbsencesForTeacherAsync(Guid teacherId, AbsenceFilterDto filterDto, int page, int size)
    {
        var query = context.Absences.Where(a => a.Status == AbsenceStatus.Approved);
        
        return await GetAbsencesAsync(filterDto, page, size, query);
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