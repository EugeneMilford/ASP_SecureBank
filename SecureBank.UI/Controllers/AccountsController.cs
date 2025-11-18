using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

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

                // Manually attach JWT token from session
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/accounts");

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                httpResponseMessage.EnsureSuccessStatusCode();
                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                accounts = JsonSerializer.Deserialize<List<AccountDetailsDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<AccountDetailsDto>();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load accounts.";
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

            // Manually attach JWT token from session
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:7251/api/Accounts"),
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
                TempData["Error"] = $"Failed to create account: {errorContent}";
                return View(model);
            }

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<AccountDto>();

            if (response is not null)
            {
                TempData["Success"] = "Account created successfully!";
                return RedirectToAction("Index", "Accounts");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Manually attach JWT token from session
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var response = await client.GetFromJsonAsync<AccountDto>($"https://localhost:7251/api/accounts/{id}");

                if (response is not null)
                {
                    return View(response);
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
                else if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["Error"] = "You don't have permission to edit this account.";
                    return RedirectToAction("Index", "Accounts");
                }
            }

            TempData["Error"] = "Account not found.";
            return RedirectToAction("Index", "Accounts");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AccountDto request)
        {
            var client = _httpClientFactory.CreateClient();

            // Manually attach JWT token from session
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://localhost:7251/api/Accounts/{request.AccountId}"),
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "AuthUser");
            }

            if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "You don't have permission to edit this account.";
                return RedirectToAction("Index", "Accounts");
            }

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to update account: {errorContent}";
                return View(request);
            }

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<AccountDto>();

            if (response is not null)
            {
                TempData["Success"] = "Account updated successfully!";
                return RedirectToAction("Index", "Accounts");
            }

            return View();
        }
    }
}