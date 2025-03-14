using tsu_absences_api.DTOs;
using tsu_absences_api.Models;

namespace tsu_absences_api.Interfaces;

public interface IAbsenceService
{
    Task<Absence> CreateAbsenceAsync(Guid userId, CreateAbsenceDto dto);
    Task UpdateAbsenceAsync(Guid userId, UpdateAbsenceDto dto, Guid id, bool isDeanOffice);
    Task<AbsenceDetailsDto> GetAbsenceAsync(Guid userId, Guid id, bool isDeanOffice);
    Task<AbsenceListResponse> GetAbsencesAsync(AbsenceFilterDto filterDto, int page, int size, IQueryable<Absence>? query = null);

    Task<AbsenceListResponse>
        GetAbsencesForStudentAsync(Guid studentId, AbsenceFilterDto filterDto, int page, int size);

    Task<AbsenceListResponse>
        GetAbsencesForTeacherAsync(Guid teacherId, AbsenceFilterDto filterDto, int page, int size);
}