using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecureBank.API.Repositories.Implementation
{
    public class TransferRepository : ITransferRepository
    {
        private readonly BankingContext _context;

        public TransferRepository(BankingContext context)
        {
            _context = context;
        }

        public async Task<List<Transfer>> GetTransfersAsync()
        {
            return await _context.transfers
                .Include(bp => bp.Account)
                .ToListAsync();
        }

        public async Task<Transfer?> GetByIdAsync(int id)
        {
            return await _context.transfers.FirstOrDefaultAsync(x => x.TransferId == id);
        }

        public async Task<Transfer> CreateAsync(Transfer transfer)
        {
            await _context.transfers.AddAsync(transfer);
            await _context.SaveChangesAsync();
            return transfer;
        }

        public async Task<Transfer?> UpdateAsync(int id, Transfer transfer)
        {
            var existing = await _context.transfers.FindAsync(id);
            if (existing == null) return null;

            existing.AccountId = transfer.AccountId;
            existing.Name = transfer.Name;
            existing.FromAccountNumber = transfer.FromAccountNumber;
            existing.ToAccountNumber = transfer.ToAccountNumber;
            existing.Amount = transfer.Amount;
            existing.TransferDate = transfer.TransferDate;
            existing.Reference = transfer.Reference;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<Transfer?> DeleteAsync(int id)
        {
            var transfer = await _context.transfers.FindAsync(id);
            if (transfer == null) return null;
            _context.transfers.Remove(transfer);
            await _context.SaveChangesAsync();
            return transfer;
        }
    }
}
