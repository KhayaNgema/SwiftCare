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
using SwiftCare.Services.IdentityVerification;
using SwiftCare.Services.VolunteerActivation;
using SwiftCare.Services.VolunteerContracts;
using SwiftCare.Services.VolunteerOnboarding;
using SwiftCare.Services.Volunteers;
using SwiftCare.ViewModels;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Controllers
{
    [Authorize(Roles = "Human Resources Manager")]
    public class VolunteerOnboardingAdminController : Controller
    {
        private readonly ILogger<VolunteerOnboardingAdminController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IVolunteerOnboardingService _onboardingService;
        private readonly IVolunteerActivationService _activationService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IVolunteerIdentityVerificationService _identityVerificationService;
        private readonly IVolunteerContractService _contractService;
        private readonly IVolunteerAccountService _volunteerAccountService;

        public VolunteerOnboardingAdminController(
            ILogger<VolunteerOnboardingAdminController> logger,
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            IVolunteerOnboardingService onboardingService,
            IVolunteerActivationService activationService,
            IVolunteerAccountService volunteerAccountService,
            EmailService emailService,
            IConfiguration configuration,
            IVolunteerIdentityVerificationService identityVerificationService,
            IVolunteerContractService contractService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _onboardingService = onboardingService;
            _activationService = activationService;
            _volunteerAccountService = volunteerAccountService;
            _emailService = emailService;
            _configuration = configuration;
            _contractService = contractService;
            _identityVerificationService = identityVerificationService;
        }

        // ============================================================
        // VOLUNTEERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Volunteers(
            CancellationToken cancellationToken)
        {
            var volunteers = await _context.Volunteers
                .AsNoTracking()
                .OrderBy(v => v.LastName)
                .ThenBy(v => v.FirstName)
                .ToListAsync(cancellationToken);

            return View(volunteers);
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
        // REVIEW VOLUNTEER ONBOARDING
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Review(
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
            {
                return NotFound();
            }


            // ============================================================
            // LATEST VOLUNTEER CONTRACT
            // ============================================================
            //
            // IMPORTANT:
            // We intentionally retrieve the latest contract for this
            // onboarding record.
            //
            // The View does not need a direct VolunteerContract navigation
            // property on VolunteerOnboarding.
            //
            // ============================================================

            var latestContract =
                await _context.VolunteerContracts
                    .AsNoTracking()
                    .Where(x =>
                        x.VolunteerOnboardingId == id)
                    .OrderByDescending(x => x.CreatedDate)
                    .ThenByDescending(x => x.VolunteerContractId)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            ViewBag.Contract = latestContract;


            // ============================================================
            // LATEST DIDIT IDENTITY VERIFICATION
            // ============================================================

            var latestIdentityVerification =
                await _context.VolunteerIdentityVerifications
                    .AsNoTracking()
                    .Where(x =>
                        x.VolunteerOnboardingId == id)
                    .OrderByDescending(x => x.VerifiedDate)
                    .ThenByDescending(x => x.ModifiedDate)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            ViewBag.IdentityVerification =
                latestIdentityVerification;


            return View(onboarding);
        }


        // ============================================================
        // APPROVE VOLUNTEER CONTRACT
        // ============================================================

        // ============================================================
        // APPROVE VOLUNTEER CONTRACT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveContract(
            int onboardingId,
            string? reviewNotes,
            CancellationToken cancellationToken)
        {
            try
            {
                // ========================================================
                // GET ADMIN
                // ========================================================

                var admin =
                    await _userManager.GetUserAsync(User);

                if (admin == null)
                {
                    TempData["Error"] =
                        "Administrator account could not be found.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }


                _logger.LogInformation(
                    "[VOLUNTEER ONBOARDING] Contract approval started. " +
                    "OnboardingId={OnboardingId}, AdminId={AdminId}",
                    onboardingId,
                    admin.Id);


                // ========================================================
                // GET CONTRACT
                // ========================================================

                var contract =
                    await _contractService.GetContractByOnboardingAsync(
                        onboardingId,
                        cancellationToken);

                if (contract == null)
                {
                    TempData["Error"] =
                        "No volunteer service agreement was found.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }


                // ========================================================
                // APPROVE SIGNED CONTRACT
                // ========================================================

                await _contractService.ApproveContractAsync(
                    onboardingId,
                    admin.Id,
                    reviewNotes,
                    cancellationToken);


                _logger.LogInformation(
                    "[VOLUNTEER ONBOARDING] Contract approved. " +
                    "ContractId={ContractId}, OnboardingId={OnboardingId}",
                    contract.VolunteerContractId,
                    onboardingId);


                // ========================================================
                // LOAD ONBOARDING
                // ========================================================

                var onboarding =
                    await _context.VolunteerOnboardings
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                onboardingId,
                            cancellationToken);

                if (onboarding == null)
                {
                    TempData["Error"] =
                        "Volunteer onboarding record could not be found.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }


                // ========================================================
                // CREATE VOLUNTEER ACCOUNT / PROFILE
                //
                // This creates the Volunteer record but does NOT activate
                // the Volunteer.
                //
                // Volunteer status:
                //     PendingActivation
                //
                // Activation remains a separate step.
                // ========================================================

                Volunteer volunteer;

                try
                {
                    volunteer =
                        await _volunteerAccountService
                            .CreateFromOnboardingAsync(
                                onboardingId,
                                admin.Id,
                                cancellationToken);

                    _logger.LogInformation(
                        "[VOLUNTEER ONBOARDING] Volunteer account/profile " +
                        "created successfully after contract approval. " +
                        "VolunteerId={VolunteerId}, " +
                        "OnboardingId={OnboardingId}, " +
                        "Status={Status}, " +
                        "SupervisorId={SupervisorId}",
                        volunteer.Id,
                        onboardingId,
                        volunteer.Status,
                        volunteer.SupervisorId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[VOLUNTEER ONBOARDING] Contract was approved, " +
                        "but Volunteer account/profile creation failed. " +
                        "OnboardingId={OnboardingId}, AdminId={AdminId}",
                        onboardingId,
                        admin.Id);

                    TempData["Error"] =
                        "The contract was approved, but the Volunteer account " +
                        "could not be created. Please review the onboarding record.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }


                // ========================================================
                // MOVE ONBOARDING TO FINAL ADMINISTRATIVE VERIFICATION
                // ========================================================

                if (onboarding.Status ==
                    VolunteerOnboardingStatus.ContractPending)
                {
                    onboarding.Status =
                        VolunteerOnboardingStatus.AdminVerification;

                    onboarding.CurrentStep = 4;

                    onboarding.ModifiedDate =
                        DateTime.Now;

                    onboarding.ModifiedBy =
                        admin.Id;

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    _logger.LogInformation(
                        "[VOLUNTEER ONBOARDING] Onboarding moved to " +
                        "AdminVerification after contract approval. " +
                        "OnboardingId={OnboardingId}, VolunteerId={VolunteerId}",
                        onboardingId,
                        volunteer.Id);
                }


                // ========================================================
                // SUCCESS
                // ========================================================

                TempData["Success"] =
                    "The Volunteer Service Agreement has been approved " +
                    "and the Volunteer account has been created. " +
                    "The Volunteer remains pending activation until final " +
                    "administrative verification is completed.";

                return RedirectToAction(
                    nameof(Review),
                    new { id = onboardingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[VOLUNTEER ONBOARDING] Error approving volunteer contract. " +
                    "OnboardingId={OnboardingId}",
                    onboardingId);

                TempData["Error"] =
                    "An error occurred while approving the Volunteer Service Agreement.";

                return RedirectToAction(
                    nameof(Review),
                    new { id = onboardingId });
            }
        }
        // ============================================================
        // REQUEST CONTRACT CORRECTION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestContractCorrection(
            int onboardingId,
            string correctionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                var admin =
                    await _userManager.GetUserAsync(User);

                if (admin == null)
                {
                    TempData["Error"] =
                        "Your administrator session could not be found.";

                    return RedirectToAction(
                        nameof(Index));
                }


                if (string.IsNullOrWhiteSpace(
                        correctionReason))
                {
                    TempData["Error"] =
                        "Please provide a correction reason.";

                    return RedirectToAction(
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }


                var contract =
                    await _contractService.GetContractByOnboardingAsync(
                        onboardingId,
                        cancellationToken);

                if (contract == null)
                {
                    TempData["Error"] =
                        "No Volunteer Service Agreement was found for this onboarding record.";

                    return RedirectToAction(
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }


                await _contractService.RequestCorrectionAsync(
                    onboardingId,
                    admin.Id,
                    correctionReason.Trim(),
                    cancellationToken);


                _logger.LogInformation(
                    "Volunteer contract correction requested. " +
                    "OnboardingId={OnboardingId}, ContractId={ContractId}, AdminId={AdminId}",
                    onboardingId,
                    contract.VolunteerContractId,
                    admin.Id);


                TempData["Success"] =
                    "The applicant has been asked to correct and resubmit the Volunteer Service Agreement.";


                return RedirectToAction(
                    nameof(Review),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error requesting volunteer contract correction. " +
                    "OnboardingId={OnboardingId}",
                    onboardingId);

                TempData["Error"] =
                    "The contract correction request could not be completed. " +
                    ex.Message;


                return RedirectToAction(
                    nameof(Review),
                    new
                    {
                        id = onboardingId
                    });
            }
        }


        // ============================================================
        // REJECT VOLUNTEER CONTRACT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectContract(
            int onboardingId,
            string rejectionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                var admin =
                    await _userManager.GetUserAsync(User);

                if (admin == null)
                {
                    TempData["Error"] =
                        "Your administrator session could not be found.";

                    return RedirectToAction(
                        nameof(Index));
                }


                if (string.IsNullOrWhiteSpace(
                        rejectionReason))
                {
                    TempData["Error"] =
                        "Please provide a rejection reason.";

                    return RedirectToAction(
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }


                var contract =
                    await _contractService.GetContractByOnboardingAsync(
                        onboardingId,
                        cancellationToken);

                if (contract == null)
                {
                    TempData["Error"] =
                        "No Volunteer Service Agreement was found for this onboarding record.";

                    return RedirectToAction(
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }


                await _contractService.RejectContractAsync(
                    onboardingId,
                    admin.Id,
                    rejectionReason.Trim(),
                    cancellationToken);


                _logger.LogInformation(
                    "Volunteer contract rejected. " +
                    "OnboardingId={OnboardingId}, ContractId={ContractId}, AdminId={AdminId}",
                    onboardingId,
                    contract.VolunteerContractId,
                    admin.Id);


                TempData["Success"] =
                    "The Volunteer Service Agreement has been rejected.";


                return RedirectToAction(
                    nameof(Review),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error rejecting volunteer contract. " +
                    "OnboardingId={OnboardingId}",
                    onboardingId);

                TempData["Error"] =
                    "The contract could not be rejected. " +
                    ex.Message;


                return RedirectToAction(
                    nameof(Review),
                    new
                    {
                        id = onboardingId
                    });
            }
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
                    nameof(Review),
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
                        nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
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
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }

                TempData["Success"] =
                    "Volunteer onboarding verification has started.";

                return RedirectToAction(
                    nameof(Review),
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
                    nameof(Review),
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

                // IMPORTANT:
                // Interface signature is:
                // ReviewDocumentAsync(
                //     documentId,
                //     adminId,
                //     status,
                //     reviewNotes,
                //     cancellationToken)
                var success =
                    await _onboardingService.ReviewDocumentAsync(
                        documentId,
                        admin.Id,
                        status,
                        rejectionReason,
                        cancellationToken);

                if (!success)
                {
                    TempData["Error"] =
                        "The document review could not be completed.";

                    return RedirectToAction(
                        nameof(Review),
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
                    nameof(Review),
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
                _logger.LogInformation(
                    "[SWIFTCARE ONBOARDING] Admin {AdminId} attempting to complete identity verification for OnboardingId {OnboardingId}.",
                    admin.Id,
                    onboardingId);

                // ====================================================
                // 1. COMPLETE IDENTITY VERIFICATION
                // ====================================================

                var success =
                    await _onboardingService.CompleteVerificationAsync(
                        onboardingId,
                        admin.Id,
                        verificationNotes,
                        cancellationToken);

                if (!success)
                {
                    _logger.LogWarning(
                        "[SWIFTCARE ONBOARDING] Identity verification could not be completed for OnboardingId {OnboardingId}.",
                        onboardingId);

                    TempData["Error"] =
                        "Verification could not be completed. " +
                        "Please ensure all required policies, requirements, " +
                        "and documents have been completed and approved.";

                    return RedirectToAction(
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }

                _logger.LogInformation(
                    "[SWIFTCARE ONBOARDING] Identity verification completed successfully for OnboardingId {OnboardingId}.",
                    onboardingId);

                // ====================================================
                // 2. REFRESH ONBOARDING
                // ====================================================

                var onboarding =
                    await _onboardingService.GetOnboardingAsync(
                        onboardingId,
                        cancellationToken);

                if (onboarding == null)
                {
                    _logger.LogWarning(
                        "[SWIFTCARE ONBOARDING] Onboarding {OnboardingId} could not be found after verification completion.",
                        onboardingId);

                    TempData["Error"] =
                        "Verification was completed, but the onboarding record could not be found.";

                    return RedirectToAction(
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }

                _logger.LogInformation(
                    "[SWIFTCARE ONBOARDING] Onboarding {OnboardingId} current status after verification: {Status}.",
                    onboardingId,
                    onboarding.Status);

                // ====================================================
                // 3. GENERATE VOLUNTEER SERVICE AGREEMENT
                // ====================================================
                //
                // Identity verification is now complete.
                // Generate the existing Volunteer Service Agreement.
                //
                // CreateAndGenerateContractAsync() itself performs
                // the Didit verification checks before generating.
                // ====================================================

                if (onboarding.Status ==
                    VolunteerOnboardingStatus.ContractPending)
                {
                    _logger.LogInformation(
                        "[SWIFTCARE CONTRACT] Identity verification completed. Generating Volunteer Service Agreement for OnboardingId {OnboardingId}.",
                        onboardingId);

                    try
                    {
                        var contract =
                            await _contractService
                                .CreateAndGenerateContractAsync(
                                    onboardingId,
                                    cancellationToken);

                        _logger.LogInformation(
                            "[SWIFTCARE CONTRACT] Volunteer Service Agreement generated successfully. " +
                            "OnboardingId {OnboardingId}, ContractId {ContractId}, ContractReference {ContractReference}, Status {Status}.",
                            onboardingId,
                            contract.VolunteerContractId,
                            contract.ContractReference,
                            contract.Status);

                        TempData["Success"] =
                            "Identity verification completed successfully. " +
                            "The Volunteer Service Agreement has now been generated and is ready for completion.";

                        return RedirectToAction(
                            nameof(Review),
                            new
                            {
                                id = onboardingId
                            });
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "[SWIFTCARE CONTRACT] Contract generation could not be completed after identity verification. " +
                            "OnboardingId {OnboardingId}.",
                            onboardingId);

                        TempData["Error"] =
                            "Identity verification was completed, but the Volunteer Service Agreement " +
                            "could not be generated yet. Please review the onboarding requirements.";

                        return RedirectToAction(
                            nameof(Review),
                            new
                            {
                                id = onboardingId
                            });
                    }
                }

                // ====================================================
                // 4. OTHER POSSIBLE ONBOARDING STATES
                // ====================================================

                if (onboarding.Status ==
                    VolunteerOnboardingStatus.ReadyForActivation)
                {
                    _logger.LogInformation(
                        "[SWIFTCARE ONBOARDING] OnboardingId {OnboardingId} is already ReadyForActivation after verification.",
                        onboardingId);

                    TempData["Success"] =
                        "Volunteer verification completed successfully. " +
                        "The onboarding record is now ready for activation.";
                }
                else
                {
                    _logger.LogInformation(
                        "[SWIFTCARE ONBOARDING] Verification completed for OnboardingId {OnboardingId}. " +
                        "Current onboarding status: {Status}.",
                        onboardingId,
                        onboarding.Status);

                    TempData["Success"] =
                        "Verification completed successfully.";
                }

                return RedirectToAction(
                    nameof(Review),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ONBOARDING] Failed to complete verification for OnboardingId {OnboardingId}.",
                    onboardingId);

                TempData["Error"] =
                    "Verification could not be completed.";

                return RedirectToAction(
                    nameof(Review),
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
                    nameof(Review),
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
                        nameof(Review),
                        new
                        {
                            id = onboardingId
                        });
                }

                TempData["Success"] =
                    "The onboarding record was returned to the applicant for correction.";

                return RedirectToAction(
                    nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
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
                    nameof(Review),
                    new
                    {
                        id = onboardingId
                    });
            }

            return View(
                "Volunteer",
                volunteer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartIdentityVerification(
            int onboardingId,
            int identityDocumentId,
            CancellationToken cancellationToken)
        {
            try
            {
                // Make sure the selected document actually belongs to this onboarding
                // and is specifically an IdentityDocument.
                var identityDocument = await _context.VolunteerOnboardingDocuments
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId == onboardingId &&
                            x.VolunteerOnboardingDocumentId == identityDocumentId &&
                            x.DocumentType ==
                                VolunteerOnboardingDocumentType.IdentityDocument,
                        cancellationToken);

                if (identityDocument == null)
                {
                    TempData["Error"] =
                        "The selected document could not be found or is not an identity document.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }

                var verification =
                    await _identityVerificationService
                        .StartIdentityVerificationAsync(
                            onboardingId,
                            identityDocumentId,
                            cancellationToken);

                if (verification == null)
                {
                    TempData["Error"] =
                        "Unable to start identity verification.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }

                if (verification.Status ==
                    VolunteerIdentityVerificationStatus.Verified)
                {
                    TempData["Success"] =
                        "This volunteer's identity has already been verified.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }

                if (verification.Status ==
                    VolunteerIdentityVerificationStatus.Failed ||
                    verification.Status ==
                    VolunteerIdentityVerificationStatus.Rejected)
                {
                    TempData["Error"] =
                        verification.FailureReason ??
                        "Didit could not start the identity verification.";

                    return RedirectToAction(
                        nameof(Review),
                        new { id = onboardingId });
                }

                TempData["Success"] =
                    "Didit identity verification has been started successfully. " +
                    "The selected identity document has been associated with the verification session.";

                return RedirectToAction(
                    nameof(Review),
                    new { id = onboardingId });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to start Didit identity verification for onboarding {OnboardingId} using identity document {IdentityDocumentId}.",
                    onboardingId,
                    identityDocumentId);

                TempData["Error"] =
                    "Unable to start identity verification. Please try again.";

                return RedirectToAction(
                    nameof(Review),
                    new { id = onboardingId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> IdentityVerificationStatus(
       int onboardingId,
       CancellationToken cancellationToken)
        {
            var verification =
                await _context.VolunteerIdentityVerifications
                    .AsNoTracking()
                    .Where(x => x.VolunteerOnboardingId == onboardingId)
                    .OrderByDescending(x => x.ModifiedDate)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.Status,
                        x.DocumentVerified,
                        x.LivenessVerified,
                        x.FaceMatchVerified,
                        x.FaceIdentityEnrolled,
                        x.FailureReason,
                        x.ProviderVerificationId,
                        x.ProviderSessionId,
                        x.VerificationUrl,
                        x.VerificationStartedDate,
                        x.VerifiedDate,
                        x.CreatedDate,
                        x.ModifiedDate
                    })
                    .FirstOrDefaultAsync(cancellationToken);

            if (verification == null)
            {
                return Json(new
                {
                    exists = false,
                    status = "Pending"
                });
            }

            return Json(new
            {
                exists = true,
                id = verification.Id,
                status = verification.Status.ToString(),
                documentVerified = verification.DocumentVerified,
                livenessVerified = verification.LivenessVerified,
                faceMatchVerified = verification.FaceMatchVerified,
                faceIdentityEnrolled = verification.FaceIdentityEnrolled,
                failureReason = verification.FailureReason,
                providerVerificationId = verification.ProviderVerificationId,
                providerSessionId = verification.ProviderSessionId,
                verificationUrl = verification.VerificationUrl,
                verificationStartedDate = verification.VerificationStartedDate,
                verifiedDate = verification.VerifiedDate,
                createdDate = verification.CreatedDate,
                modifiedDate = verification.ModifiedDate
            });
        }
    }
}