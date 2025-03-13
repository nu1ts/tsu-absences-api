using tsu_absences_api.Data;
using tsu_absences_api.DTOs;
using tsu_absences_api.Models;
using tsu_absences_api.Services;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace AttendanceSystem.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AccountController(AppDbContext context, JwtService jwtService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            GroupId = registerDto.GroupId,
            Roles = [new() { Role = Role.Student }] 
        };


            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Пользователь успешно зарегистрирован.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Неверный email или пароль.");
            }
            
            var roles = user.Roles.Select(r => r.ToString()).ToList();
            var token = _jwtService.GenerateToken(user.Id, user.Email, roles);

            return Ok(new { Token = token });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaimValue == null || !Guid.TryParse(userIdClaimValue, out Guid userId))
            { 
                return Unauthorized("Токен не содержит идентификатор пользователя.");
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return Ok(new { user.Id, user.FullName, user.Email ,user.Roles, user.GroupId });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok("Этот метод доступен только администраторам.");
        }
    }
}