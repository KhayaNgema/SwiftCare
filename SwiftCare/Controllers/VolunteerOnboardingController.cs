using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerOnboarding;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        // DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var onboarding =
                await _onboardingService.GetOnboardingByApplicantAsync(
                    user.Id,
                    cancellationToken);

            if (onboarding == null)
            {
                TempData["Error"] =
                    "No volunteer onboarding process was found for your account.";

                return View("NoOnboarding");
            }

            return View(onboarding);
        }

        // ============================================================
        // OFFER
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Offer(
            int offerId,
            string? token,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SWIFTCARE OFFER] ==================================================");

            _logger.LogInformation(
                "[SWIFTCARE OFFER] Offer endpoint reached. OfferId={OfferId}, TokenProvided={TokenProvided}",
                offerId,
                !string.IsNullOrWhiteSpace(token));

            _logger.LogInformation(
                "[SWIFTCARE OFFER] Request Path={Path}",
                HttpContext.Request.Path);

            _logger.LogInformation(
                "[SWIFTCARE OFFER] QueryString={QueryString}",
                HttpContext.Request.QueryString);

            // ========================================================
            // BASIC REQUEST VALIDATION
            // ========================================================

            if (offerId <= 0)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Invalid OfferId: {OfferId}",
                    offerId);

                TempData["Error"] =
                    "Invalid volunteer offer.";

                return View("OfferError");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] No acceptance token supplied for OfferId={OfferId}.",
                    offerId);

                TempData["Error"] =
                    "This volunteer offer link is invalid because the acceptance token is missing.";

                return View("OfferError");
            }

            // ========================================================
            // LOAD OFFER
            // ========================================================

            var offer = await _context.VolunteerOffers
                .Include(x => x.VolunteerOpportunity)
                .Include(x => x.VolunteerOnboarding)
                    .ThenInclude(x => x.Applicant)
                .FirstOrDefaultAsync(
                    x => x.VolunteerOfferId == offerId,
                    cancellationToken);

            if (offer == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Offer {OfferId} was NOT FOUND in database.",
                    offerId);

                TempData["Error"] =
                    $"Volunteer offer {offerId} could not be found.";

                return View("OfferError");
            }

            _logger.LogInformation(
                "[SWIFTCARE OFFER] Offer found. OfferId={OfferId}, Status={Status}, ExpiryDate={ExpiryDate}, TokenExpiryDate={TokenExpiryDate}, HashExists={HashExists}",
                offer.VolunteerOfferId,
                offer.Status,
                offer.ExpiryDate,
                offer.TokenExpiryDate,
                !string.IsNullOrWhiteSpace(
                    offer.AcceptanceTokenHash));

            // ========================================================
            // KEEP TOKEN FOR VIEW
            // ========================================================

            ViewBag.Token = token;

            // ========================================================
            // CHECK OFFER STATUS
            // ========================================================

            if (offer.Status != VolunteerOfferStatus.Pending)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Offer {OfferId} is not pending. Current status={Status}.",
                    offerId,
                    offer.Status);

                TempData["Error"] =
                    "This volunteer offer is no longer available.";

                return View(offer);
            }

            // ========================================================
            // CHECK OFFER EXPIRY
            // ========================================================

            var now = DateTime.Now;

            if (offer.ExpiryDate <= now)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Offer {OfferId} expired. ExpiryDate={ExpiryDate}, Now={Now}.",
                    offerId,
                    offer.ExpiryDate,
                    now);

                TempData["Error"] =
                    "This volunteer offer has expired.";

                return View(offer);
            }

            // ========================================================
            // CHECK TOKEN EXPIRY
            // ========================================================

            if (offer.TokenExpiryDate <= now)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Token for OfferId={OfferId} expired. TokenExpiryDate={TokenExpiryDate}, Now={Now}.",
                    offerId,
                    offer.TokenExpiryDate,
                    now);

                TempData["Error"] =
                    "This volunteer offer link has expired.";

                return View(offer);
            }

            // ========================================================
            // CHECK STORED TOKEN HASH
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    offer.AcceptanceTokenHash))
            {
                _logger.LogError(
                    "[SWIFTCARE OFFER] Offer {OfferId} has NO AcceptanceTokenHash.",
                    offerId);

                TempData["Error"] =
                    "This volunteer offer does not have a valid acceptance token.";

                return View("OfferError");
            }

            // ========================================================
            // VALIDATE TOKEN
            // ========================================================

            var tokenValid =
                ValidateOfferToken(
                    token,
                    offer.AcceptanceTokenHash);

            if (!tokenValid)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] INVALID token for OfferId={OfferId}.",
                    offerId);

                TempData["Error"] =
                    "This volunteer offer link is invalid or has already expired.";

                return View("OfferError");
            }

            // ========================================================
            // TOKEN VALID
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE OFFER] VALID token for OfferId={OfferId}. ApplicantId={ApplicantId}.",
                offerId,
                offer.ApplicantId);

            _logger.LogInformation(
                "[SWIFTCARE OFFER] Rendering Offer.cshtml for OfferId={OfferId}.",
                offerId);

            // IMPORTANT:
            // Keep the RAW token available for the
            // Accept / Decline forms.
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
            int id,
            string? token,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SWIFTCARE OFFER] AcceptOffer called. OfferId={OfferId}, TokenProvided={TokenProvided}",
                id,
                !string.IsNullOrWhiteSpace(token));

            // ========================================================
            // BASIC VALIDATION
            // ========================================================

            if (id <= 0)
            {
                TempData["Error"] =
                    "Invalid volunteer offer.";

                return View("OfferError");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] AcceptOffer called without token. OfferId={OfferId}",
                    id);

                TempData["Error"] =
                    "The volunteer offer token is missing.";

                return View("OfferError");
            }

            // ========================================================
            // LOAD OFFER
            // ========================================================

            var offer = await _context.VolunteerOffers
                .Include(x => x.VolunteerOnboarding)
                .FirstOrDefaultAsync(
                    x => x.VolunteerOfferId == id,
                    cancellationToken);

            if (offer == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] AcceptOffer offer not found. OfferId={OfferId}",
                    id);

                TempData["Error"] =
                    "The volunteer offer could not be found.";

                return View("OfferError");
            }

            // ========================================================
            // TOKEN VALIDATION
            // ========================================================

            if (!ValidateOfferToken(
                    token,
                    offer.AcceptanceTokenHash))
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] INVALID acceptance token. OfferId={OfferId}",
                    id);

                TempData["Error"] =
                    "This volunteer offer link is invalid or has expired.";

                return View("OfferError");
            }

            // ========================================================
            // STATUS VALIDATION
            // ========================================================

            if (offer.Status != VolunteerOfferStatus.Pending)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Cannot accept offer. OfferId={OfferId}, Status={Status}",
                    id,
                    offer.Status);

                TempData["Error"] =
                    "This volunteer offer is no longer available.";

                return View("OfferError");
            }

            // ========================================================
            // EXPIRY VALIDATION
            // ========================================================

            var now = DateTime.Now;

            if (offer.ExpiryDate <= now ||
                offer.TokenExpiryDate <= now)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Cannot accept expired offer. OfferId={OfferId}",
                    id);

                if (offer.Status ==
                    VolunteerOfferStatus.Pending)
                {
                    offer.Status =
                        VolunteerOfferStatus.Expired;

                    offer.ModifiedDate =
                        DateTime.Now;

                    await _context.SaveChangesAsync(
                        cancellationToken);
                }

                TempData["Error"] =
                    "This volunteer offer has expired.";

                return View("OfferError");
            }

            // ========================================================
            // ACCEPT OFFER THROUGH SERVICE
            // ========================================================

            var result =
                await _onboardingService.AcceptOfferAsync(
                    id,
                    cancellationToken);

            if (!result)
            {
                TempData["Error"] =
                    "The offer could not be accepted. It may have expired or already been processed.";

                return View("OfferError");
            }

            _logger.LogInformation(
                "[SWIFTCARE OFFER] Offer accepted successfully. OfferId={OfferId}, OnboardingId={OnboardingId}",
                id,
                offer.VolunteerOnboardingId);

            TempData["Success"] =
                "Your volunteer offer has been accepted.";

            // ========================================================
            // REDIRECT TO POLICIES
            // ========================================================

            return RedirectToAction(
                nameof(Policies),
                new
                {
                    id = offer.VolunteerOnboardingId
                });
        }

        // ============================================================
        // DECLINE OFFER
        // ============================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineOffer(
            int id,
            string? token,
            string declineReason,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SWIFTCARE OFFER] DeclineOffer called. OfferId={OfferId}, TokenProvided={TokenProvided}",
                id,
                !string.IsNullOrWhiteSpace(token));

            // ========================================================
            // BASIC VALIDATION
            // ========================================================

            if (id <= 0)
            {
                TempData["Error"] =
                    "Invalid volunteer offer.";

                return View("OfferError");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] DeclineOffer called without token. OfferId={OfferId}",
                    id);

                TempData["Error"] =
                    "The volunteer offer token is missing.";

                return View("OfferError");
            }

            if (string.IsNullOrWhiteSpace(declineReason))
            {
                TempData["Error"] =
                    "Please provide a reason for declining the offer.";

                return RedirectToAction(
                    nameof(Offer),
                    new
                    {
                        offerId = id,
                        token = token
                    });
            }

            declineReason =
                declineReason.Trim();

            if (declineReason.Length > 1000)
            {
                TempData["Error"] =
                    "The decline reason cannot exceed 1000 characters.";

                return RedirectToAction(
                    nameof(Offer),
                    new
                    {
                        offerId = id,
                        token = token
                    });
            }

            // ========================================================
            // LOAD OFFER
            // ========================================================

            var offer = await _context.VolunteerOffers
                .Include(x => x.VolunteerOnboarding)
                .FirstOrDefaultAsync(
                    x => x.VolunteerOfferId == id,
                    cancellationToken);

            if (offer == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] DeclineOffer offer not found. OfferId={OfferId}",
                    id);

                TempData["Error"] =
                    "The volunteer offer could not be found.";

                return View("OfferError");
            }

            // ========================================================
            // TOKEN VALIDATION
            // ========================================================

            if (!ValidateOfferToken(
                    token,
                    offer.AcceptanceTokenHash))
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] INVALID decline token. OfferId={OfferId}",
                    id);

                TempData["Error"] =
                    "This volunteer offer link is invalid or has expired.";

                return View("OfferError");
            }

            // ========================================================
            // STATUS VALIDATION
            // ========================================================

            if (offer.Status != VolunteerOfferStatus.Pending)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Cannot decline offer. OfferId={OfferId}, Status={Status}",
                    id,
                    offer.Status);

                TempData["Error"] =
                    "This volunteer offer is no longer available.";

                return View("OfferError");
            }

            // ========================================================
            // EXPIRY VALIDATION
            // ========================================================

            var now = DateTime.Now;

            if (offer.ExpiryDate <= now ||
                offer.TokenExpiryDate <= now)
            {
                _logger.LogWarning(
                    "[SWIFTCARE OFFER] Cannot decline expired offer. OfferId={OfferId}",
                    id);

                if (offer.Status ==
                    VolunteerOfferStatus.Pending)
                {
                    offer.Status =
                        VolunteerOfferStatus.Expired;

                    offer.ModifiedDate =
                        DateTime.Now;

                    await _context.SaveChangesAsync(
                        cancellationToken);
                }

                TempData["Error"] =
                    "This volunteer offer has expired.";

                return View("OfferError");
            }

            // ========================================================
            // DECLINE OFFER THROUGH SERVICE
            // ========================================================

            var result =
                await _onboardingService.DeclineOfferAsync(
                    id,
                    declineReason,
                    cancellationToken);

            if (!result)
            {
                TempData["Error"] =
                    "The offer could not be declined.";

                return View("OfferError");
            }

            _logger.LogInformation(
                "[SWIFTCARE OFFER] Offer declined successfully. OfferId={OfferId}, OnboardingId={OnboardingId}",
                id,
                offer.VolunteerOnboardingId);

            TempData["Success"] =
                "Your offer has been declined.";

            return RedirectToAction(
                nameof(Index));
        }

        // ============================================================
        // POLICIES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Policies(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.PolicyAcceptances)
                    .Include(x => x.Applicant)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOnboardingId == id,
                        cancellationToken);

            if (onboarding == null)
                return NotFound();

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            var policies = await _context.VolunteerPolicies
                .Where(x => x.IsActive)
                .OrderBy(x => x.Title)
                .ToListAsync(cancellationToken);

            ViewBag.Policies = policies;

            return View(onboarding);
        }

        // ============================================================
        // ACCEPT POLICY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPolicy(
            int onboardingId,
            int policyId,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var onboarding =
                await _onboardingService.GetOnboardingAsync(
                    onboardingId,
                    cancellationToken);

            if (onboarding == null)
                return NotFound();

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            // ========================================================
            // ACCEPT THE POLICY
            // ========================================================

            var result =
                await _onboardingService.AcceptPolicyAsync(
                    onboardingId,
                    policyId,
                    user.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    cancellationToken);

            if (!result)
            {
                TempData["Error"] =
                    "The policy could not be accepted.";

                return RedirectToAction(
                    nameof(Policies),
                    new
                    {
                        id = onboardingId
                    });
            }

            // ========================================================
            // CHECK WHETHER ALL REQUIRED POLICIES ARE ACCEPTED
            // ========================================================

            var allAccepted =
                await _onboardingService
                    .HasAcceptedAllRequiredPoliciesAsync(
                        onboardingId,
                        cancellationToken);

            // ========================================================
            // NOT ALL POLICIES ACCEPTED YET
            // ========================================================

            TempData["Success"] =
                "Policy acceptance recorded.";

            return RedirectToAction(
                nameof(Policies),
                new
                {
                    id = onboardingId
                });
        }


        // ============================================================
        // GENERATE CONTRACT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateContract(
            int onboardingId,
            CancellationToken cancellationToken)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // ========================================================
            // LOAD ONBOARDING
            // ========================================================

            var onboarding =
                await _onboardingService.GetOnboardingAsync(
                    onboardingId,
                    cancellationToken);

            if (onboarding == null)
                return NotFound();

            // ========================================================
            // SECURITY CHECK
            // ========================================================

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            // ========================================================
            // VERIFY ALL REQUIRED POLICIES
            // ========================================================

            var allPoliciesAccepted =
                await _onboardingService
                    .HasAcceptedAllRequiredPoliciesAsync(
                        onboardingId,
                        cancellationToken);

            if (!allPoliciesAccepted)
            {
                TempData["Error"] =
                    "Please accept all required policies before continuing to your contract.";

                return RedirectToAction(
                    nameof(Policies),
                    new
                    {
                        id = onboardingId
                    });
            }

            try
            {
                // ====================================================
                // CREATE CONTRACT + GENERATE PDF
                // ====================================================

                var contract =
                    await _contractService
                        .CreateAndGenerateContractAsync(
                            onboardingId,
                            cancellationToken);

                _logger.LogInformation(
                    "[SWIFTCARE CONTRACT] Contract {ContractReference} generated successfully for onboarding {OnboardingId}.",
                    contract.ContractReference,
                    onboardingId);

                // ====================================================
                // SUCCESS
                // ====================================================

                TempData["Success"] =
                    "Your volunteer contract has been generated successfully.";

                return RedirectToAction(
                    nameof(Contract),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[SWIFTCARE CONTRACT] Contract generation could not be completed for onboarding {OnboardingId}.",
                    onboardingId);

                TempData["Error"] =
                    ex.Message;

                return RedirectToAction(
                    nameof(Policies),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE CONTRACT] Unexpected error generating contract for onboarding {OnboardingId}.",
                    onboardingId);

                TempData["Error"] =
                    "We could not generate your volunteer contract right now. Please try again.";

                return RedirectToAction(
                    nameof(Policies),
                    new
                    {
                        id = onboardingId
                    });
            }
        }

        // ============================================================
        // CONTRACT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Contract(
            int id,
            CancellationToken cancellationToken)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // ========================================================
            // LOAD ONBOARDING
            // ========================================================

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Documents)
                    .Include(x => x.Applicant)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOnboardingId == id,
                        cancellationToken);

            if (onboarding == null)
                return NotFound();

            // ========================================================
            // SECURITY CHECK
            // ========================================================

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            // ========================================================
            // LOAD GENERATED CONTRACT
            // ========================================================

            var contract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId == id &&
                            x.Status != VolunteerContractStatus.Cancelled,
                        cancellationToken);

            if (contract == null)
            {
                TempData["Error"] =
                    "Your volunteer contract has not been generated yet.";

                return RedirectToAction(
                    nameof(Policies),
                    new
                    {
                        id
                    });
            }

            // ========================================================
            // VERIFY GENERATED PDF
            // ========================================================

            if (string.IsNullOrWhiteSpace(contract.GeneratedFilePath))
            {
                TempData["Error"] =
                    "Your contract was created, but the generated PDF could not be located.";

                return RedirectToAction(
                    nameof(Policies),
                    new
                    {
                        id
                    });
            }

            // ========================================================
            // LOG CONTRACT INFORMATION
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Contract page loaded. " +
                "ContractReference={ContractReference}, " +
                "OnboardingId={OnboardingId}, " +
                "GeneratedFilePath={GeneratedFilePath}, " +
                "GeneratedFileName={GeneratedFileName}, " +
                "Status={Status}",
                contract.ContractReference,
                id,
                contract.GeneratedFilePath,
                contract.GeneratedFileName,
                contract.Status);

            // ========================================================
            // PASS CONTRACT TO VIEW
            // ========================================================

            ViewBag.Contract = contract;

            return View(onboarding);
        }


        // ============================================================
        // DOWNLOAD GENERATED CONTRACT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DownloadContract(
            int id,
            CancellationToken cancellationToken)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // ========================================================
            // LOAD ONBOARDING
            // ========================================================

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Applicant)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOnboardingId == id,
                        cancellationToken);

            if (onboarding == null)
                return NotFound();

            // ========================================================
            // SECURITY CHECK
            // ========================================================

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            // ========================================================
            // LOAD CONTRACT
            // ========================================================

            var contract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId == id &&
                            x.Status != VolunteerContractStatus.Cancelled,
                        cancellationToken);

            if (contract == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Download requested but no active contract exists. " +
                    "OnboardingId={OnboardingId}, UserId={UserId}",
                    id,
                    user.Id);

                TempData["Error"] =
                    "Your volunteer contract has not been generated yet.";

                return RedirectToAction(
                    nameof(Contract),
                    new
                    {
                        id
                    });
            }

            // ========================================================
            // VALIDATE GENERATED FILE PATH
            // ========================================================

            if (string.IsNullOrWhiteSpace(contract.GeneratedFilePath))
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Contract has no GeneratedFilePath. " +
                    "ContractId={ContractId}, ContractReference={ContractReference}",
                    contract.VolunteerContractId,
                    contract.ContractReference);

                TempData["Error"] =
                    "The generated contract PDF could not be found.";

                return RedirectToAction(
                    nameof(Contract),
                    new
                    {
                        id
                    });
            }

            // ========================================================
            // NORMALISE RELATIVE PATH
            // ========================================================

            var relativePath =
                contract.GeneratedFilePath
                    .Trim()
                    .Replace('\\', '/')
                    .TrimStart('/');

            // ========================================================
            // GET WEB ROOT
            // ========================================================

            if (string.IsNullOrWhiteSpace(_environment.WebRootPath))
            {
                _logger.LogError(
                    "[SWIFTCARE CONTRACT] WebRootPath is not configured.");

                TempData["Error"] =
                    "The contract file storage location is not available.";

                return RedirectToAction(
                    nameof(Contract),
                    new
                    {
                        id
                    });
            }

            // ========================================================
            // BUILD PHYSICAL PATH
            // ========================================================

            var filePath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));

            // ========================================================
            // LOG FILE LOCATION
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Download requested. " +
                "ContractReference={ContractReference}, " +
                "RelativePath={RelativePath}, " +
                "PhysicalPath={PhysicalPath}",
                contract.ContractReference,
                relativePath,
                filePath);

            // ========================================================
            // VERIFY FILE EXISTS
            // ========================================================

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError(
                    "[SWIFTCARE CONTRACT] Generated contract file does not exist. " +
                    "ContractReference={ContractReference}, " +
                    "PhysicalPath={PhysicalPath}",
                    contract.ContractReference,
                    filePath);

                TempData["Error"] =
                    "The generated contract file could not be found on the server.";

                return RedirectToAction(
                    nameof(Contract),
                    new
                    {
                        id
                    });
            }

            // ========================================================
            // FILE NAME
            // ========================================================

            var fileName =
                !string.IsNullOrWhiteSpace(contract.GeneratedFileName)
                    ? contract.GeneratedFileName
                    : $"{contract.ContractReference}.pdf";

            // ========================================================
            // CONTENT TYPE
            // ========================================================

            var extension =
                Path.GetExtension(filePath);

            var contentType =
                string.Equals(
                    extension,
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase)
                        ? "application/pdf"
                        : "application/octet-stream";

            // ========================================================
            // RETURN GENERATED PDF
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Returning generated contract PDF. " +
                "ContractReference={ContractReference}, " +
                "FileName={FileName}",
                contract.ContractReference,
                fileName);

            return PhysicalFile(
                filePath,
                contentType,
                fileName);
        }

        // ============================================================
        // DOCUMENTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Documents(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Documents)
                    .Include(x => x.Requirements)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOnboardingId == id,
                        cancellationToken);

            if (onboarding == null)
                return NotFound();

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            return View(onboarding);
        }

        // ============================================================
        // UPLOAD DOCUMENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int onboardingId,
            VolunteerOnboardingDocumentType documentType,
            string documentName,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var onboarding =
                await _onboardingService.GetOnboardingAsync(
                    onboardingId,
                    cancellationToken);

            if (onboarding == null)
                return NotFound();

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] =
                    "Please select a document.";

                return RedirectToAction(
                    nameof(Documents),
                    new
                    {
                        id = onboardingId
                    });
            }

            // Maximum 50 MB
            const long maxFileSize =
                50 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                TempData["Error"] =
                    "The document cannot exceed 50 MB.";

                return RedirectToAction(
                    nameof(Documents),
                    new
                    {
                        id = onboardingId
                    });
            }

            if (string.IsNullOrWhiteSpace(documentName))
            {
                documentName =
                    Path.GetFileNameWithoutExtension(
                        file.FileName);
            }

            try
            {
                // ====================================================
                // UPLOAD USING EXISTING FILE SERVICE
                // ====================================================

                var filePath =
                    await _fileUploadService.UploadFileAsync(
                        file);

                // ====================================================
                // SAVE DOCUMENT RECORD
                // ====================================================

                var document =
                    await _onboardingService.AddDocumentAsync(
                        onboardingId,
                        user.Id,
                        documentType,
                        documentName.Trim(),
                        Path.GetFileName(file.FileName),
                        filePath,
                        string.IsNullOrWhiteSpace(
                            file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType,
                        file.Length,
                        null,
                        cancellationToken);

                TempData["Success"] =
                    $"{document.DocumentName} was uploaded successfully.";

                return RedirectToAction(
                    nameof(Documents),
                    new
                    {
                        id = onboardingId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to upload onboarding document. " +
                    "OnboardingId: {OnboardingId}, ApplicantId: {ApplicantId}, DocumentType: {DocumentType}",
                    onboardingId,
                    user.Id,
                    documentType);

                TempData["Error"] =
                    "The document could not be uploaded. Please try again.";

                return RedirectToAction(
                    nameof(Documents),
                    new
                    {
                        id = onboardingId
                    });
            }
        }

        // ============================================================
        // COMPLETE / SUBMIT ONBOARDING FOR VERIFICATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForVerification(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var onboarding =
                await _onboardingService.GetOnboardingAsync(
                    id,
                    cancellationToken);

            if (onboarding == null)
                return NotFound();

            if (onboarding.ApplicantId != user.Id)
                return Forbid();

            var ready =
                await _onboardingService.IsReadyForActivationAsync(
                    id,
                    cancellationToken);

            if (!ready)
            {
                TempData["Error"] =
                    "Your onboarding information is not complete yet.";

                return RedirectToAction(
                    nameof(Documents),
                    new
                    {
                        id
                    });
            }

            TempData["Success"] =
                "Your onboarding information has been submitted for verification.";

            return RedirectToAction(
                nameof(Index));
        }

        // ============================================================
        // VALIDATE OFFER TOKEN
        // ============================================================

        private static bool ValidateOfferToken(
            string? rawToken,
            string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return false;

            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            try
            {
                // ====================================================
                // HASH THE RAW TOKEN RECEIVED FROM THE URL/FORM
                // ====================================================

                var suppliedHash =
                    HashToken(rawToken);

                // ====================================================
                // NORMALIZE HASHES
                // ====================================================

                var suppliedHashText =
                    suppliedHash.Trim();

                var storedHashText =
                    storedHash.Trim();

                // SHA-256 produces 32 bytes = 64 hex characters.
                if (storedHashText.Length != 64 ||
                    suppliedHashText.Length != 64)
                {
                    return false;
                }

                // ====================================================
                // CONVERT HEX TO BYTES
                // ====================================================

                var suppliedHashBytes =
                    Convert.FromHexString(
                        suppliedHashText);

                var storedHashBytes =
                    Convert.FromHexString(
                        storedHashText);

                // ====================================================
                // CONSTANT-TIME COMPARISON
                // ====================================================

                return CryptographicOperations.FixedTimeEquals(
                    suppliedHashBytes,
                    storedHashBytes);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        // ============================================================
        // HASH TOKEN
        // ============================================================

        private static string HashToken(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            var bytes =
                Encoding.UTF8.GetBytes(token);

            var hash =
                SHA256.HashData(bytes);

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }
    }
}