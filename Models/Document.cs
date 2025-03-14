using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.Models;

public class Document
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public Guid AbsenceId { get; set; }
    [Required]
    [StringLength(255)]
    public required string FileName { get; set; }
    [Required]
    [StringLength(1000)]
    public required string FilePath { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}