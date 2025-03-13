using System.ComponentModel.DataAnnotations;
using tsu_absences_api.Enums;

namespace tsu_absences_api.DTOs;

public class AbsenceDto
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required]
    public required string StudentName { get; set; }
    public string? Group { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [Required]
    public AbsenceType Type { get; set; }
    [Required]
    public AbsenceStatus Status { get; set; }
}