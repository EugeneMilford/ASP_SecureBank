using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Interface;
using SecureBank.API.Services.Interface;

namespace SecureBank.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BillPaymentsController : ControllerBase
    {
        private readonly IBillPaymentRepository _billPaymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthService _authService;

        public BillPaymentsController(
            IBillPaymentRepository billPaymentRepository,
            IAccountRepository accountRepository,
            IAuthService authService)
        {
            _billPaymentRepository = billPaymentRepository;
            _accountRepository = accountRepository;
            _authService = authService;
        }

        // GET: api/BillPayments - Returns only current user's bills (or all if admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillPaymentDto>>> GetBillPayments()
        {
            var userId = _authService.GetCurrentUserId(User);
            var isAdmin = _authService.IsAdmin(User);

            var allBillPayments = await _billPaymentRepository.GetBillPaymentsAsync();

            // Filter bills based on user role
            var billPayments = isAdmin
                ? allBillPayments
                : allBillPayments.Where(bp => bp.Account.UserId == userId).ToList();

            var dtos = billPayments.Select(bp => new BillPaymentDto
            {
                BillId = bp.BillId,
                AccountId = bp.AccountId,
                AccountNumber = bp.Account.AccountNumber,
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

            // Check if user can access this bill's account
            if (!await _authService.CanAccessAccountAsync(User, billPayment.AccountId))
            {
                return Forbid();
            }

            var dto = new BillPaymentDto
            {
                BillId = billPayment.BillId,
                AccountId = billPayment.AccountId,
                AccountNumber = billPayment.Account?.AccountNumber,
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
            // Check if user can access this account
            if (!await _authService.CanAccessAccountAsync(User, request.AccountId))
            {
                return Forbid();
            }

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
            var existingBill = await _billPaymentRepository.GetByIdAsync(id);
            if (existingBill == null)
                return NotFound();

            // Check if user can access this bill's account
            if (!await _authService.CanAccessAccountAsync(User, existingBill.AccountId))
            {
                return Forbid();
            }

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
            var existingBill = await _billPaymentRepository.GetByIdAsync(id);
            if (existingBill == null)
                return NotFound();

            // Check if user can access this bill's account
            if (!await _authService.CanAccessAccountAsync(User, existingBill.AccountId))
            {
                return Forbid();
            }

            var deleted = await _billPaymentRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}

