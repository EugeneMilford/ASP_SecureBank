namespace SecureBank.API.Models.DTO
{
    public class LoanDto
    {
        public int LoanId { get; set; }
        public int AccountId { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime LoanStartDate { get; set; }
        public DateTime LoanEndDate { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsLoanPaidOff { get; set; }
        public decimal MonthlyRepayment { get; set; }
    }
}
