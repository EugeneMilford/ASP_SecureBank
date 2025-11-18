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
    public class LoansControllerTests
    {
        private readonly Mock<ILoanRepository> _loanRepoMock = new();
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<IAuthService> _authServiceMock = new();

        private LoansController CreateController()
        {
            var controller = new LoansController(_loanRepoMock.Object, _accountRepoMock.Object, _auth_service_from_moq());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            return controller;

            // local helper to satisfy nullable analyzer in some environments
            IAuthService _auth_service_from_moq() => _authServiceMock.Object;
        }

        [Fact]
        public async Task GetLoans_Admin_ReturnsAllLoans()
        {
            // Arrange
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(1);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(true);

            var loans = new List<Loan>
            {
                new() { LoanId = 1, AccountId = 2, LoanAmount = 1000m, Account = new Account { AccountNumber = "A1", UserId = 2 } },
                new() { LoanId = 2, AccountId = 3, LoanAmount = 2000m, Account = new Account { AccountNumber = "A2", UserId = 3 } }
            };

            _loanRepoMock.Setup(r => r.GetLoansAsync()).ReturnsAsync(loans);

            var controller = CreateController();

            // Act
            var result = await controller.GetLoans();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<LoanDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetLoans_NonAdmin_FiltersByUser()
        {
            // Arrange
            var userId = 10;
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);

            var loans = new List<Loan>
            {
                new() { LoanId = 1, AccountId = 2, LoanAmount = 1000m, Account = new Account { AccountNumber = "A1", UserId = userId } },
                new() { LoanId = 2, AccountId = 3, LoanAmount = 2000m, Account = new Account { AccountNumber = "A2", UserId = 99 } }
            };

            _loanRepoMock.Setup(r => r.GetLoansAsync()).ReturnsAsync(loans);

            var controller = CreateController();

            // Act
            var result = await controller.GetLoans();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<LoanDto>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal(1, returned.First().LoanId);
        }

        [Fact]
        public async Task GetLoan_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 5;
            _loanRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Loan?)null);

            var controller = CreateController();

            // Act
            var result = await controller.GetLoan(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetLoan_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 6;
            var loan = new Loan { LoanId = id, AccountId = 20 };
            _loanRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(loan);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), loan.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.GetLoan(id);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task AddLoan_AccountNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddLoanRequestDto
            {
                AccountId = 10,
                LoanAmount = 1000m,
                InterestRate = 5.0m,
                LoanStartDate = DateTime.UtcNow,
                LoanEndDate = DateTime.UtcNow.AddYears(1),
                RemainingAmount = 1000m,
                IsLoanPaidOff = false
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), request.AccountId)).ReturnsAsync(true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync((Account?)null);

            var controller = CreateController();

            // Act
            var result = await controller.AddLoan(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Account not found.", bad.Value);
        }

        [Fact]
        public async Task AddLoan_Success_ReturnsCreatedAndUpdatesAccount()
        {
            // Arrange
            var accountId = 11;
            var startingBalance = 500m;
            var loanAmount = 1500m;

            var request = new AddLoanRequestDto
            {
                AccountId = accountId,
                LoanAmount = loanAmount,
                InterestRate = 4.5m,
                LoanStartDate = DateTime.UtcNow,
                LoanEndDate = DateTime.UtcNow.AddYears(2),
                RemainingAmount = loanAmount,
                IsLoanPaidOff = false
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), accountId)).ReturnsAsync(true);

            var account = new Account { AccountId = accountId, Balance = startingBalance, AccountNumber = "ACC11" };
            _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepoMock.Setup(r => r.UpdateAsync(accountId, It.IsAny<Account>())).ReturnsAsync((int id, Account a) => a);

            var createdLoan = new Loan
            {
                LoanId = 77,
                AccountId = accountId,
                LoanAmount = loanAmount,
                InterestRate = request.InterestRate,
                LoanStartDate = request.LoanStartDate,
                LoanEndDate = request.LoanEndDate,
                RemainingAmount = request.RemainingAmount,
                IsLoanPaidOff = request.IsLoanPaidOff
            };

            _loanRepoMock.Setup(r => r.CreateAsync(It.IsAny<Loan>())).ReturnsAsync(createdLoan);

            var controller = CreateController();

            // Act
            var result = await controller.AddLoan(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<LoanDto>(created.Value);
            Assert.Equal(createdLoan.LoanId, dto.LoanId);
            Assert.Equal("ACC11", dto.AccountNumber);

            // Verify account balance updated (loan amount added)
            _accountRepoMock.Verify(r => r.UpdateAsync(accountId, It.Is<Account>(a => a.Balance == startingBalance + loanAmount)), Times.Once);
            _loanRepoMock.Verify(r => r.CreateAsync(It.IsAny<Loan>()), Times.Once);
        }

        [Fact]
        public async Task UpdateLoan_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 88;
            var request = new AddLoanRequestDto
            {
                AccountId = 1,
                LoanAmount = 100,
                InterestRate = 1.0m,
                LoanStartDate = DateTime.UtcNow,
                LoanEndDate = DateTime.UtcNow.AddMonths(1),
                RemainingAmount = 100,
                IsLoanPaidOff = false
            };

            _loanRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Loan?)null);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateLoan(id, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateLoan_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 89;
            var existing = new Loan { LoanId = id, AccountId = 50 };
            _loanRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            var request = new AddLoanRequestDto
            {
                AccountId = existing.AccountId,
                LoanAmount = 200,
                InterestRate = 2.0m,
                LoanStartDate = DateTime.UtcNow,
                LoanEndDate = DateTime.UtcNow.AddMonths(6),
                RemainingAmount = 200,
                IsLoanPaidOff = false
            };

            // Act
            var result = await controller.UpdateLoan(id, request);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task UpdateLoan_Success_ReturnsOk()
        {
            // Arrange
            var id = 90;
            var existing = new Loan { LoanId = id, AccountId = 60 };
            _loanRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(true);

            var request = new AddLoanRequestDto
            {
                AccountId = existing.AccountId,
                LoanAmount = 2500m,
                InterestRate = 3.5m,
                LoanStartDate = DateTime.UtcNow,
                LoanEndDate = DateTime.UtcNow.AddYears(3),
                RemainingAmount = 2500m,
                IsLoanPaidOff = false
            };

            var updated = new Loan
            {
                LoanId = id,
                AccountId = request.AccountId,
                LoanAmount = request.LoanAmount,
                InterestRate = request.InterestRate,
                LoanStartDate = request.LoanStartDate,
                LoanEndDate = request.LoanEndDate,
                RemainingAmount = request.RemainingAmount,
                IsLoanPaidOff = request.IsLoanPaidOff
            };

            _loanRepoMock.Setup(r => r.UpdateAsync(id, It.IsAny<Loan>())).ReturnsAsync(updated);
            _accountRepoMock.Setup(r => r.GetByIdAsync(updated.AccountId)).ReturnsAsync(new Account { AccountId = updated.AccountId, AccountNumber = "ACC60" });

            var controller = CreateController();

            // Act
            var result = await controller.UpdateLoan(id, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<LoanDto>(ok.Value);
            Assert.Equal(updated.LoanId, dto.LoanId);
            Assert.Equal("ACC60", dto.AccountNumber);
        }

        [Fact]
        public async Task DeleteLoan_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 120;
            var existing = new Loan { LoanId = id, AccountId = 300 };
            _loanRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteLoan(id);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteLoan_Success_ReturnsNoContent()
        {
            // Arrange
            var id = 121;
            var existing = new Loan { LoanId = id, AccountId = 310 };
            _loanRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(true);
            _loanRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(existing);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteLoan(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _loanRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}