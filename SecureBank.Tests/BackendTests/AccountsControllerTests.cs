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
    public class AccountsControllerTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<IAuthService> _authServiceMock = new();

        private AccountsController CreateController()
        {
            var controller = new AccountsController(_accountRepoMock.Object, _authServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
            return controller;
        }

        [Fact]
        public async Task GetAccounts_Admin_ReturnsAllAccounts()
        {
            // Arrange
            var accounts = new List<Account>
            {
                new() { AccountId = 1, AccountNumber = "A1", Balance = 100, UserId = 2 },
                new() { AccountId = 2, AccountNumber = "A2", Balance = 200, UserId = 3 }
            };

            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(99);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(true);
            _accountRepoMock.Setup(r => r.GetAccountsAsync()).ReturnsAsync(accounts);

            var controller = CreateController();

            // Act
            var result = await controller.GetAccounts();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<AccountDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetAccounts_NonAdmin_ReturnsOnlyUserAccounts()
        {
            // Arrange
            var userId = 42;
            var allAccounts = new List<Account>
            {
                new() { AccountId = 1, AccountNumber = "A1", Balance = 100, UserId = userId },
                new() { AccountId = 2, AccountNumber = "A2", Balance = 200, UserId = 999 }
            };

            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);
            _accountRepoMock.Setup(r => r.GetAccountsByUserIdAsync(userId)).ReturnsAsync(allAccounts.Where(a => a.UserId == userId).ToList());

            var controller = CreateController();

            // Act
            var result = await controller.GetAccounts();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<AccountDto>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal("A1", returned.First().AccountNumber);
        }

        [Fact]
        public async Task GetAccount_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 10;
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), id)).ReturnsAsync(false);
            var controller = CreateController();

            // Act
            var result = await controller.GetAccount(id);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetAccount_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 11;
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), id)).ReturnsAsync(true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Account?)null);

            var controller = CreateController();

            // Act
            var result = await controller.GetAccount(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task AddAccount_AssignsCurrentUser_ReturnsCreatedAtAction()
        {
            // Arrange
            var userId = 5;
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            var request = new AddAccountRequestDto
            {
                AccountNumber = "NEW123",
                Balance = 500,
                AccountType = "Checking",
                CreatedDate = DateTime.UtcNow
            };

            var createdAccount = new Account
            {
                AccountId = 99,
                AccountNumber = request.AccountNumber,
                Balance = request.Balance,
                AccountType = request.AccountType,
                CreatedDate = request.CreatedDate,
                UserId = userId
            };

            _accountRepoMock.Setup(r => r.CreateAsync(It.IsAny<Account>())).ReturnsAsync(createdAccount);

            var controller = CreateController();

            // Act
            var result = await controller.AddAccount(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<AccountDto>(created.Value);
            Assert.Equal(createdAccount.AccountId, dto.AccountId);
            _accountRepoMock.Verify(r => r.CreateAsync(It.Is<Account>(a => a.UserId == userId && a.AccountNumber == request.AccountNumber)), Times.Once);
        }

        [Fact]
        public async Task UpdateAccount_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 7;
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), id)).ReturnsAsync(false);
            var controller = CreateController();

            var request = new AddAccountRequestDto
            {
                AccountNumber = "X",
                Balance = 1,
                AccountType = "S",
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var result = await controller.UpdateAccount(id, request);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task UpdateAccount_WhenFound_ReturnsOk()
        {
            // Arrange
            var id = 8;
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), id)).ReturnsAsync(true);

            var request = new AddAccountRequestDto
            {
                AccountNumber = "UU",
                Balance = 123,
                AccountType = "Savings",
                CreatedDate = DateTime.UtcNow
            };

            var updatedAccount = new Account
            {
                AccountId = id,
                AccountNumber = request.AccountNumber,
                Balance = request.Balance,
                AccountType = request.AccountType,
                CreatedDate = request.CreatedDate
            };

            _accountRepoMock.Setup(r => r.UpdateAsync(id, It.IsAny<Account>())).ReturnsAsync(updatedAccount);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateAccount(id, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<AccountDto>(ok.Value);
            Assert.Equal(id, dto.AccountId);
        }

        [Fact]
        public async Task DeleteAccount_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 12;
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), id)).ReturnsAsync(false);
            var controller = CreateController();

            // Act
            var result = await controller.DeleteAccount(id);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteAccount_WhenDeleted_ReturnsNoContent()
        {
            // Arrange
            var id = 13;
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), id)).ReturnsAsync(true);

            _accountRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(new Account { AccountId = id });

            var controller = CreateController();

            // Act
            var result = await controller.DeleteAccount(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _accountRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}
