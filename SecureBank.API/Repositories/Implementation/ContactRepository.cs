using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Repositories.Implementation
{
    public class ContactRepository : IContactRepository
    {
        private readonly BankingContext _context;

        public ContactRepository(BankingContext context)
        {
            _context = context;
        }

        public async Task<List<Contact>> GetContactsAsync()
        {
            return await _context.contacts.ToListAsync();
        }

        public async Task<Contact?> GetByIdAsync(int id)
        {
            return await _context.contacts.FirstOrDefaultAsync(x => x.ContactId == id);
        }

        public async Task<Contact> CreateAsync(Contact contact)
        {
            await _context.contacts.AddAsync(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task<Contact?> UpdateAsync(int id, Contact contact)
        {
            var existing = await _context.contacts.FindAsync(id);
            if (existing == null) return null;

            existing.Name = contact.Name;
            existing.Surname = contact.Surname;
            existing.PhoneNumber = contact.PhoneNumber;
            existing.Subject = contact.Subject;
            existing.Message = contact.Message;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<Contact?> DeleteAsync(int id)
        {
            var contact = await _context.contacts.FindAsync(id);
            if (contact == null) return null;

            _context.contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return contact;
        }
    }
}
