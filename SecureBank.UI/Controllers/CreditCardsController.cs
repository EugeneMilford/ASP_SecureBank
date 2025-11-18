using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SecureBank.UI.Controllers
{
    public class CreditCardsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CreditCardsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display all credit cards
        public async Task<IActionResult> Index()
        {
            List<CreditCardDto> creditCards = new();
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Attach JWT token
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/creditcards");

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                httpResponseMessage.EnsureSuccessStatusCode();

                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                creditCards = JsonSerializer.Deserialize<List<CreditCardDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CreditCardDto>();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load credit cards.";
            }
            return View(creditCards);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var response = await client.GetAsync("https://localhost:7251/api/Accounts");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "AuthUser");
                }

                response.EnsureSuccessStatusCode();

                var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();

                ViewBag.Accounts = accounts
                    .Select(a => new SelectListItem
                    {
                        Value = a.AccountId.ToString(),
                        Text = a.AccountNumber
                    }).ToList();

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load accounts.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddCreditViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:7251/api/CreditCards"),
                Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "AuthUser");
            }

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();

                // Repopulate dropdown on error
                var accountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
                if (accountsResponse.IsSuccessStatusCode)
                {
                    var accounts = await accountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                    ViewBag.Accounts = accounts
                        .Select(a => new SelectListItem
                        {
                            Value = a.AccountId.ToString(),
                            Text = a.AccountNumber
                        }).ToList();
                }

                ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                return View(model);
            }

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<CreditCardDto>();
            if (response is not null)
            {
                TempData["Success"] = "Credit card created successfully!";
                return RedirectToAction("Index", "CreditCards");
            }

            // Repopulate accounts if needed
            var accountsResponse2 = await client.GetAsync("https://localhost:7251/api/Accounts");
            if (accountsResponse2.IsSuccessStatusCode)
            {
                var accounts2 = await accountsResponse2.Content.ReadFromJsonAsync<List<AccountDto>>();
                ViewBag.Accounts = accounts2
                    .Select(a => new SelectListItem
                    {
                        Value = a.AccountId.ToString(),
                        Text = a.AccountNumber
                    }).ToList();
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Get the specific credit card
            var creditCardResponse = await client.GetAsync($"https://localhost:7251/api/creditcards/{id}");
            creditCardResponse.EnsureSuccessStatusCode();
            var creditCard = await creditCardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

            // If AccountNumber is missing, try to get it from the account using AccountId
            if (creditCard != null && string.IsNullOrEmpty(creditCard.AccountNumber) && creditCard.AccountId > 0)
            {
                try
                {
                    var account = await client.GetFromJsonAsync<AccountDto>(
                        $"https://localhost:7251/api/accounts/{creditCard.AccountId}"
                    );
                    if (account != null)
                    {
                        creditCard.AccountNumber = account.AccountNumber;
                    }
                }
                catch
                {
                    // If getting account fails, try getting all credit cards to find the account number
                    var allCardsResponse = await client.GetAsync("https://localhost:7251/api/creditcards");
                    if (allCardsResponse.IsSuccessStatusCode)
                    {
                        var allCards = await allCardsResponse.Content.ReadFromJsonAsync<List<CreditCardDto>>();
                        var matchingCard = allCards?.FirstOrDefault(c => c.CreditId == id);
                        if (matchingCard != null && !string.IsNullOrEmpty(matchingCard.AccountNumber))
                        {
                            creditCard.AccountNumber = matchingCard.AccountNumber;
                        }
                    }
                }
            }

            return View(creditCard);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var creditCard = await client.GetFromJsonAsync<CreditCardDto>($"https://localhost:7251/api/creditcards/{id}");
            if (creditCard is not null)
            {
                return View(creditCard);
            }
            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreditCardDto request)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Update the credit card
            var cardUpdateRequest = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://localhost:7251/api/creditcards/{request.CreditId}"),
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var cardUpdateResponse = await client.SendAsync(cardUpdateRequest);
            if (!cardUpdateResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to update credit card.");
                return View(request);
            }

            var updatedCard = await cardUpdateResponse.Content.ReadFromJsonAsync<CreditCardDto>();
            if (updatedCard is not null)
            {
                TempData["Success"] = "Credit card updated successfully!";
                return RedirectToAction("Index", "CreditCards");
            }

            ModelState.AddModelError("", "Unknown error updating credit card.");
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Get Credit Card info for confirmation
            var cardResponse = await client.GetAsync($"https://localhost:7251/api/creditcards/{id}");
            cardResponse.EnsureSuccessStatusCode();
            var creditCard = await cardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

            return View(creditCard);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Get Credit Card info
            var cardResponse = await client.GetAsync($"https://localhost:7251/api/creditcards/{id}");
            cardResponse.EnsureSuccessStatusCode();
            var creditCard = await cardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

            if (creditCard != null)
            {
                // Get the account using AccountId
                var account = await client.GetFromJsonAsync<AccountDto>(
                    $"https://localhost:7251/api/accounts/{creditCard.AccountId}"
                );

                if (account != null)
                {
                    // When deleting a credit card, subtract the current balance from the account
                    account.Balance = account.Balance - creditCard.CurrentBalance;

                    var accountUpdateRequest = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri($"https://localhost:7251/api/accounts/{account.AccountId}"),
                        Content = new StringContent(JsonSerializer.Serialize(account), Encoding.UTF8, "application/json")
                    };

                    var accountUpdateResponse = await client.SendAsync(accountUpdateRequest);
                    if (!accountUpdateResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await accountUpdateResponse.Content.ReadAsStringAsync();
                        TempData["Error"] = "Failed to update account balance.";
                    }
                }
            }

            // Delete the credit card
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/creditcards/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            TempData["Success"] = "Credit card deleted successfully!";
            return RedirectToAction("Index", "CreditCards");
        }

        // Additional action for processing charges
        [HttpPost]
        public async Task<IActionResult> ProcessCharge(int id, decimal amount)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var chargeRequest = new
            {
                Amount = amount
            };

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://localhost:7251/api/creditcards/{id}/charge"),
                Content = new StringContent(JsonSerializer.Serialize(chargeRequest), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                TempData["Success"] = "Charge processed successfully!";
                return RedirectToAction("Details", new { id = id });
            }

            var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();
            TempData["Error"] = $"Failed to process charge: {errorContent}";
            return RedirectToAction("Details", new { id = id });
        }

        // Additional action for processing payments
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int id, decimal amount)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var paymentRequest = new
            {
                Amount = amount
            };

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://localhost:7251/api/creditcards/{id}/payment"),
                Content = new StringContent(JsonSerializer.Serialize(paymentRequest), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                TempData["Success"] = "Payment processed successfully!";
                return RedirectToAction("Details", new { id = id });
            }

            var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();
            TempData["Error"] = $"Failed to process payment: {errorContent}";
            return RedirectToAction("Details", new { id = id });
        }
    }
}
