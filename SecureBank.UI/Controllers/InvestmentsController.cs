using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models.DTO;
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
                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/investments");

                httpResponseMessage.EnsureSuccessStatusCode();

                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                investments = JsonSerializer.Deserialize<List<InvestmentDto>>(stringResponseBody, new JsonSerializerOptions
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
        public IActionResult Add()
        {
            return View();
        }

        // Add a new investment
        [HttpPost]
        public async Task<IActionResult> Add(InvestmentDto investmentDto)
        {
            if (!ModelState.IsValid)
            {
                return View(investmentDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var content = new StringContent(JsonSerializer.Serialize(investmentDto), System.Text.Encoding.UTF8, "application/json");
                var httpResponseMessage = await client.PostAsync("https://localhost:7251/api/investments", content);

                httpResponseMessage.EnsureSuccessStatusCode();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
                ModelState.AddModelError(string.Empty, "Error posting investment.");
                return View(investmentDto);
            }
        }
    }
}
