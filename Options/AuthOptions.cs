using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace tsu_absences_api.Options;

public static class AuthOptions
{
    public const string Issuer = "tsu_absences_api";
    public const string Audience = "tsu_absences_api";
    
    private const string Key = "VN7$g#nLkL59x!P@wYEc&Zu2sQXrbHT3oJ4p8A!mcD9^F*t6"; 

    public static SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        var keyBytes = Encoding.UTF8.GetBytes(Key);
        return new SymmetricSecurityKey(keyBytes);
    }
}