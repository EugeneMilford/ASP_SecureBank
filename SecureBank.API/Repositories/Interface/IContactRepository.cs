using SecureBank.API.Models.Domain;

namespace SecureBank.API.Repositories.Interface
{
    public interface IContactRepository
    {
        Task<List<Contact>> GetContactsAsync();
        Task<Contact?> GetByIdAsync(int id);
        Task<Contact> CreateAsync(Contact contact);
        Task<Contact?> UpdateAsync(int id, Contact contact);
        Task<Contact?> DeleteAsync(int id);
    }
}
