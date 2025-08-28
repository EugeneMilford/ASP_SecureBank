using SecureBank.API.Models.Domain;

namespace SecureBank.API.Repositories.Interface
{
    public interface IBillPaymentRepository
    {
        Task<List<BillPayment>> GetBillPaymentsAsync();
        Task<BillPayment?> GetByIdAsync(int id);
        Task<BillPayment> CreateAsync(BillPayment billPayment);
        Task<BillPayment?> UpdateAsync(int id, BillPayment billPayment);
        Task<BillPayment?> DeleteAsync(int id);
    }
}
