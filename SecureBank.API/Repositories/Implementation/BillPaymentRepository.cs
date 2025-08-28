using Microsoft.EntityFrameworkCore;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Repositories.Implementation
{
    public class BillPaymentRepository : IBillPaymentRepository
    {
        private readonly BankingContext _context;

        public BillPaymentRepository(BankingContext context)
        {
            _context = context;
        }

        public async Task<List<BillPayment>> GetBillPaymentsAsync()
        {
            return await _context.billPayments.ToListAsync();
        }

        public async Task<BillPayment?> GetByIdAsync(int id)
        {
            return await _context.billPayments.FirstOrDefaultAsync(x => x.BillId == id);
        }

        public async Task<BillPayment> CreateAsync(BillPayment billPayment)
        {
            await _context.billPayments.AddAsync(billPayment);
            await _context.SaveChangesAsync();
            return billPayment;
        }

        public async Task<BillPayment?> UpdateAsync(int id, BillPayment billPayment)
        {
            var existing = await _context.billPayments.FindAsync(id);
            if (existing == null) return null;

            existing.AccountId = billPayment.AccountId;
            existing.Amount = billPayment.Amount;
            existing.PaymentDate = billPayment.PaymentDate;
            existing.Biller = billPayment.Biller;
            existing.ReferenceNumber = billPayment.ReferenceNumber;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<BillPayment?> DeleteAsync(int id)
        {
            var billPayment = await _context.billPayments.FindAsync(id);
            if (billPayment == null) return null;
            _context.billPayments.Remove(billPayment);
            await _context.SaveChangesAsync();
            return billPayment;
        }
    }

}
