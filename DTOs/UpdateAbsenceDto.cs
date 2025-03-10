using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.DTOs;

public class UpdateAbsenceDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? DeclarationToDean { get; set; }
    public List<IFormFile>? Documents { get; set; }
    public List<Guid>? RemovedDocuments { get; set; }
}