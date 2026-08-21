using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.ViewModels;

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
        public IActionResult OurEquipment()
        {
            var equipment = _context.OurEquipment
                .ToList();

            return View(equipment);
        }

        public IActionResult EquipmentList()
        {
            var equipment = _context.Equipment
                .ToList();

            return View(equipment);
        }


        // ============================================================
        // ASSIGN EQUIPMENT PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AssignEquipment(
            string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                return NotFound();
            }


            var resident =
                await _context.Residents
                    .FirstOrDefaultAsync(
                        r => r.Id == residentId
                    );


            if (resident == null)
            {
                return NotFound();
            }


            var model =
                new AssignEquipmentViewModel
                {
                    AssignedToId =
                        resident.Id,

                    ResidentName =
                        $"{resident.FirstName} {resident.LastName}",

                    RoomNo =
                        resident.RoomNumber,

                    ProfilePicture =
                        resident.ProfilePicture
                };


            return View(model);
        }

        // ============================================================
        // GET EQUIPMENT BY BARCODE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetEquipmentByBarcode(
            string barcodeValue)
        {
            if (string.IsNullOrWhiteSpace(barcodeValue))
            {
                return Json(new
                {
                    success = false,
                    message = "No barcode value was provided."
                });
            }


            var equipment =
                await _context.OurEquipment
                    .FirstOrDefaultAsync(
                        e => e.BarcodeValue == barcodeValue
                    );


            if (equipment == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "No equipment was found with this barcode."
                });
            }


            if (equipment.Status != EquipmentStatus.Available)
            {
                return Json(new
                {
                    success = false,
                    message =
                        $"This equipment is currently {equipment.Status} and cannot be assigned."
                });
            }


            return Json(new
            {
                success = true,

                equipment = new
                {
                    equipmentId =
                        equipment.EquipmentId,

                    equipmentName =
                        equipment.EquipmentName,

                    imageUrl =
                        equipment.ImageUrl,

                    description =
                        equipment.Description,

                    barcodeValue =
                        equipment.BarcodeValue,

                    status =
                        equipment.Status.ToString()
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEquipment(
    AssignEquipmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!model.EquipmentId.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please scan and select equipment before assigning it."
                );

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.AssignedToId))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No resident was selected."
                );

                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            var equipment =
                await _context.OurEquipment
                    .FirstOrDefaultAsync(
                        e =>
                            e.EquipmentId ==
                            model.EquipmentId.Value
                    );

            if (equipment == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The selected equipment could not be found."
                );

                return View(model);
            }

            if (equipment.Status != EquipmentStatus.Available)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This equipment is no longer available for assignment."
                );

                return View(model);
            }

            var resident =
                await _context.Residents
                    .FirstOrDefaultAsync(
                        r =>
                            r.Id ==
                            model.AssignedToId
                    );

            if (resident == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The selected resident could not be found."
                );

                return View(model);
            }


            // ============================================================
            // ASSIGN EQUIPMENT
            // ============================================================

            equipment.AssignedToId =
                model.AssignedToId;

            equipment.AssignmentNotes =
                model.AssignmentNotes;

            equipment.AssignedAt =
                DateTime.Now;

            equipment.Status =
                EquipmentStatus.InUse;

            equipment.AssignedById = user.Id;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                $"{equipment.EquipmentName} has successfully been assigned to " +
                $"{resident.FirstName} {resident.LastName}.";


            return RedirectToAction(
                "Details",
                "Residents",
                new
                {
                    id = model.AssignedToId
                }
            );
        }
    }
}
