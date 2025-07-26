namespace SecureBank.API.Models.DTO
{
    public class UpdateAccountRequestDto
    {
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string AccountType { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
