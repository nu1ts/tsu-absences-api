﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.DTOs;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Controllers;

//[Authorize(Roles = "Student")]
[ApiController]
[Route("api/absences")]
public class AbsencesController(IAbsenceService absenceService, IUserService userService, IFileService fileService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAbsence([FromForm] CreateAbsenceDto dto)
    {
        try
        {
            var userId = userService.GetUserId(User); // нужна функция по получению Guid пользователя из токена
            var absence = await absenceService.CreateAbsenceAsync(userId, dto);
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
}