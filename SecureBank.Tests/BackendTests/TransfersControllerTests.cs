using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecureBank.API.Controllers;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Interface;
using SecureBank.API.Services.Interface;
using Xunit;

namespace SecureBank.Tests.BackendTests
{
    public class TransfersControllerTests
    {
        private readonly Mock<ITransferRepository> _transferRepoMock = new();
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<IAuthService> _authServiceMock = new();

        private TransfersController CreateController()
        {
            var controller = new TransfersController(_transferRepoMock.Object, _accountRepoMock.Object, _auth_service_from_moq());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            return controller;

            // helper to appease analyzers
            IAuthService _auth_service_from_moq() => _authServiceMock.Object;
        }

        [Fact]
        public async Task GetTransfers_Admin_ReturnsAll()
        {
            // Arrange
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(1);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(true);

            var transfers = new List<Transfer>
            {
                new() { TransferId = 1, AccountId = 2, Amount = 100m, Account = new Account { AccountNumber = "A1", UserId = 2 } },
                new() { TransferId = 2, AccountId = 3, Amount = 200m, Account = new Account { AccountNumber = "A2", UserId = 3 } }
            };

            _transferRepoMock.Setup(r => r.GetTransfersAsync()).ReturnsAsync(transfers);

            var controller = CreateController();

            // Act
            var result = await controller.GetTransfers();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<TransferDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetTransfers_NonAdmin_FiltersByUser()
        {
            // Arrange
            var userId = 10;
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);

            var transfers = new List<Transfer>
            {
                new() { TransferId = 1, AccountId = 2, Amount = 100m, Account = new Account { AccountNumber = "A1", UserId = userId } },
                new() { TransferId = 2, AccountId = 3, Amount = 200m, Account = new Account { AccountNumber = "A2", UserId = 99 } }
            };

            _transferRepoMock.Setup(r => r.GetTransfersAsync()).ReturnsAsync(transfers);

            var controller = CreateController();

            // Act
            var result = await controller.GetTransfers();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<TransferDto>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal(1, returned.First().TransferId);
        }

        [Fact]
        public async Task GetTransfer_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 5;
            _transferRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Transfer?)null);

            var controller = CreateController();

            // Act
            var result = await controller.GetTransfer(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetTransfer_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 6;
            var transfer = new Transfer { TransferId = id, AccountId = 20 };
            _transferRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(transfer);
            _auth_service_setup_can_access(transfer.AccountId, false);

            var controller = CreateController();

            // Act
            var result = await controller.GetTransfer(id);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task AddTransfer_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var request = new AddTransferRequestDto
            {
                AccountId = 1,
                FromAccountNumber = "A1",
                ToAccountNumber = "A2",
                Amount = 10m,
                TransferDate = DateTime.UtcNow,
                Name = "X",
                Reference = "R"
            };

            _auth_service_setup_can_access(request.AccountId, false);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task AddTransfer_InvalidAmount_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddTransferRequestDto
            {
                AccountId = 1,
                FromAccountNumber = "A1",
                ToAccountNumber = "A2",
                Amount = 0m,
                TransferDate = DateTime.UtcNow
            };
            _auth_service_setup_can_access(request.AccountId, true);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Transfer amount must be greater than zero.", bad.Value);
        }

        [Fact]
        public async Task AddTransfer_MissingAccountNumbers_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddTransferRequestDto
            {
                AccountId = 1,
                FromAccountNumber = "",
                ToAccountNumber = null,
                Amount = 50m,
                TransferDate = DateTime.UtcNow
            };
            _auth_service_setup_can_access(request.AccountId, true);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Both account numbers are required.", bad.Value);
        }

        [Fact]
        public async Task AddTransfer_SameAccount_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddTransferRequestDto
            {
                AccountId = 1,
                FromAccountNumber = "SAME",
                ToAccountNumber = "SAME",
                Amount = 10m,
                TransferDate = DateTime.UtcNow
            };
            _auth_service_setup_can_access(request.AccountId, true);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Cannot transfer to the same account.", bad.Value);
        }

        [Fact]
        public async Task AddTransfer_SenderAccountNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddTransferRequestDto
            {
                AccountId = 99,
                FromAccountNumber = "A99",
                ToAccountNumber = "B1",
                Amount = 10m,
                TransferDate = DateTime.UtcNow
            };
            _auth_service_setup_can_access(request.AccountId, true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync((Account?)null);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Sender account not found.", bad.Value);
        }

        [Fact]
        public async Task AddTransfer_AccountNumberMismatch_ReturnsBadRequest()
        {
            // Arrange
            var sender = new Account { AccountId = 5, AccountNumber = "A5", Balance = 1000m };
            var request = new AddTransferRequestDto
            {
                AccountId = sender.AccountId,
                FromAccountNumber = "DIFFERENT",
                ToAccountNumber = "B2",
                Amount = 10m,
                TransferDate = DateTime.UtcNow
            };
            _auth_service_setup_can_access(request.AccountId, true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync(sender);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Account ID does not match the provided account number.", bad.Value);
        }

        [Fact]
        public async Task AddTransfer_InsufficientFunds_ReturnsBadRequest()
        {
            // Arrange
            var sender = new Account { AccountId = 7, AccountNumber = "A7", Balance = 20m };
            var request = new AddTransferRequestDto
            {
                AccountId = sender.AccountId,
                FromAccountNumber = sender.AccountNumber,
                ToAccountNumber = "B7",
                Amount = 50m,
                TransferDate = DateTime.UtcNow
            };
            _auth_service_setup_can_access(request.AccountId, true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync(sender);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Insufficient funds", bad.Value.ToString());
        }

        [Fact]
        public async Task AddTransfer_RecipientNotFound_ReturnsBadRequest()
        {
            // Arrange
            var sender = new Account { AccountId = 8, AccountNumber = "A8", Balance = 1000m };
            var request = new AddTransferRequestDto
            {
                AccountId = sender.AccountId,
                FromAccountNumber = sender.AccountNumber,
                ToAccountNumber = "UNKNOWN",
                Amount = 50m,
                TransferDate = DateTime.UtcNow
            };
            _auth_service_setup_can_access(request.AccountId, true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync(sender);
            _accountRepoMock.Setup(r => r.GetByAccountNumberAsync(request.ToAccountNumber)).ReturnsAsync((Account?)null);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal($"Recipient account '{request.ToAccountNumber}' not found.", bad.Value);
        }

        [Fact]
        public async Task AddTransfer_Success_PerformsUpdatesAndCreatesTransfer()
        {
            // Arrange
            var sender = new Account { AccountId = 10, AccountNumber = "S10", Balance = 1000m };
            var recipient = new Account { AccountId = 20, AccountNumber = "R20", Balance = 200m };
            var amount = 150m;
            var request = new AddTransferRequestDto
            {
                AccountId = sender.AccountId,
                FromAccountNumber = sender.AccountNumber,
                ToAccountNumber = recipient.AccountNumber,
                Amount = amount,
                TransferDate = DateTime.UtcNow,
                Name = "Payment",
                Reference = "Ref1"
            };

            _auth_service_setup_can_access(request.AccountId, true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(sender.AccountId)).ReturnsAsync(sender);
            _accountRepoMock.Setup(r => r.GetByAccountNumberAsync(recipient.AccountNumber)).ReturnsAsync(recipient);

            _accountRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Account>())).ReturnsAsync((int id, Account a) => a);

            var createdTransfer = new Transfer
            {
                TransferId = 999,
                AccountId = sender.AccountId,
                FromAccountNumber = sender.AccountNumber,
                ToAccountNumber = recipient.AccountNumber,
                Amount = amount,
                TransferDate = request.TransferDate,
                Name = request.Name,
                Reference = request.Reference
            };

            _transferRepoMock.Setup(r => r.CreateAsync(It.IsAny<Transfer>())).ReturnsAsync(createdTransfer);

            var controller = CreateController();

            // Act
            var result = await controller.AddTransfer(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<TransferDto>(created.Value);
            Assert.Equal(createdTransfer.TransferId, dto.TransferId);
            Assert.Equal(sender.AccountNumber, dto.AccountNumber);

            // Verify account balance updates and repository calls
            _accountRepoMock.Verify(r => r.UpdateAsync(sender.AccountId, It.Is<Account>(a => a.Balance == 1000m - amount)), Times.Once);
            _accountRepoMock.Verify(r => r.UpdateAsync(recipient.AccountId, It.Is<Account>(a => a.Balance == 200m + amount)), Times.Once);
            _transferRepoMock.Verify(r => r.CreateAsync(It.IsAny<Transfer>()), Times.Once);
        }

        [Fact]
        public async Task UpdateTransfer_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 42;
            _transferRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Transfer?)null);

            var controller = CreateController();

            var request = new AddTransferRequestDto { AccountId = 1, FromAccountNumber = "A", ToAccountNumber = "B", Amount = 10m, TransferDate = DateTime.UtcNow };

            // Act
            var result = await controller.UpdateTransfer(id, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateTransfer_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 43;
            var existing = new Transfer { TransferId = id, AccountId = 300 };
            _transferRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _auth_service_setup_can_access(existing.AccountId, false);

            var controller = CreateController();
            var request = new AddTransferRequestDto { AccountId = existing.AccountId, FromAccountNumber = "A", ToAccountNumber = "B", Amount = 10m, TransferDate = DateTime.UtcNow };

            // Act
            var result = await controller.UpdateTransfer(id, request);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task UpdateTransfer_Success_ReturnsOk()
        {
            // Arrange
            var id = 44;
            var existing = new Transfer { TransferId = id, AccountId = 60 };
            _transferRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _auth_service_setup_can_access(existing.AccountId, true);

            var request = new AddTransferRequestDto
            {
                AccountId = existing.AccountId,
                FromAccountNumber = "FROM",
                ToAccountNumber = "TO",
                Amount = 20m,
                TransferDate = DateTime.UtcNow,
                Name = "N",
                Reference = "R"
            };

            var updated = new Transfer
            {
                TransferId = id,
                AccountId = request.AccountId,
                FromAccountNumber = request.FromAccountNumber,
                ToAccountNumber = request.ToAccountNumber,
                Amount = request.Amount,
                TransferDate = request.TransferDate,
                Name = request.Name,
                Reference = request.Reference
            };

            _transferRepoMock.Setup(r => r.UpdateAsync(id, It.IsAny<Transfer>())).ReturnsAsync(updated);
            _accountRepoMock.Setup(r => r.GetByIdAsync(updated.AccountId)).ReturnsAsync(new Account { AccountId = updated.AccountId, AccountNumber = "ACC60" });

            var controller = CreateController();

            // Act
            var result = await controller.UpdateTransfer(id, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TransferDto>(ok.Value);
            Assert.Equal(updated.TransferId, dto.TransferId);
            Assert.Equal("ACC60", dto.AccountNumber);
        }

        [Fact]
        public async Task DeleteTransfer_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 70;
            var existing = new Transfer { TransferId = id, AccountId = 800 };
            _transferRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _auth_service_setup_can_access(existing.AccountId, false);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteTransfer(id);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteTransfer_Success_ReturnsNoContent()
        {
            // Arrange
            var id = 71;
            var existing = new Transfer { TransferId = id, AccountId = 801 };
            _transferRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _auth_service_setup_can_access(existing.AccountId, true);
            _transferRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(existing);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteTransfer(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _transferRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        // Helper to reduce repetition
        private void _auth_service_setup_can_access(int accountId, bool canAccess)
        {
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), accountId)).ReturnsAsync(canAccess);
            // Also set generic current user id for other flows
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(123);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);
        }
    }
}
