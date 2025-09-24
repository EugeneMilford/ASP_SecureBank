namespace SecureBank.UI.Models.DTO
{
    public class TransferDto
    {
        public int TransferId { get; set; }
        public string AccountNumber { get; set; } 
        public int AccountId { get; set; } 
        public string Name { get; set; } 
        public string FromAccountNumber { get; set; }
        public string ToAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public string Reference { get; set; }
    }
}
