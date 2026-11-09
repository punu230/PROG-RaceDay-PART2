using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiService _apiService;

        public ParticipantController(ApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsParticipant()
        {
            return HttpContext.Session.GetString("UserRole") == "Participant";
        }

        // 1. View Personal Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var userJson = await _apiService.GetAsync<JsonElement>("users/profile");

            if (userJson.ValueKind == JsonValueKind.Undefined || userJson.ValueKind == JsonValueKind.Null)
            {
                return Content("Failed to load profile.");
            }

            var profile = new
            {
                FirstName = userJson.GetProperty("firstName").GetString() ?? "",
                LastName = userJson.GetProperty("lastName").GetString() ?? "",
                Email = userJson.GetProperty("email").GetString() ?? "",
                EmergencyContact = userJson.TryGetProperty("emergencyContactName", out var ecn) ? ecn.GetString() : "Not set",
                MedicalConditions = userJson.TryGetProperty("medicalConditions", out var mc) ? mc.GetString() : "None"
            };

            ViewBag.Profile = profile;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string emergencyContactName, string medicalConditions)
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var updateData = new
            {
                emergencyContactName = emergencyContactName,
                medicalConditions = medicalConditions
            };

            var response = await _apiService.PutAsync("users/profile", updateData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Profile updated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to update profile.";
            }

            return RedirectToAction("Profile");
        }

        // 2. View Personal Race Results
        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var resultsJson = await _apiService.GetAsync<JsonElement>("users/results");

            var resultsList = new List<object>();

            if (resultsJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in resultsJson.EnumerateArray())
                {
                    resultsList.Add(new
                    {
                        EventName = item.GetProperty("eventName").GetString(),
                        CategoryName = item.GetProperty("categoryName").GetString(),
                        FinishTime = item.GetProperty("finishTime").GetString(),
                        Position = item.GetProperty("position").GetInt32()
                    });
                }
            }

            ViewBag.Results = resultsList;
            return View();
        }
    }
}
