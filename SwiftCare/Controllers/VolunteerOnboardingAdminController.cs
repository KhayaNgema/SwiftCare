using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerActivation;
using SwiftCare.Services.VolunteerOnboarding;
using SwiftCare.ViewModels;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class VolunteerOnboardingAdminController : Controller
    {
        private readonly ILogger<VolunteerOnboardingAdminController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IVolunteerOnboardingService _onboardingService;
        private readonly IVolunteerActivationService _activationService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public VolunteerOnboardingAdminController(
            ILogger<VolunteerOnboardingAdminController> logger,
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            IVolunteerOnboardingService onboardingService,
            IVolunteerActivationService activationService,
            EmailService emailService,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _onboardingService = onboardingService;
            _activationService = activationService;
            _emailService = emailService;
            _configuration = configuration;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var onboardingRecords =
                await _context.VolunteerOnboardings
                    .AsNoTracking()
                    .Include(x => x.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.VolunteerOpportunity)
                    .Include(x => x.Documents)
                    .Include(x => x.Requirements)
                    .Include(x => x.PolicyAcceptances)
                    .Include(x => x.Offers)
                    .Include(x => x.Volunteer)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync(cancellationToken);

            return View(onboardingRecords);
        }

        // ============================================================
        // DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var onboarding =
                await _context.VolunteerOnboardings
                    .AsNoTracking()
                    .Include(x => x.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.VolunteerOpportunity)
                    .Include(x => x.Documents)
                    .Include(x => x.Requirements)
                    .Include(x => x.PolicyAcceptances)
                        .ThenInclude(x => x.VolunteerPolicy)
                    .Include(x => x.Offers)
                    .Include(x => x.Volunteer)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOnboardingId == id,
                        cancellationToken);

            if (onboarding == null)
                return NotFound();

            return View(onboarding);
        }

        // ============================================================
        // CREATE ONBOARDING - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create(
            int applicationId,
            CancellationToken cancellationToken)
        {
            var application =
                await _context.VolunteerApplications
                    .AsNoTracking()
                    .Include(x => x.VolunteerOpportunity)
                    .Include(x => x.Applicant)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerApplicationId ==
                            applicationId,
                        cancellationToken);

            if (application == null)
                return NotFound();

            var existing =
                await _onboardingService.GetOnboardingByApplicationAsync(
                    applicationId,
                    cancellationToken);

            if (existing != null)
            {
                TempData["Error"] =
                    "An onboarding record already exists for this application.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = existing.VolunteerOnboardingId
                    });
            }

            ViewBag.Application = application;

            return View(application);
        }

        // ============================================================
        // CREATE ONBOARDING - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Create")]
        public async Task<IActionResult> CreatePost(
            int applicationId,
            CancellationToken cancellationToken)
        {
            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
                return Challenge();

            try
            {
                var existing =
                    await _onboardingService.GetOnboardingByApplicationAsync(
                        applicationId,
                        cancellationToken);

                if (existing != null)
                {
                    TempData["Error"] =
                        "An onboarding record already exists for this application.";

                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id = existing.VolunteerOnboardingId
                        });
                }

                var onboarding =
                    await _onboardingService.CreateOnboardingAsync(
                        applicationId,
                        admin.Id,
                        cancellationToken);

                TempData["Success"] =
                    "Volunteer onboarding was created successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboarding.VolunteerOnboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to create onboarding for ApplicationId {ApplicationId}.",
                    applicationId);

                TempData["Error"] =
                    "The volunteer onboarding record could not be created.";

                return RedirectToAction(
                    nameof(Index));
            }
        }

        // ============================================================
        // CREATE OFFER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOffer(
            int onboardingId,
            DateTime expiryDate,
            CancellationToken cancellationToken)
        {
            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
                return Challenge();

            if (expiryDate <= DateTime.Now)
            {
                TempData["Error"] =
                    "The offer expiry date must be in the future.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }

            try
            {
                var offer =
                    await _onboardingService.CreateOfferAsync(
                        onboardingId,
                        expiryDate,
                        admin.Id,
                        cancellationToken);

                TempData["Success"] =
                    $"Offer {offer.Offer.OfferReference} was created successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to create offer for OnboardingId {OnboardingId}.",
                    onboardingId);

                TempData["Error"] =
                    "The volunteer offer could not be created.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
        }

        // ============================================================
        // START ADMIN VERIFICATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartVerification(
            int onboardingId,
            CancellationToken cancellationToken)
        {
            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
                return Challenge();

            try
            {
                var success =
                    await _onboardingService.StartVerificationAsync(
                        onboardingId,
                        admin.Id,
                        cancellationToken);

                if (!success)
                {
                    TempData["Error"] =
                        "The onboarding verification could not be started.";

                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id = onboardingId
                        });
                }

                TempData["Success"] =
                    "Volunteer onboarding verification has started.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to start verification. OnboardingId {OnboardingId}",
                    onboardingId);

                TempData["Error"] =
                    "Verification could not be started.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
        }

        // ============================================================
        // REVIEW DOCUMENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewDocument(
            int documentId,
            VolunteerOnboardingDocumentStatus status,
            string? rejectionReason,
            CancellationToken cancellationToken)
        {
            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
                return Challenge();

            try
            {
                var document =
                    await _context.VolunteerOnboardingDocuments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingDocumentId ==
                                documentId,
                            cancellationToken);

                if (document == null)
                    return NotFound();

                var success =
                    await _onboardingService.ReviewDocumentAsync(
                        documentId,
                        status,
                        admin.Id,
                        rejectionReason,
                        cancellationToken);

                if (!success)
                {
                    TempData["Error"] =
                        "The document review could not be completed.";

                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id = document.VolunteerOnboardingId
                        });
                }

                if (status ==
                    VolunteerOnboardingDocumentStatus.Approved)
                {
                    TempData["Success"] =
                        $"{document.DocumentName} was approved.";
                }
                else
                {
                    TempData["Success"] =
                        $"{document.DocumentName} was marked as {status}.";
                }

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = document.VolunteerOnboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to review document. DocumentId {DocumentId}",
                    documentId);

                TempData["Error"] =
                    "The document could not be reviewed.";

                return RedirectToAction(
                    nameof(Index));
            }
        }

        // ============================================================
        // COMPLETE VERIFICATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteVerification(
            int onboardingId,
            string? verificationNotes,
            CancellationToken cancellationToken)
        {
            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
                return Challenge();

            try
            {
                var success =
                    await _onboardingService.CompleteVerificationAsync(
                        onboardingId,
                        admin.Id,
                        verificationNotes,
                        cancellationToken);

                if (!success)
                {
                    TempData["Error"] =
                        "Verification could not be completed. " +
                        "Please ensure all required policies, requirements, " +
                        "and documents have been completed and approved.";

                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id = onboardingId
                        });
                }

                TempData["Success"] =
                    "Volunteer verification completed. " +
                    "The onboarding record is ready for activation.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to complete verification. OnboardingId {OnboardingId}",
                    onboardingId);

                TempData["Error"] =
                    "Verification could not be completed.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
        }

        // ============================================================
        // REJECT FOR CORRECTION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectForCorrection(
            int onboardingId,
            string reason,
            CancellationToken cancellationToken)
        {
            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
                return Challenge();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] =
                    "A correction reason is required.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }

            try
            {
                var success =
                    await _onboardingService.RejectForCorrectionAsync(
                        onboardingId,
                        admin.Id,
                        reason.Trim(),
                        cancellationToken);

                if (!success)
                {
                    TempData["Error"] =
                        "The onboarding record could not be returned for correction.";

                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id = onboardingId
                        });
                }

                TempData["Success"] =
                    "The onboarding record was returned to the applicant for correction.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to reject onboarding for correction. OnboardingId {OnboardingId}",
                    onboardingId);

                TempData["Error"] =
                    "The onboarding record could not be returned for correction.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
        }

        // ============================================================
        // CHECK READY FOR ACTIVATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckReadyForActivation(
            int onboardingId,
            CancellationToken cancellationToken)
        {
            try
            {
                var ready =
                    await _onboardingService.IsReadyForActivationAsync(
                        onboardingId,
                        cancellationToken);

                if (ready)
                {
                    TempData["Success"] =
                        "This volunteer is ready for activation.";
                }
                else
                {
                    TempData["Error"] =
                        "This volunteer is not yet ready for activation. " +
                        "Please check the onboarding requirements, policies, " +
                        "and documents.";
                }

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to check activation readiness. OnboardingId {OnboardingId}",
                    onboardingId);

                TempData["Error"] =
                    "The activation readiness check failed.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }
        }

        // ============================================================
        // ACTIVATION PAGE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Activate(
            int id,
            CancellationToken cancellationToken)
        {
            var model =
                await _activationService.GetActivationViewModelAsync(
                    id,
                    cancellationToken);

            if (model == null)
                return NotFound();

            if (!model.ReadyForActivation)
            {
                TempData["Error"] =
                    "This volunteer is not ready for activation.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id
                    });
            }

            return View(model);
        }

        // ============================================================
        // ACTIVATE VOLUNTEER - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            VolunteerActivationViewModel model,
            CancellationToken cancellationToken)
        {
            if (model == null)
            {
                TempData["Error"] =
                    "Invalid activation request.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var rebuiltModel =
                    await _activationService
                        .GetActivationViewModelAsync(
                            model.VolunteerOnboardingId,
                            cancellationToken);

                if (rebuiltModel != null)
                {
                    rebuiltModel.VolunteerNumber =
                        model.VolunteerNumber;

                    rebuiltModel.Department =
                        model.Department;

                    rebuiltModel.SupervisorId =
                        model.SupervisorId;

                    rebuiltModel.StartDate =
                        model.StartDate;

                    rebuiltModel.EndDate =
                        model.EndDate;

                    rebuiltModel.Bio =
                        model.Bio;

                    rebuiltModel.Motivation =
                        model.Motivation;

                    rebuiltModel.PreferredArea =
                        model.PreferredArea;

                    rebuiltModel.EmergencyContactName =
                        model.EmergencyContactName;

                    rebuiltModel.EmergencyContactPhone =
                        model.EmergencyContactPhone;

                    rebuiltModel.EmergencyContactRelationship =
                        model.EmergencyContactRelationship;

                    return View(rebuiltModel);
                }

                return View(model);
            }

            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
                return Challenge();

            try
            {
                var result =
                    await _activationService.ActivateVolunteerAsync(
                        model,
                        admin.Id,
                        cancellationToken);

                if (!result.Success)
                {
                    TempData["Error"] =
                        result.Message;

                    return RedirectToAction(
                        nameof(Activate),
                        new
                        {
                            id = model.VolunteerOnboardingId
                        });
                }

                _logger.LogInformation(
                    "[SWIFTCARE ACTIVATION] Admin {AdminId} activated Volunteer {VolunteerId}. OnboardingId: {OnboardingId}",
                    admin.Id,
                    result.VolunteerUserId,
                    result.VolunteerOnboardingId);

                TempData["Success"] =
                    "Volunteer activated successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = model.VolunteerOnboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ACTIVATION] Failed to activate volunteer. OnboardingId {OnboardingId}",
                    model.VolunteerOnboardingId);

                TempData["Error"] =
                    "An unexpected error occurred while activating the volunteer.";

                return RedirectToAction(
                    nameof(Activate),
                    new
                    {
                        id = model.VolunteerOnboardingId
                    });
            }
        }

        // ============================================================
        // VIEW ACTIVE VOLUNTEER
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Volunteer(
            string id,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var volunteer =
                await _activationService.GetVolunteerAsync(
                    id,
                    cancellationToken);

            if (volunteer == null)
                return NotFound();

            return View(volunteer);
        }

        // ============================================================
        // VIEW VOLUNTEER FROM ONBOARDING
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ActivatedVolunteer(
            int onboardingId,
            CancellationToken cancellationToken)
        {
            var volunteer =
                await _activationService.GetVolunteerByOnboardingAsync(
                    onboardingId,
                    cancellationToken);

            if (volunteer == null)
            {
                TempData["Error"] =
                    "No active Volunteer account is linked to this onboarding record.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = onboardingId
                    });
            }

            return View(
                "Volunteer",
                volunteer);
        }
    }
}