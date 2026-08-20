using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder.Extensions;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.ViewModels;

namespace SwiftCare.Controllers
{
    public class TransportRequestsController : Controller
    {
        private readonly ILogger<TransportRequestsController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;

        public TransportRequestsController(ILogger<TransportRequestsController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> TransportRequests()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResidentTransportRequests()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> RequestTransport(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                return BadRequest("Resident ID is required.");
            }

            var resident = await _context.Residents
                .FirstOrDefaultAsync(r => r.Id == residentId);

            if (resident == null)
            {
                return NotFound("Resident not found.");
            }

            var viewModel = new RequestViewModel
            {
                RequestedForId = resident.Id,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                ProfilePicture = resident.ProfilePicture,
                RoomNo = resident.RoomNumber,
                PhoneNumber = resident.PhoneNumber,

                RequestReason = string.Empty,
                PickUpAddress = string.Empty,
                DestinationAddress = string.Empty,

                RequestedForDate = DateTime.Today,
                RequestedForTime = DateTime.Now
            };

            return View(viewModel);
        }

        // ============================================================
        // POST: REQUEST TRANSPORT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestTransport(RequestViewModel viewModel)
        {
            // ========================================================
            // DEBUG / VALIDATE RESIDENT ID FIRST
            // ========================================================

            if (string.IsNullOrWhiteSpace(viewModel.RequestedForId))
            {
                ModelState.AddModelError(
                    "RequestedForId",
                    "Resident ID is required."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }


            // ========================================================
            // GET CURRENT LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }


            // ========================================================
            // VERIFY RESIDENT EXISTS
            // ========================================================

            var resident = await _context.Residents
                .FirstOrDefaultAsync(r =>
                    r.Id == viewModel.RequestedForId);

            if (resident == null)
            {
                return NotFound("Resident not found.");
            }


            // ========================================================
            // VALIDATE PICKUP COORDINATES
            // ========================================================

            if (viewModel.PickUpLatitude == 0 ||
                viewModel.PickUpLongitude == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select a valid pickup address from the suggestions."
                );
            }


            // ========================================================
            // VALIDATE DESTINATION COORDINATES
            // ========================================================

            if (viewModel.DestinationLatitude == 0 ||
                viewModel.DestinationLongitude == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select a valid destination address from the suggestions."
                );
            }


            // ========================================================
            // RETURN IF VALIDATION FAILED
            // ========================================================

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }


            // ========================================================
            // CREATE TRANSPORT REQUEST
            // ========================================================

            var transportRequest = new TransportRequest
            {
                RequestedById = user.Id,

                RequestedForId = resident.Id,

                RequestReason = viewModel.RequestReason,

                RequestedAt = DateTime.Now,

                RequestedForDate = viewModel.RequestedForDate,

                RequestedForTime = viewModel.RequestedForTime,

                PickUpAddress = viewModel.PickUpAddress,

                DestinationAddress = viewModel.DestinationAddress,

                PickUpLatitude = viewModel.PickUpLatitude,

                PickUpLongitude = viewModel.PickUpLongitude,

                DestinationLatitude = viewModel.DestinationLatitude,

                DestinationLongitude = viewModel.DestinationLongitude
            };


            // ========================================================
            // SAVE TRANSPORT REQUEST
            // ========================================================

            _context.TransportRequests.Add(transportRequest);

            await _context.SaveChangesAsync();


            // ========================================================
            // SUCCESS MESSAGE
            // ========================================================

            TempData["Message"] =
                $"Transport request for {resident.FirstName} {resident.LastName} " +
                "has been submitted successfully.";


            // ========================================================
            // REDIRECT
            // ========================================================

            return RedirectToAction(
                "ResidentDetails",
                "Residents",
                new { id = resident.Id }
            );
        }
    }
}
