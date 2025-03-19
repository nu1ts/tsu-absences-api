using tsu_absences_api.Data;
using tsu_absences_api.Models;
using Microsoft.EntityFrameworkCore;

namespace tsu_absences_api.Services;

public class BlacklistService
{
    private readonly AppDbContext _dbContext;

    public BlacklistService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task AddToken(string token, DateTime expirationDate)
    {
        token = token.Trim(); 
        
        if (await _dbContext.BlacklistedTokens.AnyAsync(t => t.Token == token))
            return;
        
        var blacklistedToken = new BlacklistedToken
        {
            Token = token,
            ExpirationDate = expirationDate
        };

        _dbContext.BlacklistedTokens.Add(blacklistedToken);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<bool> IsTokenBlacklisted(string token)
    {
        token = token.Trim();

        var blacklistedToken = await _dbContext.BlacklistedTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token);

        if (blacklistedToken == null)
        {
            return false;
        }

        if (blacklistedToken.ExpirationDate >= DateTime.UtcNow)
        {
            return true;
        }

        _dbContext.BlacklistedTokens.Remove(blacklistedToken);
        await _dbContext.SaveChangesAsync();
        return false;
    }
}