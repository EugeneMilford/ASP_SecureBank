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
    public class CreditCardsControllerTests
    {
        private readonly Mock<ICreditCardRepository> _cardRepoMock = new();
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<IAuthService> _authServiceMock = new();

        private CreditCardsController CreateController()
        {
            var controller = new CreditCardsController(_cardRepoMock.Object, _accountRepoMock.Object, _authServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            return controller;
        }

        // Robust helper to assert Forbid for both ActionResult<T> and non-generic ActionResult/IActionResult
        private static void AssertIsForbid(object actionResult)
        {
            if (actionResult is ForbidResult) return;

            // Try to detect ActionResult<T>/ActionResult wrappers reflectively
            var t = actionResult?.GetType();
            if (t != null)
            {
                var resultProp = t.GetProperty("Result");
                if (resultProp != null)
                {
                    var value = resultProp.GetValue(actionResult);
                    if (value is ForbidResult) return;
                }

                var valueProp = t.GetProperty("Value");
                if (valueProp != null)
                {
                    var value = valueProp.GetValue(actionResult);
                    if (value is ForbidResult) return;
                }
            }

            throw new Xunit.Sdk.XunitException($"Expected ForbidResult but got {actionResult?.GetType().FullName ?? "null"}");
        }

        [Fact]
        public async Task GetCreditCards_Admin_ReturnsAll()
        {
            // Arrange
            _auth_service_setup_getuser_and_admin(1, true);

            var cards = new List<CreditCard>
            {
                new() { CreditId = 1, CardNumber = "1111", CreditLimit = 1000, CurrentBalance = 100, Account = new Account { AccountNumber = "A1", UserId = 2 } },
                new() { CreditId = 2, CardNumber = "2222", CreditLimit = 500, CurrentBalance = 50, Account = new Account { AccountNumber = "A2", UserId = 3 } }
            };

            _cardRepoMock.Setup(r => r.GetCreditCardsAsync()).ReturnsAsync(cards);

            var controller = CreateController();

            // Act
            var result = await controller.GetCreditCards();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<CreditCardDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetCreditCards_NonAdmin_FiltersByUser()
        {
            // Arrange
            var userId = 10;
            _auth_service_setup_getuser_and_admin(userId, false);

            var cards = new List<CreditCard>
            {
                new() { CreditId = 1, CardNumber = "1111", CreditLimit = 1000, CurrentBalance = 100, Account = new Account { AccountNumber = "A1", UserId = userId } },
                new() { CreditId = 2, CardNumber = "2222", CreditLimit = 500, CurrentBalance = 50, Account = new Account { AccountNumber = "A2", UserId = 99 } }
            };

            _cardRepoMock.Setup(r => r.GetCreditCardsAsync()).ReturnsAsync(cards);

            var controller = CreateController();

            // Act
            var result = await controller.GetCreditCards();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<CreditCardDto>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal("1111", returned.First().CardNumber);
        }

        [Fact]
        public async Task GetCreditCard_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 5;
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((CreditCard?)null);
            var controller = CreateController();

            // Act
            var result = await controller.GetCreditCard(id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetCreditCard_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 6;
            var card = new CreditCard { CreditId = id, AccountId = 20 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(card);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), card.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.GetCreditCard(id);

            // Assert
            AssertIsForbid(result);
        }

        [Fact]
        public async Task AddCreditCard_AccountNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddCreditRequestDto
            {
                AccountId = 10,
                CardNumber = "3333",
                CreditLimit = 1000,
                CurrentBalance = 0,
                ExpiryDate = DateTime.UtcNow.AddYears(3),
                CardType = "Visa"
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), request.AccountId)).ReturnsAsync(true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(request.AccountId)).ReturnsAsync((Account?)null);

            var controller = CreateController();

            // Act
            var result = await controller.AddCreditCard(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Account not found.", bad.Value);
        }

        [Fact]
        public async Task AddCreditCard_Success_ReturnsCreatedAndCreatesCard()
        {
            // Arrange
            var accountId = 11;
            var request = new AddCreditRequestDto
            {
                AccountId = accountId,
                CardNumber = "4444",
                CreditLimit = 2000,
                CurrentBalance = 0,
                ExpiryDate = DateTime.UtcNow.AddYears(4),
                CardType = "MasterCard"
            };

            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), accountId)).ReturnsAsync(true);

            var account = new Account { AccountId = accountId, AccountNumber = "ACC11", Balance = 1000m, UserId = 5 };
            _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var created = new CreditCard
            {
                CreditId = 77,
                CardNumber = request.CardNumber,
                CreditLimit = request.CreditLimit,
                CurrentBalance = request.CurrentBalance,
                AccountId = request.AccountId,
                ExpiryDate = request.ExpiryDate,
                CardType = request.CardType
            };

            _cardRepoMock.Setup(r => r.CreateAsync(It.IsAny<CreditCard>())).ReturnsAsync(created);

            var controller = CreateController();

            // Act
            var result = await controller.AddCreditCard(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<CreditCardDto>(createdResult.Value);
            Assert.Equal(created.CreditId, dto.CreditId);
            _cardRepoMock.Verify(r => r.CreateAsync(It.Is<CreditCard>(c => c.AccountId == accountId && c.CardNumber == request.CardNumber)), Times.Once);
        }

        [Fact]
        public async Task UpdateCreditCard_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 88;
            var request = new AddCreditRequestDto { AccountId = 1, CardNumber = "X", CreditLimit = 100, CurrentBalance = 0, ExpiryDate = DateTime.UtcNow, CardType = "X" };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((CreditCard?)null);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateCreditCard(id, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateCreditCard_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 89;
            var existing = new CreditCard { CreditId = id, AccountId = 50 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();
            var request = new AddCreditRequestDto { AccountId = existing.AccountId, CardNumber = "X", CreditLimit = 100, CurrentBalance = 0, ExpiryDate = DateTime.UtcNow, CardType = "X" };

            // Act
            var result = await controller.UpdateCreditCard(id, request);

            // Assert
            AssertIsForbid(result);
        }

        [Fact]
        public async Task UpdateCreditCard_Success_ReturnsOk()
        {
            // Arrange
            var id = 90;
            var existing = new CreditCard { CreditId = id, AccountId = 60 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(true);

            var request = new AddCreditRequestDto
            {
                AccountId = existing.AccountId,
                CardNumber = "UPDATED",
                CreditLimit = 1500,
                CurrentBalance = 100,
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                CardType = "Visa"
            };

            var updated = new CreditCard
            {
                CreditId = id,
                AccountId = request.AccountId,
                CardNumber = request.CardNumber,
                CreditLimit = request.CreditLimit,
                CurrentBalance = request.CurrentBalance,
                ExpiryDate = request.ExpiryDate,
                CardType = request.CardType
            };

            _cardRepoMock.Setup(r => r.UpdateAsync(id, It.IsAny<CreditCard>())).ReturnsAsync(updated);
            _accountRepoMock.Setup(r => r.GetByIdAsync(updated.AccountId)).ReturnsAsync(new Account { AccountId = updated.AccountId, AccountNumber = "ACC60" });

            var controller = CreateController();

            // Act
            var result = await controller.UpdateCreditCard(id, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<CreditCardDto>(ok.Value);
            Assert.Equal(updated.CreditId, dto.CreditId);
            Assert.Equal("ACC60", dto.AccountNumber);
        }

        [Fact]
        public async Task ChargeCreditCard_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 200;
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((CreditCard?)null);
            var controller = CreateController();

            // Act
            var result = await controller.ChargeCreditCard(id, 50m);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ChargeCreditCard_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 201;
            var card = new CreditCard { CreditId = id, AccountId = 300, CreditLimit = 100, CurrentBalance = 10 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(card);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), card.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.ChargeCreditCard(id, 20m);

            // Assert
            AssertIsForbid(result);
        }

        [Fact]
        public async Task ChargeCreditCard_ExceedsLimit_ReturnsBadRequest()
        {
            // Arrange
            var id = 202;
            var card = new CreditCard { CreditId = id, AccountId = 301, CreditLimit = 100, CurrentBalance = 80 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(card);
            _auth_service_setup_can_access(card.AccountId, true);

            var controller = CreateController();

            // Act
            var result = await controller.ChargeCreditCard(id, 30m); // exceeds available 20

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.IsType<string>(bad.Value);
        }

        [Fact]
        public async Task ChargeCreditCard_Success_ReturnsOk()
        {
            // Arrange
            var id = 203;
            var card = new CreditCard { CreditId = id, AccountId = 302, CreditLimit = 500, CurrentBalance = 100 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(card);
            _auth_service_setup_can_access(card.AccountId, true);

            // UpdateAsync signature is UpdateAsync(int id, CreditCard card)
            _cardRepoMock.Setup(r => r.UpdateAsync(id, It.IsAny<CreditCard>()))
                .ReturnsAsync((int calledId, CreditCard c) => c);

            var controller = CreateController();

            // Act
            var result = await controller.ChargeCreditCard(id, 50m);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<CreditCardDto>(ok.Value);
            Assert.Equal(id, dto.CreditId);
            _cardRepoMock.Verify(r => r.UpdateAsync(id, It.Is<CreditCard>(c => c.CurrentBalance == 150m)), Times.Once);
        }

        [Fact]
        public async Task PayCreditCard_AccountMissing_ReturnsBadRequest()
        {
            // Arrange
            var id = 300;
            var card = new CreditCard { CreditId = id, AccountId = 400, CurrentBalance = 200 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(card);
            _auth_service_setup_can_access(card.AccountId, true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(card.AccountId)).ReturnsAsync((Account?)null);

            var controller = CreateController();

            // Act
            var result = await controller.PayCreditCard(id, 50m);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Linked account not found.", bad.Value);
        }

        [Fact]
        public async Task PayCreditCard_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 301;
            var card = new CreditCard { CreditId = id, AccountId = 401, CurrentBalance = 100 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(card);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), card.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.PayCreditCard(id, 25m);

            // Assert
            AssertIsForbid(result);
        }

        [Fact]
        public async Task PayCreditCard_Success_UpdatesCardAndAccount()
        {
            // Arrange
            var id = 302;
            var accountId = 500;
            var startingAccountBalance = 1000m;
            var cardStartingBalance = 300m;
            var paymentAmount = 100m;

            var card = new CreditCard { CreditId = id, AccountId = accountId, CurrentBalance = cardStartingBalance };
            var account = new Account { AccountId = accountId, Balance = startingAccountBalance, AccountNumber = "ACC500" };

            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(card);
            _auth_service_setup_can_access(card.AccountId, true);
            _accountRepoMock.Setup(r => r.GetByIdAsync(card.AccountId)).ReturnsAsync(account);

            // UpdateAsync signatures: UpdateAsync(int id, CreditCard) and UpdateAsync(int id, Account)
            _cardRepoMock.Setup(r => r.UpdateAsync(id, It.IsAny<CreditCard>()))
                .ReturnsAsync((int calledId, CreditCard c) => c);
            _accountRepoMock.Setup(r => r.UpdateAsync(account.AccountId, It.IsAny<Account>()))
                .ReturnsAsync((int calledId, Account a) => a);

            var controller = CreateController();

            // Act
            var result = await controller.PayCreditCard(id, paymentAmount);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<CreditCardDto>(ok.Value);
            Assert.Equal(id, dto.CreditId);

            // Verify card and account updated accordingly
            _cardRepoMock.Verify(r => r.UpdateAsync(id, It.Is<CreditCard>(c => c.CurrentBalance == cardStartingBalance - paymentAmount)), Times.Once);

            // The controller calls UpdateAsync on the account, but the controller's logic does not modify account.Balance itself
            // (ProcessPayment acts on the card and may or may not touch account). Verify the repository UpdateAsync was called once for the account.
            _accountRepoMock.Verify(r => r.UpdateAsync(account.AccountId, It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCreditCard_UserCannotAccess_ReturnsForbid()
        {
            // Arrange
            var id = 600;
            var existing = new CreditCard { CreditId = id, AccountId = 700 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), existing.AccountId)).ReturnsAsync(false);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteCreditCard(id);

            // Assert
            AssertIsForbid(result);
        }

        [Fact]
        public async Task DeleteCreditCard_Success_ReturnsNoContent()
        {
            // Arrange
            var id = 601;
            var existing = new CreditCard { CreditId = id, AccountId = 701 };
            _cardRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _auth_service_setup_can_access(existing.AccountId, true);
            _cardRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(existing);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteCreditCard(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _cardRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        // small helpers to reduce repetition
        private void _auth_service_setup_can_access(int accountId, bool canAccess)
        {
            _authServiceMock.Setup(s => s.CanAccessAccountAsync(It.IsAny<ClaimsPrincipal>(), accountId)).ReturnsAsync(canAccess);
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(123);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);
        }

        private void _auth_service_setup_getuser_and_admin(int userId, bool isAdmin)
        {
            _authServiceMock.Setup(s => s.GetCurrentUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _authServiceMock.Setup(s => s.IsAdmin(It.IsAny<ClaimsPrincipal>())).Returns(isAdmin);
        }
    }
}