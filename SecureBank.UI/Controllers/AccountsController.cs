using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models.DTO;
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

        public async Task<IActionResult> Index()
        {
            List<AccountDetailsDto> accounts = new();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/accounts");
                httpResponseMessage.EnsureSuccessStatusCode();
                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                // Add this for debugging
                System.Diagnostics.Debug.WriteLine($"API Response: {stringResponseBody}");

                accounts = JsonSerializer.Deserialize<List<AccountDetailsDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<AccountDetailsDto>();

                // Add this for debugging
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
    }
}

