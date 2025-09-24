using SecureBank.API.Models.Domain;
using System;

namespace SecureBank.API.Models.DTO
{
    public class TransferDto
    {
        public int TransferId { get; set; }
        public int AccountId { get; set; } // The owner of this transfer
        public string AccountNumber { get; set; }
        public string Name { get; set; } // e.g. sender name
        public string FromAccountNumber { get; set; }
        public string ToAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public string Reference { get; set; }
    }
}
