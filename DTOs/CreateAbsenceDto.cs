using tsu_absences_api.Enums;

namespace tsu_absences_api.DTOs;

public class CreateAbsenceDto
{
    public AbsenceType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool DeclarationToDean { get; set; }
    public List<IFormFile>? Documents { get; set; }
}