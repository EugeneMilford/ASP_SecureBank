namespace SecureBank.API.Models.DTO
{
    public class UserRegisterResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
    }
}
