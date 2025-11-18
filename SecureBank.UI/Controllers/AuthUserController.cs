using Microsoft.AspNetCore.Mvc;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;

namespace SecureBank.UI.Controllers
{
    public class AuthUserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthUserController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // GET: AuthUser/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to home
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken")))
            {
                return RedirectToAction("Index", "Home");
            }

            UserLoginRequestDto obj = new UserLoginRequestDto();
            return View(obj);
        }

        // POST: AuthUser/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginRequestDto obj)
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            try
            {
                // Get API base URL from configuration
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7251";

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();

                // Serialize login request
                var jsonContent = JsonSerializer.Serialize(obj);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Call API login endpoint
                var response = await client.PostAsync($"{apiBaseUrl}/api/Users/UserLogin", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Deserialize response
                    var loginResponse = JsonSerializer.Deserialize<UserLoginResponseDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                    {
                        // Store authentication data in session
                        HttpContext.Session.SetString("JWTToken", loginResponse.Token);
                        HttpContext.Session.SetString("UserId", loginResponse.UserDetails.UserId.ToString());
                        HttpContext.Session.SetString("Username", loginResponse.UserDetails.Username);
                        HttpContext.Session.SetString("FirstName", loginResponse.UserDetails.FirstName);
                        HttpContext.Session.SetString("LastName", loginResponse.UserDetails.LastName);
                        HttpContext.Session.SetString("Email", loginResponse.UserDetails.Email);
                        HttpContext.Session.SetString("Role", loginResponse.UserDetails.Role);

                        // Set success message
                        TempData["Success"] = $"Welcome back, {loginResponse.UserDetails.FirstName}!";

                        // Redirect based on role
                        if (loginResponse.UserDetails.Role == "Admin")
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        TempData["Error"] = "Invalid response from server. Please try again.";
                        return View(obj);
                    }
                }
                else
                {
                    TempData["Error"] = "Invalid username or password.";
                    return View(obj);
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["Error"] = "Unable to connect to the server. Please try again later.";
                return View(obj);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return View(obj);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new UserRegisterRequestDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegisterRequestDto obj)
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7251";
                var client = _httpClientFactory.CreateClient();
                var jsonContent = JsonSerializer.Serialize(obj);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{apiBaseUrl}/api/Users/Register", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Registration successful! Please log in.";
                    return RedirectToAction("Login");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = error;
                    return View(obj);
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the server. Please try again later.";
                return View(obj);
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return View(obj);
            }
        }

        // GET: AuthUser/Logout
        public IActionResult Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Set logout message
            TempData["Info"] = "You have been successfully logged out.";

            // Redirect to login page
            return RedirectToAction("Index", "Welcome");
        }

        // GET: AuthUser/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
