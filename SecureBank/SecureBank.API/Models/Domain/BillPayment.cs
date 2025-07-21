using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class BillPayment
    {
        public int BillPaymentId { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string Biller { get; set; } 
        public string ReferenceNumber { get; set; }
    }
}
