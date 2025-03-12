using System.ComponentModel.DataAnnotations;
using tsu_absences_api.Enums;

namespace tsu_absences_api.DTOs;

public class AbsenceDto
{
    [Required]
    public Guid UserId { get; set; }
    [Required]
    public AbsenceType Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Required]
    public AbsenceStatus Status { get; set; }
    [Required]
    public bool DeclarationToDean { get; set; }
    public List<DocumentDto> Documents { get; set; } = [];
    [Required]
    public DateTime CreatedAt { get; set; }
    [Required]
    public DateTime UpdatedAt { get; set; }
}