using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public string Type { get; set; } // "Credit" or "Debit"
        public string Description { get; set; }
    }
}
