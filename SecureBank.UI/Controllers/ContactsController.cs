using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SecureBank.UI.Controllers
{
    public class ContactsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ContactsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: Contacts/Add - Show contact form
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        // POST: Contacts/Add - Submit contact form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Optionally attach JWT token if user is logged in
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var httpRequestMessage = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://localhost:7251/api/Contacts"),
                    Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
                };

                var httpResponseMessage = await client.SendAsync(httpRequestMessage);

                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, "Failed to send message. Please try again.");
                    return View(model);
                }

                var response = await httpResponseMessage.Content.ReadFromJsonAsync<ContactDto>();
                if (response is not null)
                {
                    // Set success flag for modal
                    TempData["ContactSuccess"] = true;
                    TempData["ContactName"] = model.Name;
                    return RedirectToAction("Add");
                }

                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to send message. Please check your connection and try again.");
                return View(model);
            }
        }
    }
}
