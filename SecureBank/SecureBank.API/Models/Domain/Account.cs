namespace SecureBank.API.Models.Domain
{
    public class Account
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string AccountType { get; set; }
        public DateTime CreatedDate { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }

        public virtual ICollection<BillPayment> BillPayments { get; set; }

        public virtual ICollection<CreditCard> CreditCards { get; set; }
        public virtual ICollection<Investment> Investments { get; set; }
    }
}
