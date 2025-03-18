using System.Security.Claims;

namespace tsu_absences_api.Interfaces;

public interface IUserService
{
    Guid GetUserId(ClaimsPrincipal user);
}