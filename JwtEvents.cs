using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using tsu_absences_api.Services;

public class JwtEvents : JwtBearerEvents
{
    private readonly BlacklistService _blacklistService;

    public JwtEvents(BlacklistService blacklistService)
    {
        _blacklistService = blacklistService;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        string? tokenRaw = (context.SecurityToken as JwtSecurityToken)?.RawData;

        if (string.IsNullOrEmpty(tokenRaw) &&
            context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            tokenRaw = authHeader.ToString().Replace("Bearer ", "").Trim();
        }

        if (string.IsNullOrEmpty(tokenRaw))
        {
            context.Fail("Access denied: No valid token provided");
            return;
        }

        if (await _blacklistService.IsTokenBlacklisted(tokenRaw))
        {
            context.Fail("Access denied: Token is blacklisted");
            return;
        }
    }
}
