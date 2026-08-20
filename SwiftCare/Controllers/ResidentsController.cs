using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;

namespace SwiftCare.Controllers
{
    public class ResidentsController : Controller
    {
        private readonly ILogger<ResidentsController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;

        public ResidentsController(ILogger<ResidentsController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        
        public async Task< IActionResult> Residents()
        {
            var residents = await _context.Residents
                .ToListAsync();

            return View(residents);
        }

        [HttpGet]
        public async Task<IActionResult> ResidentDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Resident ID is required.");
            }

            var resident = await _context.Residents
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resident == null)
            {
                return NotFound();
            }

            return View(resident);
        }
    }
}
