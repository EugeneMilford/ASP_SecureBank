using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models.DTO;
using System.Text.Json;

namespace SecureBank.UI.Controllers
{
    public class CreditCardsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CreditCardsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display all credit cards
        public async Task<IActionResult> Index()
        {
            List<CreditCardDto> creditCards = new();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/creditcards");

                httpResponseMessage.EnsureSuccessStatusCode();

                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                creditCards = JsonSerializer.Deserialize<List<CreditCardDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CreditCardDto>();
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
            }
            return View(creditCards);
        }

        // Display the add credit card form
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        // Add a new credit card
        [HttpPost]
        public async Task<IActionResult> Add(CreditCardDto creditCardDto)
        {
            if (!ModelState.IsValid)
            {
                return View(creditCardDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var content = new StringContent(JsonSerializer.Serialize(creditCardDto), System.Text.Encoding.UTF8, "application/json");
                var httpResponseMessage = await client.PostAsync("https://localhost:7251/api/creditcards", content);

                httpResponseMessage.EnsureSuccessStatusCode();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Handle error (log, show error view, etc.)
                ModelState.AddModelError(string.Empty, "Error posting credit card.");
                return View(creditCardDto);
            }
        }
    }
}
