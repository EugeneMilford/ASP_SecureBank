using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;

        public AccountsController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        // GET: api/Accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts()
        {
            var accounts = await _accountRepository.GetAccountsAsync();
            var dtos = accounts.Select(a => new AccountDto
            {
                AccountId = a.AccountId,
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                AccountType = a.AccountType,
                CreatedDate = a.CreatedDate
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/Accounts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountDto>> GetAccount(int id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
                return NotFound();

            var dto = new AccountDto
            {
                AccountId = account.AccountId,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                AccountType = account.AccountType,
                CreatedDate = account.CreatedDate
            };
            return Ok(dto);
        }

        // POST: api/Accounts
        [HttpPost]
        public async Task<ActionResult<AccountDto>> AddAccount([FromBody] AddAccountRequestDto request)
        {
            var account = new Account
            {
                AccountNumber = request.AccountNumber,
                Balance = request.Balance,
                AccountType = request.AccountType,
                CreatedDate = request.CreatedDate
            };

            var created = await _accountRepository.CreateAsync(account);

            var dto = new AccountDto
            {
                AccountId = created.AccountId,
                AccountNumber = created.AccountNumber,
                Balance = created.Balance,
                AccountType = created.AccountType,
                CreatedDate = created.CreatedDate
            };

            return CreatedAtAction(nameof(GetAccount), new { id = created.AccountId }, dto);
        }

        // PUT: api/Accounts/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<AccountDto>> UpdateAccount(int id, [FromBody] AddAccountRequestDto request)
        {
            var account = new Account
            {
                AccountNumber = request.AccountNumber,
                Balance = request.Balance,
                AccountType = request.AccountType,
                CreatedDate = request.CreatedDate
            };

            var updated = await _accountRepository.UpdateAsync(id, account);

            if (updated == null)
                return NotFound();

            var dto = new AccountDto
            {
                AccountId = updated.AccountId,
                AccountNumber = updated.AccountNumber,
                Balance = updated.Balance,
                AccountType = updated.AccountType,
                CreatedDate = updated.CreatedDate
            };

            return Ok(dto);
        }

        // DELETE: api/Accounts/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAccount(int id)
        {
            var deleted = await _accountRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}
