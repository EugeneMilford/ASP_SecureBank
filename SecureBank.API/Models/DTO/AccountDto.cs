namespace SecureBank.API.Models.DTO
{
    public class AccountDto
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public List<LoanDto> Loans { get; set; } = new();
        public List<CreditCardDto> CreditCards { get; set; } = new();
        public List<BillPaymentDto> Bills { get; set; } = new();
        public List<InvestmentDto> Investments { get; set; } = new();
    }
}
