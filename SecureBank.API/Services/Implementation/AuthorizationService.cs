using SecureBank.API.Repositories.Interface;
using SecureBank.API.Services.Interface;
using System.Security.Claims;

namespace SecureBank.API.Services.Implementation
{
    public class AuthorizationService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;

        public AuthorizationService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public int GetCurrentUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        public string GetCurrentUserRole(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        public bool IsAdmin(ClaimsPrincipal user)
        {
            return GetCurrentUserRole(user) == "Admin";
        }

        public async Task<bool> CanAccessAccountAsync(ClaimsPrincipal user, int accountId)
        {
            // Admins can access everything
            if (IsAdmin(user))
                return true;

            // Regular users can only access their own accounts
            var userId = GetCurrentUserId(user);
            var account = await _accountRepository.GetByIdWithOwnerAsync(accountId);

            return account != null && account.UserId == userId;
        }
    }
}
