using System.ComponentModel.DataAnnotations;

namespace tsu_absences_api.DTOs;

public class ErrorResponse
{
    public string? Status { get; set; }
    public string? Message { get; set; }
}