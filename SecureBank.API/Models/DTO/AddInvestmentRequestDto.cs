using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.DTO
{
    public class AddInvestmentRequestDto
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        public decimal InvestmentAmount { get; set; }

        [Required]
        public string InvestmentType { get; set; }

        [Required]
        public decimal CurrentValue { get; set; }

        [Required]
        public DateTime InvestmentDate { get; set; }
    }
}
