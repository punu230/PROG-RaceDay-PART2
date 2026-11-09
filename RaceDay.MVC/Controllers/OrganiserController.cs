using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly ApiService _apiService;

        public OrganiserController(ApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsOrganiser()
        {
            return HttpContext.Session.GetString("UserRole") == "Organiser";
        }

        // 1. DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var eventsJson = await _apiService.GetAsync<JsonElement>("events?activeOnly=false");
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
                        Distance = item.GetProperty("routeDistanceKM").GetDecimal()
                    });
                }
            }

            ViewBag.Events = eventList;
            return View();
        }

        // 2. CREATE EVENT (GET)
        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // 3. CREATE EVENT (POST)
        [HttpPost]
        public async Task<IActionResult> CreateEvent(string name, string description, DateTime eventDate, string location, decimal routeDistanceKM, DateTime registrationDeadline, IFormFile imageFile)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            string? imageUrl = null;

            // 1. Upload the image to Azure via the API
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadResponse = await _apiService.PostFileAsync("images/upload", imageFile);
                if (uploadResponse.IsSuccessStatusCode)
                {
                    var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
                    var uploadResult = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(uploadContent);
                    imageUrl = uploadResult.GetProperty("imageUrl").GetString();
                }
                else
                {
                    var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                    ViewBag.Error = $"Image Upload Failed ({(int)uploadResponse.StatusCode}): {errorContent}";
                    return View();
                }
            }

            // 2. Create the event
            var newEvent = new
            {
                name = name,
                description = description,
                eventDate = eventDate,
                location = location,
                routeDistanceKM = routeDistanceKM,
                registrationDeadline = registrationDeadline,
                imageUrl = imageUrl // Pass the Azure URL
            };

            var response = await _apiService.PostAsync("events", newEvent);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Failed to create event. Check your dates.";
            return View();
        }

        // 4. MANAGE EVENT (GET)
        [HttpGet]
        public async Task<IActionResult> ManageEvent(int id)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var eventJson = await _apiService.GetAsync<JsonElement>($"events/{id}");

            if (eventJson.ValueKind == JsonValueKind.Undefined || eventJson.ValueKind == JsonValueKind.Null)
            {
                return Content("Error: Could not load event from API.");
            }

            ViewBag.EventId = id;
            ViewBag.EventName = eventJson.GetProperty("name").GetString();

            var categoriesList = new List<object>();
            if (eventJson.TryGetProperty("categories", out var categoriesJson) && categoriesJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var cat in categoriesJson.EnumerateArray())
                {
                    categoriesList.Add(new
                    {
                        Id = cat.GetProperty("categoryID").GetInt32(),
                        Name = cat.GetProperty("name").GetString(),
                        Fee = cat.GetProperty("entryFee").GetDecimal()
                    });
                }
            }
            ViewBag.Categories = categoriesList;

            return View();
        }

        // 5. ADD CATEGORY (POST)
        [HttpPost]
        public async Task<IActionResult> AddCategory(int eventId, string name, decimal entryFee, string gender)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var newCategory = new
            {
                name = name,
                ageGroupMin = 18,
                ageGroupMax = 99,
                gender = string.IsNullOrEmpty(gender) ? null : gender,
                entryFee = entryFee
            };

            var response = await _apiService.PostAsync($"events/{eventId}/categories", newCategory);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category added successfully!";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"API Error ({(int)response.StatusCode}): {errorContent}";
            }

            return RedirectToAction("ManageEvent", new { id = eventId });
        }

        // 6. VIEW ENROLMENTS FOR AN EVENT
        [HttpGet]
        public async Task<IActionResult> ViewEnrolments(int eventId)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            ViewBag.EventId = eventId;

            var eventJson = await _apiService.GetAsync<JsonElement>($"events/{eventId}");
            var enrolmentsList = new List<object>();

            if (eventJson.TryGetProperty("categories", out var categoriesJson) && categoriesJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var cat in categoriesJson.EnumerateArray())
                {
                    int categoryId = cat.GetProperty("categoryID").GetInt32();

                    var enrolmentsJson = await _apiService.GetAsync<JsonElement>($"events/{eventId}/categories/{categoryId}/enrolments");

                    if (enrolmentsJson.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var enr in enrolmentsJson.EnumerateArray())
                        {
                            enrolmentsList.Add(new
                            {
                                EnrolmentId = enr.GetProperty("enrolmentID").GetInt32(),
                                ParticipantName = enr.GetProperty("participantName").GetString(),
                                CategoryName = cat.GetProperty("name").GetString(),
                                IsPaid = enr.GetProperty("isPaid").GetBoolean()
                            });
                        }
                    }
                }
            }

            ViewBag.Enrolments = enrolmentsList;
            return View();
        }

        // 7. ADD RESULT FOR A PARTICIPANT
        [HttpPost]
        public async Task<IActionResult> AddResult(int enrolmentId, int eventId, string finishTime, int position)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var newResult = new
            {
                finishTime = finishTime,
                position = position
            };

            var response = await _apiService.PostAsync($"enrolments/{enrolmentId}/results", newResult);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result recorded successfully!";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to record result: {errorContent}";
            }

            return RedirectToAction("ViewEnrolments", new { eventId = eventId });
        }
    }
}