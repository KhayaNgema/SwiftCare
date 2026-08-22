using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.ViewModels;

namespace SwiftCare.Controllers
{
    public class MaintenanceRequestsController : Controller
    {
        private readonly ILogger<MaintenanceRequestsController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly FileUploadService _fileUploadService;

        public MaintenanceRequestsController(ILogger<MaintenanceRequestsController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            FileUploadService fileUploadService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }
        // ============================================================
        // UPDATE MAINTENANCE REQUEST - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> UpdateMaintenanceRequest(
            int requestId
        )
        {
            if (requestId <= 0)
            {
                return BadRequest(
                    "A valid maintenance request ID is required."
                );
            }


            var request = await _context.MaintenanceRequests
                .Include(x => x.OurEquipment)
                .FirstOrDefaultAsync(
                    x => x.RequestId == requestId
                );


            if (request == null)
            {
                return NotFound();
            }


            var viewModel =
                new MaintenanceRequestUpdateViewModel
                {
                    // REQUEST

                    RequestId = request.RequestId,


                    // EQUIPMENT

                    EquipmentId = request.EquipmentId,

                    EquipmentName =
                        request.OurEquipment?.EquipmentName,

                    EquipmentImageUrl =
                        request.OurEquipment?.ImageUrl,

                    BarcodeValue =
                        request.OurEquipment?.BarcodeValue,

                    EquipmentStatus =
                        request.OurEquipment?.Status
                        ?? EquipmentStatus.Available,


                    // REQUEST INFORMATION

                    ReferenceNumber =
                        request.ReferenceNumber,

                    Priority =
                        request.Priority,

                    ProblemDescription =
                        request.Message,

                    LoggedAt =
                        request.LoggedAt,


                    // UPDATE

                    RequestStatus =
                        request.RequestStatus,

                    WorkPerformed =
                        request.WorkPerformed,


                    // EXISTING IMAGE

                    ExistingConditionImageAfter =
                        request.EquipmentConditionImageAfter
                };


            return View(viewModel);
        }

        // ============================================================
        // MAINTENANCE REQUEST DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MaintenanceRequestDetails(
            int requestId)
        {
            if (requestId <= 0)
            {
                return BadRequest(
                    "A valid maintenance request ID is required."
                );
            }


            var maintenanceRequest =
                await _context.MaintenanceRequests

                    .Include(x => x.OurEquipment)

                    .Include(x => x.LoggedBy)

                    .Include(x => x.AssignedTo)

                    .Include(x => x.ResolvedBy)

                    .FirstOrDefaultAsync(
                        x => x.RequestId == requestId
                    );


            if (maintenanceRequest == null)
            {
                return NotFound();
            }


            return View(maintenanceRequest);
        }

        // ============================================================
        // UPDATE MAINTENANCE REQUEST - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaintenanceRequest(
            MaintenanceRequestUpdateViewModel model
        )
        {
            // ========================================================
            // VALIDATE REQUEST ID
            // ========================================================

            if (model.RequestId <= 0)
            {
                return BadRequest(
                    "A valid maintenance request ID is required."
                );
            }


            // ========================================================
            // FIND REQUEST
            // ========================================================

            var request = await _context.MaintenanceRequests
                .Include(x => x.OurEquipment)
                .Include(x => x.AssignedTo)
                .Include(x => x.ResolvedBy)
                .FirstOrDefaultAsync(
                    x => x.RequestId == model.RequestId
                );


            if (request == null)
            {
                return NotFound();
            }


            // ========================================================
            // POPULATE VIEW MODEL HELPER
            // ========================================================

            void PopulateViewModel()
            {
                model.EquipmentId =
                    request.EquipmentId;

                model.EquipmentName =
                    request.OurEquipment?.EquipmentName;

                model.EquipmentImageUrl =
                    request.OurEquipment?.ImageUrl;

                model.BarcodeValue =
                    request.OurEquipment?.BarcodeValue;

                model.EquipmentStatus =
                    request.OurEquipment?.Status
                    ?? EquipmentStatus.Available;

                model.ReferenceNumber =
                    request.ReferenceNumber;

                model.Priority =
                    request.Priority;

                model.ProblemDescription =
                    request.Message;

                model.LoggedAt =
                    request.LoggedAt;

                model.ExistingConditionImageAfter =
                    request.EquipmentConditionImageAfter;
            }


            // ========================================================
            // MODEL VALIDATION
            // ========================================================

            if (!ModelState.IsValid)
            {
                PopulateViewModel();

                return View(model);
            }


            try
            {
                // ====================================================
                // GET CURRENT LOGGED-IN USER
                // ====================================================

                var currentUser =
                    await _userManager.GetUserAsync(User);


                if (currentUser == null)
                {
                    return Unauthorized();
                }


                var currentUserId =
                    currentUser.Id;


                // ====================================================
                // UPDATE BASIC REQUEST INFORMATION
                // ====================================================

                request.RequestStatus =
                    model.RequestStatus;

                request.WorkPerformed =
                    model.WorkPerformed;


                // ====================================================
                // ASSIGN CURRENT USER
                // ====================================================

                if (
                    model.RequestStatus ==
                    MaintenanceRequestStatus.Assigned
                )
                {
                    request.AssignedToId =
                        currentUserId;

                    // Equipment is now under maintenance
                    if (request.OurEquipment != null)
                    {
                        request.OurEquipment.Status =
                            EquipmentStatus.UnderMaintenance;
                    }
                }


                // ====================================================
                // UPLOAD AFTER MAINTENANCE IMAGE
                // ====================================================

                if (
                    model.EquipmentConditionImageAfter != null &&
                    model.EquipmentConditionImageAfter.Length > 0
                )
                {
                    var uploadedImage =
                        await _fileUploadService.UploadFileAsync(
                            model.EquipmentConditionImageAfter
                        );


                    if (!string.IsNullOrWhiteSpace(uploadedImage))
                    {
                        request.EquipmentConditionImageAfter =
                            uploadedImage;
                    }
                }


                // ====================================================
                // IF MAINTENANCE IS COMPLETED / RESOLVED
                // ====================================================

                if (
                    model.RequestStatus ==
                    MaintenanceRequestStatus.Completed
                )
                {
                    // =================================================
                    // SET RESOLUTION INFORMATION
                    // =================================================

                    request.ResolvedAt =
                        DateTime.Now;

                    request.ResolvedById =
                        currentUserId;


                    // =================================================
                    // ENSURE THE CURRENT USER IS ALSO THE ASSIGNED USER
                    // IF NOBODY WAS ASSIGNED BEFORE
                    // =================================================

                    if (string.IsNullOrWhiteSpace(
                        request.AssignedToId
                    ))
                    {
                        request.AssignedToId =
                            currentUserId;
                    }


                    // =================================================
                    // UPDATE EQUIPMENT STATUS
                    // =================================================

                    if (request.OurEquipment != null)
                    {
                        request.OurEquipment.Status =
                            EquipmentStatus.InUse;
                    }
                }


                // ====================================================
                // IF REQUEST IS PENDING
                // ====================================================

                else if (
                    model.RequestStatus ==
                    MaintenanceRequestStatus.Pending
                )
                {
                    // =================================================
                    // CLEAR RESOLUTION INFORMATION
                    // =================================================

                    request.ResolvedAt =
                        null;

                    request.ResolvedById =
                        null;


                    // =================================================
                    // EQUIPMENT STATUS
                    // =================================================

                    if (request.OurEquipment != null)
                    {
                        request.OurEquipment.Status =
                            EquipmentStatus.UnderMaintenance;
                    }
                }


                // ====================================================
                // IF REQUEST IS IN PROGRESS
                // ====================================================

                else if (
                    model.RequestStatus ==
                    MaintenanceRequestStatus.InProgress
                )
                {
                    // =================================================
                    // CLEAR RESOLUTION IF IT WAS PREVIOUSLY COMPLETED
                    // =================================================

                    request.ResolvedAt =
                        null;

                    request.ResolvedById =
                        null;


                    // =================================================
                    // MAKE SURE THERE IS AN ASSIGNED USER
                    // =================================================

                    if (
                        string.IsNullOrWhiteSpace(
                            request.AssignedToId
                        )
                    )
                    {
                        request.AssignedToId =
                            currentUserId;
                    }


                    // =================================================
                    // EQUIPMENT IS UNDER MAINTENANCE
                    // =================================================

                    if (request.OurEquipment != null)
                    {
                        request.OurEquipment.Status =
                            EquipmentStatus.UnderMaintenance;
                    }
                }


                // ====================================================
                // SAVE CHANGES
                // ====================================================

                await _context.SaveChangesAsync();


                // ====================================================
                // SUCCESS MESSAGE
                // ====================================================

                TempData["SuccessMessage"] =
                    "Maintenance request updated successfully.";


                // ====================================================
                // REDIRECT TO DETAILS
                // ====================================================

                return RedirectToAction(
                    nameof(MaintenanceRequestDetails),
                    new
                    {
                        requestId = request.RequestId
                    }
                );
            }
            catch (Exception ex)
            {
                // ====================================================
                // ADD ERROR
                // ====================================================

                ModelState.AddModelError(
                    "",
                    "An error occurred while updating the maintenance " +
                    $"request: {ex.Message}"
                );


                // ====================================================
                // RESTORE VIEW MODEL DATA
                // ====================================================

                PopulateViewModel();


                return View(model);
            }
        }

        // ============================================================
        // COMPLETED MAINTENANCE REQUESTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> CompletedRequests()
        {
            try
            {
                var completedRequests = await _context.MaintenanceRequests
                    .Include(x => x.OurEquipment)
                        .ThenInclude(x => x.AssignedTo)

                    .Include(x => x.LoggedBy)

                    .Include(x => x.AssignedTo)

                    .Include(x => x.ResolvedBy)

                    .Where(x =>
                        x.RequestStatus ==
                        MaintenanceRequestStatus.Completed
                    )

                    .OrderByDescending(x => x.ResolvedAt)
                    .ThenByDescending(x => x.LoggedAt)

                    .ToListAsync();


                return View(completedRequests);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"Unable to load completed maintenance requests: {ex.Message}";

                return View(new List<MaintenanceRequest>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> MaintenanceRequests()
        {
            // ============================================================
            // GET CURRENT USER
            // ============================================================

            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }

            // ============================================================
            // GET AVAILABLE + MY ACTIVE REQUESTS
            // ============================================================

            var maintenanceRequests = await _context.MaintenanceRequests

                // ========================================================
                // EQUIPMENT
                // ========================================================

                .Include(r => r.OurEquipment)
                    .ThenInclude(e => e.AssignedTo)

                // ========================================================
                // NURSE WHO LOGGED REQUEST
                // ========================================================

                .Include(r => r.LoggedBy)

                // ========================================================
                // ASSIGNED MAINTENANCE PERSON
                // ========================================================

                .Include(r => r.AssignedTo)

                // ========================================================
                // FILTER REQUESTS
                // ========================================================

                .Where(r =>

                    // Available requests
                    (
                        r.RequestStatus == MaintenanceRequestStatus.Pending &&
                        r.AssignedToId == null
                    )

                    ||

                    // Requests assigned to me
                    (
                        r.AssignedToId == currentUserId &&
                        r.RequestStatus != MaintenanceRequestStatus.Completed
                    )
                )

                // ========================================================
                // ORDER
                // ========================================================

                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.LoggedAt)

                .ToListAsync();

            return View(maintenanceRequests);
        }
        [HttpGet]
        public async Task<IActionResult> MyMaintenanceRequests()
        {
            // ============================================================
            // GET CURRENT USER
            // ============================================================

            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }

            // ============================================================
            // GET MY REQUESTS
            // ============================================================

            var maintenanceRequests = await _context.MaintenanceRequests

                // ========================================================
                // EQUIPMENT
                // ========================================================

                .Include(r => r.OurEquipment)
                    .ThenInclude(e => e.AssignedTo)

                // ========================================================
                // NURSE WHO LOGGED REQUEST
                // ========================================================

                .Include(r => r.LoggedBy)

                // ========================================================
                // ASSIGNED MAINTENANCE PERSON
                // ========================================================

                .Include(r => r.AssignedTo)

                // ========================================================
                // ONLY REQUESTS ASSIGNED TO ME
                // ========================================================

                .Where(r =>
                    r.AssignedToId == currentUserId
                )

                // ========================================================
                // INCLUDE ACTIVE + COMPLETED REQUESTS
                // ========================================================

                .Where(r =>

                    r.RequestStatus ==
                        MaintenanceRequestStatus.Assigned

                    ||

                    r.RequestStatus ==
                        MaintenanceRequestStatus.InProgress


                )

                // ========================================================
                // ORDER
                // ========================================================

                .OrderByDescending(r => r.RequestStatus ==
                    MaintenanceRequestStatus.InProgress)

                .ThenByDescending(r => r.Priority)

                .ThenByDescending(r => r.LoggedAt)

                .ToListAsync();

            return View(maintenanceRequests);
        }


        [HttpGet]
        public async Task<IActionResult> RequestMaintenance(
                   int equipmentId
               )
        {
            if (equipmentId <= 0)
            {
                return BadRequest(
                    "A valid equipment ID is required."
                );
            }


            // ========================================================
            // FIND EQUIPMENT
            // ========================================================

            var equipment = await _context.OurEquipment
                .Include(e => e.AssignedTo)
                .Include(e => e.AssignedBy)
                .FirstOrDefaultAsync(
                    e => e.EquipmentId == equipmentId
                );


            if (equipment == null)
            {
                return NotFound(
                    "The requested equipment could not be found."
                );
            }


            // ========================================================
            // CREATE VIEW MODEL
            // ========================================================

            var model = new MaintenanceRequestViewModel
            {
                EquipmentId = equipment.EquipmentId,

                EquipmentName = equipment.EquipmentName,

                ImageUrl = equipment.ImageUrl,

                Description = equipment.Description,

                BarcodeValue = equipment.BarcodeValue,

                EquipmentStatus = equipment.Status,

                AssignedToId = equipment.AssignedToId,

                AssignmentNotes = equipment.AssignmentNotes,

                AssignedAt = equipment.AssignedAt,

                Priority = MaintenancePriority.Medium
            };


            return View(model);
        }

        // ============================================================
        // POST: REQUEST MAINTENANCE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestMaintenance(
            MaintenanceRequestViewModel model)
        {
            // ========================================================
            // VALIDATE MODEL
            // ========================================================

            if (!ModelState.IsValid)
            {
                // ----------------------------------------------------
                // RELOAD EQUIPMENT INFORMATION
                // ----------------------------------------------------

                var invalidEquipment = await _context.OurEquipment
                    .FirstOrDefaultAsync(e =>
                        e.EquipmentId == model.EquipmentId);

                if (invalidEquipment != null)
                {
                    model.EquipmentName =
                        invalidEquipment.EquipmentName;

                    model.ImageUrl =
                        invalidEquipment.ImageUrl;

                    model.Description =
                        invalidEquipment.Description;

                    model.BarcodeValue =
                        invalidEquipment.BarcodeValue;

                    model.EquipmentStatus =
                        invalidEquipment.Status;

                    model.AssignedToId =
                        invalidEquipment.AssignedToId;

                    model.AssignmentNotes =
                        invalidEquipment.AssignmentNotes;

                    model.AssignedAt =
                        invalidEquipment.AssignedAt;
                }

                return View(model);
            }


            // ========================================================
            // FIND EQUIPMENT
            // ========================================================

            var equipment = await _context.OurEquipment
                .FirstOrDefaultAsync(e =>
                    e.EquipmentId == model.EquipmentId);

            if (equipment == null)
            {
                return NotFound(
                    "The selected equipment could not be found."
                );
            }


            // ========================================================
            // GET CURRENT LOGGED-IN USER
            // ========================================================

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized();
            }


            // ========================================================
            // FIND CURRENT NURSE
            // ========================================================

            var nurse = await _context.Nurses
                .FirstOrDefaultAsync(n =>
                    n.Id == currentUser.Id);

            if (nurse == null)
            {
                return BadRequest(
                    "The currently logged-in user could not be found as a nurse."
                );
            }


            // ========================================================
            // CHECK FOR EXISTING ACTIVE MAINTENANCE REQUEST
            // ========================================================

            var existingRequest =
                await _context.MaintenanceRequests
                    .AnyAsync(r =>
                        r.EquipmentId == equipment.EquipmentId &&
                        (
                            r.RequestStatus ==
                                MaintenanceRequestStatus.Pending ||

                            r.RequestStatus ==
                                MaintenanceRequestStatus.Assigned ||

                            r.RequestStatus ==
                                MaintenanceRequestStatus.InProgress
                        )
                    );

            if (existingRequest)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "There is already an active maintenance request for this equipment."
                );

                // ----------------------------------------------------
                // RELOAD EQUIPMENT DETAILS
                // ----------------------------------------------------

                model.EquipmentName =
                    equipment.EquipmentName;

                model.ImageUrl =
                    equipment.ImageUrl;

                model.Description =
                    equipment.Description;

                model.BarcodeValue =
                    equipment.BarcodeValue;

                model.EquipmentStatus =
                    equipment.Status;

                model.AssignedToId =
                    equipment.AssignedToId;

                model.AssignmentNotes =
                    equipment.AssignmentNotes;

                model.AssignedAt =
                    equipment.AssignedAt;

                return View(model);
            }


            // ========================================================
            // UPLOAD EQUIPMENT CONDITION IMAGE
            // ========================================================

            string? conditionImagePath = null;

            if (model.EquipmentConditionImageBefore != null &&
                model.EquipmentConditionImageBefore.Length > 0)
            {
                try
                {
                    conditionImagePath =
                        await _fileUploadService.UploadFileAsync(
                            model.EquipmentConditionImageBefore
                        );
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Unable to upload the equipment condition image: {ex.Message}"
                    );

                    // ------------------------------------------------
                    // RELOAD EQUIPMENT DETAILS
                    // ------------------------------------------------

                    model.EquipmentName =
                        equipment.EquipmentName;

                    model.ImageUrl =
                        equipment.ImageUrl;

                    model.Description =
                        equipment.Description;

                    model.BarcodeValue =
                        equipment.BarcodeValue;

                    model.EquipmentStatus =
                        equipment.Status;

                    model.AssignedToId =
                        equipment.AssignedToId;

                    model.AssignmentNotes =
                        equipment.AssignmentNotes;

                    model.AssignedAt =
                        equipment.AssignedAt;

                    return View(model);
                }
            }


            // ========================================================
            // GENERATE REFERENCE NUMBER
            // ========================================================

            var referenceNumber =
                $"MR-{DateTime.Now:yyyyMMddHHmmss}-{equipment.EquipmentId}";


            // ========================================================
            // CREATE MAINTENANCE REQUEST
            // ========================================================

            var maintenanceRequest =
                new MaintenanceRequest
                {
                    // ------------------------------------------------
                    // EQUIPMENT
                    // ------------------------------------------------

                    EquipmentId =
                        equipment.EquipmentId,


                    // ------------------------------------------------
                    // LOGGED BY
                    // ------------------------------------------------

                    LoggedById =
                        nurse.Id,


                    // ------------------------------------------------
                    // PRIORITY
                    // ------------------------------------------------

                    Priority =
                        model.Priority,


                    // ------------------------------------------------
                    // PROBLEM DESCRIPTION
                    // ------------------------------------------------

                    Message =
                        model.Message!,


                    // ------------------------------------------------
                    // REFERENCE NUMBER
                    // ------------------------------------------------

                    ReferenceNumber =
                        referenceNumber,


                    // ------------------------------------------------
                    // REQUEST STATUS
                    // ------------------------------------------------

                    RequestStatus =
                        MaintenanceRequestStatus.Pending,


                    // ------------------------------------------------
                    // DATE LOGGED
                    // ------------------------------------------------

                    LoggedAt =
                        DateTime.Now,


                    // ------------------------------------------------
                    // EQUIPMENT CONDITION IMAGE BEFORE MAINTENANCE
                    // ------------------------------------------------

                    EquipmentConditionImageBefore =
                        conditionImagePath
                };


            // ========================================================
            // ADD MAINTENANCE REQUEST
            // ========================================================

            await _context.MaintenanceRequests.AddAsync(
                maintenanceRequest
            );


            // ========================================================
            // CHANGE EQUIPMENT STATUS
            // ========================================================

            equipment.Status =
                EquipmentStatus.UnderMaintenance;


            // ========================================================
            // SAVE CHANGES
            // ========================================================

            await _context.SaveChangesAsync();


            // ========================================================
            // SUCCESS MESSAGE
            // ========================================================

            TempData["SuccessMessage"] =
                $"Maintenance request {referenceNumber} " +
                $"for {equipment.EquipmentName} has been submitted successfully.";


            // ========================================================
            // REDIRECT BACK TO RESIDENT DETAILS
            // ========================================================

            if (!string.IsNullOrWhiteSpace(
                equipment.AssignedToId))
            {
                return RedirectToAction(
                    "ResidentDetails",
                    "Residents",
                    new
                    {
                        id = equipment.AssignedToId
                    }
                );
            }


            // ========================================================
            // FALLBACK
            // ========================================================

            return RedirectToAction(
                "Equipment",
                "Equipment"
            );
        }
    }
}

