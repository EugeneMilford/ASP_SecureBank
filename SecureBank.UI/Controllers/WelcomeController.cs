using Microsoft.AspNetCore.Mvc;

namespace SecureBank.UI.Controllers
{
    public class WelcomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
