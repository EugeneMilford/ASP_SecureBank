using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;

namespace SecureBank.UI.Controllers
{
    public class LoansController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoansController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display all loans
        public async Task<IActionResult> Index()
        {
            List<LoanDto> loans = new();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://localhost:7251/api/loans");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                loans = JsonSerializer.Deserialize<List<LoanDto>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<LoanDto>();
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
            }
            return View(loans);
        }

        // Display the add loan form
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

            return View();
        }

        // Add a new loan
        [HttpPost]
        public async Task<IActionResult> Add(AddLoanViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:7251/api/Loans"),
                Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();

                // Repopulate dropdown on error
                var accountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
                accountsResponse.EnsureSuccessStatusCode();
                var accounts = await accountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                ViewBag.Accounts = accounts
                    .Select(a => new SelectListItem
                    {
                        Value = a.AccountId.ToString(),
                        Text = a.AccountNumber
                    }).ToList();

                ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                return View(model);
            }

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<LoanDto>();
            if (response is not null)
            {
                return RedirectToAction("Index", "Loans");
            }

            // Repopulate accounts if needed
            var accountsResponse2 = await client.GetAsync("https://localhost:7251/api/Accounts");
            accountsResponse2.EnsureSuccessStatusCode();
            var accounts2 = await accountsResponse2.Content.ReadFromJsonAsync<List<AccountDto>>();
            ViewBag.Accounts = accounts2
                .Select(a => new SelectListItem
                {
                    Value = a.AccountId.ToString(),
                    Text = a.AccountNumber
                }).ToList();

            return View(model);
        }

        // Fixed Loans Controller Details Method (improved version)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Get loan details
                var response = await client.GetAsync($"https://localhost:7251/api/loans/{id}");
                response.EnsureSuccessStatusCode();
                var loan = await response.Content.ReadFromJsonAsync<LoanDto>();

                // If AccountNumber is missing, try to get it from the account using AccountId
                if (loan != null && string.IsNullOrEmpty(loan.AccountNumber))
                {
                    // First try using AccountId if available
                    if (loan.AccountId > 0)
                    {
                        try
                        {
                            var account = await client.GetFromJsonAsync<AccountDto>(
                                $"https://localhost:7251/api/accounts/{loan.AccountId}"
                            );
                            if (account != null)
                            {
                                loan.AccountNumber = account.AccountNumber;
                            }
                        }
                        catch
                        {
                            // Fall back to getting all loans
                        }
                    }

                    // If still no account number, try getting from all loans
                    if (string.IsNullOrEmpty(loan.AccountNumber))
                    {
                        var allLoansResponse = await client.GetAsync("https://localhost:7251/api/loans");
                        if (allLoansResponse.IsSuccessStatusCode)
                        {
                            var allLoans = await allLoansResponse.Content.ReadFromJsonAsync<List<LoanDto>>();
                            var matchingLoan = allLoans?.FirstOrDefault(l => l.LoanId == id);
                            if (matchingLoan != null && !string.IsNullOrEmpty(matchingLoan.AccountNumber))
                            {
                                loan.AccountNumber = matchingLoan.AccountNumber;
                            }
                        }
                    }
                }

                return View(loan);
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
            var response = await client.GetAsync($"https://localhost:7251/api/loans/{id}");
            response.EnsureSuccessStatusCode();
            var loan = await response.Content.ReadFromJsonAsync<LoanDto>();

            return View(loan); // Show confirmation view
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Get the loan info
            var response = await client.GetAsync($"https://localhost:7251/api/loans/{id}");
            response.EnsureSuccessStatusCode();
            var loan = await response.Content.ReadFromJsonAsync<LoanDto>();

            if (loan != null)
            {
                // Get the account using AccountId (assuming LoanDto has AccountId property)
                // If LoanDto doesn't have AccountId, you'll need to add it to match the pattern
                var account = await client.GetFromJsonAsync<AccountDto>(
                    $"https://localhost:7251/api/accounts/{loan.AccountId}"
                );

                if (account != null)
                {
                    // Subtract the loan amount from the account balance
                    // When loan is deleted, the money should be removed from the account
                    account.Balance = account.Balance - loan.LoanAmount;

                    // Update the account using the same approach as Bills controller
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

            // Delete the loan
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/loans/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            return RedirectToAction("Index");
        }
    }
}
