using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Repositories.Implementation
{
    public class LoanRepository : ILoanRepository
    {
        private readonly BankingContext _context;

        public LoanRepository(BankingContext context)
        {
            _context = context;
        }

        public async Task<List<Loan>> GetLoansAsync()
        {
            return await _context.loans.ToListAsync();
        }

        public async Task<Loan?> GetByIdAsync(int id)
        {
            return await _context.loans.FirstOrDefaultAsync(x => x.LoanId == id);
        }

        public async Task<Loan> CreateAsync(Loan loan)
        {
            await _context.loans.AddAsync(loan);
            await _context.SaveChangesAsync();
            return loan;
        }

        public async Task<Loan?> UpdateAsync(int id, Loan loan)
        {
            var existing = await _context.loans.FindAsync(id);
            if (existing == null) return null;

            existing.AccountId = loan.AccountId;
            existing.LoanAmount = loan.LoanAmount;
            existing.InterestRate = loan.InterestRate;
            existing.LoanStartDate = loan.LoanStartDate;
            existing.LoanEndDate = loan.LoanEndDate;
            existing.RemainingAmount = loan.RemainingAmount;
            existing.IsLoanPaidOff = loan.IsLoanPaidOff;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<Loan?> DeleteAsync(int id)
        {
            var loan = await _context.loans.FindAsync(id);
            if (loan == null) return null;
            _context.loans.Remove(loan);
            await _context.SaveChangesAsync();
            return loan;
        }
    }
}
