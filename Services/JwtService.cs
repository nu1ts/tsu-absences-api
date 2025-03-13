using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Добавьте это пространство имен
using Microsoft.IdentityModel.Tokens;

namespace tsu_absences_api.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger; // Объявляем переменную для логгера

        public JwtService(IConfiguration config, ILogger<JwtService> logger) // Внедряем ILogger через конструктор
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Проверка на null для безопасности
        }

        public string GenerateToken(Guid userId, string email, List<string> roles)
        {
            try 
            {
                var secretKey = _config["JwtSettings:Secret"];
                var issuer = _config["JwtSettings:Issuer"];
                var audience = _config["JwtSettings:Audience"];
                var issuedAt = DateTime.UtcNow;
                var expiresAt = issuedAt.AddMinutes(60);
                var expirationMinutes = int.Parse(_config["JwtSettings:ExpirationMinutes"]);

                if (string.IsNullOrEmpty(secretKey))
                    throw new InvalidOperationException("JWT Secret Key is not configured.");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new (JwtRegisteredClaimNames.Nbf, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new (JwtRegisteredClaimNames.Exp, new DateTimeOffset(expiresAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new (JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new (JwtRegisteredClaimNames.Iss, issuer),
                    new (JwtRegisteredClaimNames.Aud, audience)
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

               var tokenDescriptor = new JwtSecurityToken(
                   issuer: issuer,
                   audience: audience,
                   claims: claims,
                   expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                   signingCredentials: credentials
               );

               return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"An error occurred while generating the JWT.");
                throw; 
            }
        }
    }
}
