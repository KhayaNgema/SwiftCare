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
            // MODEL VALIDATION
            // ========================================================

            if (!ModelState.IsValid)
            {
                model.EquipmentId = request.EquipmentId;

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


                return View(model);
            }


            try
            {
                // ====================================================
                // UPDATE STATUS
                // ====================================================

                request.RequestStatus =
                    model.RequestStatus;


                // ====================================================
                // UPDATE WORK PERFORMED
                // ====================================================

                request.WorkPerformed =
                    model.WorkPerformed;


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


                    request.EquipmentConditionImageAfter =
                        uploadedImage;
                }


                // ====================================================
                // IF MAINTENANCE IS COMPLETED
                // ====================================================

                if (
                    model.RequestStatus ==
                    MaintenanceRequestStatus.Completed
                )
                {
                    request.ResolvedAt =
                        DateTime.Now;


                    // =================================================
                    // TODO:
                    // Replace this with your logged-in maintenance
                    // person's ID from Identity.
                    // =================================================

                    var user = await _userManager.GetUserAsync(User);

                    if (user != null)
                    {
                        request.ResolvedById = user.Id;
                    }


                    if (!string.IsNullOrWhiteSpace(
                        user.Id
                    ))
                    {
                        request.ResolvedById =
                            user.Id;
                    }


                    // =================================================
                    // UPDATE EQUIPMENT STATUS
                    // =================================================

                    if (request.OurEquipment != null)
                    {
                        request.OurEquipment.Status =
                            EquipmentStatus.Available;
                    }
                }
                else
                {
                    // =================================================
                    // CLEAR RESOLUTION IF REQUEST REOPENED
                    // =================================================

                    request.ResolvedAt = null;

                    request.ResolvedById = null;


                    // =================================================
                    // EQUIPMENT IS CURRENTLY UNDER MAINTENANCE
                    // =================================================

                    if (request.OurEquipment != null)
                    {
                        request.OurEquipment.Status =
                            EquipmentStatus.UnderMaintenance;
                    }
                }


                // ====================================================
                // SAVE
                // ====================================================

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    "Maintenance request updated successfully.";


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
                ModelState.AddModelError(
                    "",
                    $"An error occurred while updating the maintenance request: {ex.Message}"
                );


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


                return View(model);
            }
        }

        // ============================================================
        // MAINTENANCE REQUESTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MaintenanceRequests()
        {
            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }


            var maintenanceRequests = await _context.MaintenanceRequests

                // ====================================================
                // EQUIPMENT
                // ====================================================

                .Include(r => r.OurEquipment)
                    .ThenInclude(e => e.AssignedTo)


                // ====================================================
                // NURSE WHO LOGGED REQUEST
                // ====================================================

                .Include(r => r.LoggedBy)


                // ====================================================
                // ASSIGNED MAINTENANCE PERSON
                // ====================================================

                .Include(r => r.AssignedTo)


                // ====================================================
                // LOAD REQUESTS FOR CURRENT MAINTENANCE PERSON
                // ====================================================

                .Where(r =>

                    r.AssignedToId == currentUserId

                    ||

                    r.AssignedToId == null

                    ||

                    r.RequestStatus == MaintenanceRequestStatus.Pending
                )


                // ====================================================
                // NEWEST FIRST
                // ====================================================

                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.LoggedAt)

                .ToListAsync();


            return View(maintenanceRequests);
        }

        // ============================================================
        // MAINTENANCE REQUESTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyMaintenanceRequests()
        {
            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }


            var maintenanceRequests = await _context.MaintenanceRequests

                // ====================================================
                // EQUIPMENT
                // ====================================================

                .Include(r => r.OurEquipment)
                    .ThenInclude(e => e.AssignedTo)


                // ====================================================
                // NURSE WHO LOGGED REQUEST
                // ====================================================

                .Include(r => r.LoggedBy)


                // ====================================================
                // ASSIGNED MAINTENANCE PERSON
                // ====================================================

                .Include(r => r.AssignedTo)


                // ====================================================
                // LOAD REQUESTS FOR CURRENT MAINTENANCE PERSON
                // ====================================================

                .Where(r =>

                    r.AssignedToId == currentUserId

                    ||

                    r.RequestStatus == MaintenanceRequestStatus.Pending

                    ||

                     r.RequestStatus == MaintenanceRequestStatus.Assigned
                     ||

                      r.RequestStatus == MaintenanceRequestStatus.InProgress
                )


                // ====================================================
                // NEWEST FIRST
                // ====================================================

                .OrderByDescending(r => r.Priority)
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

