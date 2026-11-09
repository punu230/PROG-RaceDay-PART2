using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                // 1. Send login request to API
                var response = await _apiService.PostAsync("auth/login", new { email, password });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    // 2. Check if the token actually exists
                    if (result.TryGetProperty("token", out var tokenElement))
                    {
                        // 3. Save to session
                        HttpContext.Session.SetString("JwtToken", tokenElement.GetString()!);
                        HttpContext.Session.SetString("UserRole", result.GetProperty("roleName").GetString()!);
                        HttpContext.Session.SetString("UserName", result.GetProperty("firstName").GetString()!);

                        // 4. Redirect safely
                        return result.GetProperty("roleName").GetString() == "Organiser"
                            ? RedirectToAction("Dashboard", "Organiser")
                            : RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Error = "Login failed: Invalid response from API.";
                        return View();
                    }
                }

                ViewBag.Error = "Invalid email or password.";
                return View();
            }
            catch (Exception ex)
            {
                // This will show the actual error message instead of crashing the app!
                ViewBag.Error = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string firstName, string lastName, string roleName)
        {
            var response = await _apiService.PostAsync("auth/register", new
            {
                email,
                password,
                firstName,
                lastName,
                roleName,
                // Optional fields the API expects
                organisationName = roleName == "Organiser" ? $"{firstName}'s Events" : null,
                emergencyContactName = roleName == "Participant" ? "Not provided" : null
            });
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Account created! Please log in.";
                return RedirectToAction("Login");
            }

            // Read the exact error from the API
            var errorContent = await response.Content.ReadAsStringAsync();
            ViewBag.Error = $"API Error ({(int)response.StatusCode}): {errorContent}";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
