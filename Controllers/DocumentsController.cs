using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.DTOs;
using tsu_absences_api.Exceptions;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Controllers;

[ApiController]
[Route("api/documents")]
//[Authorize(Roles = "Student")]
public class DocumentsController(IFileService fileService) : ControllerBase
{
    private Guid UserId { get; set; } =
        Guid.Parse("033d63fa-d8a8-4675-a583-1acd9cf811e1"); // тестовый Guid пользователя

    private bool IsDeanOffice { get; set; } = true; // тестовый bool
    
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
            // var userId = userService.GetUserId(User); // TODO: Получение ID пользователя из токена
            // var isDeanOffice = userService.HasRole(User, "DeanOffice");  // TODO: Нужна проверка роли
            
            var fileResult = await fileService.GetFileAsync(documentId, UserId, IsDeanOffice);
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