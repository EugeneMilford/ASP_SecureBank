using SecureBank.API.Models.Domain;

namespace SecureBank.API.Repositories.Interface
{
    public interface IInvestmentRepository
    {
        Task<List<Investment>> GetInvestmentsAsync();
        Task<Investment?> GetByIdAsync(int id);
        Task<Investment> CreateAsync(Investment investment);
        Task<Investment?> UpdateAsync(int id, Investment investment);
        Task<Investment?> DeleteAsync(int id);
    }
}
