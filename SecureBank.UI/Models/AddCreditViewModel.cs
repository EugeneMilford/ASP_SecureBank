namespace SecureBank.UI.Models
{
    public class AddCreditViewModel
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public string CardNumber { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CardType { get; set; }
    }
}
