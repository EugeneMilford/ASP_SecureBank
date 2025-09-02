using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models.DTO;
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

        // Display the add bill payment form
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        // Add a new bill payment
        [HttpPost]
        public async Task<IActionResult> Add(BillDto billDto)
        {
            if (!ModelState.IsValid)
            {
                return View(billDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var content = new StringContent(JsonSerializer.Serialize(billDto), System.Text.Encoding.UTF8, "application/json");
                var httpResponseMessage = await client.PostAsync("https://localhost:7251/api/billpayments", content);

                httpResponseMessage.EnsureSuccessStatusCode();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
                ModelState.AddModelError(string.Empty, "Error posting bill payment.");
                return View(billDto);
            }
        }
    }
}
