using SecureBank.API.Models.Domain;

namespace SecureBank.API.Repositories.Interface
{
    public interface ICreditCardRepository
    {
        Task<List<CreditCard>> GetCreditCardsAsync();
        Task<CreditCard?> GetByIdAsync(int id);
        Task<CreditCard> CreateAsync(CreditCard creditCard);
        Task<CreditCard?> UpdateAsync(int id, CreditCard creditCard);
        Task<CreditCard?> DeleteAsync(int id);
    }
}
