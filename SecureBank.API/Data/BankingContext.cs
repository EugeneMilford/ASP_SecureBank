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
        public DbSet<Transfer> transfers { get; set; }
        public DbSet<Contact> contacts { get; set; }
        public DbSet<User> users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .HasMany(a => a.BillPayments)
                .WithOne(b => b.Account)
                .HasForeignKey(b => b.AccountId);

            modelBuilder.Entity<Account>()
                .HasMany(a => a.CreditCards)
                .WithOne(c => c.Account)
                .HasForeignKey(c => c.AccountId);

            modelBuilder.Entity<Account>()
                .HasMany(a => a.Loans)
                .WithOne(l => l.Account)
                .HasForeignKey(l => l.AccountId);

            modelBuilder.Entity<Account>()
                .HasMany(a => a.Transfers)
                .WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Accounts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId);

            // Seeding Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@securebank.com",
                    Username = "admin",
                    Password = "Admin123!",
                    PhoneNumber = "+1234567890",
                    CreatedDate = DateTime.UtcNow,
                    Role = "Admin"
                }
            );
        }
    }
}
