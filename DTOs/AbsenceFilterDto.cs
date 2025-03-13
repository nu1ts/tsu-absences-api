using System.ComponentModel.DataAnnotations;
using tsu_absences_api.Enums;

namespace tsu_absences_api.DTOs;

public class AbsenceFilterDto
{
    public AbsenceStatus? Status { get; set; }
    public AbsenceType? Type { get; set; }
    public string? Group { get; set; }
    public string? StudentName { get; set; }
    public AbsenceSorting Sorting { get; set; }
}