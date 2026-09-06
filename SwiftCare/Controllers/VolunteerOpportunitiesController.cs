using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.ViewModels;
using System.Globalization;
using System.Text.Json;
using Twilio.Http;


namespace SwiftCare.Controllers
{
    public class VolunteerOpportunitiesController : Controller
    {

        private readonly ILogger<VolunteerOpportunitiesController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly FileUploadService _fileUploadService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public VolunteerOpportunitiesController(ILogger<VolunteerOpportunitiesController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            FileUploadService fileUploadService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Volunteers()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VolunteerOpportunities()
        {
            var opportunities = await _context.VolunteerOpportunities
                .Include(x => x.Requirements)
                .Include(x => x.Responsibilities)
                .Include(x => x.RequiredSkills)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(opportunities);
        }

        [HttpGet]
        public async Task<IActionResult> Opportunities()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> OpportunityDetails(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .Include(x => x.Requirements)
                .Include(x => x.Responsibilities)
                .Include(x => x.RequiredSkills)
                .FirstOrDefaultAsync(
                    x => x.VolunteerOpportunityId == opportunityId
                );

            if (opportunity == null)
            {
                return NotFound();
            }

            return View(opportunity);
        }
        // =========================================================
        // NEW OPPORTUNITY - GET
        // =========================================================

        [HttpGet]
        public IActionResult NewOpportunity()
        {
            var viewModel = new NewOpportunityViewModel();

            return View(viewModel);
        }


        // =========================================================
        // NEW OPPORTUNITY - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewOpportunity(
            NewOpportunityViewModel viewModel)
        {
            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }


            // =====================================================
            // DATE VALIDATION
            // =====================================================

            if (viewModel.StartDate.HasValue &&
                viewModel.EndDate.HasValue &&
                viewModel.EndDate.Value.Date <
                viewModel.StartDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(viewModel.EndDate),
                    "The end date cannot be before the start date."
                );
            }


            // =====================================================
            // APPLICATION DEADLINE VALIDATION
            // =====================================================

            if (viewModel.ApplicationDeadline.HasValue &&
                viewModel.StartDate.HasValue &&
                viewModel.ApplicationDeadline.Value.Date >
                viewModel.StartDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(viewModel.ApplicationDeadline),
                    "The application deadline cannot be after the opportunity start date."
                );
            }


            // =====================================================
            // TIME VALIDATION
            // =====================================================

            if (viewModel.StartDate.HasValue &&
                viewModel.EndDate.HasValue &&
                viewModel.StartTime.HasValue &&
                viewModel.EndTime.HasValue &&
                viewModel.StartDate.Value.Date ==
                viewModel.EndDate.Value.Date &&
                viewModel.EndTime.Value <=
                viewModel.StartTime.Value)
            {
                ModelState.AddModelError(
                    nameof(viewModel.EndTime),
                    "The end time must be later than the start time."
                );
            }


            // =====================================================
            // VALIDATE AGAIN
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }


            // =====================================================
            // GET CURRENT USER
            // =====================================================

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }


            // =====================================================
            // UPLOAD POSTER IMAGE
            // =====================================================

            string? posterImagePath = null;

            if (viewModel.PosterImage != null &&
                viewModel.PosterImage.Length > 0)
            {
                posterImagePath =
                    await _fileUploadService.UploadFileAsync(
                        viewModel.PosterImage
                    );
            }


            var opportunity = new VolunteerOpportunity
            {
                Title = viewModel.Title.Trim(),

                Description = viewModel.Description.Trim(),

                Category = viewModel.Category,

                Area = viewModel.Area,

                CityName = viewModel.CityName?.Trim(),

                GeoNameId = viewModel.GeoNameId,

                Province = viewModel.Province?.Trim(),

                StartDate = viewModel.StartDate,

                EndDate = viewModel.EndDate,

                StartTime = viewModel.StartTime,

                EndTime = viewModel.EndTime,

                VolunteersNeeded = viewModel.VolunteersNeeded,

                VolunteersAccepted = 0,

                MinimumAge = viewModel.MinimumAge,

                ApplicationDeadline =
                                     viewModel.ApplicationDeadline,

                PosterImage = posterImagePath,

                Status = VolunteerOpportunityStatus.Draft,

                CreatedAt = DateTime.UtcNow,

                CreatedById = currentUser.Id
            };


            // =====================================================
            // ADD REQUIREMENTS
            // =====================================================

            if (viewModel.Requirements != null)
            {
                foreach (var requirement in viewModel.Requirements
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    opportunity.Requirements.Add(
                        new VolunteerRequirement
                        {
                            Description = requirement.Trim()
                        }
                    );
                }
            }


            // =====================================================
            // ADD RESPONSIBILITIES
            // =====================================================

            if (viewModel.Responsibilities != null)
            {
                foreach (var responsibility in viewModel.Responsibilities
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    opportunity.Responsibilities.Add(
                        new VolunteerResponsibility
                        {
                            Description = responsibility.Trim()
                        }
                    );
                }
            }


            // =====================================================
            // ADD REQUIRED SKILLS
            // =====================================================

            if (viewModel.RequiredSkills != null)
            {
                foreach (var skill in viewModel.RequiredSkills
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    opportunity.RequiredSkills.Add(
                        new VolunteerRequiredSkill
                        {
                            Skill = skill.Trim()
                        }
                    );
                }
            }


            // =====================================================
            // SAVE OPPORTUNITY
            // =====================================================

            _context.VolunteerOpportunities.Add(opportunity);

            await _context.SaveChangesAsync();


            // =====================================================
            // SUCCESS MESSAGE
            // =====================================================

            TempData["SuccessMessage"] =
                "Volunteer opportunity created successfully.";


            // =====================================================
            // REDIRECT
            // =====================================================

            return RedirectToAction(nameof(Volunteers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .FirstOrDefaultAsync(x => x.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();

            opportunity.Status = VolunteerOpportunityStatus.Published;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Volunteer opportunity published successfully.";

            return RedirectToAction(
                nameof(OpportunityDetails),
                new { opportunityId }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .FirstOrDefaultAsync(x => x.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();

            opportunity.Status = VolunteerOpportunityStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Volunteer opportunity cancelled successfully.";

            return RedirectToAction(
                nameof(OpportunityDetails),
                new { opportunityId }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .FirstOrDefaultAsync(x => x.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();

            opportunity.Status = VolunteerOpportunityStatus.Closed;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Volunteer opportunity closed successfully.";

            return RedirectToAction(
                nameof(OpportunityDetails),
                new { opportunityId }
            );
        }

        [HttpPost]
        public async Task<IActionResult> Apply(VolunteerApplicationViewModel viewModel)
        {
            return View();
        }

        //HELPERS

        // =========================================================
        // SEARCH SOUTH AFRICAN CITIES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SearchCities(string? term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
                {
                    return Json(new List<object>());
                }

                var username = _configuration["GeoNames:Username"];

                if (string.IsNullOrWhiteSpace(username))
                {
                    return StatusCode(
                        500,
                        new
                        {
                            message = "GeoNames username is not configured."
                        }
                    );
                }

                var searchTerm = Uri.EscapeDataString(term.Trim());

                var url =
                    $"https://secure.geonames.org/searchJSON" +
                    $"?country=ZA" +
                    $"&featureClass=P" +
                    $"&cities=cities1000" +
                    $"&name_startsWith={searchTerm}" +
                    $"&maxRows=10" +
                    $"&username={Uri.EscapeDataString(username)}";

                var client = _httpClientFactory.CreateClient();

                client.Timeout = TimeSpan.FromSeconds(10);

                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        new
                        {
                            message = "Unable to contact the GeoNames service."
                        }
                    );
                }

                var json = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty(
                        "geonames",
                        out var cities))
                {
                    return Json(new List<object>());
                }

                var results = new List<object>();

                foreach (var city in cities.EnumerateArray())
                {
                    string? name = null;
                    string? province = null;
                    int? geoNameId = null;

                    if (city.TryGetProperty("name", out var nameElement))
                    {
                        name = nameElement.GetString();
                    }

                    if (city.TryGetProperty("adminName1", out var provinceElement))
                    {
                        province = provinceElement.GetString();
                    }

                    if (city.TryGetProperty("geonameId", out var idElement))
                    {
                        if (idElement.TryGetInt32(out var parsedId))
                        {
                            geoNameId = parsedId;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    results.Add(new
                    {
                        cityName = name,
                        geoNameId = geoNameId,
                        province = province
                    });
                }

                return Json(results);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(
                    504,
                    new
                    {
                        message = "The city search service timed out."
                    }
                );
            }
            catch (JsonException)
            {
                return StatusCode(
                    502,
                    new
                    {
                        message = "Invalid response received from the city service."
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Unable to search cities.",
                        error = ex.Message
                    }
                );
            }
        }
    }
}
