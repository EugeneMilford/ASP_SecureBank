using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecureBank.UI.Models;
using SecureBank.UI.Models.DTO;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SecureBank.UI.Controllers
{
    public class TransfersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TransfersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display all transfers
        public async Task<IActionResult> Index()
        {
            List<TransferDto> transfers = new();
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Attach JWT token
                var token = HttpContext.Session.GetString("JWTToken");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var httpResponseMessage = await client.GetAsync("https://localhost:7251/api/transfers");

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "AuthUser");
                }

                httpResponseMessage.EnsureSuccessStatusCode();

                var stringResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                transfers = JsonSerializer.Deserialize<List<TransferDto>>(stringResponseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<TransferDto>();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load transfers.";
            }
            return View(transfers);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var response = await client.GetAsync("https://localhost:7251/api/Accounts");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "AuthUser");
                }

                response.EnsureSuccessStatusCode();

                var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();

                ViewBag.Accounts = accounts
                    .Select(a => new SelectListItem
                    {
                        Value = a.AccountNumber,
                        Text = a.AccountNumber
                    }).ToList();

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load accounts.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddTransferViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (model.FromAccountNumber == model.ToAccountNumber)
            {
                ModelState.AddModelError("", "Source and destination accounts cannot be the same.");

                var accountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
                if (accountsResponse.IsSuccessStatusCode)
                {
                    var accounts = await accountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                    ViewBag.Accounts = accounts
                        .Select(a => new SelectListItem
                        {
                            Value = a.AccountNumber,
                            Text = a.AccountNumber
                        }).ToList();
                }

                return View(model);
            }

            // Set AccountId based on FromAccountNumber
            if (!string.IsNullOrEmpty(model.FromAccountNumber))
            {
                var accountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
                if (accountsResponse.IsSuccessStatusCode)
                {
                    var accounts = await accountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                    var fromAccount = accounts?.FirstOrDefault(a => a.AccountNumber == model.FromAccountNumber);
                    if (fromAccount != null)
                    {
                        model.AccountId = fromAccount.AccountId;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Source account not found.");
                        ViewBag.Accounts = accounts
                            .Select(a => new SelectListItem
                            {
                                Value = a.AccountNumber,
                                Text = a.AccountNumber
                            }).ToList();
                        return View(model);
                    }
                }
            }

            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:7251/api/Transfers"),
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

                // Repopulate dropdown on error
                var accountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
                if (accountsResponse.IsSuccessStatusCode)
                {
                    var accounts = await accountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                    ViewBag.Accounts = accounts
                        .Select(a => new SelectListItem
                        {
                            Value = a.AccountNumber,
                            Text = a.AccountNumber
                        }).ToList();
                }

                ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                return View(model);
            }

            var response = await httpResponseMessage.Content.ReadFromJsonAsync<TransferDto>();
            if (response is not null)
            {
                TempData["Success"] = "Transfer completed successfully!";
                return RedirectToAction("Index", "Transfers");
            }

            // Repopulate accounts if needed
            var accountsResponse2 = await client.GetAsync("https://localhost:7251/api/Accounts");
            if (accountsResponse2.IsSuccessStatusCode)
            {
                var accounts2 = await accountsResponse2.Content.ReadFromJsonAsync<List<AccountDto>>();
                ViewBag.Accounts = accounts2
                    .Select(a => new SelectListItem
                    {
                        Value = a.AccountNumber,
                        Text = a.AccountNumber
                    }).ToList();
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Get the specific transfer
            var transferResponse = await client.GetAsync($"https://localhost:7251/api/transfers/{id}");
            transferResponse.EnsureSuccessStatusCode();
            var transfer = await transferResponse.Content.ReadFromJsonAsync<TransferDto>();

            // If AccountNumber is missing, try to get it from the account using AccountId
            if (transfer != null && string.IsNullOrEmpty(transfer.AccountNumber) && transfer.AccountId > 0)
            {
                try
                {
                    var account = await client.GetFromJsonAsync<AccountDto>(
                        $"https://localhost:7251/api/accounts/{transfer.AccountId}"
                    );
                    if (account != null)
                    {
                        transfer.AccountNumber = account.AccountNumber;
                    }
                }
                catch
                {
                    // If getting account fails, try getting all transfers to find the account number
                    var allTransfersResponse = await client.GetAsync("https://localhost:7251/api/transfers");
                    if (allTransfersResponse.IsSuccessStatusCode)
                    {
                        var allTransfers = await allTransfersResponse.Content.ReadFromJsonAsync<List<TransferDto>>();
                        var matchingTransfer = allTransfers?.FirstOrDefault(t => t.TransferId == id);
                        if (matchingTransfer != null && !string.IsNullOrEmpty(matchingTransfer.AccountNumber))
                        {
                            transfer.AccountNumber = matchingTransfer.AccountNumber;
                        }
                    }
                }
            }

            return View(transfer);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var transfer = await client.GetFromJsonAsync<TransferDto>($"https://localhost:7251/api/transfers/{id}");
            if (transfer is not null)
            {
                return View(transfer);
            }
            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TransferDto request)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 1. Get the original transfer
            var originalTransfer = await client.GetFromJsonAsync<TransferDto>($"https://localhost:7251/api/transfers/{request.TransferId}");
            if (originalTransfer is null)
            {
                ModelState.AddModelError("", "Original transfer not found.");
                return View(request);
            }

            // 2. If amount changed, we need to update BOTH accounts involved in the transfer
            if (originalTransfer.Amount != request.Amount)
            {
                var amountDifference = request.Amount - originalTransfer.Amount;

                // Get the sender account (FromAccount)
                var senderAccount = await client.GetFromJsonAsync<AccountDto>(
                    $"https://localhost:7251/api/accounts/{originalTransfer.AccountId}"
                );
                if (senderAccount is null)
                {
                    ModelState.AddModelError("", "Sender account not found.");
                    return View(request);
                }

                // Get the recipient account (ToAccount)
                var allAccountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
                if (!allAccountsResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Failed to retrieve accounts.");
                    return View(request);
                }
                var allAccounts = await allAccountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                var recipientAccount = allAccounts?.FirstOrDefault(a => a.AccountNumber == originalTransfer.ToAccountNumber);

                if (recipientAccount is null)
                {
                    ModelState.AddModelError("", "Recipient account not found.");
                    return View(request);
                }

                // Update sender account: if amount increased, deduct more; if decreased, add back
                senderAccount.Balance -= amountDifference;

                // Update recipient account: if amount increased, add more; if decreased, subtract
                recipientAccount.Balance += amountDifference;

                // Update sender account
                var senderUpdateRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"https://localhost:7251/api/accounts/{senderAccount.AccountId}"),
                    Content = new StringContent(JsonSerializer.Serialize(senderAccount), Encoding.UTF8, "application/json")
                };
                var senderUpdateResponse = await client.SendAsync(senderUpdateRequest);
                if (!senderUpdateResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Failed to update sender account balance.");
                    return View(request);
                }

                // Update recipient account
                var recipientUpdateRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"https://localhost:7251/api/accounts/{recipientAccount.AccountId}"),
                    Content = new StringContent(JsonSerializer.Serialize(recipientAccount), Encoding.UTF8, "application/json")
                };
                var recipientUpdateResponse = await client.SendAsync(recipientUpdateRequest);
                if (!recipientUpdateResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Failed to update recipient account balance.");
                    return View(request);
                }
            }

            // 3. Update the transfer itself
            var transferUpdateRequest = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://localhost:7251/api/transfers/{request.TransferId}"),
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var transferUpdateResponse = await client.SendAsync(transferUpdateRequest);
            if (!transferUpdateResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to update transfer.");
                return View(request);
            }

            var updatedTransfer = await transferUpdateResponse.Content.ReadFromJsonAsync<TransferDto>();
            if (updatedTransfer is not null)
            {
                return RedirectToAction("Index", "Transfers");
            }

            ModelState.AddModelError("", "Unknown error updating transfer.");
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Get Transfer info for confirmation
            var transferResponse = await client.GetAsync($"https://localhost:7251/api/transfers/{id}");
            transferResponse.EnsureSuccessStatusCode();
            var transfer = await transferResponse.Content.ReadFromJsonAsync<TransferDto>();

            return View(transfer);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = _httpClientFactory.CreateClient();

            // Attach JWT token
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Get Transfer info
            var transferResponse = await client.GetAsync($"https://localhost:7251/api/transfers/{id}");
            transferResponse.EnsureSuccessStatusCode();
            var transfer = await transferResponse.Content.ReadFromJsonAsync<TransferDto>();

            if (transfer != null)
            {
                // Get the sender account (the account that sent the money)
                var senderAccount = await client.GetFromJsonAsync<AccountDto>(
                    $"https://localhost:7251/api/accounts/{transfer.AccountId}"
                );

                // Get the recipient account (the account that received the money)
                var allAccountsResponse = await client.GetAsync("https://localhost:7251/api/Accounts");
                if (allAccountsResponse.IsSuccessStatusCode)
                {
                    var allAccounts = await allAccountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
                    var recipientAccount = allAccounts?.FirstOrDefault(a => a.AccountNumber == transfer.ToAccountNumber);

                    // Reverse the transfer: add money back to sender, subtract from recipient
                    if (senderAccount != null)
                    {
                        senderAccount.Balance += transfer.Amount;

                        var senderUpdateRequest = new HttpRequestMessage()
                        {
                            Method = HttpMethod.Put,
                            RequestUri = new Uri($"https://localhost:7251/api/accounts/{senderAccount.AccountId}"),
                            Content = new StringContent(JsonSerializer.Serialize(senderAccount), Encoding.UTF8, "application/json")
                        };

                        var senderUpdateResponse = await client.SendAsync(senderUpdateRequest);
                        if (!senderUpdateResponse.IsSuccessStatusCode)
                        {
                            var errorContent = await senderUpdateResponse.Content.ReadAsStringAsync();
                            TempData["Error"] = "Failed to update sender balance.";
                        }
                    }

                    if (recipientAccount != null)
                    {
                        recipientAccount.Balance -= transfer.Amount;

                        var recipientUpdateRequest = new HttpRequestMessage()
                        {
                            Method = HttpMethod.Put,
                            RequestUri = new Uri($"https://localhost:7251/api/accounts/{recipientAccount.AccountId}"),
                            Content = new StringContent(JsonSerializer.Serialize(recipientAccount), Encoding.UTF8, "application/json")
                        };

                        var recipientUpdateResponse = await client.SendAsync(recipientUpdateRequest);
                        if (!recipientUpdateResponse.IsSuccessStatusCode)
                        {
                            var errorContent = await recipientUpdateResponse.Content.ReadAsStringAsync();
                            TempData["Error"] = "Failed to update recipient balance.";
                        }
                    }
                }
            }

            // Delete the transfer
            var deleteResponse = await client.DeleteAsync($"https://localhost:7251/api/transfers/{id}");
            deleteResponse.EnsureSuccessStatusCode();

            return RedirectToAction("Index", "Transfers");
        }
    }
}