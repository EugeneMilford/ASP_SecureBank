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
    public class InvestmentsControllerTests
    {
        private readonly Mock<IInvestmentRepository> _invRepoMock = new();
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<IAuthService> _authServiceMock = new();

        private InvestmentsController CreateController()
        {
            var controller = new InvestmentsController(_invRepoMock.Object, _accountRepoMock.Object, _authServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            return controller;
        }

        [Fact]
        public async Task GetInvestments_Admin_ReturnsAll()
        {
            // Arrange
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(1);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(true);

            var investments = new List<Investment>
            {
                new() { InvestmentId = 1, AccountId = 2, InvestmentAmount = 100m, CurrentValue = 120m, Account = new Account { AccountNumber = "A1", UserId = 2 } },
                new() { InvestmentId = 2, AccountId = 3, InvestmentAmount = 200m, CurrentValue = 250m, Account = new Account { AccountNumber = "A2", UserId = 3 } }
            };

            _invRepoMock.Setup(r => r.GetInvestmentsAsync()).ReturnsAsync(investments);

            var controller = CreateController();

            // Act
            var result = await controller.GetInvestments();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<InvestmentDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetInvestments_NonAdmin_FiltersByUser()
        {
            // Arrange
            var userId = 10;
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);

            var investments = new List<Investment>
            {
                new() { InvestmentId = 1, AccountId = 2, InvestmentAmount = 100m, CurrentValue = 120m, Account = new Account { AccountNumber = "A1", UserId = userId } },
                new() { InvestmentId = 2, AccountId = 3, InvestmentAmount = 200m, CurrentValue = 250m, Account = new Account { AccountNumber = "A2", UserId = 99 } }
            };

            _invRepoMock.Setup(r => r.GetInvestmentsAsync()).ReturnsAsync(investments);

            var controller = CreateController();

            // Act
            var result = await controller.GetInvestments();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<InvestmentDto>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal(1, returned.First().InvestmentId);
        }

        [Fact]
        public async Task GetInvestment_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 5;
            _invRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Investment?)null);

            var controller = CreateController();

            // Act
            var result = await controller.GetInvestment(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetInvestment_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 6;
            var inv = new Investment { InvestmentId = id, AccountId = 20 };
            _invRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(inv);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), inv.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.GetInvestment(id);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task AddInvestment_AccountNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddInvestmentRequestDto
            {
                AccountId = 10,
                InvestmentAmount = 100m,
                InvestmentType = "Stock",
                CurrentValue = 100m,
                InvestmentDate = DateTime.UtcNow
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), request.AccountId)).ReturnsAsync(true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync((Account?)null);

            var controller = CreateController();

            // Act
            var result = await controller.AddInvestment(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Account not found.", bad.Value);
        }

        [Fact]
        public async Task AddInvestment_InsufficientBalance_ReturnsBadRequest()
        {
            // Arrange
            var accountId = 11;
            var account = new Account { AccountId = accountId, Balance = 50m };
            var request = new AddInvestmentRequestDto
            {
                AccountId = accountId,
                InvestmentAmount = 100m,
                InvestmentType = "Bond",
                CurrentValue = 100m,
                InvestmentDate = DateTime.UtcNow
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), accountId)).ReturnsAsync(true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var controller = CreateController();

            // Act
            var result = await controller.AddInvestment(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Insufficient account balance.", bad.Value);
        }

        [Fact]
        public async Task AddInvestment_Success_ReturnsCreatedAndUpdatesAccount()
        {
            // Arrange
            var accountId = 12;
            var startingBalance = 1000m;
            var investAmount = 300m;

            var request = new AddInvestmentRequestDto
            {
                AccountId = accountId,
                InvestmentAmount = investAmount,
                InvestmentType = "ETF",
                CurrentValue = 300m,
                InvestmentDate = DateTime.UtcNow
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), accountId)).ReturnsAsync(true);

            var account = new Account { AccountId = accountId, Balance = startingBalance, AccountNumber = "ACC12" };
            _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepoMock.Setup(r => r.UpdateAsync(accountId, It.IsAny<Account>())).ReturnsAsync((int id, Account a) => a);

            var createdInv = new Investment
            {
                InvestmentId = 77,
                AccountId = accountId,
                InvestmentAmount = investAmount,
                InvestmentType = request.InvestmentType,
                CurrentValue = request.CurrentValue,
                InvestmentDate = request.InvestmentDate
            };

            _invRepoMock.Setup(r => r.CreateAsync(It.IsAny<Investment>())).ReturnsAsync(createdInv);

            var controller = CreateController();

            // Act
            var result = await controller.AddInvestment(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<InvestmentDto>(created.Value);
            Assert.Equal(createdInv.InvestmentId, dto.InvestmentId);
            Assert.Equal("ACC12", dto.AccountNumber);

            // Verify account balance deducted
            _accountRepoMock.Verify(r => r.UpdateAsync(accountId, It.Is<Account>(a => a.Balance == startingBalance - investAmount)), Times.Once);
            _invRepoMock.Verify(r => r.CreateAsync(It.IsAny<Investment>()), Times.Once);
        }

        [Fact]
        public async Task UpdateInvestment_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 88;
            var request = new AddInvestmentRequestDto
            {
                AccountId = 1,
                InvestmentAmount = 100m,
                InvestmentType = "X",
                CurrentValue = 100m,
                InvestmentDate = DateTime.UtcNow
            };

            _invRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Investment?)null);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateInvestment(id, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateInvestment_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 89;
            var existing = new Investment { InvestmentId = id, AccountId = 50 };
            _invRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            var request = new AddInvestmentRequestDto
            {
                AccountId = existing.AccountId,
                InvestmentAmount = 200m,
                InvestmentType = "Y",
                CurrentValue = 200m,
                InvestmentDate = DateTime.UtcNow
            };

            // Act
            var result = await controller.UpdateInvestment(id, request);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task UpdateInvestment_Success_ReturnsOk()
        {
            // Arrange
            var id = 90;
            var existing = new Investment { InvestmentId = id, AccountId = 60 };
            _invRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(true);

            var request = new AddInvestmentRequestDto
            {
                AccountId = existing.AccountId,
                InvestmentAmount = 250m,
                InvestmentType = "Bond",
                CurrentValue = 260m,
                InvestmentDate = DateTime.UtcNow
            };

            var updated = new Investment
            {
                InvestmentId = id,
                AccountId = request.AccountId,
                InvestmentAmount = request.InvestmentAmount,
                InvestmentType = request.InvestmentType,
                CurrentValue = request.CurrentValue,
                InvestmentDate = request.InvestmentDate
            };

            _invRepoMock.Setup(r => r.UpdateAsync(id, It.IsAny<Investment>())).ReturnsAsync(updated);
            _accountRepoMock.Setup(r => r.GetByIdAsync(updated.AccountId)).ReturnsAsync(new Account { AccountId = updated.AccountId, AccountNumber = "ACC60" });

            var controller = CreateController();

            // Act
            var result = await controller.UpdateInvestment(id, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<InvestmentDto>(ok.Value);
            Assert.Equal(updated.InvestmentId, dto.InvestmentId);
            Assert.Equal("ACC60", dto.AccountNumber);
        }

        [Fact]
        public async Task DeleteInvestment_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 120;
            var existing = new Investment { InvestmentId = id, AccountId = 300 };
            _invRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteInvestment(id);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteInvestment_Success_ReturnsNoContent()
        {
            // Arrange
            var id = 121;
            var existing = new Investment { InvestmentId = id, AccountId = 310 };
            _invRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(true);
            _invRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(existing);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteInvestment(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _invRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}
