using System.ComponentModel.DataAnnotations;
using tsu_absences_api.Enums;

namespace tsu_absences_api.DTOs;

public class AbsenceExportDto
{
    [Required]
    public required string StudentName { get; set; }
    public string? Group { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [Required]
    public AbsenceType Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Required]
    public AbsenceStatus Status { get; set; }
    public bool? DeclarationToDean { get; set; }
}