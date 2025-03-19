using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
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
                RejectionReason = a.RejectionReason,
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
    
    public async Task<AbsenceListResponse> GetAbsencesAsync(AbsenceFilterDto filterDto, AbsenceSorting sorting, int page, int size, IQueryable<Absence>? query = null)
    {
        query ??= context.Absences.AsQueryable();
        
        query = ApplyFilters(query, filterDto);
        query = ApplySorting(query, sorting);
        
        var totalAbsences = await query.CountAsync();
        var absences = await query.Skip((page - 1) * size)
                                  .Take(size)
                                  .Join(context.Users, 
                                      a => a.UserId,
                                      u => u.Id,
                                      (a, u) => new AbsenceDto { 
                                          Id = a.Id,
                                          UserId = a.UserId,
                                          StudentName = u.FullName,
                                          Group = u.GroupId,
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
    public async Task<AbsenceListResponse> GetAbsencesForStudentAsync(
        Guid studentId, 
        AbsenceFilterDto filterDto, 
        AbsenceSorting sorting, 
        int page, 
        int size)
    {
        var query = context.Absences.Where(a => a.UserId == studentId);
        return await GetAbsencesAsync(filterDto, sorting, page, size, query);
    }
    public async Task<AbsenceListResponse> GetAbsencesForTeacherAsync(
        AbsenceFilterDto filterDto, 
        AbsenceSorting sorting, 
        int page, 
        int size, 
        Guid? teacherId,
        bool? onlyMy = false)
    {
        IQueryable<Absence> query;

        if (teacherId == null)
        {
            query = context.Absences
                .Where(a => a.Status == AbsenceStatus.Approved);
        }
        else if (onlyMy == true)
        {
            query = context.Absences
                .Where(a => a.UserId == teacherId);
        }
        else
        {
            query = context.Absences
                .Where(a => a.UserId == teacherId || a.Status == AbsenceStatus.Approved);
        }

        return await GetAbsencesAsync(filterDto, sorting, page, size, query);
    }
    
    public async Task<byte[]> ExportAbsencesToExcelAsync(AbsenceFilterDto filterDto, DateTime? startDate, DateTime? endDate, 
        List<Guid>? studentIds, IQueryable<Absence>? query = null, bool isDeanOffice = true)
    {
        query ??= context.Absences.AsQueryable();
        
        query = ApplyFilters(query, filterDto, startDate, endDate, studentIds);

        var absences = await query
            .Join(context.Users, 
                a => a.UserId,
                u => u.Id,
                (a, u) => new AbsenceExportDto
                {
                    StudentName = u.FullName,
                    Group = u.GroupId,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    Type = a.Type,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Status = a.Status,
                    DeclarationToDean = a.DeclarationToDean
                })
            .ToListAsync();
        
        return GenerateExcelFile(absences, isDeanOffice);
    }
    public async Task<byte[]> ExportAbsencesToExcelForTeacherAsync(AbsenceFilterDto filterDto, DateTime? startDate, DateTime? endDate, 
        List<Guid>? studentIds)
    {
        var query = context.Absences.Where(a => a.Status == AbsenceStatus.Approved);
        return await ExportAbsencesToExcelAsync(filterDto, startDate, endDate, studentIds, query, false);
    }
    private static byte[] GenerateExcelFile(List<AbsenceExportDto> absences, bool isDeanOffice)
    {
        ExcelPackage.License.SetNonCommercialOrganization("Team 18");

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Absences");

        worksheet.Cells[1, 1].Value = "Номер №";
        worksheet.Cells[1, 2].Value = "Студент";
        worksheet.Cells[1, 3].Value = "Группа";
        worksheet.Cells[1, 4].Value = "Тип";
        worksheet.Cells[1, 5].Value = "Дата начала";
        worksheet.Cells[1, 6].Value = "Дата окончания";
        worksheet.Cells[1, 7].Value = "Статус";
        if (isDeanOffice)
        {
            worksheet.Cells[1, 8].Value = "Заявление в деканат";
            worksheet.Cells[1, 9].Value = "Дата создания";
            worksheet.Cells[1, 10].Value = "Дата изменения";
        }
        
        for (var i = 0; i < absences.Count; i++)
        {
            var a = absences[i];
            worksheet.Cells[i + 2, 1].Value = i + 1;
            worksheet.Cells[i + 2, 2].Value = a.StudentName;
            worksheet.Cells[i + 2, 3].Value = a.Group;
            worksheet.Cells[i + 2, 4].Value = a.Type.ToString();
            worksheet.Cells[i + 2, 5].Value = a.StartDate?.ToShortDateString();
            worksheet.Cells[i + 2, 6].Value = a.EndDate?.ToShortDateString();
            worksheet.Cells[i + 2, 7].Value = a.Status.ToString();

            if (!isDeanOffice)
                continue;
            
            worksheet.Cells[i + 2, 8].Value = a.DeclarationToDean?.ToString();
            worksheet.Cells[i + 2, 9].Value = a.CreatedAt.ToShortDateString();
            worksheet.Cells[i + 2, 10].Value = a.UpdatedAt?.ToShortDateString();
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }
    
    public async Task ApproveAbsenceAsync(Guid id)
    {
        var absence = await context.Absences.FindAsync(id);
    
        if (absence == null)
            throw new KeyNotFoundException("Absence not found.");

        if (absence.Status == AbsenceStatus.Approved)
            throw new ArgumentException("Absence is already approved.");

        if (absence.Status != AbsenceStatus.Pending)
            throw new ArgumentException("Absence is in an invalid state for approval.");

        absence.Status = AbsenceStatus.Approved;
    
        await context.SaveChangesAsync();
    }
    
    public async Task RejectAbsenceAsync(Guid id, string? reason)
    {
        var absence = await context.Absences.FindAsync(id);
    
        if (absence == null)
            throw new KeyNotFoundException("Absence not found.");

        if (absence.Status == AbsenceStatus.Rejected)
            throw new ArgumentException("Absence is already rejected.");

        if (absence.Status != AbsenceStatus.Pending)
            throw new ArgumentException("Absence cannot be rejected in its current status.");

        absence.Status = AbsenceStatus.Rejected;
        absence.RejectionReason = reason;

        await context.SaveChangesAsync();
    }
    
    public async Task ExtendAbsenceAsync(Guid userId, Guid id, ExtendAbsenceDto dto, bool isDeanOffice)
    {
        var absence = await context.Absences.FirstOrDefaultAsync(a => a.Id == id);
        
        if (absence == null)
            throw new KeyNotFoundException("Absence not found.");
        
        if (absence.UserId != userId && isDeanOffice != true)
            throw new ForbiddenAccessException("You don't have permission to extend this absence.");

        if (absence.Type == AbsenceType.Academic && isDeanOffice != true)
            throw new ForbiddenAccessException("Only the dean's office can extend academic absences.");

        if (absence.Type != AbsenceType.Sick && absence.Type != AbsenceType.Family && absence.Type != AbsenceType.Academic)
            throw new ArgumentException("This type of absence can't be extended.");
        
        switch (absence.Type)
        {
            case AbsenceType.Sick:
                if (dto.Documents == null || dto.Documents.Count == 0)
                    throw new ArgumentException("At least one new document is required for extending a sick absence.");
                break;

            case AbsenceType.Family:
                if (absence.DeclarationToDean == false && dto.DeclarationToDean == true)
                    break;

                if (dto.Documents?.Count > 0)
                    break;

                if (!dto.DeclarationToDean.HasValue || !dto.DeclarationToDean.Value)
                    throw new ArgumentException("A declaration to the dean is required to extend a family absence.");
                break;

            case AbsenceType.Academic:
                if (isDeanOffice != true)
                    throw new ForbiddenAccessException("Only the dean's office can extend academic absences.");
                break;

            default:
                throw new ArgumentException("Unsupported absence type.");
        }
        
        if (dto.Documents?.Count > 0)
        {
            foreach (var file in dto.Documents)
            {
                var existingDocument = await context.Documents
                    .FirstOrDefaultAsync(d => d.AbsenceId == absence.Id && d.FileName == file.FileName);
                
                if (existingDocument != null)
                    throw new ArgumentException($"File with the name '{file.FileName}' already exists for this absence.");

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

        absence.EndDate = dto.NewEndDate;
        absence.UpdatedAt = DateTime.UtcNow;
        
        if (isDeanOffice && absence.Type == AbsenceType.Academic && dto.ApproveImmediately == true)
            absence.Status = AbsenceStatus.Approved;
        else
            absence.Status = AbsenceStatus.Pending;

        await context.SaveChangesAsync();
    }
    
    private IQueryable<Absence> ApplyFilters(IQueryable<Absence> query, AbsenceFilterDto filterDto,
        DateTime? startDate = null, DateTime? endDate = null, List<Guid>? studentIds = null)
    {
        if (startDate.HasValue || endDate.HasValue)
        {
            query = query.Where(a => 
                (!startDate.HasValue || a.StartDate >= startDate) &&
                (!endDate.HasValue || a.EndDate <= endDate));
        }

        if (studentIds?.Count > 0)
            query = query.Where(a => studentIds.Contains(a.UserId));
    
        if (filterDto.Status != null)
            query = query.Where(a => a.Status == filterDto.Status);

        if (filterDto.Type != null)
            query = query.Where(a => a.Type == filterDto.Type);

        if (!string.IsNullOrEmpty(filterDto.Group))
        {
            query = query.Join(context.Users, 
                    a => a.UserId, 
                    u => u.Id, 
                    (a, u) => new { Absence = a, User = u })
                .Where(x => x.User.GroupId!.Contains(filterDto.Group))
                .Select(x => x.Absence);
        }

        if (!string.IsNullOrEmpty(filterDto.StudentName))
        {
            query = query.Join(context.Users, 
                    a => a.UserId, 
                    u => u.Id, 
                    (a, u) => new { Absence = a, User = u })
                .Where(x => x.User.FullName.Contains(filterDto.StudentName))
                .Select(x => x.Absence);
        }

        return query;
    }
    private static IQueryable<Absence> ApplySorting(IQueryable<Absence> query, AbsenceSorting sorting)
    {
        return sorting switch
        {
            AbsenceSorting.CreateDesc => query.OrderByDescending(a => a.CreatedAt),
            AbsenceSorting.CreateAsc => query.OrderBy(a => a.CreatedAt),
            AbsenceSorting.UpdateDesc => query.OrderByDescending(a => a.UpdatedAt),
            AbsenceSorting.UpdateAsc => query.OrderBy(a => a.UpdatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };
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