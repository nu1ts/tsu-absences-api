using System.IdentityModel.Tokens.Jwt;
using tsu_absences_api.Data;
using tsu_absences_api.Models;
using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Exceptions;
using System.Security.Claims;

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
                UserRoles = new List<UserRoleMapping>()
            };

            var defaultRole = new UserRoleMapping { UserId = user.Id, Role = UserRole.Student };
            user.UserRoles.Add(defaultRole);
            
            await _dbContext.Users.AddAsync(user);
            await _dbContext.UserRoles.AddAsync(defaultRole);
            await _dbContext.SaveChangesAsync();
            
            var token = _tokenService.GenerateJwtToken(user.Id, user.UserRoles.Select(ur => ur.Role).ToList());
            
            return new TokenResponse { Token = token };
        }

        public async Task<TokenResponse> LoginUser(LoginCredentials credentials)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == credentials.Email);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(credentials.Password, user.Password))
                throw new LoginException();
            
            var token = _tokenService.GenerateJwtToken(user.Id, user.UserRoles.Select(ur => ur.Role).ToList());

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
        
        public async Task<List<UserDto>> GetUsers(string? name, UserRole? role, int page, int size, ClaimsPrincipal user)
        {
            var query = _dbContext.Users
                .Include(u => u.UserRoles)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(u => u.FullName.Contains(name));
                
            if (role.HasValue)
                query = query.Where(u => u.UserRoles.Any(r => r.Role == role.Value));

            return await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Roles = u.UserRoles.Select(ur => ur.Role).ToList()
                })
                .ToListAsync();
        }

        public async Task UpdateUserRoles(Guid id, List<UserRole> newRoles)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new UserException();

            _dbContext.UserRoles.RemoveRange(user.UserRoles);

            user.UserRoles = newRoles.Select(r => new UserRoleMapping
            {
                UserId = user.Id,
                Role = r
            }).ToList();

            await _dbContext.SaveChangesAsync();
        }

    }
}