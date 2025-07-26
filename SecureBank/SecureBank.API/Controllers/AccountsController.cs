using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;

namespace SecureBank.API.Controllers
{
    // https://localhost:7043/api/accounts
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly BankingContext _context;

        public AccountsController(BankingContext context)
        {
            _context = context;
        }

        // GET: api/Accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            // Get data from database
            var accountsDomain = await _context.accounts.ToListAsync();

            // Map Domain Models to DTO
            var accountDto = new List<AccountDto>();

            foreach (var accountDomain in accountsDomain)
            {
                accountDto.Add(new AccountDto()
                {
                    AccountId = accountDomain.AccountId,
                    AccountNumber = accountDomain.AccountNumber,
                    Balance = accountDomain.Balance,
                    AccountType = accountDomain.AccountType,
                    CreatedDate = accountDomain.CreatedDate
                });
            }

            // Return DTO
            return Ok(accountDto);
        }

        // GET: api/Accounts/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult<Account>> GetAccountById([FromRoute] int id)
        {
            var accountDomain = await _context.accounts.FirstOrDefaultAsync(x => x.AccountId == id);

            if (accountDomain == null)
            {
                return NotFound(new { success = false, message = "Account not found" });
            }

            // Convert Model to DTO
            var accountDto = new AccountDto
            {
                AccountId = accountDomain.AccountId,
                AccountNumber = accountDomain.AccountNumber,
                Balance = accountDomain.Balance,
                AccountType = accountDomain.AccountType,
                CreatedDate = accountDomain.CreatedDate
            };

            return Ok(accountDto);
        }

        // POST: //localhost:7043/api/account
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount([FromBody] AddAccountRequestDto addAccountRequestDto) 
        {
            // Convert DTO to model
            var accountDomainModel = new Account
            {
                AccountNumber = addAccountRequestDto.AccountNumber,
                Balance = addAccountRequestDto.Balance,
                AccountType = addAccountRequestDto.AccountType,
                CreatedDate = addAccountRequestDto.CreatedDate
            };

            // Using domain model to create Account
            _context.accounts.Add(accountDomainModel);
            _context.SaveChanges();

            // Map domain model back to DTO
            var accountDto = new AccountDto
            {
                AccountId = accountDomainModel.AccountId,
                AccountNumber = accountDomainModel.AccountNumber, 
                Balance = accountDomainModel.Balance,
                CreatedDate = accountDomainModel.CreatedDate
            };

            return CreatedAtAction(nameof(GetAccountById), new { id = accountDto.AccountId }, accountDto);
        }

        // PUT: //localhost:7043/api/Accounts/5
        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateAccount([FromRoute] int id, [FromBody] UpdateAccountRequestDto updateAccountRequestDto) 
        {
            // Check if Account exists
            var accountDomainModel = _context.accounts.FirstOrDefault(x => x.AccountId == id);

            if (accountDomainModel == null) 
            {
                return NotFound();
            }

            // Map DTO to Domain Model
            accountDomainModel.AccountNumber = updateAccountRequestDto.AccountNumber;
            accountDomainModel.Balance = updateAccountRequestDto.Balance;
            accountDomainModel.AccountType = updateAccountRequestDto.AccountType;
            accountDomainModel.CreatedDate = updateAccountRequestDto.CreatedDate;

            await _context.SaveChangesAsync();

            // Convert Domain Model to DTO
            var accountDto = new AccountDto
            {
                AccountId = accountDomainModel.AccountId,
                AccountNumber = accountDomainModel.AccountNumber,
                Balance = accountDomainModel.Balance,
                CreatedDate = accountDomainModel.CreatedDate
            };

            return Ok(accountDto);
        }
    }
}
