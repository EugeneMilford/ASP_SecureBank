namespace SecureBank.UI.Models.DTO
{
    public class AccountDetailsDto
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string AccountType { get; set; }
        public DateTime CreatedDate { get; set; }

        public List<LoanDto> Loans { get; set; } = new();
        public List<CreditCardDto> CreditCards { get; set; } = new();
        public List<BillDto> Bills { get; set; } = new();
        public List<InvestmentDto> Investments { get; set; } = new();
        public List<TransferDto> Transfers { get; set; } = new();
    }
}
