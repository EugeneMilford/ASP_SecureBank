namespace SecureBank.UI.Models
{
    public class AddLoanViewModel
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal LoanAmount { get; set; }
        public float InterestRate { get; set; }
        public DateTime LoanStartDate { get; set; }
        public DateTime LoanEndDate { get; set; }
    }
}
