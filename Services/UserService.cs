using System.IdentityModel.Tokens.Jwt;
using tsu_absences_api.Data;
using tsu_absences_api.Models;
using tsu_absences_api.DTOs;
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
            
            query = query.Where(u => !u.UserRoles.Any(r => r.Role == UserRole.Admin));
            
            query = query.OrderBy(u => u.FullName);

            return await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Roles = u.UserRoles.Select(ur => ur.Role).ToList(),
                    GroupId = u.GroupId
                })
                .ToListAsync();
        }

        public async Task<UserDto> GetUserById(Guid id)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new UserException();

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role).ToList(),
                GroupId = user.GroupId
            };
        }

        public async Task DeleteUser(Guid id, ClaimsPrincipal currentUser)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new UserException();

            var currentUserId = Guid.Parse(currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (currentUserId == id)
                throw new ForbiddenAccessException("You cannot delete yourself");

            var currentRoles = currentUser.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => Enum.Parse<UserRole>(c.Value))
                .ToList();

            bool isTargetAdmin = user.UserRoles.Any(r => r.Role == UserRole.Admin);
            bool isCurrentDean = currentRoles.Contains(UserRole.DeanOffice);
            
            if (isCurrentDean && isTargetAdmin)
                throw new ForbiddenAccessException("DeanOffice cannot delete an Admin");

            _dbContext.UserRoles.RemoveRange(user.UserRoles);
            _dbContext.Users.Remove(user); 

            await _dbContext.SaveChangesAsync();
        }

        public async Task<UserDto> UpdateUser(Guid id, UserUpdateDto updatedUser)
        {
             var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new UserException();

            user.FullName = updatedUser.FullName;
            user.Email = updatedUser.Email;
            
            if (!string.IsNullOrEmpty(updatedUser.GroupId))
            {
                user.GroupId = updatedUser.GroupId;
            }

            await _dbContext.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role).ToList(),
                GroupId = user.GroupId
            };
        }


        public async Task UpdateUserRoles(Guid id, List<UserRole> newRoles, ClaimsPrincipal currentUser)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new UserException();

            var currentRoles = currentUser.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => Enum.Parse<UserRole>(c.Value))
                .ToList();

            if (newRoles.Contains(UserRole.Admin) || newRoles.Contains(UserRole.DeanOffice))
            {
                if (!currentRoles.Contains(UserRole.Admin))
                    throw new ForbiddenAccessException("Only Admin can assign Admin or DeanOffice roles");
            }

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