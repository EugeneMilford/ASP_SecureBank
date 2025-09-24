using SecureBank.API.Models.Domain;

namespace SecureBank.API.Repositories.Interface
{
    public interface ITransferRepository
    {
        Task<List<Transfer>> GetTransfersAsync();
        Task<Transfer?> GetByIdAsync(int id);
        Task<Transfer> CreateAsync(Transfer transfer);
        Task<Transfer?> UpdateAsync(int id, Transfer transfer);
        Task<Transfer?> DeleteAsync(int id);
    }
}
