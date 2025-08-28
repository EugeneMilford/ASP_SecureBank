using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class Investment
    {
        [Key]
        public int InvestmentId { get; set; }

        [Required]
        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        public decimal InvestmentAmount { get; set; }

        [Required]
        public string InvestmentType { get; set; } // E.g., "Stocks", "Bonds", "Mutual Funds"

        [Required]
        public decimal CurrentValue { get; set; }

        [Required]
        public DateTime InvestmentDate { get; set; }

        // Method to calculate returns on investment
        public decimal CalculateReturns()
        {
            return CurrentValue - InvestmentAmount; // Simple return calculation
        }
    }
}
