using tsu_absences_api.Models;
using tsu_absences_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace tsu_absences_api.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
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
        [ProducesResponseType(typeof(Response), 200)]
        [ProducesResponseType(typeof(void),401)]
        [ProducesResponseType(typeof(Response), 500)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized(new { message = "User ID not found in token" });

            var user = await _userService.GetUserById(Guid.Parse(userId));
            return Ok(user);
        }
    }
}