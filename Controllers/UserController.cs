using tsu_absences_api.Models;
using tsu_absences_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace blog_api.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }
        
        [HttpPost("register")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenResponse),200)]
        [ProducesResponseType(typeof(void),400)]
        [ProducesResponseType(typeof(Response),500)]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var tokenResponse = await _userService.RegisterUser(model);

            return Ok(tokenResponse);
        }
        
        [HttpPost("login")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenResponse),200)]
        [ProducesResponseType(typeof(Response),400)]
        [ProducesResponseType(typeof(Response),500)]
        public async Task<IActionResult> Login([FromBody] LoginCredentials model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var tokenResponse = await _userService.LoginUser(model);

            return Ok(tokenResponse);
        }
        
        [HttpPost("logout")]
        [Authorize]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Response), 200)]
        [ProducesResponseType(typeof(void),401)]
        [ProducesResponseType(typeof(Response), 500)]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var response = await _userService.LogoutUser(token);

            return Ok(response);
        }
        
        [HttpGet("profile")]
        [Authorize]
        [Produces("application/json")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(typeof(void), 401)]
        [ProducesResponseType(typeof(Response), 500)]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.Claims.FirstOrDefault(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _userService.GetUserProfile(userId);

            return Ok(user);
        }

        /*[HttpGet("roles")]
        [Authorize]
        public IActionResult GetRoles()
        {
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Ok(roles);
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok("This method is accessible only by Admins.");
        }*/
    }
}