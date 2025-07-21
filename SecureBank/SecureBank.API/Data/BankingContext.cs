using Microsoft.EntityFrameworkCore;
using SecureBank.API.Models.Domain;

namespace SecureBank.API.Data
{
    public class BankingContext : DbContext
    {
        public BankingContext(DbContextOptions<BankingContext> options)
            : base(options)
        {
        }

        public DbSet<Account> accounts { get; set; }
        public DbSet<Investment> investments { get; set; }
        public DbSet<Transaction> transactions { get; set; }
        public DbSet<CreditCard> creditCards { get; set; }
        public DbSet<BillPayment> billPayments { get; set; }
    }
}
