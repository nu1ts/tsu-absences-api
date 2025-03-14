using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.DTOs;

public class DocumentDto
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public required string FileName { get; set; }
}