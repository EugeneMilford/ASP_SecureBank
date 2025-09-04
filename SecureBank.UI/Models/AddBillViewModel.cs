namespace SecureBank.UI.Models
{
    public class AddBillViewModel
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Biller { get; set; }
        public string ReferenceNumber { get; set; }
    }
}
