using System;

namespace SecureBank.API.Models.DTO
{
    public class AddTransferRequestDto
    {
        public int AccountId { get; set; } // The owner of this transfer
        public string Name { get; set; } // e.g. sender name
        public string FromAccountNumber { get; set; } // The owner's Account
        public string ToAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public string Reference { get; set; }
    }
}