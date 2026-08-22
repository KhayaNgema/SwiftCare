using Microsoft.AspNetCore.Authorization;
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
        // SCHEDULED TRIPS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ScheduledTrips()
        {
            try
            {
                var today = DateTime.Today;

                var scheduledTrips = await _context.TransportRequests
                    .Include(x => x.Resident)
                    .Include(x => x.Driver)
                    .Include(x => x.Vehicle)
                    .Include(x => x.Nurse)
                    .Include(x => x.Trip)
                    .Where(x =>
                        x.Status != RequestStatus.Completed &&
                        x.Status != RequestStatus.Rejected &&
                        x.RequestedForDate >= today
                    )
                    .OrderBy(x => x.RequestedForDate)
                    .ThenBy(x => x.RequestedForTime)
                    .ToListAsync();

                return View(scheduledTrips);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"Unable to load scheduled trips: {ex.Message}";

                return View(
                    new List<TransportRequest>()
                );
            }
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
                // FIND TRANSPORT REQUEST
                // ========================================================

                var transportRequest =
                    await _context.TransportRequests
                        .AsNoTracking()
                        .FirstOrDefaultAsync(tr =>
                            tr.RequestId == requestId.Value);


                // ========================================================
                // REQUEST NOT FOUND
                // ========================================================

                if (transportRequest == null)
                {
                    ViewBag.ErrorMessage =
                        $"Transport request with ID {requestId.Value} was not found.";

                    return View(null);
                }


                // ========================================================
                // LOAD RESIDENT
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                    transportRequest.RequestedForId
                ))
                {
                    transportRequest.Resident =
                        await _context.Residents
                            .AsNoTracking()
                            .FirstOrDefaultAsync(r =>
                                r.Id ==
                                transportRequest.RequestedForId);
                }


                // ========================================================
                // LOAD NURSE
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                    transportRequest.RequestedById
                ))
                {
                    transportRequest.Nurse =
                        await _context.Nurses
                            .AsNoTracking()
                            .FirstOrDefaultAsync(n =>
                                n.Id ==
                                transportRequest.RequestedById);
                }


                // ========================================================
                // LOAD ASSIGNED DRIVER
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                    transportRequest.AssignedToId
                ))
                {
                    transportRequest.Driver =
                        await _context.Drivers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d =>
                                d.Id ==
                                transportRequest.AssignedToId);
                }


                // ========================================================
                // LOAD ASSIGNED VEHICLE
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
                // LOAD TRIP
                // ========================================================

                transportRequest.Trip =
                    await _context.Trips
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t =>
                            t.RequestId ==
                            transportRequest.RequestId);


                // ========================================================
                // LOAD ALL DRIVERS
                // ========================================================

                var drivers =
                    await _context.Drivers
                        .AsNoTracking()
                        .OrderBy(d => d.FirstName)
                        .ThenBy(d => d.LastName)
                        .ToListAsync();


                ViewBag.Drivers = drivers;


                // ========================================================
                // LOAD AVAILABLE VEHICLES
                //
                // Include currently assigned vehicle.
                // ========================================================

                var vehicles =
                    await _context.Vehicles
                        .AsNoTracking()
                        .Where(v =>
                            v.Status == VehicleStatus.Available
                            ||
                            (
                                transportRequest.VehicleId.HasValue
                                &&
                                v.VehicleId ==
                                transportRequest.VehicleId.Value
                            )
                        )
                        .OrderBy(v => v.VehicleName)
                        .ToListAsync();


                ViewBag.Vehicles = vehicles;


                // ========================================================
                // DEBUG INFORMATION
                //
                // Useful while testing.
                // ========================================================

                ViewBag.DriverCount = drivers.Count;

                ViewBag.VehicleCount = vehicles.Count;


                // ========================================================
                // RETURN VIEW
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
        // POST: ASSIGN VEHICLE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AssignVehicle(
            int requestId,
            int vehicleId)
        {
            try
            {
                // ========================================================
                // VALIDATE REQUEST
                // ========================================================

                if (requestId <= 0)
                {
                    TempData["ErrorMessage"] =
                        "Invalid transport request.";

                    return RedirectToAction(
                        nameof(TransportRequestDetails),
                        new { requestId }
                    );
                }


                // ========================================================
                // FIND TRANSPORT REQUEST
                // ========================================================

                var transportRequest =
                    await _context.TransportRequests
                        .FirstOrDefaultAsync(tr =>
                            tr.RequestId == requestId);


                if (transportRequest == null)
                {
                    TempData["ErrorMessage"] =
                        "Transport request could not be found.";

                    return RedirectToAction(
                        nameof(TransportRequests)
                    );
                }


                // ========================================================
                // FIND VEHICLE
                // ========================================================

                var vehicle =
                    await _context.Vehicles
                        .FirstOrDefaultAsync(v =>
                            v.VehicleId == vehicleId);


                if (vehicle == null)
                {
                    TempData["ErrorMessage"] =
                        "The selected vehicle could not be found.";

                    return RedirectToAction(
                        nameof(TransportRequestDetails),
                        new { requestId }
                    );
                }


                // ========================================================
                // CHECK VEHICLE AVAILABILITY
                //
                // Allow the currently assigned vehicle to remain selected.
                // ========================================================

                if (vehicle.Status != VehicleStatus.Available &&
                    transportRequest.VehicleId != vehicleId)
                {
                    TempData["ErrorMessage"] =
                        "The selected vehicle is currently unavailable.";

                    return RedirectToAction(
                        nameof(TransportRequestDetails),
                        new { requestId }
                    );
                }


                // ========================================================
                // STORE OLD VEHICLE
                // ========================================================

                var oldVehicleId =
                    transportRequest.VehicleId;


                // ========================================================
                // ASSIGN NEW VEHICLE
                // ========================================================

                transportRequest.VehicleId =
                    vehicle.VehicleId;


                // ========================================================
                // MARK VEHICLE AS UNAVAILABLE
                //
                // This prevents another request from selecting it.
                // ========================================================

                vehicle.Status =
                    VehicleStatus.Unavailable;


                // ========================================================
                // RELEASE OLD VEHICLE IF CHANGED
                // ========================================================

                if (oldVehicleId.HasValue &&
                    oldVehicleId.Value != vehicle.VehicleId)
                {
                    var oldVehicle =
                        await _context.Vehicles
                            .FirstOrDefaultAsync(v =>
                                v.VehicleId ==
                                oldVehicleId.Value);


                    if (oldVehicle != null)
                    {
                        oldVehicle.Status =
                            VehicleStatus.Available;
                    }
                }


                // ========================================================
                // SAVE
                // ========================================================

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    $"{vehicle.VehicleName} has been assigned successfully.";


                return RedirectToAction(
                    nameof(TransportRequestDetails),
                    new { requestId }
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"Unable to assign vehicle: {ex.Message}";


                return RedirectToAction(
                    nameof(TransportRequestDetails),
                    new { requestId }
                );
            }
        }


        // ============================================================
        // POST: ASSIGN DRIVER
        // ============================================================

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(
            int requestId,
            string driverId
        )
        {
            try
            {
                // ====================================================
                // VALIDATE
                // ====================================================

                if (requestId <= 0)
                {
                    TempData["ErrorMessage"] =
                        "Invalid transport request.";

                    return RedirectToAction(
                        nameof(TransportRequestDetails),
                        new
                        {
                            requestId
                        }
                    );
                }


                if (string.IsNullOrWhiteSpace(
                    driverId
                ))
                {
                    TempData["ErrorMessage"] =
                        "Please select a driver.";

                    return RedirectToAction(
                        nameof(TransportRequestDetails),
                        new
                        {
                            requestId
                        }
                    );
                }


                // ====================================================
                // FIND TRANSPORT REQUEST
                // ====================================================

                var transportRequest =
                    await _context.TransportRequests
                        .FirstOrDefaultAsync(tr =>
                            tr.RequestId ==
                            requestId);


                if (transportRequest == null)
                {
                    TempData["ErrorMessage"] =
                        "Transport request not found.";

                    return RedirectToAction(
                        nameof(TransportRequests)
                    );
                }


                // ====================================================
                // VALIDATE DRIVER
                // ====================================================

                var driver =
                    await _context.Drivers
                        .FirstOrDefaultAsync(d =>
                            d.Id == driverId);


                if (driver == null)
                {
                    TempData["ErrorMessage"] =
                        "The selected driver could not be found.";

                    return RedirectToAction(
                        nameof(TransportRequestDetails),
                        new
                        {
                            requestId
                        }
                    );
                }


                // ====================================================
                // ASSIGN DRIVER
                // ====================================================

                transportRequest.AssignedToId =
                    driver.Id;


                // Update request status
                if (transportRequest.Status ==
                    RequestStatus.Pending)
                {
                    transportRequest.Status =
                        RequestStatus.Driver_Assigned;
                }


                // ====================================================
                // SAVE
                // ====================================================

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    $"{driver.FirstName} {driver.LastName} " +
                    "has been assigned to this transport request.";


                return RedirectToAction(
                    nameof(TransportRequestDetails),
                    new
                    {
                        requestId
                    }
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Something went wrong while assigning the driver: " +
                    ex.Message;


                return RedirectToAction(
                    nameof(TransportRequestDetails),
                    new
                    {
                        requestId
                    }
                );
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
