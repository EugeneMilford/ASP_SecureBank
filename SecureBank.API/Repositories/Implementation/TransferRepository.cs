using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
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
                .Include(t => t.Account)
                .ToListAsync();
        }

        public async Task<List<Transfer>> GetTransfersByUserIdAsync(int userId)
        {
            return await _context.transfers
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId)
                .ToListAsync();
        }

        public async Task<Transfer?> GetByIdAsync(int id)
        {
            return await _context.transfers
                .FirstOrDefaultAsync(x => x.TransferId == id);
        }

        public async Task<Transfer?> GetByIdWithAccountAsync(int id)
        {
            return await _context.transfers
                .Include(t => t.Account)
                .FirstOrDefaultAsync(x => x.TransferId == id);
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

        // Atomic operation: move funds between accounts and create transfer record
        public async Task<Transfer> CreateTransferWithAccountUpdateAsync(
            int senderAccountId,
            string fromAccountNumber,
            string toAccountNumber,
            decimal amount,
            DateTime transferDate,
            string name,
            string reference,
            int currentUserId)
        {
            // Load sender account (no tracking required beyond updates)
            var sender = await _context.accounts
                .FirstOrDefaultAsync(a => a.AccountId == senderAccountId);

            if (sender == null)
                throw new KeyNotFoundException("Sender account not found.");

            // Verify account number matches
            if (!string.Equals(sender.AccountNumber, fromAccountNumber, StringComparison.Ordinal))
                throw new InvalidOperationException("Sender account number does not match the provided account ID.");

            // Ownership check
            if (sender.UserId != currentUserId)
                throw new UnauthorizedAccessException("You do not own the sender account.");

            if (amount <= 0)
                throw new InvalidOperationException("Transfer amount must be greater than zero.");

            if (sender.Balance < amount)
                throw new InvalidOperationException("Insufficient funds.");

            // Load recipient by account number
            var recipient = await _context.accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == toAccountNumber);

            if (recipient == null)
                throw new KeyNotFoundException("Recipient account not found.");

            if (string.Equals(fromAccountNumber, toAccountNumber, StringComparison.Ordinal))
                throw new InvalidOperationException("Cannot transfer to the same account.");

            // Use explicit DB transaction for atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update balances
                sender.Balance -= amount;
                recipient.Balance += amount;

                _context.accounts.Update(sender);
                _context.accounts.Update(recipient);

                var transfer = new Transfer
                {
                    AccountId = senderAccountId,
                    Name = name,
                    FromAccountNumber = fromAccountNumber,
                    ToAccountNumber = toAccountNumber,
                    Amount = amount,
                    TransferDate = transferDate,
                    Reference = reference
                };

                await _context.transfers.AddAsync(transfer);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // attach account navigation for convenience
                transfer.Account = sender;
                return transfer;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}