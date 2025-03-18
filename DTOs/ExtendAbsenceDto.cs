namespace tsu_absences_api.DTOs;
public class ExtendAbsenceDto
{
    public DateTime? NewEndDate { get; set; }
    public List<IFormFile>? Documents { get; set; }
    public bool? DeclarationToDean { get; set; }
    public bool? ApproveImmediately { get; set; }
}