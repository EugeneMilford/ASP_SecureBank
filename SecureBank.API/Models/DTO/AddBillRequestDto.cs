namespace SecureBank.API.Models.DTO
{
    public class AddBillRequestDto
    {
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Biller { get; set; }
        public string ReferenceNumber { get; set; }
    }
}
