using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvestmentsController : ControllerBase
    {
        private readonly IInvestmentRepository _investmentRepository;
        private readonly IAccountRepository _accountRepository;

        public InvestmentsController(IInvestmentRepository investmentRepository, IAccountRepository accountRepository)
        {
            _investmentRepository = investmentRepository;
            _accountRepository = accountRepository;
        }

        // GET: api/Investments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvestmentDto>>> GetInvestments()
        {
            var investments = await _investmentRepository.GetInvestmentsAsync();
            var dtos = investments.Select(inv => new InvestmentDto
            {
                InvestmentId = inv.InvestmentId,
                AccountId = inv.AccountId,
                AccountNumber = inv.Account.AccountNumber,
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

            var dto = new InvestmentDto
            {
                InvestmentId = inv.InvestmentId,
                AccountId = inv.AccountId,
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

            var dto = new InvestmentDto
            {
                InvestmentId = updated.InvestmentId,
                AccountId = updated.AccountId,
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
            var deleted = await _investmentRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}
