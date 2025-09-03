namespace SecureBank.UI.Models.DTO
{
    public class InvestmentDto
    {
        public int InvestmentId { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal InvestmentAmount { get; set; }
        public string InvestmentType { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime InvestmentDate { get; set; }
        public decimal Returns { get; set; }
    }
}
