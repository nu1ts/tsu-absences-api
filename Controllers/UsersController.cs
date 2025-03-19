using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tsu_absences_api.Services;
using tsu_absences_api.Models;
using tsu_absences_api.DTOs;

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
    [Authorize(Roles = "Admin, DeanOffice, Teacher")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? name,
        [FromQuery] UserRole? role,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var users = await _userService.GetUsers(name, role, page, size, User);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin, DeanOffice")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetUserById(id);
        return Ok(user);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin, DeanOffice")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _userService.DeleteUser(id, User);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin, DeanOffice")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto updatedUser)
    {
        var user = await _userService.UpdateUser(id, updatedUser);
        return Ok(user);
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin, DeanOffice")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] List<UserRole> roles)
    {
        await _userService.UpdateUserRoles(id, roles, User);
        return Ok(new { Message = "User roles updated successfully" });
    }
}
