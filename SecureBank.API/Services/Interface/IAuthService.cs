using System.Security.Claims;

namespace SecureBank.API.Services.Interface
{
    public interface IAuthService
    {
        int GetCurrentUserId(ClaimsPrincipal user);
        string GetCurrentUserRole(ClaimsPrincipal user);
        bool IsAdmin(ClaimsPrincipal user);
        Task<bool> CanAccessAccountAsync(ClaimsPrincipal user, int accountId);
    }
}
