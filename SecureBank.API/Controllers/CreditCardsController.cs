using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Implementation;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditCardsController : ControllerBase
    {
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IAccountRepository _accountRepository;

        public CreditCardsController(ICreditCardRepository creditCardRepository, IAccountRepository accountRepository)
        {
            _creditCardRepository = creditCardRepository;
            _accountRepository = accountRepository;
        }

        // GET: api/CreditCards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CreditCardDto>>> GetCreditCards()
        {
            var cards = await _creditCardRepository.GetCreditCardsAsync();
            var dtos = cards.Select(card => new CreditCardDto
            {
                CreditId = card.CreditId,
                CardNumber = card.CardNumber,
                CreditLimit = card.CreditLimit,
                CurrentBalance = card.CurrentBalance,
                AccountId = card.AccountId,
                AccountNumber = card.Account.AccountNumber,
                ExpiryDate = card.ExpiryDate,
                CardType = card.CardType
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/CreditCards/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CreditCardDto>> GetCreditCard(int id)
        {
            var card = await _creditCardRepository.GetByIdAsync(id);
            if (card == null)
                return NotFound();

            var dto = new CreditCardDto
            {
                CreditId = card.CreditId,
                CardNumber = card.CardNumber,
                CreditLimit = card.CreditLimit,
                CurrentBalance = card.CurrentBalance,
                AccountId = card.AccountId,
                ExpiryDate = card.ExpiryDate,
                CardType = card.CardType
            };
            return Ok(dto);
        }

        // POST: api/CreditCards
        [HttpPost]
        public async Task<ActionResult<CreditCardDto>> AddCreditCard([FromBody] AddCreditRequestDto request)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
                return BadRequest("Account not found.");

            var card = new CreditCard
            {
                CardNumber = request.CardNumber,
                CreditLimit = request.CreditLimit,
                CurrentBalance = request.CurrentBalance,
                AccountId = request.AccountId,
                ExpiryDate = request.ExpiryDate,
                CardType = request.CardType,
                Account = account // Link to Account
            };

            var created = await _creditCardRepository.CreateAsync(card);

            var dto = new CreditCardDto
            {
                CreditId = created.CreditId,
                CardNumber = created.CardNumber,
                CreditLimit = created.CreditLimit,
                CurrentBalance = created.CurrentBalance,
                AccountId = created.AccountId,
                ExpiryDate = created.ExpiryDate,
                CardType = created.CardType
            };

            return CreatedAtAction(nameof(GetCreditCard), new { id = created.CreditId }, dto);
        }

        // POST: api/CreditCards/{id}/charge
        [HttpPost("{id}/charge")]
        public async Task<ActionResult<CreditCardDto>> ChargeCreditCard(int id, [FromBody] decimal amount)
        {
            var card = await _creditCardRepository.GetByIdAsync(id);
            if (card == null)
                return NotFound();

            try
            {
                card.ProcessCharge(amount);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
            await _creditCardRepository.UpdateAsync(id, card);

            var dto = new CreditCardDto
            {
                CreditId = card.CreditId,
                CardNumber = card.CardNumber,
                CreditLimit = card.CreditLimit,
                CurrentBalance = card.CurrentBalance,
                AccountId = card.AccountId,
                ExpiryDate = card.ExpiryDate,
                CardType = card.CardType
            };

            return Ok(dto);
        }

        // POST: api/CreditCards/{id}/payment
        [HttpPost("{id}/payment")]
        public async Task<ActionResult<CreditCardDto>> PayCreditCard(int id, [FromBody] decimal amount)
        {
            var card = await _creditCardRepository.GetByIdAsync(id);
            if (card == null)
                return NotFound();

            var account = await _accountRepository.GetByIdAsync(card.AccountId);
            if (account == null)
                return BadRequest("Linked account not found.");

            card.Account = account; // Ensure Account is loaded
            card.ProcessPayment(amount);
            await _creditCardRepository.UpdateAsync(id, card);
            await _accountRepository.UpdateAsync(account.AccountId, account);

            var dto = new CreditCardDto
            {
                CreditId = card.CreditId,
                CardNumber = card.CardNumber,
                CreditLimit = card.CreditLimit,
                CurrentBalance = card.CurrentBalance,
                AccountId = card.AccountId,
                ExpiryDate = card.ExpiryDate,
                CardType = card.CardType
            };

            return Ok(dto);
        }

        // DELETE: api/BillPayments/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCreditCard(int id)
        {
            var deleted = await _creditCardRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}
