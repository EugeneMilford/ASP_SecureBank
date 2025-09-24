namespace SecureBank.API.Models.DTO
{
    public class AddContactRequestDto
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}
