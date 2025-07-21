using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class Investment
    {
        public int InvestmentId { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public decimal InvestmentAmount { get; set; }

        public string InvestmentType { get; set; } // E.g., "Stocks", "Bonds", "Mutual Funds"

        public decimal CurrentValue { get; set; }

        public DateTime InvestmentDate { get; set; }
    }
}
