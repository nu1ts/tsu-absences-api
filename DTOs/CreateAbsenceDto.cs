using System.ComponentModel.DataAnnotations;
using tsu_absences_api.Enums;

namespace tsu_absences_api.DTOs;

public class CreateAbsenceDto
{
    [Required]
    public AbsenceType Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Required]
    public bool DeclarationToDean { get; set; } = false;
    public List<IFormFile>? Documents { get; set; }
}