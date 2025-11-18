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
    public class BillPaymentsControllerTests
    {
        private readonly Mock<IBillPaymentRepository> _billRepoMock = new();
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<IAuthService> _authServiceMock = new();

        private BillPaymentsController CreateController()
        {
            var controller = new BillPaymentsController(_billRepoMock.Object, _accountRepoMock.Object, _authServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            return controller;
        }

        [Fact]
        public async Task GetBillPayments_Admin_ReturnsAll()
        {
            // Arrange
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(1);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(true);

            var bills = new List<BillPayment>
            {
                new() { BillId = 1, AccountId = 2, Amount = 50, Account = new Account { AccountNumber = "A1", UserId = 2 } },
                new() { BillId = 2, AccountId = 3, Amount = 75, Account = new Account { AccountNumber = "A2", UserId = 3 } }
            };

            _billRepoMock.Setup(r => r.GetBillPaymentsAsync()).ReturnsAsync(bills);

            var controller = CreateController();

            // Act
            var result = await controller.GetBillPayments();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<BillPaymentDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetBillPayments_User_ReturnsFiltered()
        {
            // Arrange
            var userId = 10;
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);

            var bills = new List<BillPayment>
            {
                new() { BillId = 1, AccountId = 2, Amount = 50, Account = new Account { AccountNumber = "A1", UserId = userId } },
                new() { BillId = 2, AccountId = 3, Amount = 75, Account = new Account { AccountNumber = "A2", UserId = 99 } }
            };

            _billRepoMock.Setup(r => r.GetBillPaymentsAsync()).ReturnsAsync(bills);

            var controller = CreateController();

            // Act
            var result = await controller.GetBillPayments();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<BillPaymentDto>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal(1, returned.First().BillId);
        }

        [Fact]
        public async Task GetBillPayment_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 5;
            var bill = new BillPayment { BillId = id, AccountId = 100 };
            _billRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(bill);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), bill.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.GetBillPayment(id);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetBillPayment_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 6;
            _billRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((BillPayment?)null);

            var controller = CreateController();

            // Act
            var result = await controller.GetBillPayment(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task AddBillPayment_InsufficientFunds_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddBillRequestDto
            {
                AccountId = 2,
                Amount = 500,
                PaymentDate = DateTime.UtcNow,
                Biller = "Electric",
                ReferenceNumber = "REF1"
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), request.AccountId)).ReturnsAsync(true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync(new Account { AccountId = request.AccountId, Balance = 100 });

            var controller = CreateController();

            // Act
            var result = await controller.AddBillPayment(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Insufficient funds.", bad.Value);
        }

        [Fact]
        public async Task AddBillPayment_Success_ReturnsCreatedAndUpdatesAccount()
        {
            // Arrange
            var accountId = 3;
            var startingBalance = 500m;
            var amount = 125m;

            var request = new AddBillRequestDto
            {
                AccountId = accountId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                Biller = "Water Co",
                ReferenceNumber = "R123"
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), accountId)).ReturnsAsync(true);

            var account = new Account { AccountId = accountId, Balance = startingBalance, AccountNumber = "ACC123" };
            _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepoMock.Setup(r => r.UpdateAsync(accountId, It.IsAny<Account>())).ReturnsAsync((int id, Account a) => a);

            var createdBill = new BillPayment
            {
                BillId = 77,
                AccountId = accountId,
                Amount = amount,
                PaymentDate = request.PaymentDate,
                Biller = request.Biller,
                ReferenceNumber = request.ReferenceNumber
            };

            _billRepoMock.Setup(r => r.CreateAsync(It.IsAny<BillPayment>())).ReturnsAsync(createdBill);

            var controller = CreateController();

            // Act
            var result = await controller.AddBillPayment(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<BillPaymentDto>(created.Value);
            Assert.Equal(createdBill.BillId, dto.BillId);

            // Verify account update called and balance deducted
            _accountRepoMock.Verify(r => r.UpdateAsync(accountId, It.Is<Account>(a => a.Balance == startingBalance - amount)), Times.Once);
            _billRepoMock.Verify(r => r.CreateAsync(It.IsAny<BillPayment>()), Times.Once);
        }

        [Fact]
        public async Task UpdateBillPayment_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 9;
            var existing = new BillPayment { BillId = id, AccountId = 50 };
            _billRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            var request = new AddBillRequestDto
            {
                AccountId = existing.AccountId,
                Amount = 10,
                PaymentDate = DateTime.UtcNow,
                Biller = "X",
                ReferenceNumber = "Y"
            };

            // Act
            var result = await controller.UpdateBillPayment(id, request);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task DeleteBillPayment_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 20;
            var existing = new BillPayment { BillId = id, AccountId = 300 };
            _billRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteBillPayment(id);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteBillPayment_WhenDeleted_ReturnsNoContent()
        {
            // Arrange
            var id = 21;
            var existing = new BillPayment { BillId = id, AccountId = 10 };
            _billRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(true);
            _billRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(existing);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteBillPayment(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _billRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}
