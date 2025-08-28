using SecureBank.API.Models.Domain;

namespace SecureBank.API.Repositories.Interface
{
    public interface ILoanRepository
    {
        Task<List<Loan>> GetLoansAsync();
        Task<Loan?> GetByIdAsync(int id);
        Task<Loan> CreateAsync(Loan loan);
        Task<Loan?> UpdateAsync(int id, Loan loan);
        Task<Loan?> DeleteAsync(int id);
    }
}
