using tsu_absences_api.DTOs;
using tsu_absences_api.Models;

namespace tsu_absences_api.Interfaces;

public interface IAbsenceService
{
    Task<Absence> CreateAbsenceAsync(Guid userId, CreateAbsenceDto dto);
    Task UpdateAbsenceAsync(Guid userId, UpdateAbsenceDto dto, Guid id, bool isDeanOffice);
    Task<AbsenceDto> GetAbsenceAsync(Guid userId, Guid id, bool isDeanOffice);
}