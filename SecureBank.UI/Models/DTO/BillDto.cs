namespace SecureBank.UI.Models.DTO
{
    public class BillDto
    {
        public int BillId { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Biller { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReferenceNumber { get; set; }
    }
}
