using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.DTOs;
using tsu_absences_api.Enums;
using tsu_absences_api.Exceptions;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Controllers;

//[Authorize(Roles = "Student")]
[ApiController]
[Route("api/absences")]
public class AbsencesController(IAbsenceService absenceService, /*IUserService userService,*/ IFileService fileService)
    : ControllerBase
{
    private Guid UserId { get; set; } =
        Guid.Parse("033d63fa-d8a8-4675-a583-1acd9cf811e1"); // тестовый Guid пользователя

    private bool IsDeanOffice { get; set; } = true; // тестовый bool

    [HttpPost]
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
            //var userId = userService.GetUserId(User); // TODO: Нужна функция по получению ID пользователя из токена
            var absence = await absenceService.CreateAbsenceAsync(UserId, dto);
            return Ok(absence.Id);
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

    [HttpGet("{id:guid}")]
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
            // var userId = userService.GetUserId(User);  // TODO: Нужна функция по получению ID пользователя из токена
            // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // TODO: Нужна проверка роли

            var absenceDto = await absenceService.GetAbsenceAsync(UserId, id, IsDeanOffice);
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
            // var userId = userService.GetUserId(User);  // TODO: Нужна функция по получению ID пользователя из токена
            // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // TODO: Нужна проверка роли

            await absenceService.UpdateAbsenceAsync(UserId, dto, id, IsDeanOffice);
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
        [FromQuery] int size = 10)
    {
        if (page <= 0 || size <= 0)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid pagination parameters." });

        try
        {
            // var userId = userService.GetUserId(User);  // TODO: Нужна функция по получению ID пользователя из токена
            // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // TODO: Нужна проверка роли

            if (IsDeanOffice)
            {
                var absences = await absenceService.GetAbsencesAsync(filterDto, sorting, page, size);
                return Ok(absences);
            }

            if (User.IsInRole("Student"))
            {
                var absences = await absenceService
                    .GetAbsencesForStudentAsync(UserId, filterDto, sorting, page, size);
                return Ok(absences);
            }

            if (User.IsInRole("Teacher"))
            {
                var absences = await absenceService.GetAbsencesForTeacherAsync(filterDto, sorting, page, size);
                return Ok(absences);
            }

            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
    
    [HttpGet("export")]
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

        // var userId = userService.GetUserId(User);  // TODO: Нужна функция по получению ID пользователя из токена
        // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // TODO: Нужна проверка роли

        try
        {
            if (IsDeanOffice)
            {
                var fileBytes = await absenceService
                    .ExportAbsencesToExcelAsync(filterDto, startDate, endDate, studentIds);
                var fileName = $"Absences_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            } 
            
            if (User.IsInRole("Teacher"))
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
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
    
    [HttpPatch("{id:guid}/approve")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> ApproveAbsence(Guid id)
    {
        // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // TODO: Нужна проверка роли
        
        try
        {
            if (!IsDeanOffice)
                return StatusCode(403, 
                    new ErrorResponse { Status = "403", Message = "You don't have permission to approve absences." });
            
            await absenceService.ApproveAbsenceAsync(id);

            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = "Absence not found." });
        }
        catch (InvalidOperationException ex)
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
        
        // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // TODO: Нужна проверка роли
        
        try
        {
            if (!IsDeanOffice)
                return StatusCode(403, 
                    new ErrorResponse { Status = "403", Message = "You don't have permission to reject absences." });

            await absenceService.RejectAbsenceAsync(id, dto?.Reason);

            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = "Absence not found." });
        }
        catch (InvalidOperationException ex)
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
}