using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;

namespace SecureBank.UI.Controllers
{
    public class AccountsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountsController(IHttpClientFactory httpClientFactory)
        {
            this._httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<AccountDetailsDto> accounts = new();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/accounts");
                httpResponseMessage.EnsureSuccessStatusCode();
                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                // Debugging
                System.Diagnostics.Debug.WriteLine($"API Response: {stringResponseBody}");

                accounts = JsonSerializer.Deserialize<List<AccountDetailsDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<AccountDetailsDto>();

                // Debugging
                foreach (var account in accounts)
                {
                    System.Diagnostics.Debug.WriteLine($"Account {account.AccountNumber} has {account.Bills.Count} bills");
                }
            }
            catch (Exception ex)
            {
                // handle error, log, etc.
            }
            return View(accounts);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddAccountViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:7251/api/Accounts"),
                Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            httpResponseMessage.EnsureSuccessStatusCode();

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<AccountDto>();

            if (response is not null)
            {
                return RedirectToAction("Index", "Accounts");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetFromJsonAsync<AccountDto>($"https://localhost:7251/api/accounts/{id.ToString()}");

            if (response is not null)
            {
                return View(response);
            }

            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AccountDto request)
        {
            var client = _httpClientFactory.CreateClient();

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://localhost:7251/api/Accounts/{request.AccountId}"),
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);
            httpResponseMessage.EnsureSuccessStatusCode();

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<AccountDto>();

            if (response is not null)
            {
                return RedirectToAction("Index", "Accounts", new { id = request.AccountId });
            }

            return View();
        }
    }
}

