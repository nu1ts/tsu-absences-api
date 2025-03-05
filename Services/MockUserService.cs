using System.Security.Claims;
using tsu_absences_api.Interfaces;

namespace tsu_absences_api.Services;

public class MockUserService : IUserService
{
    public Guid GetUserId(ClaimsPrincipal user)
    {
        return Guid.NewGuid();
    }
}