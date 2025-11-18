using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SecureBank.UI.Controllers
{
    public class BillPaymentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BillPaymentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display all bill payments
        public async Task<IActionResult> Index()
        {
            List<BillDto> bills = new();
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Attach JWT token
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/billpayments");

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                httpResponseMessage.EnsureSuccessStatusCode();

                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                bills = JsonSerializer.Deserialize<List<BillDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<BillDto>();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load bill payments.";
            }
            return View(bills);
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
        public async Task<IActionResult> Add(AddBillViewModel model)
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
                RequestUri = new Uri("https://localhost:7251/api/BillPayments"),
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

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<BillDto>();
            if (response is not null)
            {
                TempData["Success"] = "Bill payment created successfully!";
                return RedirectToAction("Index", "BillPayments");
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

            // Get the specific bill
            var billResponse = await client.GetAsync($"https://localhost:7251/api/billpayments/{id}");
            billResponse.EnsureSuccessStatusCode();
            var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>();

            // If AccountNumber is missing, try to get it from the account using AccountId
            if (bill != null && string.IsNullOrEmpty(bill.AccountNumber) && bill.AccountId > 0)
            {
                try
                {
                    var account = await client.GetFromJsonAsync<AccountDto>(
                        $"https://localhost:7251/api/accounts/{bill.AccountId}"
                    );
                    if (account != null)
                    {
                        bill.AccountNumber = account.AccountNumber;
                    }
                }
                catch
                {
                    // If getting account fails, try getting all bills to find the account number
                    var allBillsResponse = await client.GetAsync("https://localhost:7251/api/billpayments");
                    if (allBillsResponse.IsSuccessStatusCode)
                    {
                        var allBills = await allBillsResponse.Content.ReadFromJsonAsync<List<BillDto>>();
                        var matchingBill = allBills?.FirstOrDefault(b => b.BillId == id);
                        if (matchingBill != null && !string.IsNullOrEmpty(matchingBill.AccountNumber))
                        {
                            bill.AccountNumber = matchingBill.AccountNumber;
                        }
                    }
                }
            }

            return View(bill);
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

            var bill = await client.GetFromJsonAsync<BillDto>($"https://localhost:7251/api/billpayments/{id}");
            if (bill is not null)
            {
                return View(bill);
            }
            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BillDto request)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 1. Get the original bill
            var originalBill = await client.GetFromJsonAsync<BillDto>($"https://localhost:7251/api/billpayments/{request.BillId}");
            if (originalBill is null)
            {
                ModelState.AddModelError("", "Original bill not found.");
                return View(request);
            }

            // 2. If amount changed, update the account by AccountId
            if (originalBill.Amount != request.Amount)
            {
                // Get the associated account using AccountId from the bill
                var account = await client.GetFromJsonAsync<AccountDto>(
                    $"https://localhost:7251/api/accounts/{originalBill.AccountId}"
                );
                if (account is null)
                {
                    ModelState.AddModelError("", "Associated account not found.");
                    return View(request);
                }

                // Calculate new balance: Remove original bill amount, add new bill amount
                account.Balance = account.Balance + originalBill.Amount - request.Amount;

                var accountUpdateRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"https://localhost:7251/api/accounts/{account.AccountId}"),
                    Content = new StringContent(JsonSerializer.Serialize(account), Encoding.UTF8, "application/json")
                };
                var accountUpdateResponse = await client.SendAsync(accountUpdateRequest);
                if (!accountUpdateResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Failed to update account balance.");
                    return View(request);
                }
            }

            // 3. Update the bill itself
            var billUpdateRequest = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://localhost:7251/api/billpayments/{request.BillId}"),
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var billUpdateResponse = await client.SendAsync(billUpdateRequest);
            if (!billUpdateResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to update bill payment.");
                return View(request);
            }

            var updatedBill = await billUpdateResponse.Content.ReadFromJsonAsync<BillDto>();
            if (updatedBill is not null)
            {
                return RedirectToAction("Index", "BillPayments");
            }

            ModelState.AddModelError("", "Unknown error updating bill.");
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

            // Get Bill info for confirmation
            var billResponse = await client.GetAsync($"https://localhost:7251/api/billpayments/{id}");
            billResponse.EnsureSuccessStatusCode();
            var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>();

            return View(bill);
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

            // Get Bill info
            var billResponse = await client.GetAsync($"https://localhost:7251/api/billpayments/{id}");
            billResponse.EnsureSuccessStatusCode();
            var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>();

            if (bill != null)
            {
                // Get the account using AccountId
                var account = await client.GetFromJsonAsync<AccountDto>(
                    $"https://localhost:7251/api/accounts/{bill.AccountId}"
                );

                if (account != null)
                {
                    // Add the bill amount back to the account balance
                    account.Balance = account.Balance + bill.Amount;

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

            // Delete the bill payment
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/billpayments/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            return RedirectToAction("Index", "BillPayments");
        }
    }
}