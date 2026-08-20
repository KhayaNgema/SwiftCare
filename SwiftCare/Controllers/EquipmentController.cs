using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SwiftCare.Data;
using SwiftCare.Models;

namespace SwiftCare.Controllers
{
    public class EquipmentController : Controller
    {
        private readonly ILogger<EquipmentController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;

        public EquipmentController(ILogger<EquipmentController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult EquipmentList()
        {
            var equipment = _context.Equipment
                .ToList();

            return View(equipment);
        }
    }
}
