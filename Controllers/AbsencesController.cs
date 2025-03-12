using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.DTOs;
using tsu_absences_api.Exceptions;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Controllers;

//[Authorize(Roles = "Student")]
[ApiController]
[Route("api/absences")]
public class AbsencesController(IAbsenceService absenceService, /*IUserService userService,*/ IFileService fileService) : ControllerBase
{
    private Guid UserId { get; set; } = Guid.Parse("033d63fa-d8a8-4675-a583-1acd9cf811e1"); // тестовый Guid пользователя
    private bool IsDeanOffice { get; set; } = false; // тестовый bool
    
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
            //var userId = userService.GetUserId(User); // нужна функция по получению Guid пользователя из токена
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
    [ProducesResponseType(typeof(AbsenceDto), 200)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetAbsence(Guid id)
    {
        try
        {
            // var userId = userService.GetUserId(User);  // Получение ID пользователя из токена
            // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // Проверка роли

            var absenceDto = await absenceService.GetAbsenceAsync(UserId, id, IsDeanOffice);
            return Ok(absenceDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse { Status = "401", Message = "You aren't authorized." });
        }
        catch (ForbiddenAccessException)
        {
            return StatusCode(403, new ErrorResponse { Status = "403", Message = "You don't have permission to view this absence." });
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
            //var userId = userService.GetUserId(User);  // нужна функция по получению Guid пользователя из токена
            //var isDeanOffice = userService.HasRole(User, "DeanOffice");  // нужна функция по проверке роли пользователя

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
        catch (ForbiddenAccessException)
        {
            return StatusCode(403, new ErrorResponse { Status = "403", Message = "You don't have permission to edit this absence." });
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
}