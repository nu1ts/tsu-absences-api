using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.DTOs;
using tsu_absences_api.Exceptions;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(Roles = "Student, DeanOffice")]
public class DocumentsController(IFileService fileService) : ControllerBase
{
    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetDocument(Guid documentId)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Status = "400", Message = "Invalid data provided." });
        
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID.");
            
            var isDeanOffice = User.IsInRole("DeanOffice");
            
            var fileResult = await fileService.GetFileAsync(documentId, userId, isDeanOffice);
            return fileResult;
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(401, new ErrorResponse { Status = "401", Message = ex.Message });
        }
        catch (ForbiddenAccessException ex)
        {
            return StatusCode(403, new ErrorResponse { Status = "403", Message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Status = "404", Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse { Status = "500", Message = "Internal Server Error" });
        }
    }
}