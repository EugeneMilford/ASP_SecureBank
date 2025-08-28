using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.DTO
{
    public class AddCreditRequestDto
    {
        [Required]
        [MaxLength(16)]
        public string CardNumber { get; set; }

        [Required]
        public decimal CreditLimit { get; set; }

        [Required]
        public decimal CurrentBalance { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public string CardType { get; set; }
    }
}
