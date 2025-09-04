using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;

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
                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/billpayments");

                httpResponseMessage.EnsureSuccessStatusCode();

                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                bills = JsonSerializer.Deserialize<List<BillDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<BillDto>();
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
            }
            return View(bills);
        }

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

        [HttpPost]
        public async Task<IActionResult> Add(AddBillViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:7251/api/BillPayments"),
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

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<BillDto>();
            if (response is not null)
            {
                return RedirectToAction("Index", "BillPayments");
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

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var billResponse = await client.GetAsync($"https://localhost:7251/api/billpayments/{id}");
            billResponse.EnsureSuccessStatusCode();
            var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>();
            return View(bill);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient();

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
            // Get Bill info for confirmation (optional)
            var client = _httpClientFactory.CreateClient();
            var billResponse = await client.GetAsync($"https://localhost:7251/api/billpayments/{id}");
            billResponse.EnsureSuccessStatusCode();
            var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>();

            return View(bill); // Show confirmation view
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Get Bill info
            var billResponse = await client.GetAsync($"https://localhost:7251/api/billpayments/{id}");
            billResponse.EnsureSuccessStatusCode();
            var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>();

            if (bill != null)
            {
                // Get the AccountDto using AccountNumber
                var accountsResponse = await client.GetAsync($"https://localhost:7251/api/accounts?accountNumber={bill.AccountNumber}");
                accountsResponse.EnsureSuccessStatusCode();
                var accounts = await accountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                var account = accounts.FirstOrDefault();

                if (account != null)
                {
                    // Add the bill amount back to the account balance
                    var newBalance = account.Balance + bill.Amount;

                    // Prepare request body 
                    var updateRequest = new
                    {
                        AccountNumber = account.AccountNumber,
                        Balance = newBalance,
                        AccountType = account.AccountType,
                        CreatedDate = account.CreatedDate
                    };

                    var updateHttpRequest = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri($"https://localhost:7251/api/accounts/{account.AccountId}"),
                        Content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json")
                    };

                    var updateResponse = await client.SendAsync(updateHttpRequest);
                    updateResponse.EnsureSuccessStatusCode();
                }
            }

            // Delete the bill payment
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/billpayments/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            return RedirectToAction("Index", "BillPayments");
        }
    }
}
