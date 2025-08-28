using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Repositories.Implementation
{
    public class InvestmentRepository : IInvestmentRepository
    {
        private readonly BankingContext _context;

        public InvestmentRepository(BankingContext context)
        {
            _context = context;
        }

        public async Task<List<Investment>> GetInvestmentsAsync()
        {
            return await _context.investments.ToListAsync();
        }

        public async Task<Investment?> GetByIdAsync(int id)
        {
            return await _context.investments
                .Include(i => i.Account) // Optional: eager load account
                .FirstOrDefaultAsync(i => i.InvestmentId == id);
        }

        public async Task<Investment> CreateAsync(Investment investment)
        {
            await _context.investments.AddAsync(investment);
            await _context.SaveChangesAsync();
            return investment;
        }

        public async Task<Investment?> UpdateAsync(int id, Investment investment)
        {
            var existing = await _context.investments.FindAsync(id);
            if (existing == null) return null;

            existing.AccountId = investment.AccountId;
            existing.InvestmentAmount = investment.InvestmentAmount;
            existing.InvestmentType = investment.InvestmentType;
            existing.CurrentValue = investment.CurrentValue;
            existing.InvestmentDate = investment.InvestmentDate;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<Investment?> DeleteAsync(int id)
        {
            var investment = await _context.investments.FindAsync(id);
            if (investment == null) return null;
            _context.investments.Remove(investment);
            await _context.SaveChangesAsync();
            return investment;
        }
    }
}
