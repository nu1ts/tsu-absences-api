namespace tsu_absences_api.DTOs;

public class AbsenceListResponse
{
    public int Size { get; set; }
    public int Count { get; set; }
    public int Current { get; set; }
    public List<AbsenceDto> Absences { get; set; } = [];
}