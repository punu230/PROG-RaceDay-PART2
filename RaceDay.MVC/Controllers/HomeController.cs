using RaceDay.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _apiService;

        public HomeController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // 1. THE MISSING INDEX METHOD (Loads the home page)
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch ONLY active events for the public
                var eventsJson = await _apiService.GetAsync<JsonElement>("events?activeOnly=true");

                var eventList = new List<object>();

                if (eventsJson.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in eventsJson.EnumerateArray())
                    {
                        eventList.Add(new
                        {
                            Id = item.GetProperty("eventID").GetInt32(),
                            Name = item.GetProperty("name").GetString(),
                            Date = item.GetProperty("eventDate").GetDateTime(),
                            Location = item.GetProperty("location").GetString(),
                            Distance = item.GetProperty("routeDistanceKM").GetDecimal(),
                            ImageUrl = item.TryGetProperty("imageUrl", out var img) ? img.GetString() : null // <--- ADD THIS
                        });
                    }
                }

                ViewBag.Events = eventList;
                return View();
            }
            catch (Exception ex)
            {
                // If the API is offline, show the error instead of crashing
                return Content($"Error connecting to API: {ex.Message}");
            }
        }

        // 2. THE EVENT DETAILS METHOD
        [HttpGet]
        public async Task<IActionResult> EventDetails(int id)
        {
            var eventJson = await _apiService.GetAsync<JsonElement>($"events/{id}");

            if (eventJson.ValueKind == JsonValueKind.Undefined || eventJson.ValueKind == JsonValueKind.Null)
            {
                return NotFound("Event not found.");
            }

            var viewModel = new EventDetailsViewModel
            {
                Id = eventJson.GetProperty("eventID").GetInt32(),
                Name = eventJson.GetProperty("name").GetString() ?? "",
                Description = eventJson.GetProperty("description").GetString() ?? "",
                Date = eventJson.GetProperty("eventDate").GetDateTime(),
                Location = eventJson.GetProperty("location").GetString() ?? "",
                Distance = eventJson.GetProperty("routeDistanceKM").GetDecimal(),
                Organiser = eventJson.GetProperty("organiserName").GetString() ?? "",
                ImageUrl = eventJson.TryGetProperty("imageUrl", out var img) ? img.GetString() : null
            };

            if (eventJson.TryGetProperty("categories", out var categoriesJson) && categoriesJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var cat in categoriesJson.EnumerateArray())
                {
                    viewModel.Categories.Add(new CategoryViewModel
                    {
                        Id = cat.GetProperty("categoryID").GetInt32(),
                        Name = cat.GetProperty("name").GetString() ?? "",
                        MinAge = cat.GetProperty("ageGroupMin").ValueKind != JsonValueKind.Null ? cat.GetProperty("ageGroupMin").GetInt32() : 0,
                        MaxAge = cat.GetProperty("ageGroupMax").ValueKind != JsonValueKind.Null ? cat.GetProperty("ageGroupMax").GetInt32() : 99,
                        Gender = cat.GetProperty("gender").ValueKind != JsonValueKind.Null ? cat.GetProperty("gender").GetString() : null,
                        Fee = cat.GetProperty("entryFee").GetDecimal()
                    });
                }
            }

            return View(viewModel);
        }

        // 3. THE ENROL METHOD
        [HttpPost]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            if (HttpContext.Session.GetString("JwtToken") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                TempData["Error"] = "Only participants can enrol in events.";
                return RedirectToAction("Index");
            }

            var response = await _apiService.PostAsync($"categories/{categoryId}/enrol", new { });

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "You have successfully enrolled!";
                return RedirectToAction("MyEnrolments");
            }

            TempData["Error"] = "Failed to enrol. You might already be registered or the deadline has passed.";
            return RedirectToAction("Index");
        }

        // 4. PRIVACY (Just to keep the default footer link working)
        public IActionResult Privacy()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            // 1. Check if user is logged in as a Participant
            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                TempData["Error"] = "You must be logged in as a Participant to view your enrolments.";
                return RedirectToAction("Login", "Auth");
            }

            // 2. Fetch the participant's enrolments from the API
            var enrolmentsJson = await _apiService.GetAsync<JsonElement>("users/enrolments");

            var enrolmentList = new List<object>();

            if (enrolmentsJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in enrolmentsJson.EnumerateArray())
                {
                    enrolmentList.Add(new
                    {
                        Id = item.GetProperty("enrolmentID").GetInt32(),
                        EventName = item.GetProperty("eventName").GetString(),
                        EventDate = item.GetProperty("eventDate").GetDateTime(),
                        CategoryName = item.GetProperty("categoryName").GetString(),
                        IsPaid = item.GetProperty("isPaid").GetBoolean(),
                        Fee = item.GetProperty("entryFee").GetDecimal()
                    });
                }
            }

            ViewBag.Enrolments = enrolmentList;
            return View();
        }
    }
}