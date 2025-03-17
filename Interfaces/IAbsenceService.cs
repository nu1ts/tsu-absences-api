using tsu_absences_api.DTOs;
using tsu_absences_api.Enums;
using tsu_absences_api.Models;

namespace tsu_absences_api.Interfaces;

public interface IAbsenceService
{
    Task<Absence> CreateAbsenceAsync(Guid userId, CreateAbsenceDto dto);
    Task UpdateAbsenceAsync(Guid userId, UpdateAbsenceDto dto, Guid id, bool isDeanOffice);
    Task<AbsenceDetailsDto> GetAbsenceAsync(Guid userId, Guid id, bool isDeanOffice);
    Task<AbsenceListResponse> GetAbsencesAsync(AbsenceFilterDto filterDto, AbsenceSorting sorting, int page, int size, IQueryable<Absence>? query = null);
    Task<AbsenceListResponse>
        GetAbsencesForStudentAsync(Guid studentId, AbsenceFilterDto filterDto, AbsenceSorting sorting, int page, int size);
    Task<AbsenceListResponse>
        GetAbsencesForTeacherAsync(AbsenceFilterDto filterDto, AbsenceSorting sorting, int page, int size);
    Task<byte[]> ExportAbsencesToExcelAsync(AbsenceFilterDto filterDto, DateTime? startDate, DateTime? endDate,
        List<Guid>? studentIds, IQueryable<Absence>? query = null, bool isDeanOffice = true);
    Task<byte[]> ExportAbsencesToExcelForTeacherAsync(AbsenceFilterDto filterDto, DateTime? startDate, DateTime? endDate,
        List<Guid>? studentIds);
    Task ApproveAbsenceAsync(Guid id);
    Task RejectAbsenceAsync(Guid id, string? reason);
    Task ExtendAbsenceAsync(Guid userId, Guid absenceId, ExtendAbsenceDto dto, bool? isDeanOffice = true);
}