using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

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

                // Attach JWT token
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync("https://localhost:7251/api/loans");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                loans = JsonSerializer.Deserialize<List<LoanDto>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<LoanDto>();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load loans.";
            }
            return View(loans);
        }

        // Display the add loan form
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Attach JWT token
                var token = HttpContext.Session.GetString("JWTToken");

                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Please log in to continue.";
                    return RedirectToAction("Login", "AuthUser");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("https://localhost:7251/api/Accounts");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = $"Unable to load accounts. Status: {response.StatusCode}";
                    return RedirectToAction("Index");
                }

                var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();

                if (accounts == null || !accounts.Any())
                {
                    TempData["Error"] = "No accounts available. Please create an account first.";
                    return RedirectToAction("Index", "Accounts");
                }

                ViewBag.Accounts = accounts
                    .Select(a => new SelectListItem
                    {
                        Value = a.AccountId.ToString(),
                        Text = a.AccountNumber
                    }).ToList();

                return View();
            }
            catch (HttpRequestException ex)
            {
                TempData["Error"] = $"Connection error: {ex.Message}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unable to load accounts: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Add a new loan
        [HttpPost]
        public async Task<IActionResult> Add(AddLoanViewModel model)
        {
            try
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
                    RequestUri = new Uri("https://localhost:7251/api/Loans"),
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
                    try
                    {
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
                    }
                    catch
                    {
                        // If can't load accounts, just show error
                        ViewBag.Accounts = new List<SelectListItem>();
                    }

                    ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                    return View(model);
                }

                var response = await httpResponseMessage.Content.ReadFromJsonAsync<LoanDto>();
                if (response is not null)
                {
                    TempData["Success"] = "Loan created successfully!";
                    return RedirectToAction("Index", "Loans");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Fixed Loans Controller Details Method (improved version)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Attach JWT token
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // Get loan details
                var response = await client.GetAsync($"https://localhost:7251/api/loans/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

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
                TempData["Error"] = "Unable to load loan details.";
                return RedirectToAction("Index");
            }
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

            try
            {
                var loan = await client.GetFromJsonAsync<LoanDto>($"https://localhost:7251/api/loans/{id}");

                if (loan is not null)
                {
                    // Get accounts for dropdown
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

                    return View(loan);
                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }
            }

            TempData["Error"] = "Loan not found.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(LoanDto request)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var loanUpdateRequest = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://localhost:7251/api/loans/{request.LoanId}"),
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var loanUpdateResponse = await client.SendAsync(loanUpdateRequest);

            if (loanUpdateResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "AuthUser");
            }

            if (!loanUpdateResponse.IsSuccessStatusCode)
            {
                var errorContent = await loanUpdateResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to update loan: {errorContent}");
                return View(request);
            }

            var updatedLoan = await loanUpdateResponse.Content.ReadFromJsonAsync<LoanDto>();
            if (updatedLoan is not null)
            {
                TempData["Success"] = "Loan updated successfully!";
                return RedirectToAction("Index", "Loans");
            }

            ModelState.AddModelError("", "Unknown error updating loan.");
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

            try
            {
                var response = await client.GetAsync($"https://localhost:7251/api/loans/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                response.EnsureSuccessStatusCode();
                var loan = await response.Content.ReadFromJsonAsync<LoanDto>();

                return View(loan);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load loan.";
                return RedirectToAction("Index");
            }
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

            // Get the loan info
            var response = await client.GetAsync($"https://localhost:7251/api/loans/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "AuthUser");
            }

            response.EnsureSuccessStatusCode();
            var loan = await response.Content.ReadFromJsonAsync<LoanDto>();

            if (loan != null)
            {
                // Get the account using AccountId
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
                        var errorContent = await accountUpdateResponse.Content.ReadAsStringAsync();
                        TempData["Warning"] = "Loan deleted but failed to update account balance.";
                    }
                }
            }

            // Delete the loan
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/loans/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            TempData["Success"] = "Loan deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}