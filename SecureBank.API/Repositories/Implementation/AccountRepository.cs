using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Repositories.Implementation
{
    public class AccountRepository : IAccountRepository
    {
        private readonly BankingContext _context;

        public AccountRepository(BankingContext context)
        {
            _context = context;
        }

        public async Task<List<Account>> GetAccountsAsync()
        {
            return await _context.accounts
                .Include(a => a.BillPayments)
                .Include(a => a.Loans)
                .Include(a => a.CreditCards)
                .Include(a => a.Investments)
                .Include(a => a.Transfers)
                .ToListAsync();
        }

        public async Task<Account?> GetByIdAsync(int id)
        {
            return await _context.accounts
                .Include(a => a.BillPayments)
                .Include(a => a.Loans)
                .Include(a => a.CreditCards)
                .Include(a => a.Investments)
                .FirstOrDefaultAsync(x => x.AccountId == id);
        }

        public async Task<Account> CreateAsync(Account account)
        {
            await _context.accounts.AddAsync(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account?> UpdateAsync(int id, Account account)
        {
            var existing = await _context.Set<Account>().FindAsync(id);
            if (existing == null) return null;
            existing.AccountNumber = account.AccountNumber;
            existing.Balance = account.Balance;
            existing.AccountType = account.AccountType;
            existing.CreatedDate = account.CreatedDate;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<Account?> DeleteAsync(int id)
        {
            var account = await _context.Set<Account>().FindAsync(id);
            if (account == null) return null;
            _context.Set<Account>().Remove(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
        {
            return await _context.accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }
    }
}
