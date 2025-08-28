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
        public DbSet<CreditCard> creditCards { get; set; }
        public DbSet<BillPayment> billPayments { get; set; }
        public DbSet<Loan> loans { get; set; }
        public DbSet<Investment> investments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CreditCard>()
                .HasOne(c => c.Account);

            modelBuilder.Entity<BillPayment>()
                .HasOne(c => c.Account);

            modelBuilder.Entity<Loan>()
                .HasOne(c => c.Account);

            modelBuilder.Entity<Investment>()
                .HasOne(c => c.Account);
        }
    }
}
