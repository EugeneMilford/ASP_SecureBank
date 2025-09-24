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
                CreatedDate = a.CreatedDate,

                // Bills
                Bills = a.BillPayments?.Select(b => new BillPaymentDto
                {
                    BillId = b.BillId,
                    AccountId = b.AccountId,
                    AccountNumber = b.Account.AccountNumber, 
                    Amount = b.Amount,
                    PaymentDate = b.PaymentDate,
                    Biller = b.Biller,
                    ReferenceNumber = b.ReferenceNumber
                }).ToList() ?? new List<BillPaymentDto>(),

                // Loans
                Loans = a.Loans?.Select(l => new LoanDto
                {
                    LoanId = l.LoanId,
                    AccountId = l.AccountId,
                    LoanAmount = l.LoanAmount,
                    InterestRate = l.InterestRate,
                    LoanStartDate = l.LoanStartDate,
                    LoanEndDate = l.LoanEndDate,
                    RemainingAmount = l.RemainingAmount,
                    IsLoanPaidOff = l.IsLoanPaidOff
                }).ToList() ?? new List<LoanDto>(),

                // Credit Cards
                CreditCards = a.CreditCards?.Select(c => new CreditCardDto
                {
                    CreditId = c.CreditId,
                    CardNumber = c.CardNumber,
                    CreditLimit = c.CreditLimit,
                    CurrentBalance = c.CurrentBalance,
                    AccountId = c.AccountId,
                    AccountNumber = c.Account.AccountNumber,
                    ExpiryDate = c.ExpiryDate,
                    CardType = c.CardType
                    }).ToList() ?? new List<CreditCardDto>(),

                // Investments
                Investments = a.Investments?.Select(i => new InvestmentDto
                {
                    InvestmentId = i.InvestmentId,
                    AccountId = i.AccountId,
                    AccountNumber = i.Account.AccountNumber,
                    InvestmentAmount = i.InvestmentAmount,
                    InvestmentType = i.InvestmentType,
                    CurrentValue = i.CurrentValue,
                    InvestmentDate = i.InvestmentDate,
                    Returns = i.CurrentValue - i.InvestmentAmount
                }).ToList() ?? new List<InvestmentDto>(),

                // Transfers
                Transfers = a.Transfers?.Select(t => new TransferDto
                {
                    TransferId = t.TransferId,
                    AccountId = t.AccountId,
                    Name = t.Name,
                    AccountNumber = t.Account.AccountNumber,
                    FromAccountNumber = t.FromAccountNumber,
                    ToAccountNumber = t.ToAccountNumber,
                    Amount = t.Amount,
                    TransferDate = t.TransferDate,
                    Reference = t.Reference
                }).ToList() ?? new List<TransferDto>()
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
