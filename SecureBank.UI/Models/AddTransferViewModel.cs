namespace SecureBank.UI.Models
{
    public class AddTransferViewModel
    {
        public int AccountId { get; set; } 
        public string Name { get; set; } // e.g. sender name
        public string AccountNumber { get; set; }
        public string FromAccountNumber { get; set; }
        public string ToAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; } = DateTime.Now; // Default to current date
        public string Reference { get; set; }
    }
}
