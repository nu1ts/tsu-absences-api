using tsu_absences_api.Data;
using tsu_absences_api.DTOs;
using tsu_absences_api.Models;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace AttendanceSystem.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerDto)
        {
            if (_context.Users.Any(u => u.Email == registerDto.Email))
            {
                return BadRequest("Пользователь с таким email уже существует.");
            }

            var user = new User
            {
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Пользователь успешно зарегистрирован.");
        }
    }
}