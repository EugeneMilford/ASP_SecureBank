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
    public class InvestmentsController : ControllerBase
    {
        private readonly IInvestmentRepository _investmentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthService _authService;

        public InvestmentsController(
            IInvestmentRepository investmentRepository,
            IAccountRepository accountRepository,
            IAuthService authService)
        {
            _investmentRepository = investmentRepository;
            _accountRepository = accountRepository;
            _authService = authService;
        }

        // GET: api/Investments - Returns only current user's investments (or all if admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvestmentDto>>> GetInvestments()
        {
            var userId = _authService.GetCurrentUserId(User);
            var isAdmin = _authService.IsAdmin(User);

            var allInvestments = await _investmentRepository.GetInvestmentsAsync();

            // Filter investments based on user role
            var investments = isAdmin
                ? allInvestments
                : allInvestments.Where(i => i.Account.UserId == userId).ToList();

            var dtos = investments.Select(inv => new InvestmentDto
            {
                InvestmentId = inv.InvestmentId,
                AccountId = inv.AccountId,
                AccountNumber = inv.Account?.AccountNumber,
                InvestmentAmount = inv.InvestmentAmount,
                InvestmentType = inv.InvestmentType,
                CurrentValue = inv.CurrentValue,
                InvestmentDate = inv.InvestmentDate,
                Returns = inv.CalculateReturns()
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/Investments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InvestmentDto>> GetInvestment(int id)
        {
            var inv = await _investmentRepository.GetByIdAsync(id);
            if (inv == null)
                return NotFound();

            // Check if user can access this investment's account
            if (!await _authService.CanAccessAccountAsync(User, inv.AccountId))
            {
                return Forbid();
            }

            var dto = new InvestmentDto
            {
                InvestmentId = inv.InvestmentId,
                AccountId = inv.AccountId,
                AccountNumber = inv.Account?.AccountNumber,
                InvestmentAmount = inv.InvestmentAmount,
                InvestmentType = inv.InvestmentType,
                CurrentValue = inv.CurrentValue,
                InvestmentDate = inv.InvestmentDate,
                Returns = inv.CalculateReturns()
            };
            return Ok(dto);
        }

        // POST: api/Investments
        [HttpPost]
        public async Task<ActionResult<InvestmentDto>> AddInvestment([FromBody] AddInvestmentRequestDto request)
        {
            // Check if user can access this account
            if (!await _authService.CanAccessAccountAsync(User, request.AccountId))
            {
                return Forbid();
            }

            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
                return BadRequest("Account not found.");

            // Deduct investment amount from account balance
            if (account.Balance < request.InvestmentAmount)
                return BadRequest("Insufficient account balance.");

            account.Balance -= request.InvestmentAmount;
            await _accountRepository.UpdateAsync(account.AccountId, account);

            var inv = new Investment
            {
                AccountId = request.AccountId,
                InvestmentAmount = request.InvestmentAmount,
                InvestmentType = request.InvestmentType,
                CurrentValue = request.CurrentValue,
                InvestmentDate = request.InvestmentDate
            };

            var created = await _investmentRepository.CreateAsync(inv);

            var dto = new InvestmentDto
            {
                InvestmentId = created.InvestmentId,
                AccountId = created.AccountId,
                AccountNumber = account.AccountNumber,
                InvestmentAmount = created.InvestmentAmount,
                InvestmentType = created.InvestmentType,
                CurrentValue = created.CurrentValue,
                InvestmentDate = created.InvestmentDate,
                Returns = created.CalculateReturns()
            };

            return CreatedAtAction(nameof(GetInvestment), new { id = created.InvestmentId }, dto);
        }

        // PUT: api/Investments/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<InvestmentDto>> UpdateInvestment(int id, [FromBody] AddInvestmentRequestDto request)
        {
            var existingInvestment = await _investmentRepository.GetByIdAsync(id);
            if (existingInvestment == null)
                return NotFound();

            // Check if user can access this investment's account
            if (!await _authService.CanAccessAccountAsync(User, existingInvestment.AccountId))
            {
                return Forbid();
            }

            var inv = new Investment
            {
                AccountId = request.AccountId,
                InvestmentAmount = request.InvestmentAmount,
                InvestmentType = request.InvestmentType,
                CurrentValue = request.CurrentValue,
                InvestmentDate = request.InvestmentDate
            };

            var updated = await _investmentRepository.UpdateAsync(id, inv);

            if (updated == null)
                return NotFound();

            // Resolve account number if possible
            var account = await _accountRepository.GetByIdAsync(updated.AccountId);

            var dto = new InvestmentDto
            {
                InvestmentId = updated.InvestmentId,
                AccountId = updated.AccountId,
                AccountNumber = account?.AccountNumber,
                InvestmentAmount = updated.InvestmentAmount,
                InvestmentType = updated.InvestmentType,
                CurrentValue = updated.CurrentValue,
                InvestmentDate = updated.InvestmentDate,
                Returns = updated.CalculateReturns()
            };

            return Ok(dto);
        }

        // DELETE: api/Investments/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteInvestment(int id)
        {
            var existingInvestment = await _investmentRepository.GetByIdAsync(id);
            if (existingInvestment == null)
                return NotFound();

            // Check if user can access this investment's account
            if (!await _authService.CanAccessAccountAsync(User, existingInvestment.AccountId))
            {
                return Forbid();
            }

            var deleted = await _investmentRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}
