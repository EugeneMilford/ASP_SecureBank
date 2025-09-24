using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;

namespace SecureBank.UI.Controllers
{
    public class InvestmentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public InvestmentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display all investments
        public async Task<IActionResult> Index()
        {
            List<InvestmentDto> investments = new();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://localhost:7251/api/investments");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                investments = JsonSerializer.Deserialize<List<InvestmentDto>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<InvestmentDto>();
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
            }
            return View(investments);
        }

        // Display the add investment form
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7251/api/Accounts");
            response.EnsureSuccessStatusCode();

            var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();

            ViewBag.Accounts = accounts
                .Select(a => new SelectListItem
                {
                    Value = a.AccountId.ToString(),
                    Text = a.AccountNumber
                }).ToList();

            // Add investment types dropdown
            ViewBag.InvestmentTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Stocks", Text = "Stocks" },
                new SelectListItem { Value = "Bonds", Text = "Bonds" },
                new SelectListItem { Value = "Mutual Funds", Text = "Mutual Funds" },
                new SelectListItem { Value = "ETFs", Text = "ETFs" },
                new SelectListItem { Value = "Fixed Deposit", Text = "Fixed Deposit" },
                new SelectListItem { Value = "Real Estate", Text = "Real Estate" }
            };

            return View();
        }

        // Add a new investment
        [HttpPost]
        public async Task<IActionResult> Add(AddInvestmentViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:7251/api/Investments"),
                Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();

                // Repopulate dropdowns on error
                await PopulateDropdowns(client);

                ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                return View(model);
            }

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<InvestmentDto>();
            if (response is not null)
            {
                return RedirectToAction("Index", "Investments");
            }

            // Repopulate dropdowns if needed
            await PopulateDropdowns(client);

            return View(model);
        }

        // Investment Details Method
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Get investment details
                var response = await client.GetAsync($"https://localhost:7251/api/investments/{id}");
                response.EnsureSuccessStatusCode();
                var investment = await response.Content.ReadFromJsonAsync<InvestmentDto>();

                // If AccountNumber is missing, try to get it from the account using AccountId
                if (investment != null && string.IsNullOrEmpty(investment.AccountNumber))
                {
                    // First try using AccountId if available
                    if (investment.AccountId > 0)
                    {
                        try
                        {
                            var account = await client.GetFromJsonAsync<AccountDto>(
                                $"https://localhost:7251/api/accounts/{investment.AccountId}"
                            );
                            if (account != null)
                            {
                                investment.AccountNumber = account.AccountNumber;
                            }
                        }
                        catch
                        {
                            // Fall back to getting all investments
                        }
                    }

                    // If still no account number, try getting from all investments
                    if (string.IsNullOrEmpty(investment.AccountNumber))
                    {
                        var allInvestmentsResponse = await client.GetAsync("https://localhost:7251/api/investments");
                        if (allInvestmentsResponse.IsSuccessStatusCode)
                        {
                            var allInvestments = await allInvestmentsResponse.Content.ReadFromJsonAsync<List<InvestmentDto>>();
                            var matchingInvestment = allInvestments?.FirstOrDefault(i => i.InvestmentId == id);
                            if (matchingInvestment != null && !string.IsNullOrEmpty(matchingInvestment.AccountNumber))
                            {
                                investment.AccountNumber = matchingInvestment.AccountNumber;
                            }
                        }
                    }
                }

                return View(investment);
            }
            catch (Exception ex)
            {
                // Handle error
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://localhost:7251/api/investments/{id}");
            response.EnsureSuccessStatusCode();
            var investment = await response.Content.ReadFromJsonAsync<InvestmentDto>();

            return View(investment); // Show confirmation view
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Get the investment info
            var response = await client.GetAsync($"https://localhost:7251/api/investments/{id}");
            response.EnsureSuccessStatusCode();
            var investment = await response.Content.ReadFromJsonAsync<InvestmentDto>();

            if (investment != null)
            {
                // Get the account using AccountId
                var account = await client.GetFromJsonAsync<AccountDto>(
                    $"https://localhost:7251/api/accounts/{investment.AccountId}"
                );

                if (account != null)
                {
                    // When investment is deleted, return the current value to the account
                    // Use CurrentValue instead of InvestmentAmount to account for gains/losses
                    account.Balance = account.Balance + investment.CurrentValue;

                    // Update the account
                    var accountUpdateRequest = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri($"https://localhost:7251/api/accounts/{account.AccountId}"),
                        Content = new StringContent(JsonSerializer.Serialize(account), Encoding.UTF8, "application/json")
                    };

                    var accountUpdateResponse = await client.SendAsync(accountUpdateRequest);
                    if (!accountUpdateResponse.IsSuccessStatusCode)
                    {
                        // Handle error - maybe add model state error or log
                        var errorContent = await accountUpdateResponse.Content.ReadAsStringAsync();
                        // You might want to return an error view here instead of continuing
                    }
                }
            }

            // Delete the investment
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/investments/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            return RedirectToAction("Index");
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(HttpClient client)
        {
            var accountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
            accountsResponse.EnsureSuccessStatusCode();
            var accounts = await accountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
            ViewBag.Accounts = accounts
                .Select(a => new SelectListItem
                {
                    Value = a.AccountId.ToString(),
                    Text = a.AccountNumber
                }).ToList();

            ViewBag.InvestmentTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Stocks", Text = "Stocks" },
                new SelectListItem { Value = "Bonds", Text = "Bonds" },
                new SelectListItem { Value = "Mutual Funds", Text = "Mutual Funds" },
                new SelectListItem { Value = "ETFs", Text = "ETFs" },
                new SelectListItem { Value = "Fixed Deposit", Text = "Fixed Deposit" },
                new SelectListItem { Value = "Real Estate", Text = "Real Estate" }
            };
        }
    }
}