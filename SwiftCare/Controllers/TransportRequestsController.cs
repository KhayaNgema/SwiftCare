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

        // ============================================================
        // GET: TRANSPORT REQUEST DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> TransportRequestDetails(int? requestId)
        {
            try
            {
                // ========================================================
                // VALIDATE REQUEST ID
                // ========================================================

                if (!requestId.HasValue || requestId.Value <= 0)
                {
                    ViewBag.ErrorMessage =
                        "Invalid transport request ID.";

                    return View(null);
                }


                // ========================================================
                // LOAD TRANSPORT REQUESTS FIRST
                // ========================================================

                var transportRequests = await _context.TransportRequests
                    .AsNoTracking()
                    .ToListAsync();


                // ========================================================
                // FIND THE REQUEST IN MEMORY
                // ========================================================

                var transportRequest = transportRequests
                    .FirstOrDefault(tr =>
                        tr.RequestId == requestId.Value);


                // ========================================================
                // REQUEST NOT FOUND
                // ========================================================

                if (transportRequest == null)
                {
                    ViewBag.ErrorMessage =
                        $"Transport request with ID {requestId.Value} was not found.";


                    ViewBag.DebugMessage =
                        transportRequests.Any()
                            ? $"Request IDs currently available: {string.Join(", ", transportRequests.Select(x => x.RequestId))}"
                            : "No transport requests were found in the database.";


                    return View(null);
                }


                // ========================================================
                // LOAD RESIDENT SAFELY
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                    transportRequest.RequestedForId))
                {
                    transportRequest.Resident =
                        await _context.Residents
                            .AsNoTracking()
                            .FirstOrDefaultAsync(r =>
                                r.Id == transportRequest.RequestedForId);
                }


                // ========================================================
                // LOAD NURSE SAFELY
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                    transportRequest.RequestedById))
                {
                    transportRequest.Nurse =
                        await _context.Nurses
                            .AsNoTracking()
                            .FirstOrDefaultAsync(n =>
                                n.Id == transportRequest.RequestedById);
                }


                // ========================================================
                // LOAD DRIVER SAFELY
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                    transportRequest.AssignedToId))
                {
                    transportRequest.Driver =
                        await _context.Drivers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d =>
                                d.Id == transportRequest.AssignedToId);
                }


                // ========================================================
                // LOAD VEHICLE SAFELY
                // ========================================================

                if (transportRequest.VehicleId.HasValue)
                {
                    transportRequest.Vehicle =
                        await _context.Vehicles
                            .AsNoTracking()
                            .FirstOrDefaultAsync(v =>
                                v.VehicleId ==
                                transportRequest.VehicleId.Value);
                }


                // ========================================================
                // LOAD TRIP SAFELY
                // ========================================================

                transportRequest.Trip =
                    await _context.Trips
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t =>
                            t.RequestId ==
                            transportRequest.RequestId);


                // ========================================================
                // RETURN DETAILS PAGE
                // ========================================================

                return View(transportRequest);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "An error occurred while loading the transport request.";

                ViewBag.ExceptionMessage =
                    ex.Message;

                ViewBag.InnerException =
                    ex.InnerException?.Message;

                ViewBag.StackTrace =
                    ex.StackTrace;

                return View(null);
            }
        }

        // ============================================================
        // GET: TRANSPORT REQUESTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> TransportRequests()
        {
            try
            {
                // ========================================================
                // LOAD ALL TRANSPORT REQUESTS
                //
                // Only include relationships that are actually used
                // on this page.
                // ========================================================

                var transportRequests = await _context.TransportRequests
                    .AsNoTracking()
                    .Include(tr => tr.Resident)
                    .Include(tr => tr.Driver)
                    .Include(tr => tr.Vehicle)
                    .OrderByDescending(tr => tr.RequestedAt)
                    .ToListAsync();


                // ========================================================
                // DEBUG COUNT
                // ========================================================

                ViewBag.RequestCount = transportRequests.Count;


                // ========================================================
                // RETURN REQUESTS
                // ========================================================

                return View(transportRequests);
            }
            catch (Exception ex)
            {
                // ========================================================
                // SHOW ACTUAL ERROR
                // ========================================================

                ViewBag.ErrorMessage = ex.Message;

                ViewBag.InnerException = ex.InnerException?.Message;

                ViewBag.StackTrace = ex.StackTrace;


                // Return empty model so the error can be displayed
                return View(new List<TransportRequest>());
            }
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
                .Where(r => r.Id == residentId)
                .FirstOrDefaultAsync();

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
            try
            {
                // ========================================================
                // VALIDATE RESIDENT ID
                // ========================================================

                if (string.IsNullOrWhiteSpace(viewModel.RequestedForId))
                {
                    ModelState.AddModelError(
                        nameof(viewModel.RequestedForId),
                        "Resident ID is required."
                    );
                }


                // ========================================================
                // RETURN VALIDATION ERRORS
                // ========================================================

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
                    ModelState.AddModelError(
                        "",
                        "Your session has expired. Please log in again."
                    );

                    return View(viewModel);
                }


                // ========================================================
                // VERIFY RESIDENT EXISTS
                // ========================================================

                var resident = await _context.Residents
                    .FirstOrDefaultAsync(
                        r => r.Id == viewModel.RequestedForId
                    );

                if (resident == null)
                {
                    ModelState.AddModelError(
                        nameof(viewModel.RequestedForId),
                        "Resident not found."
                    );

                    return View(viewModel);
                }


                // ========================================================
                // VALIDATE PICKUP COORDINATES
                // ========================================================

                if (viewModel.PickUpLatitude == 0 ||
                    viewModel.PickUpLongitude == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Please select a valid pickup address from the Google suggestions."
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
                        "Please select a valid destination address from the Google suggestions."
                    );
                }


                // ========================================================
                // RETURN VALIDATION ERRORS
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


                return RedirectToAction(
                    "ResidentDetails",
                    "Residents",
                    new { id = resident.Id }
                );
            }
            catch (Exception ex)
            {
                // ========================================================
                // SHOW FULL ERROR ON THE PAGE
                // ========================================================

                var errorMessage = $@"
ERROR MESSAGE:
{ex.Message}

INNER EXCEPTION:
{ex.InnerException?.Message ?? "No inner exception"}

STACK TRACE:
{ex.StackTrace}
";

                ModelState.AddModelError(
                    "",
                    errorMessage
                );

                return View(viewModel);
            }
        }
    }
}
