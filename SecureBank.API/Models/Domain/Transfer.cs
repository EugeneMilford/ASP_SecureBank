using System;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class Transfer
    {
        [Key]
        public int TransferId { get; set; }

        public int AccountId { get; set; } // The owner of this transfer 
        public Account Account { get; set; }

        // Sender/transfer details
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } // e.g. sender name

        [Required]
        [MaxLength(20)]
        public string FromAccountNumber { get; set; }

        [Required]
        [MaxLength(20)]
        public string ToAccountNumber { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransferDate { get; set; }

        public string Reference { get; set; }
    }
}
