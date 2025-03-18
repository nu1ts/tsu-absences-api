using System.IdentityModel.Tokens.Jwt;
using tsu_absences_api.Data;
using tsu_absences_api.Models;
using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Exceptions;

namespace tsu_absences_api.Services
{
    public class UserService
    {
        private readonly AppDbContext _dbContext;
        private readonly TokenService _tokenService;
        private readonly BlacklistService _blacklistService;

        public UserService(AppDbContext dbContext, TokenService tokenService, BlacklistService blacklistService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _blacklistService = blacklistService;
        }

        public async Task<TokenResponse> RegisterUser(UserRegisterModel model)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Email == model.Email))
                throw new EmailException(model.Email);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = model.FullName,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Email = model.Email,
                GroupId = model.GroupId,
                Roles = new List<UserRole> { UserRole.Student }
            };
            
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            
            var token = _tokenService.GenerateJwtToken(user.Id, user.Roles);
            
            return new TokenResponse { Token = token };
        }

        public async Task<TokenResponse> LoginUser(LoginCredentials credentials)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == credentials.Email);
            if (user == null)
                throw new LoginException();

            if (!BCrypt.Net.BCrypt.Verify(credentials.Password, user.Password))
                throw new LoginException();

            var token = _tokenService.GenerateJwtToken(user.Id, user.Roles);

            return new TokenResponse { Token = token };
        }
        
        public async Task<Response> LogoutUser(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token is required for logout");
            }

            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var expirationDate = jwtToken.ValidTo;

            await _blacklistService.AddToken(token, expirationDate);

            return new Response { Status = "200 OK", Message = "Logged out" };
        }
        
        public async Task<UserDto> GetUserProfile(Guid userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
                throw new UserException();
            
            var userDto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };

            return userDto;
        }
    }
}