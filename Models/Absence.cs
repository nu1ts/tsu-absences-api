using System.ComponentModel.DataAnnotations;
using tsu_absences_api.Enums;

namespace tsu_absences_api.Models;

public class Absence
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public AbsenceType Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Required]
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Pending;
    [Required]
    public bool DeclarationToDean { get; set; }
    [StringLength(500)]
    public string? RejectionReason { get; set; }
    public List<Guid> Documents { get; set; } = [];
}