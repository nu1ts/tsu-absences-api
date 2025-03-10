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
    public async Task<IActionResult> CreateAbsence([FromForm] CreateAbsenceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        try
        {
            //var userId = userService.GetUserId(User); // нужна функция по получению Guid пользователя из токена
            var absence = await absenceService.CreateAbsenceAsync(UserId, dto);
            return Ok(absence.Id);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "You are not authorized." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAbsence(Guid id, [FromForm] UpdateAbsenceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        try
        {
            //var userId = userService.GetUserId(User);  // нужна функция по получению Guid пользователя из токена
            //var isDeanOffice = userService.HasRole(User, "DeanOffice");  // нужна функция по проверке роли пользователя

            await absenceService.UpdateAbsenceAsync(id, UserId, IsDeanOffice, dto);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "You are not authorized." });
        }
        catch (ForbiddenAccessException)
        {
            return StatusCode(403, new { error = "You do not have permission to edit this absence." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Absence not found." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}