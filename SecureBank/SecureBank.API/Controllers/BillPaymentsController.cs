using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SecureBank.API.Controllers
{
    // https://localhost:7043/api/billpayments
    [Route("api/[controller]")]
    [ApiController]
    public class BillPaymentsController : ControllerBase
    {
        // GET: https://localhost:7043/api/billpayments
        [HttpGet]
        public async Task<ActionResult> GetBillPayments()
        {
            return Ok();
        }
    }
}
