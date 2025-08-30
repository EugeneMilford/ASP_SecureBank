using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillPaymentsController : ControllerBase
    {
        private readonly IBillPaymentRepository _billPaymentRepository;
        private readonly IAccountRepository _accountRepository;

        public BillPaymentsController(IBillPaymentRepository billPaymentRepository, IAccountRepository accountRepository)
        {
            _billPaymentRepository = billPaymentRepository;
            _accountRepository = accountRepository;
        }

        // GET: api/BillPayments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillPaymentDto>>> GetBillPayments()
        {
            var billPayments = await _billPaymentRepository.GetBillPaymentsAsync();
            var dtos = billPayments.Select(bp => new BillPaymentDto
            {
                BillId = bp.BillId,
                AccountId = bp.AccountId,
                Amount = bp.Amount,
                PaymentDate = bp.PaymentDate,
                Biller = bp.Biller,
                ReferenceNumber = bp.ReferenceNumber
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/BillPayments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BillPaymentDto>> GetBillPayment(int id)
        {
            var billPayment = await _billPaymentRepository.GetByIdAsync(id);
            if (billPayment == null)
                return NotFound();

            var dto = new BillPaymentDto
            {
                BillId = billPayment.BillId,
                AccountId = billPayment.AccountId,
                Amount = billPayment.Amount,
                PaymentDate = billPayment.PaymentDate,
                Biller = billPayment.Biller,
                ReferenceNumber = billPayment.ReferenceNumber
            };
            return Ok(dto);
        }

        // POST: api/BillPayments
        [HttpPost]
        public async Task<ActionResult<BillPaymentDto>> AddBillPayment([FromBody] AddBillRequestDto request)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
                return BadRequest("Account not found.");

            // Deduct payment amount from account balance
            if (account.Balance < request.Amount)
                return BadRequest("Insufficient funds.");

            account.Balance -= request.Amount;
            await _accountRepository.UpdateAsync(account.AccountId, account);

            var billPayment = new BillPayment
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate,
                Biller = request.Biller,
                ReferenceNumber = request.ReferenceNumber
            };

            var created = await _billPaymentRepository.CreateAsync(billPayment);

            var dto = new BillPaymentDto
            {
                BillId = created.BillId,
                AccountId = created.AccountId,
                Amount = created.Amount,
                PaymentDate = created.PaymentDate,
                Biller = created.Biller,
                ReferenceNumber = created.ReferenceNumber
            };

            return CreatedAtAction(nameof(GetBillPayment), new { id = created.BillId }, dto);
        }

        // PUT: api/BillPayments/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<BillPaymentDto>> UpdateBillPayment(int id, [FromBody] AddBillRequestDto request)
        {
            var billPayment = new BillPayment
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate,
                Biller = request.Biller,
                ReferenceNumber = request.ReferenceNumber
            };

            var updated = await _billPaymentRepository.UpdateAsync(id, billPayment);

            if (updated == null)
                return NotFound();

            var dto = new BillPaymentDto
            {
                BillId = updated.BillId,
                AccountId = updated.AccountId,
                Amount = updated.Amount,
                PaymentDate = updated.PaymentDate,
                Biller = updated.Biller,
                ReferenceNumber = updated.ReferenceNumber
            };

            return Ok(dto);
        }

        // DELETE: api/BillPayments/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBillPayment(int id)
        {
            var deleted = await _billPaymentRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}

