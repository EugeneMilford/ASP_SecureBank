namespace SecureBank.API.Models.DTO
{
    public class BillPaymentDto
    {
        public int BillId { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Biller { get; set; }
        public string ReferenceNumber { get; set; }
    }
}
