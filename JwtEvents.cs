using System.IdentityModel.Tokens.Jwt;
using tsu_absences_api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace tsu_absences_api;

public class JwtEvents : JwtBearerEvents
{
    private readonly BlacklistService _blacklistService;

    public JwtEvents(BlacklistService blacklistService)
    {
        _blacklistService = blacklistService;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        if (context.SecurityToken is JwtSecurityToken { RawData: var tokenRaw } 
            && await _blacklistService.IsTokenBlacklisted(tokenRaw))
        {
            context.Fail("Access denied: Token is banned");
        }
    }
}