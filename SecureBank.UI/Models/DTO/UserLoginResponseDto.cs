using SecureBank.UI.Models.Users;

namespace SecureBank.UI.Models.DTO
{
    public class UserLoginResponseDto
    {
        public User UserDetails { get; set; }
        public string Token { get; set; }
    }
}
