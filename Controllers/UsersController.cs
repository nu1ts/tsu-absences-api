using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.Services;
using tsu_absences_api.Models;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? name,
        [FromQuery] UserRole? role,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var users = await _userService.GetUsers(name, role, page, size, User);
        return Ok(users);
    }

    //...
}
