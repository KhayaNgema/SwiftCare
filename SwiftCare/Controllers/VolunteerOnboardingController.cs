using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerContracts;
using SwiftCare.Services.VolunteerOnboarding;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Controllers
{
    [Authorize]
    public class VolunteerOnboardingController : Controller
    {
        private readonly ILogger<VolunteerOnboardingController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IVolunteerOnboardingService _onboardingService;
        private readonly IVolunteerContractService _contractService;
        private readonly FileUploadService _fileUploadService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public VolunteerOnboardingController(
            ILogger<VolunteerOnboardingController> logger,
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            IVolunteerOnboardingService onboardingService,
            IVolunteerContractService contractService,
            FileUploadService fileUploadService,
            EmailService emailService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _onboardingService = onboardingService;
            _contractService = contractService;
            _fileUploadService = fileUploadService;
            _emailService = emailService;
            _configuration = configuration;
            _environment = environment;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var onboarding =
                await _onboardingService
                    .GetOnboardingByApplicantAsync(
                        userId,
                        cancellationToken);

            if (onboarding == null)
            {
                return NotFound();
            }

            return View(onboarding);
        }

        // ============================================================
        // OFFER
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        [Route("VolunteerOnboarding/Offer")]
        public async Task<IActionResult> Offer(
            int offerId,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (offerId <= 0 ||
                string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Message =
                    "This volunteer offer link is invalid.";

                return View("OfferError");
            }

            var offer =
                await _context.VolunteerOffers
                    .Include(x => x.VolunteerOnboarding)
                        .ThenInclude(x => x.Applicant)

                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.VolunteerOpportunity)

                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOfferId == offerId,
                        cancellationToken);

            if (offer == null)
            {
                ViewBag.Message =
                    "The volunteer offer could not be found.";

                return View("OfferError");
            }

            if (offer.Status !=
                VolunteerOfferStatus.Pending)
            {
                ViewBag.Message =
                    "This volunteer offer is no longer available.";

                return View("OfferError");
            }

            if (offer.TokenExpiryDate < DateTime.Now)
            {
                ViewBag.Message =
                    "This volunteer offer has expired.";

                return View("OfferError");
            }

            if (string.IsNullOrWhiteSpace(
                    offer.AcceptanceTokenHash))
            {
                ViewBag.Message =
                    "This volunteer offer is invalid.";

                return View("OfferError");
            }

            var suppliedHash =
                HashToken(token);

            if (!string.Equals(
                    suppliedHash,
                    offer.AcceptanceTokenHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Message =
                    "This volunteer offer link is invalid.";

                return View("OfferError");
            }

            // IMPORTANT:
            // The Razor page needs this token for the
            // Accept and Decline forms.
            ViewBag.Token = token;

            return View(offer);
        }

        // ============================================================
        // ACCEPT OFFER
        // ============================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(
    int offerId,
    string token,
    CancellationToken cancellationToken = default)
        {
            if (offerId <= 0 ||
                string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Message =
                    "This volunteer offer link is invalid.";

                return View("OfferError");
            }

            try
            {
                var result =
                    await _onboardingService
                        .AcceptOfferAsync(
                            offerId,
                            token,
                            cancellationToken);

                if (!result.Success)
                {
                    ViewBag.Message =
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "We could not accept this volunteer offer."
                            : result.Message;

                    return View("OfferError");
                }

                /*
                 * RESTORED FROM THE OLD WORKING FLOW.
                 *
                 * Policies retrieves the current applicant's
                 * onboarding record through Identity.
                 *
                 * Do not pass OnboardingId here because
                 * VolunteerOfferActionResult does not contain it.
                 */
                return RedirectToAction(
                    nameof(Policies));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error accepting volunteer offer {OfferId}.",
                    offerId);

                ViewBag.Message =
                    "We could not process the offer acceptance.";

                return View("OfferError");
            }
        }

        // ============================================================
        // POLICIES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Policies(
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // ============================================================
            // GET CURRENT APPLICANT ONBOARDING
            // ============================================================

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.PolicyAcceptances)
                    .Include(x => x.Applicant)
                    .FirstOrDefaultAsync(
                        x => x.ApplicantId == userId,
                        cancellationToken);

            if (onboarding == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE POLICIES] No onboarding found for applicant {ApplicantId}.",
                    userId);

                return NotFound();
            }

            _logger.LogInformation(
                "[SWIFTCARE POLICIES] Loading policies for ApplicantId={ApplicantId}, OnboardingId={OnboardingId}.",
                userId,
                onboarding.VolunteerOnboardingId);


            // ============================================================
            // GET ACTIVE REQUIRED POLICIES
            // ============================================================

            var policies =
                await _context.VolunteerPolicies
                    .AsNoTracking()
                    .Include(x => x.Items)
                    .Where(x =>
                        x.IsActive &&
                        x.IsRequired)
                    .OrderBy(x => x.VolunteerPolicyId)
                    .ToListAsync(
                        cancellationToken);


            // ============================================================
            // FILTER ACTIVE POLICY ITEMS
            // ============================================================

            foreach (var policy in policies)
            {
                policy.Items =
                    policy.Items
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.DisplayOrder)
                        .ToList();
            }


            // ============================================================
            // GET ACCEPTED POLICY IDS
            // ============================================================

            var acceptedPolicyIds =
                await _context.VolunteerPolicyAcceptances
                    .AsNoTracking()
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        onboarding.VolunteerOnboardingId &&
                        x.Accepted)
                    .Select(x =>
                        x.VolunteerPolicyId)
                    .Distinct()
                    .ToListAsync(
                        cancellationToken);


            // ============================================================
            // PASS DATA TO VIEW
            // ============================================================

            ViewBag.Policies =
                policies;

            ViewBag.AcceptedPolicyIds =
                acceptedPolicyIds;


            // ============================================================
            // LOGGING
            // ============================================================

            _logger.LogInformation(
                "[SWIFTCARE POLICIES] Policies loaded. " +
                "OnboardingId={OnboardingId}, " +
                "PolicyCount={PolicyCount}, " +
                "AcceptedPolicyCount={AcceptedPolicyCount}.",
                onboarding.VolunteerOnboardingId,
                policies.Count,
                acceptedPolicyIds.Count);


            foreach (var policy in policies)
            {
                _logger.LogInformation(
                    "[SWIFTCARE POLICIES] Policy loaded. " +
                    "PolicyId={PolicyId}, " +
                    "Title={Title}, " +
                    "Version={Version}, " +
                    "Required={Required}, " +
                    "Items={ItemCount}.",
                    policy.VolunteerPolicyId,
                    policy.Title,
                    policy.Version,
                    policy.IsRequired,
                    policy.Items?.Count ?? 0);
            }


            return View(onboarding);
        }

        // ============================================================
        // DECLINE OFFER
        // ============================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineOffer(
            int offerId,
            string token,
            string declineReason,
            CancellationToken cancellationToken = default)
        {
            if (offerId <= 0 ||
                string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Message =
                    "This volunteer offer link is invalid.";

                return View("OfferError");
            }

            if (string.IsNullOrWhiteSpace(declineReason))
            {
                ViewBag.Message =
                    "Please provide a reason for declining the volunteer offer.";

                return View("OfferError");
            }

            try
            {
                var result =
                    await _onboardingService
                        .DeclineOfferAsync(
                            offerId,
                            token,
                            declineReason,
                            cancellationToken);

                if (!result.Success)
                {
                    ViewBag.Message =
                        string.IsNullOrWhiteSpace(
                            result.Message)
                            ? "We could not process this volunteer offer decline."
                            : result.Message;

                    return View("OfferError");
                }

                /*
                 * RESTORED FROM THE OLD FLOW.
                 *
                 * The Index action handles the applicant's
                 * authenticated onboarding dashboard.
                 */
                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error declining volunteer offer {OfferId}.",
                    offerId);

                ViewBag.Message =
                    "We could not process the offer decline.";

                return View("OfferError");
            }
        }

        // ============================================================
        // ACCEPT POLICY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPolicy(
            int policyId,
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var onboarding =
                await _onboardingService
                    .GetOnboardingByApplicantAsync(
                        userId,
                        cancellationToken);

            if (onboarding == null)
            {
                return NotFound();
            }

            try
            {
                /*
                 * IMPORTANT:
                 *
                 * AcceptPolicyAsync returns bool.
                 */
                var result =
                    await _onboardingService
                        .AcceptPolicyAsync(
                            onboarding.VolunteerOnboardingId,
                            policyId,
                            userId,
                            HttpContext
                                .Connection
                                .RemoteIpAddress?
                                .ToString(),
                            Request.Headers["User-Agent"]
                                .ToString(),
                            cancellationToken);

                if (!result)
                {
                    TempData["ErrorMessage"] =
                        "We could not record this policy acceptance. " +
                        "Please make sure the policy is active and required.";

                    return RedirectToAction(
                        nameof(Policies));
                }

                TempData["SuccessMessage"] =
                    "Policy accepted successfully.";

                /*
                 * AcceptPolicyAsync returns bool, therefore determine
                 * whether every required policy has been accepted.
                 */
                var allPoliciesAccepted =
                    await _onboardingService
                        .HasAcceptedAllRequiredPoliciesAsync(
                            onboarding.VolunteerOnboardingId,
                            cancellationToken);

                if (allPoliciesAccepted)
                {
                    return RedirectToAction(
                        nameof(Contract));
                }

                return RedirectToAction(
                    nameof(Policies));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error accepting policy {PolicyId} for onboarding {OnboardingId}.",
                    policyId,
                    onboarding.VolunteerOnboardingId);

                TempData["ErrorMessage"] =
                    "We could not record the policy acceptance.";

                return RedirectToAction(
                    nameof(Policies));
            }
        }

        // ============================================================
        // CONTRACT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Contract(
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Applicant)

                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x =>
                            x.VolunteerOpportunity)

                    .Include(x => x.Documents)

                    .Include(x => x.Requirements)

                    .FirstOrDefaultAsync(
                        x =>
                            x.ApplicantId == userId,
                        cancellationToken);

            if (onboarding == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // IDENTITY VERIFICATION MUST BE COMPLETED
            // BEFORE CONTRACT ACCESS
            // --------------------------------------------------------

            var identityVerification =
                await _context.VolunteerIdentityVerifications
                    .AsNoTracking()
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        onboarding.VolunteerOnboardingId)
                    .OrderByDescending(x =>
                        x.VerifiedDate)
                    .ThenByDescending(x =>
                        x.ModifiedDate)
                    .ThenByDescending(x =>
                        x.Id)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (identityVerification == null ||
                identityVerification.Status !=
                    VolunteerIdentityVerificationStatus.Verified ||
                !identityVerification.DocumentVerified ||
                !identityVerification.LivenessVerified ||
                !identityVerification.FaceMatchVerified)
            {
                TempData["ErrorMessage"] =
                    "Your identity verification must be completed before you can access the volunteer service agreement.";

                return RedirectToAction(
                    nameof(Index));
            }

            var contract =
                await _contractService
                    .GetContractByOnboardingAsync(
                        onboarding.VolunteerOnboardingId,
                        cancellationToken);

            if (contract == null)
            {
                TempData["ErrorMessage"] =
                    "Your volunteer service agreement has not been generated yet.";

                return View(onboarding);
            }

            ViewBag.Contract =
                contract;

            ViewBag.IdentityVerification =
                identityVerification;

            return View(onboarding);
        }

        // ============================================================
        // DOWNLOAD GENERATED CONTRACT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DownloadContract(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (onboardingId <= 0)
            {
                return BadRequest();
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId &&
                            x.ApplicantId == userId,
                        cancellationToken);

            if (onboarding == null)
            {
                return NotFound();
            }

            var contract =
                await _contractService
                    .GetContractByOnboardingAsync(
                        onboardingId,
                        cancellationToken);

            if (contract == null ||
                string.IsNullOrWhiteSpace(
                    contract.GeneratedFilePath))
            {
                return NotFound();
            }

            var fullPath =
                GetSafeWebRootFilePath(
                    contract.GeneratedFilePath);

            if (fullPath == null ||
                !System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning(
                    "Generated volunteer contract file not found. " +
                    "OnboardingId={OnboardingId}, Path={Path}",
                    onboardingId,
                    contract.GeneratedFilePath);

                return NotFound();
            }

            var fileName =
                string.IsNullOrWhiteSpace(
                    contract.GeneratedFileName)
                    ? $"{contract.ContractReference}.pdf"
                    : contract.GeneratedFileName;

            return PhysicalFile(
                fullPath,
                "application/pdf",
                fileName);
        }

        // ============================================================
        // UPLOAD SIGNED CONTRACT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSignedContract(
            int onboardingId,
            IFormFile signedContract,
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (onboardingId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid onboarding request.";

                return RedirectToAction(
                    nameof(Contract));
            }

            if (signedContract == null ||
                signedContract.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select your signed PDF contract.";

                return RedirectToAction(
                    nameof(Contract));
            }

            try
            {
                var onboarding =
                    await _context.VolunteerOnboardings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                onboardingId &&
                                x.ApplicantId == userId,
                            cancellationToken);

                if (onboarding == null)
                {
                    return NotFound();
                }

                var contract =
                    await _contractService
                        .GetContractByOnboardingAsync(
                            onboardingId,
                            cancellationToken);

                if (contract == null)
                {
                    TempData["ErrorMessage"] =
                        "Your volunteer service agreement could not be found.";

                    return RedirectToAction(
                        nameof(Contract));
                }

                if (contract.Status !=
                        VolunteerContractStatus.AwaitingApplicant &&
                    contract.Status !=
                        VolunteerContractStatus.CorrectionRequired)
                {
                    TempData["ErrorMessage"] =
                        "The signed contract cannot currently be uploaded.";

                    return RedirectToAction(
                        nameof(Contract));
                }

                var uploadedContract =
                    await _contractService
                        .UploadCompletedContractAsync(
                            onboardingId,
                            signedContract,
                            userId,
                            cancellationToken);

                TempData["SuccessMessage"] =
                    "Your signed volunteer service agreement has been submitted successfully. It is now awaiting administrative review.";

                _logger.LogInformation(
                    "Applicant uploaded signed volunteer contract. " +
                    "OnboardingId={OnboardingId}, " +
                    "ContractId={ContractId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    uploadedContract.VolunteerContractId,
                    userId);

                return RedirectToAction(
                    nameof(Contract));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid signed contract upload. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(Contract));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Signed contract upload rejected. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(Contract));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error uploading signed contract. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    "We could not upload your signed contract. Please try again.";

                return RedirectToAction(
                    nameof(Contract));
            }
        }

        // ============================================================
        // RESUBMIT CORRECTED SIGNED CONTRACT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitSignedContract(
            int onboardingId,
            IFormFile signedContract,
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (onboardingId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid onboarding request.";

                return RedirectToAction(
                    nameof(Contract));
            }

            if (signedContract == null ||
                signedContract.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select the corrected signed PDF contract.";

                return RedirectToAction(
                    nameof(Contract));
            }

            try
            {
                var onboarding =
                    await _context.VolunteerOnboardings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                onboardingId &&
                                x.ApplicantId == userId,
                            cancellationToken);

                if (onboarding == null)
                {
                    return NotFound();
                }

                var contract =
                    await _contractService
                        .GetContractByOnboardingAsync(
                            onboardingId,
                            cancellationToken);

                if (contract == null)
                {
                    TempData["ErrorMessage"] =
                        "Your volunteer service agreement could not be found.";

                    return RedirectToAction(
                        nameof(Contract));
                }

                if (contract.Status !=
                    VolunteerContractStatus.CorrectionRequired)
                {
                    TempData["ErrorMessage"] =
                        "This contract does not currently require a correction.";

                    return RedirectToAction(
                        nameof(Contract));
                }

                var updatedContract =
                    await _contractService
                        .ResubmitContractAsync(
                            onboardingId,
                            signedContract,
                            userId,
                            cancellationToken);

                TempData["SuccessMessage"] =
                    "Your corrected signed contract has been resubmitted and is now awaiting administrative review.";

                _logger.LogInformation(
                    "Applicant resubmitted signed volunteer contract. " +
                    "OnboardingId={OnboardingId}, " +
                    "ContractId={ContractId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    updatedContract.VolunteerContractId,
                    userId);

                return RedirectToAction(
                    nameof(Contract));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid signed contract resubmission. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(Contract));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Signed contract resubmission rejected. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(Contract));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error resubmitting signed contract. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    "We could not resubmit your corrected contract. Please try again.";

                return RedirectToAction(
                    nameof(Contract));
            }
        }

        // ============================================================
        // DOWNLOAD SIGNED CONTRACT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DownloadSignedContract(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (onboardingId <= 0)
            {
                return BadRequest();
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId &&
                            x.ApplicantId == userId,
                        cancellationToken);

            if (onboarding == null)
            {
                return NotFound();
            }

            var contract =
                await _contractService
                    .GetContractByOnboardingAsync(
                        onboardingId,
                        cancellationToken);

            if (contract == null ||
                string.IsNullOrWhiteSpace(
                    contract.SignedFilePath))
            {
                return NotFound();
            }

            var fullPath =
                GetSafeWebRootFilePath(
                    contract.SignedFilePath);

            if (fullPath == null ||
                !System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning(
                    "Signed volunteer contract file not found. " +
                    "OnboardingId={OnboardingId}, Path={Path}",
                    onboardingId,
                    contract.SignedFilePath);

                return NotFound();
            }

            var fileName =
                string.IsNullOrWhiteSpace(
                    contract.SignedFileName)
                    ? $"{contract.ContractReference}-Signed.pdf"
                    : contract.SignedFileName;

            return PhysicalFile(
                fullPath,
                "application/pdf",
                fileName);
        }

        // ============================================================
        // DOCUMENTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Documents(
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Applicant)

                    .Include(x => x.Documents)

                    .Include(x => x.Requirements)

                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x =>
                            x.VolunteerOpportunity)

                    .FirstOrDefaultAsync(
                        x =>
                            x.ApplicantId == userId,
                        cancellationToken);

            if (onboarding == null)
            {
                return NotFound();
            }

            return View(onboarding);
        }

        // ============================================================
        // GENERIC DOCUMENT UPLOAD
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int onboardingId,
            VolunteerOnboardingDocumentType documentType,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (onboardingId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid onboarding request.";

                return RedirectToAction(
                    nameof(Documents));
            }

            if (file == null ||
                file.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a file to upload.";

                return RedirectToAction(
                    nameof(Documents));
            }

            try
            {
                // ----------------------------------------------------
                // VERIFY APPLICANT OWNS ONBOARDING
                // ----------------------------------------------------

                var onboarding =
                    await _context.VolunteerOnboardings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                onboardingId &&
                                x.ApplicantId == userId,
                            cancellationToken);

                if (onboarding == null)
                {
                    return NotFound();
                }

                // ----------------------------------------------------
                // UPLOAD PHYSICAL FILE
                // ----------------------------------------------------

                var uploadedPath =
                    await _fileUploadService
                        .UploadFileAsync(file);

                if (string.IsNullOrWhiteSpace(
                        uploadedPath))
                {
                    TempData["ErrorMessage"] =
                        "The document could not be uploaded.";

                    return RedirectToAction(
                        nameof(Documents));
                }

                // ----------------------------------------------------
                // ADD THROUGH ONBOARDING SERVICE
                // ----------------------------------------------------

                await _onboardingService
                    .AddDocumentAsync(
                        onboardingId,
                        userId,
                        documentType,
                        Path.GetFileName(
                            file.FileName),
                        uploadedPath,
                        cancellationToken);

                TempData["SuccessMessage"] =
                    "Document uploaded successfully.";

                _logger.LogInformation(
                    "Applicant uploaded onboarding document. " +
                    "OnboardingId={OnboardingId}, " +
                    "DocumentType={DocumentType}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    documentType,
                    userId);

                return RedirectToAction(
                    nameof(Documents));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid onboarding document upload. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(Documents));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Onboarding document upload rejected. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(Documents));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error uploading onboarding document. " +
                    "OnboardingId={OnboardingId}, " +
                    "ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["ErrorMessage"] =
                    "We could not upload the document. Please try again.";

                return RedirectToAction(
                    nameof(Documents));
            }
        }

        // ============================================================
        // SUBMIT DOCUMENTS FOR VERIFICATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForVerification(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (onboardingId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid onboarding request.";

                return RedirectToAction(
                    nameof(Documents));
            }

            try
            {
                var onboarding =
                    await _context.VolunteerOnboardings
                        .Include(x => x.Documents)
                        .Include(x => x.Requirements)
                        .Include(x => x.PolicyAcceptances)
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                onboardingId &&
                                x.ApplicantId == userId,
                            cancellationToken);

                if (onboarding == null)
                {
                    return NotFound();
                }

                // ----------------------------------------------------
                // REQUIRED POLICIES
                // ----------------------------------------------------

                var requiredPolicies =
                    await _context.VolunteerPolicies
                        .AsNoTracking()
                        .Where(x =>
                            x.IsActive &&
                            x.IsRequired)
                        .Select(x =>
                            x.VolunteerPolicyId)
                        .ToListAsync(
                            cancellationToken);

                var acceptedPolicyIds =
                    onboarding.PolicyAcceptances
                        .Select(x =>
                            x.VolunteerPolicyId)
                        .Distinct()
                        .ToHashSet();

                var policiesComplete =
                    requiredPolicies.All(
                        id =>
                            acceptedPolicyIds.Contains(id));

                if (!policiesComplete)
                {
                    TempData["ErrorMessage"] =
                        "Please accept all required policies before submitting your documents.";

                    return RedirectToAction(
                        nameof(Documents));
                }

                // ----------------------------------------------------
                // LATEST DOCUMENT PER DOCUMENT TYPE
                // ----------------------------------------------------

                var latestDocuments =
                    onboarding.Documents
                        .GroupBy(x =>
                            x.DocumentType)
                        .Select(group =>
                            group
                                .OrderByDescending(
                                    x =>
                                        x.UploadedDate)
                                .ThenByDescending(
                                    x =>
                                        x.ModifiedDate)
                                .ThenByDescending(
                                    x =>
                                        x.VolunteerOnboardingDocumentId)
                                .First())
                        .ToList();

                // ----------------------------------------------------
                // REJECTED / RESUBMISSION DOCUMENTS
                // ----------------------------------------------------

                var rejectedDocuments =
                    latestDocuments.Any(
                        x =>
                            x.Status ==
                                VolunteerOnboardingDocumentStatus.Rejected ||
                            x.Status ==
                                VolunteerOnboardingDocumentStatus.ResubmissionRequired);

                if (rejectedDocuments)
                {
                    TempData["ErrorMessage"] =
                        "Please correct all rejected documents before submitting for verification.";

                    return RedirectToAction(
                        nameof(Documents));
                }

                // ----------------------------------------------------
                // PENDING DOCUMENTS
                // ----------------------------------------------------

                var pendingDocuments =
                    latestDocuments.Any(
                        x =>
                            x.Status ==
                                VolunteerOnboardingDocumentStatus.PendingUpload ||
                            x.Status ==
                                VolunteerOnboardingDocumentStatus.Uploaded ||
                            x.Status ==
                                VolunteerOnboardingDocumentStatus.UnderReview);

                if (pendingDocuments)
                {
                    TempData["ErrorMessage"] =
                        "Please ensure all required documents have been reviewed before continuing.";

                    return RedirectToAction(
                        nameof(Documents));
                }

                // ----------------------------------------------------
                // ALL LATEST DOCUMENTS MUST BE APPROVED
                // ----------------------------------------------------

                var hasUnapprovedDocuments =
                    latestDocuments.Any(
                        x =>
                            x.Status !=
                            VolunteerOnboardingDocumentStatus.Approved);

                if (hasUnapprovedDocuments)
                {
                    TempData["ErrorMessage"] =
                        "All current documents must be approved before continuing.";

                    return RedirectToAction(
                        nameof(Documents));
                }

                // ----------------------------------------------------
                // MOVE TO ADMIN VERIFICATION
                // ----------------------------------------------------

                var now =
                    DateTime.Now;

                onboarding.DocumentsSubmittedDate =
                    now;

                onboarding.VerificationStartedDate ??=
                    now;

                onboarding.Status =
                    VolunteerOnboardingStatus.AdminVerification;

                onboarding.CurrentStep =
                    5;

                onboarding.ModifiedDate =
                    now;

                onboarding.ModifiedBy =
                    userId;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Applicant submitted onboarding for administrative verification. " +
                    "OnboardingId={OnboardingId}, ApplicantId={ApplicantId}",
                    onboardingId,
                    userId);

                TempData["SuccessMessage"] =
                    "Your documents have been submitted for administrative verification.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error submitting onboarding {OnboardingId} for verification.",
                    onboardingId);

                TempData["ErrorMessage"] =
                    "We could not submit your onboarding for verification.";

                return RedirectToAction(
                    nameof(Documents));
            }
        }

        // ============================================================
        // SAFE WEB ROOT FILE PATH
        // ============================================================

        private string? GetSafeWebRootFilePath(
            string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var webRootPath =
                _environment.WebRootPath ??
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot");

            var normalizedRelativePath =
                relativePath
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar)
                    .Replace(
                        '\\',
                        Path.DirectorySeparatorChar)
                    .TrimStart(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

            var fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        webRootPath,
                        normalizedRelativePath));

            var normalizedWebRoot =
                Path.GetFullPath(
                    webRootPath)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                    normalizedWebRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Blocked unsafe file path: {RelativePath}",
                    relativePath);

                return null;
            }

            return fullPath;
        }

        // ============================================================
        // HASH OFFER TOKEN
        // ============================================================

        private static string HashToken(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException(
                    "Token cannot be null or empty.",
                    nameof(token));
            }

            using var sha256 =
                System.Security.Cryptography.SHA256.Create();

            var bytes =
                System.Text.Encoding.UTF8.GetBytes(
                    token);

            var hash =
                sha256.ComputeHash(bytes);

            return Convert.ToHexString(hash);
        }
    }
}