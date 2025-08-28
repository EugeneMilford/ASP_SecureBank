using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class CreditCard
    {
        [Key]
        public int CreditId { get; set; }

        [Required]
        [MaxLength(16)]
        public string CardNumber { get; set; }

        [Required]
        public decimal CreditLimit { get; set; }

        [Required]
        public decimal CurrentBalance { get; set; }

        [Required]
        public int AccountId { get; set; }  // Link to Account
        public Account Account { get; set; }  

        public DateTime ExpiryDate { get; set; }
        public string CardType { get; set; } // e.g., "Visa", "MasterCard"

        // Method to process charges and payments to the card
        public void ProcessCharge(decimal amount)
        {
            if (CurrentBalance + amount > CreditLimit)
            {
                throw new InvalidOperationException("Credit limit exceeded");
            }
            CurrentBalance += amount;
        }

        public void ProcessPayment(decimal amount)
        {
            CurrentBalance -= amount;
            Account.UpdateBalance(amount);  // Credit payment back to the account
        }
    }
}
