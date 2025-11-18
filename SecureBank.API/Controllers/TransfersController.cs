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
    public class TransfersController : ControllerBase
    {
        private readonly ITransferRepository _transferRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthService _authService;

        public TransfersController(
            ITransferRepository transferRepository,
            IAccountRepository accountRepository,
            IAuthService authService)
        {
            _transferRepository = transferRepository;
            _accountRepository = accountRepository;
            _authService = authService;
        }

        // GET: api/Transfers - Returns only current user's transfers (or all if admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransferDto>>> GetTransfers()
        {
            var userId = _authService.GetCurrentUserId(User);
            var isAdmin = _authService.IsAdmin(User);

            var allTransfers = await _transferRepository.GetTransfersAsync();

            // Filter transfers based on user role
            var transfers = isAdmin
                ? allTransfers
                : allTransfers.Where(t => t.Account.UserId == userId).ToList();

            var dtos = transfers.Select(t => new TransferDto
            {
                TransferId = t.TransferId,
                AccountId = t.AccountId,
                Name = t.Name,
                AccountNumber = t.Account?.AccountNumber,
                FromAccountNumber = t.FromAccountNumber,
                ToAccountNumber = t.ToAccountNumber,
                Amount = t.Amount,
                TransferDate = t.TransferDate,
                Reference = t.Reference
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/Transfers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TransferDto>> GetTransfer(int id)
        {
            var transfer = await _transferRepository.GetByIdAsync(id);
            if (transfer == null)
                return NotFound();

            // Check if user can access this transfer's account
            if (!await _authService.CanAccessAccountAsync(User, transfer.AccountId))
            {
                return Forbid();
            }

            var dto = new TransferDto
            {
                TransferId = transfer.TransferId,
                AccountId = transfer.AccountId,
                Name = transfer.Name,
                AccountNumber = transfer.Account?.AccountNumber,
                FromAccountNumber = transfer.FromAccountNumber,
                ToAccountNumber = transfer.ToAccountNumber,
                Amount = transfer.Amount,
                TransferDate = transfer.TransferDate,
                Reference = transfer.Reference
            };
            return Ok(dto);
        }

        // POST: api/Transfers
        [HttpPost]
        public async Task<ActionResult<TransferDto>> AddTransfer([FromBody] AddTransferRequestDto request)
        {
            try
            {
                // Check if user can access this account
                if (!await _authService.CanAccessAccountAsync(User, request.AccountId))
                {
                    return Forbid();
                }

                // Validation
                if (request.Amount <= 0)
                    return BadRequest("Transfer amount must be greater than zero.");

                if (string.IsNullOrEmpty(request.FromAccountNumber) || string.IsNullOrEmpty(request.ToAccountNumber))
                    return BadRequest("Both account numbers are required.");

                if (request.FromAccountNumber == request.ToAccountNumber)
                    return BadRequest("Cannot transfer to the same account.");

                // Get sender account
                var senderAccount = await _accountRepository.GetByIdAsync(request.AccountId);
                if (senderAccount == null)
                    return BadRequest("Sender account not found.");

                // Verify sender account number matches
                if (senderAccount.AccountNumber != request.FromAccountNumber)
                    return BadRequest("Account ID does not match the provided account number.");

                // Check sufficient funds
                if (senderAccount.Balance < request.Amount)
                    return BadRequest($"Insufficient funds. Available balance: {senderAccount.Balance:C}");

                // Get recipient account
                var recipientAccount = await _accountRepository.GetByAccountNumberAsync(request.ToAccountNumber);
                if (recipientAccount == null)
                    return BadRequest($"Recipient account '{request.ToAccountNumber}' not found.");

                // Perform the transfer
                // 1. Deduct from sender
                senderAccount.Balance -= request.Amount;
                await _accountRepository.UpdateAsync(senderAccount.AccountId, senderAccount);

                // 2. Add to recipient
                recipientAccount.Balance += request.Amount;
                await _accountRepository.UpdateAsync(recipientAccount.AccountId, recipientAccount);

                // Create transfer record
                var transfer = new Transfer
                {
                    AccountId = request.AccountId,
                    Name = request.Name,
                    FromAccountNumber = request.FromAccountNumber,
                    ToAccountNumber = request.ToAccountNumber,
                    Amount = request.Amount,
                    TransferDate = request.TransferDate,
                    Reference = request.Reference
                };

                var created = await _transferRepository.CreateAsync(transfer);

                var dto = new TransferDto
                {
                    TransferId = created.TransferId,
                    AccountId = created.AccountId,
                    AccountNumber = senderAccount.AccountNumber,
                    Name = created.Name,
                    FromAccountNumber = created.FromAccountNumber,
                    ToAccountNumber = created.ToAccountNumber,
                    Amount = created.Amount,
                    TransferDate = created.TransferDate,
                    Reference = created.Reference
                };

                return CreatedAtAction(nameof(GetTransfer), new { id = created.TransferId }, dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing the transfer.");
            }
        }

        // PUT: api/Transfers/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TransferDto>> UpdateTransfer(int id, [FromBody] AddTransferRequestDto request)
        {
            var existingTransfer = await _transferRepository.GetByIdAsync(id);
            if (existingTransfer == null)
                return NotFound();

            // Check if user can access this transfer's account
            if (!await _authService.CanAccessAccountAsync(User, existingTransfer.AccountId))
            {
                return Forbid();
            }

            var transfer = new Transfer
            {
                AccountId = request.AccountId,
                Name = request.Name,
                FromAccountNumber = request.FromAccountNumber,
                ToAccountNumber = request.ToAccountNumber,
                Amount = request.Amount,
                TransferDate = request.TransferDate,
                Reference = request.Reference
            };

            var updated = await _transferRepository.UpdateAsync(id, transfer);

            if (updated == null)
                return NotFound();

            // Resolve account number if possible
            var account = await _accountRepository.GetByIdAsync(updated.AccountId);

            var dto = new TransferDto
            {
                TransferId = updated.TransferId,
                AccountId = updated.AccountId,
                Name = updated.Name,
                AccountNumber = account?.AccountNumber,
                FromAccountNumber = updated.FromAccountNumber,
                ToAccountNumber = updated.ToAccountNumber,
                Amount = updated.Amount,
                TransferDate = updated.TransferDate,
                Reference = updated.Reference
            };

            return Ok(dto);
        }

        // DELETE: api/Transfers/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTransfer(int id)
        {
            var existingTransfer = await _transferRepository.GetByIdAsync(id);
            if (existingTransfer == null)
                return NotFound();

            // Check if user can access this transfer's account
            if (!await _authService.CanAccessAccountAsync(User, existingTransfer.AccountId))
            {
                return Forbid();
            }

            var deleted = await _transferRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}