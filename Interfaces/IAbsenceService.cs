using tsu_absences_api.DTOs;
using tsu_absences_api.Models;

namespace tsu_absences_api.Interfaces;

public interface IAbsenceService
{
    Task<Absence> CreateAbsenceAsync(Guid userId, CreateAbsenceDto dto);
    Task UpdateAbsenceAsync(Guid id, Guid userId, bool isDeanOffice, UpdateAbsenceDto dto);
}