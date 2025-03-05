using tsu_absences_api.Data;
using tsu_absences_api.DTOs;
using tsu_absences_api.Enums;
using tsu_absences_api.Interfaces;
using tsu_absences_api.Models;

namespace tsu_absences_api.Services;

public class AbsenceService(AppDbContext context, IFileService fileService) : IAbsenceService
{
    public async Task<Absence> CreateAbsenceAsync(Guid userId, CreateAbsenceDto dto)
    {
        if (dto.Type != AbsenceType.Family && dto.DeclarationToDean)
        {
            throw new Exception("Заявление в деканат доступно только для типа заявки 'Семейные обстоятельства'.");
        }
        
        switch (dto.Type)
        {
            case AbsenceType.Academic:
                if (dto.Documents == null || dto.Documents.Count == 0)
                {
                    throw new Exception("Для учебного пропуска обязателен документ.");
                }
                break;

            case AbsenceType.Family:
                if (!dto.DeclarationToDean)
                {
                    throw new Exception("Для семейных обстоятельств необходимо подать заявление в деканат.");
                }
                break;

            case AbsenceType.Sick:
                break;

            default:
                throw new Exception("Неверный тип отсутствия.");
        }

        var absence = new Absence
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = dto.Type,
            StartDate = dto.StartDate,
            EndDate = dto.Type == AbsenceType.Sick ? null : dto.EndDate,
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
}