using SecureBank.API.Models.Domain;

namespace SecureBank.API.Models.DTO
{
    public class UserLoginResponseDto
    {
        public User UserDetails { get; set; }
        public string Token { get; set; }
    }
}
