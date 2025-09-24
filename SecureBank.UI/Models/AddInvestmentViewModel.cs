namespace SecureBank.UI.Models
{
    public class AddInvestmentViewModel
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal InvestmentAmount { get; set; }
        public string InvestmentType { get; set; }
        public float ExpectedReturn { get; set; }
        public DateTime InvestmentStartDate { get; set; }
        public DateTime MaturityDate { get; set; }
    }
}
