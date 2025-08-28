using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Repositories.Implementation
{
    public class CreditCardRepository : ICreditCardRepository
    {
        private readonly BankingContext _context;

        public CreditCardRepository(BankingContext context)
        {
            _context = context;
        }

        public async Task<List<CreditCard>> GetCreditCardsAsync()
        {
            return await _context.creditCards.ToListAsync();
        }

        public async Task<CreditCard?> GetByIdAsync(int id)
        {
            return await _context.creditCards
                .Include(c => c.Account) // Optional: eager load account
                .FirstOrDefaultAsync(c => c.CreditId == id);
        }

        public async Task<CreditCard> CreateAsync(CreditCard creditCard)
        {
            await _context.creditCards.AddAsync(creditCard);
            await _context.SaveChangesAsync();
            return creditCard;
        }

        public async Task<CreditCard?> UpdateAsync(int id, CreditCard creditCard)
        {
            var existing = await _context.creditCards.FindAsync(id);
            if (existing == null) return null;

            existing.CardNumber = creditCard.CardNumber;
            existing.CreditLimit = creditCard.CreditLimit;
            existing.CurrentBalance = creditCard.CurrentBalance;
            existing.AccountId = creditCard.AccountId;
            existing.ExpiryDate = creditCard.ExpiryDate;
            existing.CardType = creditCard.CardType;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<CreditCard?> DeleteAsync(int id)
        {
            var creditCard = await _context.creditCards.FindAsync(id);
            if (creditCard == null) return null;
            _context.creditCards.Remove(creditCard);
            await _context.SaveChangesAsync();
            return creditCard;
        }
    }
}
