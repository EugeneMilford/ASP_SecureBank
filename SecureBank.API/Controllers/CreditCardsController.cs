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
    public class CreditCardsController : ControllerBase
    {
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthService _authService;

        public CreditCardsController(
            ICreditCardRepository creditCardRepository,
            IAccountRepository accountRepository,
            IAuthService authService)
        {
            _creditCardRepository = creditCardRepository;
            _accountRepository = accountRepository;
            _authService = authService;
        }

        // GET: api/CreditCards - Returns only current user's cards (or all if admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CreditCardDto>>> GetCreditCards()
        {
            var userId = _authService.GetCurrentUserId(User);
            var isAdmin = _authService.IsAdmin(User);

            var allCards = await _creditCardRepository.GetCreditCardsAsync();

            // Filter cards based on user role
            var cards = isAdmin
                ? allCards
                : allCards.Where(c => c.Account.UserId == userId).ToList();

            var dtos = cards.Select(card => new CreditCardDto
            {
                CreditId = card.CreditId,
                CardNumber = card.CardNumber,
                CreditLimit = card.CreditLimit,
                CurrentBalance = card.CurrentBalance,
                AccountId = card.AccountId,
                AccountNumber = card.Account?.AccountNumber,
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

            // Check if user can access this card's account
            if (!await _authService.CanAccessAccountAsync(User, card.AccountId))
            {
                return Forbid();
            }

            var dto = new CreditCardDto
            {
                CreditId = card.CreditId,
                CardNumber = card.CardNumber,
                CreditLimit = card.CreditLimit,
                CurrentBalance = card.CurrentBalance,
                AccountId = card.AccountId,
                AccountNumber = card.Account?.AccountNumber,
                ExpiryDate = card.ExpiryDate,
                CardType = card.CardType
            };
            return Ok(dto);
        }

        // POST: api/CreditCards
        [HttpPost]
        public async Task<ActionResult<CreditCardDto>> AddCreditCard([FromBody] AddCreditRequestDto request)
        {
            // Check if user can access this account
            if (!await _authService.CanAccessAccountAsync(User, request.AccountId))
            {
                return Forbid();
            }

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
                Account = account
            };

            var created = await _creditCardRepository.CreateAsync(card);

            var dto = new CreditCardDto
            {
                CreditId = created.CreditId,
                CardNumber = created.CardNumber,
                CreditLimit = created.CreditLimit,
                CurrentBalance = created.CurrentBalance,
                AccountId = created.AccountId,
                AccountNumber = account.AccountNumber,
                ExpiryDate = created.ExpiryDate,
                CardType = created.CardType
            };

            return CreatedAtAction(nameof(GetCreditCard), new { id = created.CreditId }, dto);
        }

        // PUT: api/CreditCards/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<CreditCardDto>> UpdateCreditCard(int id, [FromBody] AddCreditRequestDto request)
        {
            var existingCard = await _creditCardRepository.GetByIdAsync(id);
            if (existingCard == null)
                return NotFound();

            // Check if user can access this card's account
            if (!await _authService.CanAccessAccountAsync(User, existingCard.AccountId))
            {
                return Forbid();
            }

            var card = new CreditCard
            {
                CardNumber = request.CardNumber,
                CreditLimit = request.CreditLimit,
                CurrentBalance = request.CurrentBalance,
                AccountId = request.AccountId,
                ExpiryDate = request.ExpiryDate,
                CardType = request.CardType
            };

            var updated = await _creditCardRepository.UpdateAsync(id, card);

            if (updated == null)
                return NotFound();

            // Resolve account number if possible
            var account = await _accountRepository.GetByIdAsync(updated.AccountId);

            var dto = new CreditCardDto
            {
                CreditId = updated.CreditId,
                CardNumber = updated.CardNumber,
                CreditLimit = updated.CreditLimit,
                CurrentBalance = updated.CurrentBalance,
                AccountId = updated.AccountId,
                AccountNumber = account?.AccountNumber,
                ExpiryDate = updated.ExpiryDate,
                CardType = updated.CardType
            };

            return Ok(dto);
        }

        // POST: api/CreditCards/{id}/charge
        [HttpPost("{id}/charge")]
        public async Task<ActionResult<CreditCardDto>> ChargeCreditCard(int id, [FromBody] decimal amount)
        {
            var card = await _creditCardRepository.GetByIdAsync(id);
            if (card == null)
                return NotFound();

            // Check if user can access this card's account
            if (!await _authService.CanAccessAccountAsync(User, card.AccountId))
            {
                return Forbid();
            }

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

            // Check if user can access this card's account
            if (!await _authService.CanAccessAccountAsync(User, card.AccountId))
            {
                return Forbid();
            }

            var account = await _accountRepository.GetByIdAsync(card.AccountId);
            if (account == null)
                return BadRequest("Linked account not found.");

            card.Account = account;
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

        // DELETE: api/CreditCards/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCreditCard(int id)
        {
            var existingCard = await _creditCardRepository.GetByIdAsync(id);
            if (existingCard == null)
                return NotFound();

            // Check if user can access this card's account
            if (!await _authService.CanAccessAccountAsync(User, existingCard.AccountId))
            {
                return Forbid();
            }

            var deleted = await _creditCardRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}