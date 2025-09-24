namespace SecureBank.API.Models.Domain
{
    public class Account
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string AccountType { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual ICollection<BillPayment> BillPayments { get; set; }
        public virtual ICollection<CreditCard> CreditCards { get; set; }
        public virtual ICollection<Loan> Loans { get; set; }
        public virtual ICollection<Investment> Investments { get; set; }
        public virtual ICollection<Transfer> Transfers { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public Account()
        {
            BillPayments = new List<BillPayment>();
            CreditCards = new List<CreditCard>();
            Loans = new List<Loan>();
            Investments = new List<Investment>();
            Transfers = new List<Transfer>();
        }

        public void UpdateBalance(decimal amount)
        {
            Balance += amount;
        }
    }
}
