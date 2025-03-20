using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.DTOs;
using tsu_absences_api.Enums;
using tsu_absences_api.Exceptions;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Controllers;

[ApiController]
[Route("api/absences")]
public class AbsencesController(IAbsenceService absenceService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Student")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Guid), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> CreateAbsence([FromForm] CreateAbsenceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid data provided." });

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID.");
            
            var absence = await absenceService.CreateAbsenceAsync(userId, dto);
            return Ok(absence.Id);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Status = "400", Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Student, DeanOffice")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AbsenceDetailsDto), 200)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetAbsence(Guid id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID.");
            
            var isDeanOffice = User.IsInRole("DeanOffice");
            
            var absenceDto = await absenceService.GetAbsenceAsync(userId, id, isDeanOffice);
            return Ok(absenceDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (ForbiddenAccessException)
        {
            return StatusCode(403,
                new ErrorResponse { Status = "403", Message = "You don't have permission to view this absence." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = "Absence not found." });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Student, DeanOffice")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> UpdateAbsence(Guid id, [FromForm] UpdateAbsenceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid data provided." });

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID.");
            
            var isDeanOffice = User.IsInRole("DeanOffice");
            
            await absenceService.UpdateAbsenceAsync(userId, dto, id, isDeanOffice);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Status = "400", Message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (ForbiddenAccessException ex)
        {
            return StatusCode(403,
                new ErrorResponse { Status = "403", Message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = "Absence not found." });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Student, DeanOffice, Teacher")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AbsenceListResponse), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetAbsences(
        [FromQuery] AbsenceFilterDto filterDto,
        [FromQuery] AbsenceSorting sorting,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] bool onlyMy = false)
    {
        if (page <= 0 || size <= 0)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid pagination parameters." });

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID.");
            
            var isDeanOffice = User.IsInRole("DeanOffice");
            var isStudent = User.IsInRole("Student");
            var isTeacher = User.IsInRole("Teacher");
            
            AbsenceListResponse? absences;
            
            if (isDeanOffice)
            {
                absences = await absenceService.GetAbsencesAsync(filterDto, sorting, page, size);
                return Ok(absences);
            }

            if (isStudent && !isTeacher)
            {
                absences = await absenceService.GetAbsencesForStudentAsync(userId, filterDto, sorting, page, size);
                return Ok(absences);
            }

            if (isTeacher && !isStudent)
            {
                absences = await absenceService.GetAbsencesForTeacherAsync(filterDto, sorting, page, size);
                return Ok(absences);
            }
            
            absences = await absenceService.GetAbsencesForTeacherAsync(filterDto, sorting, page, size, userId, onlyMy);
            return Ok(absences);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
    
    [HttpGet("export")]
    [Authorize(Roles = "DeanOffice, Teacher")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> ExportAbsences(
        [FromQuery] AbsenceFilterDto filterDto,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<Guid>? studentIds)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid data provided." });

        try
        {
            var isDeanOffice = User.IsInRole("DeanOffice");
            var isTeacher = User.IsInRole("Teacher");

            if (isDeanOffice)
            {
                var fileBytes = await absenceService
                    .ExportAbsencesToExcelAsync(filterDto, startDate, endDate, studentIds);
                var fileName = $"Absences_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            if (isTeacher && !isDeanOffice)
            {
                var fileBytes = await absenceService
                    .ExportAbsencesToExcelForTeacherAsync(filterDto, startDate, endDate, studentIds);
                var fileName = $"Absences_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            return StatusCode(403,
                new ErrorResponse { Status = "403", Message = "You don't have permission to export absences." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Status = "400", Message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (ForbiddenAccessException)
        {
            return StatusCode(403, new ErrorResponse { Status = "403", Message = "You don't have permission to export absences." });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
    
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "DeanOffice")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> ApproveAbsence(Guid id)
    {
        try
        {
            var isDeanOffice = User.IsInRole("DeanOffice");
            
            if (!isDeanOffice)
                return StatusCode(403, 
                    new ErrorResponse { Status = "403", Message = "You don't have permission to approve absences." });
            
            await absenceService.ApproveAbsenceAsync(id);

            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = "Absence not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Status = "400", Message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
    
    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "DeanOffice")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> RejectAbsence(Guid id, [FromBody] RejectAbsenceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid data provided." });
        
        try
        {
            var isDeanOffice = User.IsInRole("DeanOffice");
            
            if (!isDeanOffice)
                return StatusCode(403, 
                    new ErrorResponse { Status = "403", Message = "You don't have permission to reject absences." });

            await absenceService.RejectAbsenceAsync(id, dto.Reason);

            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = "Absence not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Status = "400", Message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
    
    [HttpPatch("{id:guid}/extend")]
    [Authorize(Roles = "Student, DeanOffice")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> ExtendAbsence(Guid id, [FromForm] ExtendAbsenceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid data provided." });

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID.");
        
            var isDeanOffice = User.IsInRole("DeanOffice");
            
            await absenceService.ExtendAbsenceAsync(userId, id, dto, isDeanOffice);

            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (ForbiddenAccessException ex)
        {
            return StatusCode(403, 
                new ErrorResponse { Status = "403", Message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = "Absence not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Status = "400", Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
}