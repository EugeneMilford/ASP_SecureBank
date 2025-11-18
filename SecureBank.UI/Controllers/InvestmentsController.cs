using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

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

                // Attach JWT token
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync("https://localhost:7251/api/investments");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                investments = JsonSerializer.Deserialize<List<InvestmentDto>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<InvestmentDto>();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load investments.";
            }
            return View(investments);
        }

        // Display the add investment form
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
            catch (HttpRequestException ex)
            {
                TempData["Error"] = $"Connection error: {ex.Message}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unable to load form: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Add a new investment
        [HttpPost]
        public async Task<IActionResult> Add(AddInvestmentViewModel model)
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
                    RequestUri = new Uri("https://localhost:7251/api/Investments"),
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

                    // Repopulate dropdowns on error
                    try
                    {
                        await PopulateDropdowns(client);
                    }
                    catch
                    {
                        ViewBag.Accounts = new List<SelectListItem>();
                        ViewBag.InvestmentTypes = new List<SelectListItem>();
                    }

                    ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                    return View(model);
                }

                var response = await httpResponseMessage.Content.ReadFromJsonAsync<InvestmentDto>();
                if (response is not null)
                {
                    TempData["Success"] = "Investment created successfully!";
                    return RedirectToAction("Index", "Investments");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Investment Details Method
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

                // Get investment details
                var response = await client.GetAsync($"https://localhost:7251/api/investments/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                response.EnsureSuccessStatusCode();
                var investment = await response.Content.ReadFromJsonAsync<InvestmentDto>();

                // If AccountNumber is missing, try to get it from the account using AccountId
                if (investment != null && string.IsNullOrEmpty(investment.AccountNumber))
                {
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
                TempData["Error"] = "Unable to load investment details.";
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
                var investment = await client.GetFromJsonAsync<InvestmentDto>($"https://localhost:7251/api/investments/{id}");

                if (investment is not null)
                {
                    // Populate dropdowns
                    await PopulateDropdowns(client);
                    return View(investment);
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

            TempData["Error"] = "Investment not found.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(InvestmentDto request)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var investmentUpdateRequest = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://localhost:7251/api/investments/{request.InvestmentId}"),
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var investmentUpdateResponse = await client.SendAsync(investmentUpdateRequest);

            if (investmentUpdateResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "AuthUser");
            }

            if (!investmentUpdateResponse.IsSuccessStatusCode)
            {
                var errorContent = await investmentUpdateResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to update investment: {errorContent}");
                return View(request);
            }

            var updatedInvestment = await investmentUpdateResponse.Content.ReadFromJsonAsync<InvestmentDto>();
            if (updatedInvestment is not null)
            {
                TempData["Success"] = "Investment updated successfully!";
                return RedirectToAction("Index", "Investments");
            }

            ModelState.AddModelError("", "Unknown error updating investment.");
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
                var response = await client.GetAsync($"https://localhost:7251/api/investments/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                response.EnsureSuccessStatusCode();
                var investment = await response.Content.ReadFromJsonAsync<InvestmentDto>();

                return View(investment);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load investment.";
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

            // Get the investment info
            var response = await client.GetAsync($"https://localhost:7251/api/investments/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "AuthUser");
            }

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
                        var errorContent = await accountUpdateResponse.Content.ReadAsStringAsync();
                        TempData["Warning"] = "Investment deleted but failed to update account balance.";
                    }
                }
            }

            // Delete the investment
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/investments/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            TempData["Success"] = "Investment deleted successfully!";
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