using SecureBank.API.Models.Domain;

namespace SecureBank.API.Repositories.Interface
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAccountsAsync();
        Task<Account?> GetByIdAsync(int id);
        Task<Account?> GetByAccountNumberAsync(string accountNumber);
        Task<Account> CreateAsync(Account account);
        Task<Account?> UpdateAsync(int id, Account account);
        Task<Account?> DeleteAsync(int id);
    }
}
