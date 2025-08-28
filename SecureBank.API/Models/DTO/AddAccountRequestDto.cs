namespace SecureBank.API.Models.DTO
{
    public class AddAccountRequestDto
    {
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; } // initial balance
        public string AccountType { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
