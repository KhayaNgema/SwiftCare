using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.ViewModels;

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
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Resident ID is required.");
            }


            // ============================================================
            // FIND RESIDENT
            // ============================================================

            var resident = await _context.Residents
                .FirstOrDefaultAsync(r => r.Id == id);


            if (resident == null)
            {
                return NotFound();
            }


            // ============================================================
            // GET EQUIPMENT ASSIGNED TO THIS RESIDENT
            // ============================================================

            var assignedEquipment = await _context.OurEquipment
                .Include(e => e.AssignedBy)
                .Where(e => e.AssignedToId == resident.Id)
                .OrderByDescending(e => e.AssignedAt)
                .ToListAsync();


            // ============================================================
            // BUILD VIEW MODEL
            // ============================================================

            var viewModel = new ResidentDetailsViewModel
            {
                Resident = resident,

                AssignedEquipment = assignedEquipment
            };


            return View(viewModel);
        }
    }
}
