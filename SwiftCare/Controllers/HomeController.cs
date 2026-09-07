using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SwiftCare.Data;
using SwiftCare.Models;
using System.Diagnostics;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace SwiftCare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;

        public HomeController(ILogger<HomeController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }
        public IActionResult ErrorPage(string errorMessage)
        {
            ViewData["ErrorMessage"] = errorMessage;
            return View();
        }
        public async Task<IActionResult> Index(string tab)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                var roles = await _userManager.GetRolesAsync(user);

                    return RedirectToAction("Dashboard");
            }
            else
            {
                var opportunities = await _context.VolunteerOpportunities
                    .Where(x => x.Status == VolunteerOpportunityStatus.Published)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                ViewBag.ActiveTab = string.IsNullOrEmpty(tab) ? "sportnews" : tab;
                return View(opportunities);
            }
        }


        public async Task<IActionResult> Dashboard()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);

            var roles = await _userManager.GetRolesAsync(user);


            if (roles.Contains("Administrator"))
            {
                return View("AdminDashboard");
            }

            else if (roles.Contains("Head Doctor"))
            {
                return View("HeadDoctorDashboard");
            }
            else if (roles.Contains("Doctor"))
            {

                return View("DoctorDashboard");
            }
            else if (roles.Contains("Head Nurse"))
            {

                return View("HeadNurseDashboard");
            }
            else if (roles.Contains("Nurse"))
            {

                return View("NurseDashboard");
            }
            else if (roles.Contains("Supplier Admin"))
            {

                return View("SupplierAdminDashboard");
            }
            else if (roles.Contains("Driver"))
            {

                return View("DriverDashboard");
            }
            else if (roles.Contains("Maintainance Person"))
            {

                return View("MaintainancePersonDashboard");
            }
            else if (roles.Contains("Resident"))
            {

                return View("ResidentDashboard");
            }
            else if (roles.Contains("Next Of Kin"))
            {

                return View("NextOfKinDashboard");
            }
            else if (roles.Contains("Cleaner"))
            {

                return View("CleanerDashboard");
            }
            else if (roles.Contains("Chef"))
            {

                return View("ChefDashboard");
            }
            else if (roles.Contains("Applicant"))
            {
                var opportunities = await _context.VolunteerOpportunities
                        .Where(x => x.Status == VolunteerOpportunityStatus.Published)
                        .OrderBy(x => x.ApplicationDeadline)
                        .ThenBy(x => x.StartDate)
                        .ToListAsync();

                return View("ApplicantDashboard", opportunities);
            }
            else
            {
                return View("Index");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
