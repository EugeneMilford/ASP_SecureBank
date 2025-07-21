using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class CreditCard
    {
        public int CreditCardId { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public string CardNumber { get; set; }

        public decimal CreditLimit { get; set; }

        public decimal CurrentBalance { get; set; }

        public DateTime ExpiryDate { get; set; }
        public string CardType { get; set; } // e.g., "Visa", "MasterCard"
    }
}
