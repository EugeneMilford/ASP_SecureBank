using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models.DTO;
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
                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/loans");

                httpResponseMessage.EnsureSuccessStatusCode();

                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                loans = JsonSerializer.Deserialize<List<LoanDto>>(stringResponseBody, new JsonSerializerOptions
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
        public IActionResult Add()
        {
            return View();
        }

        // Add a new loan
        [HttpPost]
        public async Task<IActionResult> Add(LoanDto loanDto)
        {
            if (!ModelState.IsValid)
            {
                return View(loanDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var content = new StringContent(JsonSerializer.Serialize(loanDto), System.Text.Encoding.UTF8, "application/json");
                var httpResponseMessage = await client.PostAsync("https://localhost:7251/api/loans", content);

                httpResponseMessage.EnsureSuccessStatusCode();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
                ModelState.AddModelError(string.Empty, "Error posting loan.");
                return View(loanDto);
            }
        }
    }
}
