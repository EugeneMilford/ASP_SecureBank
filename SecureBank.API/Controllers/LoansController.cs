using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class LoansController : ControllerBase
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthService _authService;

        public LoansController(
            ILoanRepository loanRepository,
            IAccountRepository accountRepository,
            IAuthService authService)
        {
            _loanRepository = loanRepository;
            _accountRepository = accountRepository;
            _authService = authService;
        }

        // GET: api/Loans - Returns only current user's loans (or all if admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoanDto>>> GetLoans()
        {
            var userId = _authService.GetCurrentUserId(User);
            var isAdmin = _authService.IsAdmin(User);

            var allLoans = await _loanRepository.GetLoansAsync();

            // Filter loans based on user role
            var loans = isAdmin
                ? allLoans
                : allLoans.Where(l => l.Account.UserId == userId).ToList();

            var dtos = loans.Select(l => new LoanDto
            {
                LoanId = l.LoanId,
                AccountId = l.AccountId,
                AccountNumber = l.Account?.AccountNumber,
                LoanAmount = l.LoanAmount,
                InterestRate = l.InterestRate,
                LoanStartDate = l.LoanStartDate,
                LoanEndDate = l.LoanEndDate,
                RemainingAmount = l.RemainingAmount,
                IsLoanPaidOff = l.IsLoanPaidOff,
                MonthlyRepayment = l.CalculateMonthlyRepayment()
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/Loans/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<LoanDto>> GetLoan(int id)
        {
            var loan = await _loanRepository.GetByIdAsync(id);
            if (loan == null)
                return NotFound();

            // Check if user can access this loan's account
            if (!await _authService.CanAccessAccountAsync(User, loan.AccountId))
            {
                return Forbid();
            }

            var dto = new LoanDto
            {
                LoanId = loan.LoanId,
                AccountId = loan.AccountId,
                AccountNumber = loan.Account?.AccountNumber,
                LoanAmount = loan.LoanAmount,
                InterestRate = loan.InterestRate,
                LoanStartDate = loan.LoanStartDate,
                LoanEndDate = loan.LoanEndDate,
                RemainingAmount = loan.RemainingAmount,
                IsLoanPaidOff = loan.IsLoanPaidOff,
                MonthlyRepayment = loan.CalculateMonthlyRepayment()
            };
            return Ok(dto);
        }

        // POST: api/Loans
        [HttpPost]
        public async Task<ActionResult<LoanDto>> AddLoan([FromBody] AddLoanRequestDto request)
        {
            // Check if user can access this account
            if (!await _authService.CanAccessAccountAsync(User, request.AccountId))
            {
                return Forbid();
            }

            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
                return BadRequest("Account not found.");

            // Add loan amount to account balance
            account.Balance += request.LoanAmount;
            await _accountRepository.UpdateAsync(account.AccountId, account);

            var loan = new Loan
            {
                AccountId = request.AccountId,
                LoanAmount = request.LoanAmount,
                InterestRate = request.InterestRate,
                LoanStartDate = request.LoanStartDate,
                LoanEndDate = request.LoanEndDate,
                RemainingAmount = request.RemainingAmount,
                IsLoanPaidOff = request.IsLoanPaidOff
            };

            var created = await _loanRepository.CreateAsync(loan);

            var dto = new LoanDto
            {
                LoanId = created.LoanId,
                AccountId = created.AccountId,
                AccountNumber = account.AccountNumber,
                LoanAmount = created.LoanAmount,
                InterestRate = created.InterestRate,
                LoanStartDate = created.LoanStartDate,
                LoanEndDate = created.LoanEndDate,
                RemainingAmount = created.RemainingAmount,
                IsLoanPaidOff = created.IsLoanPaidOff,
                MonthlyRepayment = created.CalculateMonthlyRepayment()
            };

            return CreatedAtAction(nameof(GetLoan), new { id = created.LoanId }, dto);
        }

        // PUT: api/Loans/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<LoanDto>> UpdateLoan(int id, [FromBody] AddLoanRequestDto request)
        {
            var existingLoan = await _loanRepository.GetByIdAsync(id);
            if (existingLoan == null)
                return NotFound();

            // Check if user can access this loan's account
            if (!await _authService.CanAccessAccountAsync(User, existingLoan.AccountId))
            {
                return Forbid();
            }

            var loan = new Loan
            {
                AccountId = request.AccountId,
                LoanAmount = request.LoanAmount,
                InterestRate = request.InterestRate,
                LoanStartDate = request.LoanStartDate,
                LoanEndDate = request.LoanEndDate,
                RemainingAmount = request.RemainingAmount,
                IsLoanPaidOff = request.IsLoanPaidOff
            };

            var updated = await _loanRepository.UpdateAsync(id, loan);

            if (updated == null)
                return NotFound();

            // Resolve account number if possible
            var account = await _accountRepository.GetByIdAsync(updated.AccountId);

            var dto = new LoanDto
            {
                LoanId = updated.LoanId,
                AccountId = updated.AccountId,
                AccountNumber = account?.AccountNumber,
                LoanAmount = updated.LoanAmount,
                InterestRate = updated.InterestRate,
                LoanStartDate = updated.LoanStartDate,
                LoanEndDate = updated.LoanEndDate,
                RemainingAmount = updated.RemainingAmount,
                IsLoanPaidOff = updated.IsLoanPaidOff,
                MonthlyRepayment = updated.CalculateMonthlyRepayment()
            };

            return Ok(dto);
        }

        // DELETE: api/Loans/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteLoan(int id)
        {
            var existingLoan = await _loanRepository.GetByIdAsync(id);
            if (existingLoan == null)
                return NotFound();

            // Check if user can access this loan's account
            if (!await _authService.CanAccessAccountAsync(User, existingLoan.AccountId))
            {
                return Forbid();
            }

            var deleted = await _loanRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}

