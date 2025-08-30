using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IAccountRepository _accountRepository;

        public LoansController(ILoanRepository loanRepository, IAccountRepository accountRepository)
        {
            _loanRepository = loanRepository;
            _accountRepository = accountRepository;
        }

        // GET: api/Loans
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoanDto>>> GetLoans()
        {
            var loans = await _loanRepository.GetLoansAsync();
            var dtos = loans.Select(l => new LoanDto
            {
                LoanId = l.LoanId,
                AccountId = l.AccountId,
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

            var dto = new LoanDto
            {
                LoanId = loan.LoanId,
                AccountId = loan.AccountId,
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

            var dto = new LoanDto
            {
                LoanId = updated.LoanId,
                AccountId = updated.AccountId,
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
            var deleted = await _loanRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}

