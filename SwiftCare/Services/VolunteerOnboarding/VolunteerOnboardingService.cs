using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services.Volunteers;
using System.Security.Cryptography;
using System.Text;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public class VolunteerOnboardingService : IVolunteerOnboardingService
    {
        private readonly SwiftCareDbContext _context;
        private readonly IVolunteerAccountService _volunteerAccountService;

        public VolunteerOnboardingService(
            SwiftCareDbContext context,
            IVolunteerAccountService volunteerAccountService)
        {
            _context = context;
            _volunteerAccountService = volunteerAccountService;
        }

        // ============================================================
        // CREATE ONBOARDING
        // ============================================================

        public async Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding> CreateOnboardingAsync(
            int volunteerApplicationId,
            string? adminId = null,
            CancellationToken cancellationToken = default)
        {
            var application =
                await _context.VolunteerApplications
                    .Include(x => x.VolunteerOpportunity)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerApplicationId == volunteerApplicationId,
                        cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException(
                    "The volunteer application could not be found.");
            }

            var existing =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x => x.VolunteerApplicationId == volunteerApplicationId,
                        cancellationToken);

            // --------------------------------------------------------
            // Repair existing onboarding records.
            // --------------------------------------------------------

            if (existing != null)
            {
                Console.WriteLine(
                    $"[CreateOnboarding] Existing onboarding found. " +
                    $"OnboardingId={existing.VolunteerOnboardingId}");

                var nowExisting = DateTime.Now;

                await EnsureDefaultRequirementsAsync(
                    existing.VolunteerOnboardingId,
                    nowExisting,
                    cancellationToken);

                await SyncAutomaticRequirementsAsync(
                    existing.VolunteerOnboardingId,
                    null,
                    cancellationToken);

                await _context.SaveChangesAsync(
                    cancellationToken);

                return await GetOnboardingAsync(
                           existing.VolunteerOnboardingId,
                           cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(application.ApplicantId))
            {
                throw new InvalidOperationException(
                    "The volunteer application does not have an applicant.");
            }

            var now = DateTime.Now;

            var onboarding = new SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding
            {
                VolunteerApplicationId =
                    application.VolunteerApplicationId,

                ApplicantId =
                    application.ApplicantId,

                Status =
                    VolunteerOnboardingStatus.OfferPending,

                CurrentStep = 1,

                CreatedDate = now,
                ModifiedDate = now,
                CreatedBy = adminId
            };

            _context.VolunteerOnboardings.Add(onboarding);

            await _context.SaveChangesAsync(
                cancellationToken);

            await EnsureDefaultRequirementsAsync(
                onboarding.VolunteerOnboardingId,
                now,
                cancellationToken);

            await SyncAutomaticRequirementsAsync(
                onboarding.VolunteerOnboardingId,
                null,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            return await GetOnboardingAsync(
                       onboarding.VolunteerOnboardingId,
                       cancellationToken);
        }

        // ============================================================
        // GET ONBOARDING
        // ============================================================

        public async Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerOnboardings
                .Include(x => x.VolunteerApplication)
                    .ThenInclude(x => x.VolunteerOpportunity)
                .Include(x => x.Offers)
                .Include(x => x.PolicyAcceptances)
                    .ThenInclude(x => x.VolunteerPolicy)
                .Include(x => x.Documents)
                .Include(x => x.Requirements)
                .Include(x => x.Volunteer)
                .FirstOrDefaultAsync(
                    x => x.VolunteerOnboardingId == volunteerOnboardingId,
                    cancellationToken);
        }

        // ============================================================
        // GET BY APPLICATION
        // ============================================================

        public async Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingByApplicationAsync(
            int volunteerApplicationId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerOnboardings
                .Include(x => x.VolunteerApplication)
                    .ThenInclude(x => x.VolunteerOpportunity)
                .Include(x => x.Offers)
                .Include(x => x.PolicyAcceptances)
                    .ThenInclude(x => x.VolunteerPolicy)
                .Include(x => x.Documents)
                .Include(x => x.Requirements)
                .Include(x => x.Volunteer)
                .FirstOrDefaultAsync(
                    x => x.VolunteerApplicationId == volunteerApplicationId,
                    cancellationToken);
        }

        // ============================================================
        // GET BY APPLICANT
        // ============================================================

        public async Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingByApplicantAsync(
            string applicantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(applicantId))
            {
                return null;
            }

            return await _context.VolunteerOnboardings
                .Include(x => x.VolunteerApplication)
                    .ThenInclude(x => x.VolunteerOpportunity)
                .Include(x => x.Offers)
                .Include(x => x.PolicyAcceptances)
                    .ThenInclude(x => x.VolunteerPolicy)
                .Include(x => x.Documents)
                .Include(x => x.Requirements)
                .Include(x => x.Volunteer)
                .FirstOrDefaultAsync(
                    x => x.ApplicantId == applicantId,
                    cancellationToken);
        }

        // ============================================================
        // CREATE OFFER
        // ============================================================

        public async Task<VolunteerOfferCreationResult> CreateOfferAsync(
            int volunteerOnboardingId,
            DateTime expiryDate,
            string? adminId = null,
            CancellationToken cancellationToken = default)
        {
            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.VolunteerOpportunity)
                    .Include(x => x.Requirements)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOnboardingId == volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                throw new InvalidOperationException(
                    "The volunteer onboarding record could not be found.");
            }

            if (onboarding.Status != VolunteerOnboardingStatus.OfferPending)
            {
                throw new InvalidOperationException(
                    "An offer cannot be created at the current onboarding stage.");
            }

            if (expiryDate <= DateTime.Now)
            {
                throw new InvalidOperationException(
                    "The offer expiry date must be in the future.");
            }

            var opportunity =
                onboarding.VolunteerApplication?.VolunteerOpportunity;

            if (opportunity == null)
            {
                throw new InvalidOperationException(
                    "The volunteer opportunity could not be found.");
            }

            await EnsureDefaultRequirementsAsync(
                volunteerOnboardingId,
                DateTime.Now,
                cancellationToken);

            var previousOffers =
                await _context.VolunteerOffers
                    .Where(x =>
                        x.VolunteerOnboardingId == volunteerOnboardingId &&
                        x.Status == VolunteerOfferStatus.Pending)
                    .ToListAsync(cancellationToken);

            var now = DateTime.Now;

            foreach (var previousOffer in previousOffers)
            {
                previousOffer.Status =
                    VolunteerOfferStatus.Cancelled;

                previousOffer.ModifiedDate =
                    now;

                previousOffer.CreatedByAdminId =
                    adminId;
            }

            var offerAcceptanceRequirement =
                await GetRequirementAsync(
                    volunteerOnboardingId,
                    VolunteerOnboardingRequirementType.OfferAcceptance,
                    cancellationToken);

            if (offerAcceptanceRequirement != null)
            {
                MarkRequirementIncomplete(
                    offerAcceptanceRequirement);
            }

            var rawAcceptanceToken =
                GenerateSecureToken();

            var tokenHash =
                HashToken(rawAcceptanceToken);

            var offerReference =
                await GenerateOfferReferenceAsync(
                    cancellationToken);

            var offer =
                new VolunteerOffer
                {
                    VolunteerOnboardingId =
                        onboarding.VolunteerOnboardingId,

                    VolunteerApplicationId =
                        onboarding.VolunteerApplicationId,

                    ApplicantId =
                        onboarding.ApplicantId,

                    OfferReference =
                        offerReference,

                    PositionTitle =
                        opportunity.Title,

                    VolunteerOpportunityId =
                        opportunity.VolunteerOpportunityId,

                    IssuedDate =
                        now,

                    ExpiryDate =
                        expiryDate,

                    Status =
                        VolunteerOfferStatus.Pending,

                    AcceptanceTokenHash =
                        tokenHash,

                    TokenExpiryDate =
                        expiryDate,

                    CreatedByAdminId =
                        adminId,

                    CreatedDate =
                        now,

                    ModifiedDate =
                        now
                };

            _context.VolunteerOffers.Add(offer);

            onboarding.OfferSentDate =
                now;

            onboarding.Status =
                VolunteerOnboardingStatus.OfferPending;

            onboarding.CurrentStep =
                1;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            return new VolunteerOfferCreationResult
            {
                Offer = offer,
                RawAcceptanceToken = rawAcceptanceToken
            };
        }

        // ============================================================
        // ACCEPT OFFER - SECURE TOKEN VERSION
        //
        // This is the version used by the public offer link.
        // ============================================================

        public async Task<VolunteerOfferActionResult> AcceptOfferAsync(
            int volunteerOfferId,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (volunteerOfferId <= 0)
            {
                return VolunteerOfferActionResult.Failed(
                    "Invalid volunteer offer.");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return VolunteerOfferActionResult.Failed(
                    "The offer token is missing.");
            }

            var offer =
                await _context.VolunteerOffers
                    .Include(x => x.VolunteerOnboarding)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOfferId == volunteerOfferId,
                        cancellationToken);

            if (offer == null)
            {
                return VolunteerOfferActionResult.Failed(
                    "The volunteer offer could not be found.");
            }

            if (offer.Status != VolunteerOfferStatus.Pending)
            {
                return VolunteerOfferActionResult.Failed(
                    "This volunteer offer is no longer available.");
            }

            var now = DateTime.Now;

            if (now > offer.ExpiryDate ||
                now > offer.TokenExpiryDate)
            {
                offer.Status =
                    VolunteerOfferStatus.Expired;

                offer.ModifiedDate =
                    now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return VolunteerOfferActionResult.Failed(
                    "This volunteer offer has expired.");
            }

            var suppliedHash =
                HashToken(token);

            if (!CryptographicEquals(
                    suppliedHash,
                    offer.AcceptanceTokenHash))
            {
                return VolunteerOfferActionResult.Failed(
                    "The volunteer offer link is invalid.");
            }

            var onboarding =
                offer.VolunteerOnboarding;

            if (onboarding == null)
            {
                return VolunteerOfferActionResult.Failed(
                    "The onboarding record associated with this offer could not be found.");
            }

            await EnsureDefaultRequirementsAsync(
                onboarding.VolunteerOnboardingId,
                now,
                cancellationToken);

            offer.Status =
                VolunteerOfferStatus.Accepted;

            offer.AcceptedDate =
                now;

            offer.ModifiedDate =
                now;

            onboarding.OfferAcceptedDate =
                now;

            onboarding.Status =
                VolunteerOnboardingStatus.PoliciesPending;

            onboarding.CurrentStep =
                2;

            onboarding.ModifiedDate =
                now;

            var requirement =
                await GetRequirementAsync(
                    onboarding.VolunteerOnboardingId,
                    VolunteerOnboardingRequirementType.OfferAcceptance,
                    cancellationToken);

            if (requirement != null)
            {
                MarkRequirementCompleted(
                    requirement,
                    null);
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            var allPoliciesAccepted =
                await HasAcceptedAllRequiredPoliciesAsync(
                    onboarding.VolunteerOnboardingId,
                    cancellationToken);

            return VolunteerOfferActionResult.Successful(
                "Volunteer offer accepted successfully.",
                allPoliciesAccepted);
        }

        // ============================================================
        // ACCEPT OFFER - INTERNAL/LEGACY VERSION
        // ============================================================

        public async Task<bool> AcceptOfferAsync(
             int volunteerOfferId,
             CancellationToken cancellationToken = default)
        {
            var offer =
                await _context.VolunteerOffers
                    .Include(x => x.VolunteerOnboarding)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOfferId ==
                            volunteerOfferId,
                        cancellationToken);

            if (offer == null)
            {
                return false;
            }

            if (offer.Status != VolunteerOfferStatus.Pending)
            {
                return false;
            }

            if (DateTime.Now > offer.ExpiryDate)
            {
                offer.Status =
                    VolunteerOfferStatus.Expired;

                offer.ModifiedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return false;
            }

            offer.Status =
                VolunteerOfferStatus.Accepted;

            offer.AcceptedDate =
                DateTime.Now;

            offer.ModifiedDate =
                DateTime.Now;

            var onboarding =
                offer.VolunteerOnboarding;

            if (onboarding != null)
            {
                onboarding.OfferAcceptedDate =
                    DateTime.Now;

                onboarding.Status =
                    VolunteerOnboardingStatus.PoliciesPending;

                onboarding.CurrentStep = 2;

                onboarding.ModifiedDate =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }
        // ============================================================
        // DECLINE OFFER - SECURE TOKEN VERSION
        // ============================================================

        public async Task<VolunteerOfferActionResult> DeclineOfferAsync(
            int volunteerOfferId,
            string token,
            string declineReason,
            CancellationToken cancellationToken = default)
        {
            if (volunteerOfferId <= 0)
            {
                return VolunteerOfferActionResult.Failed(
                    "Invalid volunteer offer.");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return VolunteerOfferActionResult.Failed(
                    "The offer token is missing.");
            }

            if (string.IsNullOrWhiteSpace(declineReason))
            {
                return VolunteerOfferActionResult.Failed(
                    "A reason is required when declining the offer.");
            }

            declineReason =
                declineReason.Trim();

            if (declineReason.Length > 1000)
            {
                return VolunteerOfferActionResult.Failed(
                    "The decline reason cannot exceed 1000 characters.");
            }

            var offer =
                await _context.VolunteerOffers
                    .Include(x => x.VolunteerOnboarding)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOfferId == volunteerOfferId,
                        cancellationToken);

            if (offer == null)
            {
                return VolunteerOfferActionResult.Failed(
                    "The volunteer offer could not be found.");
            }

            if (offer.Status != VolunteerOfferStatus.Pending)
            {
                return VolunteerOfferActionResult.Failed(
                    "This volunteer offer is no longer available.");
            }

            var now = DateTime.Now;

            if (now > offer.ExpiryDate ||
                now > offer.TokenExpiryDate)
            {
                offer.Status =
                    VolunteerOfferStatus.Expired;

                offer.ModifiedDate =
                    now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return VolunteerOfferActionResult.Failed(
                    "This volunteer offer has expired.");
            }

            var suppliedHash =
                HashToken(token);

            if (!CryptographicEquals(
                    suppliedHash,
                    offer.AcceptanceTokenHash))
            {
                return VolunteerOfferActionResult.Failed(
                    "The volunteer offer link is invalid.");
            }

            var onboarding =
                offer.VolunteerOnboarding;

            offer.Status =
                VolunteerOfferStatus.Declined;

            offer.DeclinedDate =
                now;

            offer.DeclineReason =
                declineReason;

            offer.ModifiedDate =
                now;

            if (onboarding != null)
            {
                onboarding.Status =
                    VolunteerOnboardingStatus.OfferDeclined;

                onboarding.OfferDeclinedDate =
                    now;

                onboarding.OfferDeclineReason =
                    declineReason;

                onboarding.ModifiedDate =
                    now;

                var requirement =
                    await GetRequirementAsync(
                        onboarding.VolunteerOnboardingId,
                        VolunteerOnboardingRequirementType.OfferAcceptance,
                        cancellationToken);

                if (requirement != null)
                {
                    MarkRequirementIncomplete(
                        requirement);
                }
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            return VolunteerOfferActionResult.Successful(
                "The volunteer offer has been declined.",
                false);
        }

        // ============================================================
        // DECLINE OFFER - INTERNAL/LEGACY VERSION
        // ============================================================

        public async Task<bool> DeclineOfferAsync(
            int volunteerOfferId,
            string declineReason,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(declineReason))
            {
                throw new ArgumentException(
                    "A reason is required when declining the offer.",
                    nameof(declineReason));
            }

            declineReason =
                declineReason.Trim();

            if (declineReason.Length > 1000)
            {
                throw new ArgumentException(
                    "The decline reason cannot exceed 1000 characters.",
                    nameof(declineReason));
            }

            var offer =
                await _context.VolunteerOffers
                    .Include(x => x.VolunteerOnboarding)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOfferId == volunteerOfferId,
                        cancellationToken);

            if (offer == null ||
                offer.Status != VolunteerOfferStatus.Pending)
            {
                return false;
            }

            var now = DateTime.Now;

            offer.Status =
                VolunteerOfferStatus.Declined;

            offer.DeclinedDate =
                now;

            offer.DeclineReason =
                declineReason;

            offer.ModifiedDate =
                now;

            var onboarding =
                offer.VolunteerOnboarding;

            if (onboarding != null)
            {
                onboarding.Status =
                    VolunteerOnboardingStatus.OfferDeclined;

                onboarding.OfferDeclinedDate =
                    now;

                onboarding.OfferDeclineReason =
                    declineReason;

                onboarding.ModifiedDate =
                    now;

                var requirement =
                    await GetRequirementAsync(
                        onboarding.VolunteerOnboardingId,
                        VolunteerOnboardingRequirementType.OfferAcceptance,
                        cancellationToken);

                if (requirement != null)
                {
                    MarkRequirementIncomplete(
                        requirement);
                }
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // ============================================================
        // GET ACTIVE OFFER
        // ============================================================

        public async Task<VolunteerOffer?> GetActiveOfferAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerOffers
                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId &&

                        x.Status ==
                        VolunteerOfferStatus.Pending &&

                        x.ExpiryDate >
                        DateTime.Now &&
                        x.TokenExpiryDate >
                        DateTime.Now,
                    cancellationToken);
        }

        // ============================================================
        // ACCEPT POLICY
        // ============================================================

        public async Task<bool> AcceptPolicyAsync(
            int volunteerOnboardingId,
            int volunteerPolicyId,
            string applicantId,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                return false;
            }

            if (onboarding.ApplicantId != applicantId)
            {
                throw new UnauthorizedAccessException(
                    "The applicant does not belong to this onboarding record.");
            }

            var policy =
                await _context.VolunteerPolicies
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerPolicyId ==
                            volunteerPolicyId &&
                            x.IsActive,
                        cancellationToken);

            if (policy == null ||
                !policy.IsRequired)
            {
                return false;
            }

            var existing =
                await _context.VolunteerPolicyAcceptances
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId &&
                            x.VolunteerPolicyId ==
                            volunteerPolicyId,
                        cancellationToken);

            var now = DateTime.Now;

            if (existing != null)
            {
                existing.Accepted =
                    true;

                existing.AcceptedDate =
                    now;

                existing.PolicyVersion =
                    policy.Version;

                existing.IpAddress =
                    ipAddress;

                existing.UserAgent =
                    userAgent;
            }
            else
            {
                _context.VolunteerPolicyAcceptances.Add(
                    new VolunteerPolicyAcceptance
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        VolunteerPolicyId =
                            volunteerPolicyId,

                        ApplicantId =
                            applicantId,

                        Accepted =
                            true,

                        AcceptedDate =
                            now,

                        PolicyVersion =
                            policy.Version,

                        IpAddress =
                            ipAddress,

                        UserAgent =
                            userAgent,

                        CreatedDate =
                            now
                    });
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            await EnsureDefaultRequirementsAsync(
                volunteerOnboardingId,
                now,
                cancellationToken);

            await SyncPolicyRequirementsAsync(
                volunteerOnboardingId,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            if (await HasAcceptedAllRequiredPoliciesAsync(
                    volunteerOnboardingId,
                    cancellationToken))
            {
                onboarding.Status =
                    VolunteerOnboardingStatus.DocumentsPending;

                onboarding.CurrentStep =
                    3;

                onboarding.PoliciesAcceptedDate =
                    now;

                onboarding.ModifiedDate =
                    now;

                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            return true;
        }

        // ============================================================
        // CHECK REQUIRED POLICIES
        // ============================================================

        public async Task<bool> HasAcceptedAllRequiredPoliciesAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
        {
            var requiredPolicies =
                await _context.VolunteerPolicies
                    .Where(x =>
                        x.IsActive &&
                        x.IsRequired)
                    .Select(x => new
                    {
                        x.VolunteerPolicyId,
                        x.Version
                    })
                    .ToListAsync(cancellationToken);

            if (!requiredPolicies.Any())
            {
                return true;
            }

            var acceptedPolicies =
                await _context.VolunteerPolicyAcceptances
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId &&
                        x.Accepted)
                    .Select(x => new
                    {
                        x.VolunteerPolicyId,
                        x.PolicyVersion
                    })
                    .ToListAsync(cancellationToken);

            return requiredPolicies.All(required =>
                acceptedPolicies.Any(accepted =>
                    accepted.VolunteerPolicyId ==
                    required.VolunteerPolicyId &&
                    accepted.PolicyVersion ==
                    required.Version));
        }

        // ============================================================
        // ADD DOCUMENT
        // ============================================================

        public async Task<VolunteerOnboardingDocument> AddDocumentAsync(
            int volunteerOnboardingId,
            string applicantId,
            VolunteerOnboardingDocumentType documentType,
            string documentName,
            string fileName,
            string filePath,
            string contentType,
            long fileSize,
            string? fileHash = null,
            CancellationToken cancellationToken = default)
        {
            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                throw new InvalidOperationException(
                    "The onboarding record could not be found.");
            }

            if (onboarding.ApplicantId != applicantId)
            {
                throw new UnauthorizedAccessException(
                    "The applicant does not belong to this onboarding record.");
            }

            // --------------------------------------------------------
            // IMPORTANT:
            //
            // VolunteerContract is no longer handled as a normal
            // onboarding document.
            //
            // Signed contracts are handled by:
            //
            // IVolunteerContractService.UploadCompletedContractAsync()
            //
            // Therefore we reject attempts to push a contract through
            // this generic document endpoint.
            // --------------------------------------------------------

            if (documentType ==
                VolunteerOnboardingDocumentType.VolunteerContract)
            {
                throw new InvalidOperationException(
                    "The Volunteer Service Agreement must be uploaded through the contract workflow.");
            }

            const long maximumFileSize =
                50L * 1024L * 1024L;

            if (fileSize <= 0)
            {
                throw new ArgumentException(
                    "The uploaded file is empty.",
                    nameof(fileSize));
            }

            if (fileSize > maximumFileSize)
            {
                throw new ArgumentException(
                    "The onboarding document cannot exceed 50 MB.",
                    nameof(fileSize));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "A file name is required.",
                    nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "A file path is required.",
                    nameof(filePath));
            }

            var now = DateTime.Now;

            var previousDocuments =
                await _context.VolunteerOnboardingDocuments
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId &&
                        x.DocumentType ==
                        documentType &&
                        x.Status !=
                        VolunteerOnboardingDocumentStatus.Approved)
                    .ToListAsync(cancellationToken);

            foreach (var previous in previousDocuments)
            {
                previous.Status =
                    VolunteerOnboardingDocumentStatus.Rejected;

                previous.ModifiedDate =
                    now;

                previous.RejectionReason =
                    "Superseded by a newer document upload.";
            }

            var document =
                new VolunteerOnboardingDocument
                {
                    VolunteerOnboardingId =
                        volunteerOnboardingId,

                    ApplicantId =
                        applicantId,

                    DocumentType =
                        documentType,

                    DocumentName =
                        documentName.Trim(),

                    FileName =
                        fileName.Trim(),

                    FilePath =
                        filePath.Trim(),

                    ContentType =
                        string.IsNullOrWhiteSpace(contentType)
                            ? "application/octet-stream"
                            : contentType.Trim(),

                    FileSize =
                        fileSize,

                    Status =
                        VolunteerOnboardingDocumentStatus.Uploaded,

                    UploadedDate =
                        now,

                    FileHash =
                        fileHash,

                    CreatedDate =
                        now,

                    ModifiedDate =
                        now
                };

            _context.VolunteerOnboardingDocuments.Add(
                document);

            onboarding.Status =
                VolunteerOnboardingStatus.DocumentsPending;

            onboarding.CurrentStep =
                3;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                applicantId;

            await _context.SaveChangesAsync(
                cancellationToken);

            await SyncDocumentRequirementsAsync(
                volunteerOnboardingId,
                null,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            return document;
        }

        // ============================================================
        // REVIEW DOCUMENT
        // ============================================================

        public async Task<bool> ReviewDocumentAsync(
            int documentId,
            VolunteerOnboardingDocumentStatus status,
            string adminId,
            string? rejectionReason = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
                    nameof(adminId));
            }

            Console.WriteLine(
                $"[ReviewDocument] Starting document review. " +
                $"DocumentId={documentId}, " +
                $"Status={status}, " +
                $"AdminId={adminId}");

            var document =
                await _context.VolunteerOnboardingDocuments
                    .Include(x => x.VolunteerOnboarding)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingDocumentId ==
                            documentId,
                        cancellationToken);

            if (document == null)
            {
                Console.WriteLine(
                    $"[ReviewDocument] Document not found. " +
                    $"DocumentId={documentId}");

                return false;
            }

            Console.WriteLine(
                $"[ReviewDocument] Document found. " +
                $"DocumentId={document.VolunteerOnboardingDocumentId}, " +
                $"OnboardingId={document.VolunteerOnboardingId}, " +
                $"DocumentType={document.DocumentType}, " +
                $"CurrentStatus={document.Status}");

            // ------------------------------------------------------------
            // VALIDATE REJECTION / RESUBMISSION REASON
            // ------------------------------------------------------------

            if (status ==
                    VolunteerOnboardingDocumentStatus.Rejected ||
                status ==
                    VolunteerOnboardingDocumentStatus.ResubmissionRequired)
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    throw new ArgumentException(
                        "A reason is required when rejecting or requesting resubmission.",
                        nameof(rejectionReason));
                }

                document.RejectionReason =
                    rejectionReason.Trim();

                Console.WriteLine(
                    $"[ReviewDocument] Rejection/resubmission reason supplied. " +
                    $"DocumentId={documentId}");
            }
            else
            {
                document.RejectionReason =
                    null;
            }

            var now = DateTime.Now;

            // ------------------------------------------------------------
            // UPDATE DOCUMENT
            // ------------------------------------------------------------

            document.Status =
                status;

            document.ReviewedDate =
                now;

            document.ReviewedByAdminId =
                adminId;

            document.ModifiedDate =
                now;

            Console.WriteLine(
                $"[ReviewDocument] Document status updated. " +
                $"DocumentId={documentId}, " +
                $"NewStatus={status}");

            // ------------------------------------------------------------
            // IF DOCUMENT IS REJECTED / RESUBMISSION REQUIRED
            // MOVE ONBOARDING BACK TO CORRECTION
            // ------------------------------------------------------------

            if (document.VolunteerOnboarding != null)
            {
                if (status ==
                        VolunteerOnboardingDocumentStatus.Rejected ||
                    status ==
                        VolunteerOnboardingDocumentStatus.ResubmissionRequired)
                {
                    document.VolunteerOnboarding.Status =
                        VolunteerOnboardingStatus.RejectedForCorrection;

                    document.VolunteerOnboarding.ModifiedDate =
                        now;

                    document.VolunteerOnboarding.ModifiedBy =
                        adminId;

                    Console.WriteLine(
                        $"[ReviewDocument] Onboarding moved to RejectedForCorrection. " +
                        $"OnboardingId={document.VolunteerOnboardingId}");
                }
            }

            // ------------------------------------------------------------
            // SAVE DOCUMENT REVIEW
            // ------------------------------------------------------------

            await _context.SaveChangesAsync(
                cancellationToken);

            Console.WriteLine(
                $"[ReviewDocument] Document review saved. " +
                $"DocumentId={documentId}");

            // ------------------------------------------------------------
            // SYNCHRONISE AUTOMATIC REQUIREMENTS
            // ------------------------------------------------------------

            await SyncDocumentRequirementsAsync(
                document.VolunteerOnboardingId,
                adminId,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            Console.WriteLine(
                $"[ReviewDocument] Document requirements synchronised. " +
                $"OnboardingId={document.VolunteerOnboardingId}");

            // ------------------------------------------------------------
            // IF NOT APPROVED, WE ARE DONE
            // ------------------------------------------------------------

            if (status !=
                VolunteerOnboardingDocumentStatus.Approved)
            {
                Console.WriteLine(
                    $"[ReviewDocument] Review completed without advancing onboarding. " +
                    $"DocumentId={documentId}, " +
                    $"Status={status}");

                return true;
            }

            // ------------------------------------------------------------
            // RELOAD ONBOARDING WITH REQUIREMENTS AND DOCUMENTS
            // ------------------------------------------------------------

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Requirements)
                    .Include(x => x.Documents)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            document.VolunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                Console.WriteLine(
                    $"[ReviewDocument] Onboarding not found after document review. " +
                    $"OnboardingId={document.VolunteerOnboardingId}");

                return true;
            }

            // ------------------------------------------------------------
            // GET LATEST DOCUMENT FOR EACH DOCUMENT TYPE
            //
            // IMPORTANT:
            // Old rejected/uploaded documents must NOT block onboarding.
            // Only the latest document for each type is considered.
            // ------------------------------------------------------------

            var latestDocuments =
                onboarding.Documents
                    .GroupBy(x => x.DocumentType)
                    .Select(group =>
                        group
                            .OrderByDescending(
                                x => x.UploadedDate)
                            .ThenByDescending(
                                x => x.ModifiedDate)
                            .ThenByDescending(
                                x => x.VolunteerOnboardingDocumentId)
                            .First())
                    .ToList();

            Console.WriteLine(
                $"[ReviewDocument] Latest document count=" +
                $"{latestDocuments.Count}");

            foreach (var latestDocument in latestDocuments)
            {
                Console.WriteLine(
                    $"[ReviewDocument] LatestDocument: " +
                    $"DocumentId={latestDocument.VolunteerOnboardingDocumentId}, " +
                    $"Type={latestDocument.DocumentType}, " +
                    $"Status={latestDocument.Status}");
            }

            // ------------------------------------------------------------
            // REQUIRED DOCUMENT REQUIREMENTS
            // ------------------------------------------------------------

            var documentRequirements =
                onboarding.Requirements
                    .Where(x =>
                        x.IsRequired &&
                        (
                            x.RequirementType ==
                                VolunteerOnboardingRequirementType.IdentityVerification ||

                            x.RequirementType ==
                                VolunteerOnboardingRequirementType.QualificationVerification ||

                            x.RequirementType ==
                                VolunteerOnboardingRequirementType.DocumentUpload
                        ))
                    .ToList();

            var allRequiredDocumentRequirementsComplete =
                documentRequirements.All(
                    x => x.IsCompleted);

            Console.WriteLine(
                $"[ReviewDocument] Required document requirements complete=" +
                $"{allRequiredDocumentRequirementsComplete}");

            foreach (var requirement in documentRequirements)
            {
                Console.WriteLine(
                    $"[ReviewDocument] Requirement: " +
                    $"Type={requirement.RequirementType}, " +
                    $"Required={requirement.IsRequired}, " +
                    $"Completed={requirement.IsCompleted}");
            }

            // ------------------------------------------------------------
            // CHECK LATEST DOCUMENTS ONLY
            //
            // An old rejected document should not block the applicant
            // once a newer document of the same type has been approved.
            // ------------------------------------------------------------

            var hasUnapprovedLatestDocuments =
                latestDocuments.Any(
                    x =>
                        x.Status !=
                        VolunteerOnboardingDocumentStatus.Approved);

            Console.WriteLine(
                $"[ReviewDocument] HasUnapprovedLatestDocuments=" +
                $"{hasUnapprovedLatestDocuments}");

            // ------------------------------------------------------------
            // CHECK WHETHER ANY DOCUMENTS EXIST
            // ------------------------------------------------------------

            var hasLatestDocuments =
                latestDocuments.Any();

            Console.WriteLine(
                $"[ReviewDocument] HasLatestDocuments=" +
                $"{hasLatestDocuments}");

            // ------------------------------------------------------------
            // DEBUG DOCUMENT STATE
            // ------------------------------------------------------------

            Console.WriteLine(
                $"[ReviewDocument] OnboardingId=" +
                $"{onboarding.VolunteerOnboardingId}, " +
                $"CurrentStatus={onboarding.Status}, " +
                $"CurrentStep={onboarding.CurrentStep}, " +
                $"RequiredDocumentRequirementsComplete=" +
                $"{allRequiredDocumentRequirementsComplete}, " +
                $"HasUnapprovedLatestDocuments=" +
                $"{hasUnapprovedLatestDocuments}");

            // ------------------------------------------------------------
            // MOVE TO CONTRACT PENDING
            //
            // Only do this when:
            //
            // 1. All required document requirements are complete
            // 2. Every latest document is approved
            //
            // Historical/superseded documents are ignored.
            // ------------------------------------------------------------

            if (allRequiredDocumentRequirementsComplete &&
                hasLatestDocuments &&
                !hasUnapprovedLatestDocuments)
            {
                onboarding.Status =
                    VolunteerOnboardingStatus.ContractPending;

                onboarding.CurrentStep =
                    4;

                onboarding.ModifiedDate =
                    now;

                onboarding.ModifiedBy =
                    adminId;

                await _context.SaveChangesAsync(
                    cancellationToken);

                Console.WriteLine(
                    $"[ReviewDocument] SUCCESS. " +
                    $"Onboarding {onboarding.VolunteerOnboardingId} " +
                    $"moved to ContractPending. " +
                    $"CurrentStep={onboarding.CurrentStep}");
            }
            else
            {
                Console.WriteLine(
                    $"[ReviewDocument] Onboarding NOT moved to ContractPending. " +
                    $"RequiredRequirementsComplete=" +
                    $"{allRequiredDocumentRequirementsComplete}, " +
                    $"HasLatestDocuments=" +
                    $"{hasLatestDocuments}, " +
                    $"HasUnapprovedLatestDocuments=" +
                    $"{hasUnapprovedLatestDocuments}");
            }

            Console.WriteLine(
                $"[ReviewDocument] Completed successfully. " +
                $"DocumentId={documentId}");

            return true;
        }
        // ============================================================
        // COMPLETE REQUIREMENT
        // ============================================================

        public async Task<bool> CompleteRequirementAsync(
            int volunteerOnboardingId,
            VolunteerOnboardingRequirementType requirementType,
            string adminId,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            if (volunteerOnboardingId <= 0)
            {
                throw new ArgumentException(
                    "A valid volunteer onboarding ID is required.",
                    nameof(volunteerOnboardingId));
            }

            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
                    nameof(adminId));
            }

            Console.WriteLine(
                $"[CompleteRequirement] Starting. " +
                $"OnboardingId={volunteerOnboardingId}, " +
                $"RequirementType={requirementType}, " +
                $"AdminId={adminId}");

            // ============================================================
            // LOAD ONBOARDING
            // ============================================================

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Requirements)
                    .Include(x => x.Volunteer)
                        .ThenInclude(x => x!.Supervisor)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                Console.WriteLine(
                    $"[CompleteRequirement] Onboarding not found. " +
                    $"OnboardingId={volunteerOnboardingId}");

                return false;
            }

            // ============================================================
            // FIND REQUIREMENT
            // ============================================================

            var requirement =
                onboarding.Requirements
                    .FirstOrDefault(
                        x =>
                            x.RequirementType ==
                            requirementType);

            if (requirement == null)
            {
                Console.WriteLine(
                    $"[CompleteRequirement] Requirement not found. " +
                    $"OnboardingId={volunteerOnboardingId}, " +
                    $"RequirementType={requirementType}");

                return false;
            }

            var now = DateTime.Now;

            // ============================================================
            // SPECIAL HANDLING
            // SUPERVISOR ASSIGNMENT
            // ============================================================

            if (requirementType ==
                VolunteerOnboardingRequirementType.SupervisorAssignment)
            {
                Console.WriteLine(
                    $"[CompleteRequirement] Checking Supervisor assignment. " +
                    $"OnboardingId={volunteerOnboardingId}");

                // --------------------------------------------------------
                // VOLUNTEER MUST EXIST
                // --------------------------------------------------------

                var volunteer = onboarding.Volunteer;

                if (volunteer == null)
                {
                    Console.WriteLine(
                        $"[CompleteRequirement] Cannot complete SupervisorAssignment. " +
                        $"Volunteer account does not exist. " +
                        $"OnboardingId={volunteerOnboardingId}");

                    return false;
                }

                // --------------------------------------------------------
                // SUPERVISOR MUST BE ASSIGNED
                // --------------------------------------------------------

                if (string.IsNullOrWhiteSpace(volunteer.SupervisorId))
                {
                    Console.WriteLine(
                        $"[CompleteRequirement] Cannot complete SupervisorAssignment. " +
                        $"Volunteer has no SupervisorId. " +
                        $"VolunteerId={volunteer.Id}");

                    return false;
                }

                // --------------------------------------------------------
                // SUPERVISOR MUST EXIST
                // --------------------------------------------------------

                var supervisor =
                    await _context.Supervisors
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id == volunteer.SupervisorId &&
                                x.IsActive,
                            cancellationToken);

                if (supervisor == null)
                {
                    Console.WriteLine(
                        $"[CompleteRequirement] Cannot complete SupervisorAssignment. " +
                        $"Assigned Supervisor was not found or is inactive. " +
                        $"VolunteerId={volunteer.Id}, " +
                        $"SupervisorId={volunteer.SupervisorId}");

                    return false;
                }

                // --------------------------------------------------------
                // COMPLETE SUPERVISOR ASSIGNMENT
                // --------------------------------------------------------

                requirement.IsCompleted = true;
                requirement.CompletedDate = now;
                requirement.CompletedByAdminId = adminId;
                requirement.ModifiedDate = now;

                onboarding.ModifiedDate = now;
                onboarding.ModifiedBy = adminId;

                Console.WriteLine(
                    $"[CompleteRequirement] SupervisorAssignment completed. " +
                    $"OnboardingId={volunteerOnboardingId}, " +
                    $"VolunteerId={volunteer.Id}, " +
                    $"SupervisorId={supervisor.Id}, " +
                    $"SupervisorName={supervisor.FirstName} {supervisor.LastName}");

                // --------------------------------------------------------
                // CHECK WHETHER ALL REQUIRED ACTIVATION REQUIREMENTS
                // ARE NOW COMPLETE
                // --------------------------------------------------------

                var activationRequirementTypes =
                    new[]
                    {
                VolunteerOnboardingRequirementType.OfferAcceptance,
                VolunteerOnboardingRequirementType.PolicyAcceptance,
                VolunteerOnboardingRequirementType.CodeOfConduct,
                VolunteerOnboardingRequirementType.IdentityVerification,
                VolunteerOnboardingRequirementType.DocumentUpload,
                VolunteerOnboardingRequirementType.Contract,
                VolunteerOnboardingRequirementType.SupervisorAssignment
                    };

                var incompleteRequiredRequirements =
                    onboarding.Requirements
                        .Where(x =>
                            activationRequirementTypes.Contains(
                                x.RequirementType) &&
                            !x.IsCompleted)
                        .ToList();

                if (incompleteRequiredRequirements.Count == 0)
                {
                    Console.WriteLine(
                        $"[CompleteRequirement] ALL activation requirements completed. " +
                        $"OnboardingId={volunteerOnboardingId}");

                    onboarding.Status =
                        VolunteerOnboardingStatus.ReadyForActivation;

                    onboarding.CurrentStep = 6;

                    onboarding.ModifiedDate = now;
                    onboarding.ModifiedBy = adminId;
                }
                else
                {
                    Console.WriteLine(
                        $"[CompleteRequirement] SupervisorAssignment completed, " +
                        $"but other activation requirements remain incomplete. " +
                        $"OnboardingId={volunteerOnboardingId}, " +
                        $"RemainingCount={incompleteRequiredRequirements.Count}");
                }

                // --------------------------------------------------------
                // SAVE
                // --------------------------------------------------------

                await _context.SaveChangesAsync(
                    cancellationToken);

                Console.WriteLine(
                    $"[CompleteRequirement] SUCCESS. " +
                    $"OnboardingId={volunteerOnboardingId}, " +
                    $"RequirementType={requirementType}");

                return true;
            }

            // ============================================================
            // COMPLETE NORMAL REQUIREMENT
            // ============================================================

            requirement.IsCompleted = true;

            requirement.CompletedDate = now;

            requirement.CompletedByAdminId = adminId;

            requirement.ModifiedDate = now;

            Console.WriteLine(
                $"[CompleteRequirement] Requirement completed. " +
                $"RequirementId={requirement.VolunteerOnboardingRequirementId}, " +
                $"RequirementType={requirement.RequirementType}, " +
                $"CompletedByAdminId={adminId}");

            // ============================================================
            // CONTRACT REQUIREMENT
            //
            // Once the signed contract has been approved and the contract
            // requirement is completed:
            //
            // 1. Create the Volunteer account.
            // 2. Automatically assign Supervisor based on Area.
            // 3. Automatically complete SupervisorAssignment if a
            //    Supervisor was successfully assigned.
            // 4. Move onboarding into administrator verification.
            // ============================================================

            if (requirementType ==
                VolunteerOnboardingRequirementType.Contract)
            {
                var volunteer =
                    await _volunteerAccountService
                        .CreateFromOnboardingAsync(
                            volunteerOnboardingId,
                            adminId,
                            cancellationToken);

                // --------------------------------------------------------
                // AUTOMATIC SUPERVISOR ASSIGNMENT
                // --------------------------------------------------------

                if (volunteer != null &&
                    !string.IsNullOrWhiteSpace(volunteer.SupervisorId))
                {
                    var supervisorAssignmentRequirement =
                        onboarding.Requirements
                            .FirstOrDefault(
                                x =>
                                    x.RequirementType ==
                                    VolunteerOnboardingRequirementType
                                        .SupervisorAssignment);

                    if (supervisorAssignmentRequirement != null)
                    {
                        supervisorAssignmentRequirement.IsCompleted = true;

                        supervisorAssignmentRequirement.CompletedDate = now;

                        supervisorAssignmentRequirement.CompletedByAdminId =
                            adminId;

                        supervisorAssignmentRequirement.ModifiedDate = now;

                        Console.WriteLine(
                            $"[CompleteRequirement] SupervisorAssignment automatically completed. " +
                            $"VolunteerId={volunteer.Id}, " +
                            $"SupervisorId={volunteer.SupervisorId}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"[CompleteRequirement] WARNING: SupervisorAssignment requirement " +
                            $"does not exist. " +
                            $"OnboardingId={volunteerOnboardingId}");
                    }
                }
                else
                {
                    Console.WriteLine(
                        $"[CompleteRequirement] WARNING: Volunteer was created " +
                        $"without a Supervisor assignment. " +
                        $"SupervisorAssignment remains incomplete. " +
                        $"OnboardingId={volunteerOnboardingId}");
                }

                // --------------------------------------------------------
                // MOVE TO ADMIN VERIFICATION
                // --------------------------------------------------------

                onboarding.Status =
                    VolunteerOnboardingStatus.AdminVerification;

                onboarding.CurrentStep = 5;

                if (!onboarding.ContractSubmittedDate.HasValue)
                {
                    onboarding.ContractSubmittedDate = now;
                }

                onboarding.ModifiedDate = now;
                onboarding.ModifiedBy = adminId;
            }
            else
            {
                onboarding.ModifiedDate = now;

                onboarding.ModifiedBy = adminId;
            }

            // ============================================================
            // SAVE
            // ============================================================

            await _context.SaveChangesAsync(
                cancellationToken);

            Console.WriteLine(
                $"[CompleteRequirement] SUCCESS. " +
                $"OnboardingId={volunteerOnboardingId}, " +
                $"RequirementType={requirementType}");

            return true;
        }
        // ============================================================
        // START VERIFICATION
        // ============================================================

        public async Task<bool> StartVerificationAsync(
            int volunteerOnboardingId,
            string adminId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
                    nameof(adminId));
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                return false;
            }

            var now = DateTime.Now;

            onboarding.Status =
                VolunteerOnboardingStatus.AdminVerification;

            onboarding.CurrentStep =
                5;

            onboarding.VerificationStartedDate ??=
                now;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // ============================================================
        // COMPLETE VERIFICATION
        // ============================================================

        public async Task<bool> CompleteVerificationAsync(
            int volunteerOnboardingId,
            string adminId,
            string? verificationNotes = null,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine(
                $"[CompleteVerification] START - " +
                $"OnboardingId={volunteerOnboardingId}, " +
                $"AdminId={adminId}");

            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
                    nameof(adminId));
            }

            try
            {
                var onboarding =
                    await _context.VolunteerOnboardings
                        .Include(x => x.Documents)
                        .Include(x => x.Requirements)
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                volunteerOnboardingId,
                            cancellationToken);

                if (onboarding == null)
                {
                    Console.WriteLine(
                        "[CompleteVerification] Onboarding not found.");

                    return false;
                }

                Console.WriteLine(
                    $"[CompleteVerification] Loaded onboarding. " +
                    $"Status={onboarding.Status}, " +
                    $"Step={onboarding.CurrentStep}");

                // ----------------------------------------------------
                // ENSURE REQUIREMENTS
                // ----------------------------------------------------

                await EnsureDefaultRequirementsAsync(
                    volunteerOnboardingId,
                    DateTime.Now,
                    cancellationToken);

                // ----------------------------------------------------
                // SYNC AUTOMATIC REQUIREMENTS
                // ----------------------------------------------------

                await SyncAutomaticRequirementsAsync(
                    volunteerOnboardingId,
                    adminId,
                    cancellationToken);

                await _context.SaveChangesAsync(
                    cancellationToken);

                // ----------------------------------------------------
                // RELOAD REQUIREMENTS + DOCUMENTS
                // ----------------------------------------------------

                await _context.Entry(onboarding)
                    .Collection(x => x.Requirements)
                    .LoadAsync(cancellationToken);

                await _context.Entry(onboarding)
                    .Collection(x => x.Documents)
                    .LoadAsync(cancellationToken);

                Console.WriteLine(
                    $"[CompleteVerification] Reloaded. " +
                    $"Requirements={onboarding.Requirements.Count}, " +
                    $"Documents={onboarding.Documents.Count}");

                // ----------------------------------------------------
                // REQUIRED POLICIES
                // ----------------------------------------------------

                var policiesAccepted =
                    await HasAcceptedAllRequiredPoliciesAsync(
                        volunteerOnboardingId,
                        cancellationToken);

                Console.WriteLine(
                    $"[CompleteVerification] Required policies accepted={policiesAccepted}");

                if (!policiesAccepted)
                {
                    throw new InvalidOperationException(
                        "The applicant has not accepted all required policies.");
                }

                // ----------------------------------------------------
                // DIDIT
                // ----------------------------------------------------

                var identityVerification =
                    await _context.VolunteerIdentityVerifications
                        .AsNoTracking()
                        .Where(x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId)
                        .OrderByDescending(x => x.ModifiedDate)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                if (identityVerification == null)
                {
                    throw new InvalidOperationException(
                        "Identity verification has not been completed.");
                }

                Console.WriteLine(
                    $"[CompleteVerification] Didit verification: " +
                    $"Id={identityVerification.Id}, " +
                    $"Status={identityVerification.Status}, " +
                    $"Document={identityVerification.DocumentVerified}, " +
                    $"Liveness={identityVerification.LivenessVerified}, " +
                    $"FaceMatch={identityVerification.FaceMatchVerified}");

                if (identityVerification.Status !=
                    VolunteerIdentityVerificationStatus.Verified)
                {
                    throw new InvalidOperationException(
                        $"Identity verification is not complete. " +
                        $"Current status: {identityVerification.Status}.");
                }

                if (!identityVerification.DocumentVerified ||
                    !identityVerification.LivenessVerified ||
                    !identityVerification.FaceMatchVerified)
                {
                    throw new InvalidOperationException(
                        "Identity verification is marked as verified, " +
                        "but the document, liveness, and face verification " +
                        "checks have not all completed successfully.");
                }

                // ----------------------------------------------------
                // PRE-CONTRACT REQUIREMENTS
                // ----------------------------------------------------

                var preContractRequirementTypes =
                    new[]
                    {
                        VolunteerOnboardingRequirementType.OfferAcceptance,
                        VolunteerOnboardingRequirementType.PolicyAcceptance,
                        VolunteerOnboardingRequirementType.CodeOfConduct,
                        VolunteerOnboardingRequirementType.IdentityVerification,
                        VolunteerOnboardingRequirementType.QualificationVerification,
                        VolunteerOnboardingRequirementType.DocumentUpload
                    };

                var incompletePreContractRequirements =
                    onboarding.Requirements
                        .Where(x =>
                            x.IsRequired &&
                            preContractRequirementTypes.Contains(
                                x.RequirementType) &&
                            !x.IsCompleted)
                        .ToList();

                foreach (
                    var requirement
                    in incompletePreContractRequirements)
                {
                    Console.WriteLine(
                        $"[CompleteVerification] PRE-CONTRACT INCOMPLETE - " +
                        $"Id={requirement.VolunteerOnboardingRequirementId}, " +
                        $"Type={requirement.RequirementType}, " +
                        $"Title={requirement.Title}");
                }

                if (incompletePreContractRequirements.Any())
                {
                    var names =
                        string.Join(
                            ", ",
                            incompletePreContractRequirements
                                .Select(x => x.Title));

                    throw new InvalidOperationException(
                        $"The following requirements must be completed before the Volunteer Service Agreement: {names}");
                }

                // ----------------------------------------------------
                // CONTRACT REQUIREMENT
                // ----------------------------------------------------

                var contractRequirement =
                    onboarding.Requirements
                        .FirstOrDefault(
                            x =>
                                x.RequirementType ==
                                VolunteerOnboardingRequirementType.Contract);

                if (contractRequirement == null)
                {
                    throw new InvalidOperationException(
                        "The Volunteer Service Agreement requirement could not be found.");
                }

                // ----------------------------------------------------
                // CONTRACT NOT APPROVED YET
                // ----------------------------------------------------

                if (!contractRequirement.IsCompleted)
                {
                    var nowContract =
                        DateTime.Now;

                    onboarding.Status =
                        VolunteerOnboardingStatus.ContractPending;

                    onboarding.CurrentStep =
                        4;

                    onboarding.ModifiedDate =
                        nowContract;

                    onboarding.ModifiedBy =
                        adminId;

                    if (!string.IsNullOrWhiteSpace(verificationNotes))
                    {
                        onboarding.VerificationNotes =
                            verificationNotes.Trim();
                    }

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    Console.WriteLine(
                        "[CompleteVerification] Contract requirement is incomplete.");

                    Console.WriteLine(
                        "[CompleteVerification] Moving onboarding to ContractPending.");

                    return true;
                }

                // ----------------------------------------------------
                // CONTRACT IS APPROVED
                //
                // Now we are allowed to perform final verification.
                // ----------------------------------------------------

                Console.WriteLine(
                    "[CompleteVerification] Contract requirement is COMPLETE.");

                // ----------------------------------------------------
                // ALL DOCUMENTS MUST BE APPROVED
                // ----------------------------------------------------

                var allDocuments =
                    onboarding.Documents?.ToList()
                    ?? new List<VolunteerOnboardingDocument>();

                var unapprovedDocuments =
                    allDocuments
                        .Where(x =>
                            x.Status !=
                            VolunteerOnboardingDocumentStatus.Approved)
                        .ToList();

                Console.WriteLine(
                    $"[CompleteVerification] Documents={allDocuments.Count}, " +
                    $"Unapproved={unapprovedDocuments.Count}");

                if (unapprovedDocuments.Any())
                {
                    var documentDetails =
                        string.Join(
                            ", ",
                            unapprovedDocuments.Select(
                                x =>
                                    $"ID={x.VolunteerOnboardingDocumentId}, " +
                                    $"Type={x.DocumentType}, " +
                                    $"Status={x.Status}"));

                    throw new InvalidOperationException(
                        $"One or more onboarding documents have not been approved. " +
                        $"{documentDetails}");
                }

                // ----------------------------------------------------
                // FINAL STATUS
                // ----------------------------------------------------

                var now =
                    DateTime.Now;

                onboarding.Status =
                    VolunteerOnboardingStatus.Completed;

                onboarding.CurrentStep =
                    6;

                onboarding.VerifiedDate =
                    now;

                onboarding.VerifiedByAdminId =
                    adminId;

                onboarding.VerificationNotes =
                    string.IsNullOrWhiteSpace(verificationNotes)
                        ? null
                        : verificationNotes.Trim();

                onboarding.ModifiedDate =
                    now;

                onboarding.ModifiedBy =
                    adminId;

                await _context.SaveChangesAsync(
                    cancellationToken);

                Console.WriteLine(
                    $"[CompleteVerification] SUCCESS - " +
                    $"OnboardingId={volunteerOnboardingId}, " +
                    $"Status={onboarding.Status}, " +
                    $"Step={onboarding.CurrentStep}");

                return true;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    $"[CompleteVerification] CANCELLED - " +
                    $"OnboardingId={volunteerOnboardingId}");

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[CompleteVerification] ERROR - " +
                    $"OnboardingId={volunteerOnboardingId}");

                Console.WriteLine(
                    $"[CompleteVerification] Type={ex.GetType().FullName}");

                Console.WriteLine(
                    $"[CompleteVerification] Message={ex.Message}");

                Console.WriteLine(
                    $"[CompleteVerification] StackTrace={ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine(
                        $"[CompleteVerification] InnerException={ex.InnerException.Message}");
                }

                throw;
            }
        }

        // ============================================================
        // REJECT FOR CORRECTION
        // ============================================================

        public async Task<bool> RejectForCorrectionAsync(
            int volunteerOnboardingId,
            string adminId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
                    nameof(adminId));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "A correction reason is required.",
                    nameof(reason));
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                return false;
            }

            var now =
                DateTime.Now;

            onboarding.Status =
                VolunteerOnboardingStatus.RejectedForCorrection;

            onboarding.VerificationNotes =
                reason.Trim();

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // ============================================================
        // READY FOR ACTIVATION
        // ============================================================

        // ============================================================
        // READY FOR ACTIVATION
        // ============================================================

        public async Task<bool> IsReadyForActivationAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine(
                $"[IsReadyForActivation] START - " +
                $"OnboardingId={volunteerOnboardingId}");

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Documents)
                    .Include(x => x.Requirements)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                Console.WriteLine(
                    "[IsReadyForActivation] Onboarding not found.");

                return false;
            }

            // ------------------------------------------------------------
            // SYNCHRONISE REQUIREMENTS
            // ------------------------------------------------------------

            await EnsureDefaultRequirementsAsync(
                volunteerOnboardingId,
                DateTime.Now,
                cancellationToken);

            await SyncAutomaticRequirementsAsync(
                volunteerOnboardingId,
                null,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            // ------------------------------------------------------------
            // RELOAD REQUIREMENTS + DOCUMENTS
            // ------------------------------------------------------------

            await _context.Entry(onboarding)
                .Collection(x => x.Requirements)
                .LoadAsync(cancellationToken);

            await _context.Entry(onboarding)
                .Collection(x => x.Documents)
                .LoadAsync(cancellationToken);

            Console.WriteLine(
                $"[IsReadyForActivation] Status={onboarding.Status}, " +
                $"Requirements={onboarding.Requirements.Count}, " +
                $"Documents={onboarding.Documents.Count}");

            // ------------------------------------------------------------
            // ONBOARDING MUST ALREADY BE READY FOR ACTIVATION
            // ------------------------------------------------------------

            if (onboarding.Status !=
                VolunteerOnboardingStatus.ReadyForActivation)
            {
                Console.WriteLine(
                    $"[IsReadyForActivation] FALSE - " +
                    $"Status is {onboarding.Status}.");

                return false;
            }

            // ------------------------------------------------------------
            // REQUIRED POLICIES
            // ------------------------------------------------------------

            if (!await HasAcceptedAllRequiredPoliciesAsync(
                    volunteerOnboardingId,
                    cancellationToken))
            {
                Console.WriteLine(
                    "[IsReadyForActivation] FALSE - " +
                    "Required policies have not all been accepted.");

                return false;
            }

            // ------------------------------------------------------------
            // ONLY TRUE ONBOARDING/VERIFICATION REQUIREMENTS BLOCK
            //
            // IMPORTANT:
            // Do NOT use:
            //
            // onboarding.Requirements.Any(x => x.IsRequired && !x.IsCompleted)
            //
            // because that would make Orientation/Training block activation.
            // ------------------------------------------------------------

            var blockingIncompleteRequirements =
                onboarding.Requirements
                    .Where(IsFinalActivationBlockingRequirement)
                    .Where(x => !x.IsCompleted)
                    .ToList();

            if (blockingIncompleteRequirements.Any())
            {
                Console.WriteLine(
                    "[IsReadyForActivation] FALSE - " +
                    "Blocking requirements are incomplete.");

                foreach (var requirement in
                         blockingIncompleteRequirements)
                {
                    Console.WriteLine(
                        $"[IsReadyForActivation] BLOCKING REQUIREMENT: " +
                        $"Id={requirement.VolunteerOnboardingRequirementId}, " +
                        $"Type={requirement.RequirementType}, " +
                        $"Title={requirement.Title}");
                }

                return false;
            }

            // ------------------------------------------------------------
            // DOCUMENTS
            //
            // All currently uploaded onboarding documents must be approved.
            // ------------------------------------------------------------

            var unapprovedDocuments =
                onboarding.Documents
                    .Where(x =>
                        x.Status !=
                        VolunteerOnboardingDocumentStatus.Approved)
                    .ToList();

            if (unapprovedDocuments.Any())
            {
                Console.WriteLine(
                    $"[IsReadyForActivation] FALSE - " +
                    $"{unapprovedDocuments.Count} document(s) not approved.");

                foreach (var document in unapprovedDocuments)
                {
                    Console.WriteLine(
                        $"[IsReadyForActivation] DOCUMENT BLOCKING: " +
                        $"Id={document.VolunteerOnboardingDocumentId}, " +
                        $"Type={document.DocumentType}, " +
                        $"Status={document.Status}");
                }

                return false;
            }

            // ------------------------------------------------------------
            // DIDIT
            // ------------------------------------------------------------

            var identityVerification =
                await _context.VolunteerIdentityVerifications
                    .AsNoTracking()
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId)
                    .OrderByDescending(x => x.ModifiedDate)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (identityVerification == null)
            {
                Console.WriteLine(
                    "[IsReadyForActivation] FALSE - " +
                    "No Didit verification found.");

                return false;
            }

            if (identityVerification.Status !=
                VolunteerIdentityVerificationStatus.Verified)
            {
                Console.WriteLine(
                    $"[IsReadyForActivation] FALSE - " +
                    $"Didit status={identityVerification.Status}");

                return false;
            }

            if (!identityVerification.DocumentVerified ||
                !identityVerification.LivenessVerified ||
                !identityVerification.FaceMatchVerified)
            {
                Console.WriteLine(
                    "[IsReadyForActivation] FALSE - " +
                    "Didit verification flags are incomplete.");

                return false;
            }

            // ------------------------------------------------------------
            // SUCCESS
            // ------------------------------------------------------------

            Console.WriteLine(
                $"[IsReadyForActivation] SUCCESS - " +
                $"OnboardingId={volunteerOnboardingId}");

            return true;
        }
        // ============================================================
        // CURRENT STATUS
        // ============================================================

        public async Task<VolunteerOnboardingStatus> GetCurrentStatusAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerOnboardings
                .Where(x =>
                    x.VolunteerOnboardingId ==
                    volunteerOnboardingId)
                .Select(x => x.Status)
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        // ============================================================
        // ENSURE DEFAULT REQUIREMENTS
        // ============================================================

        private async Task EnsureDefaultRequirementsAsync(
            int volunteerOnboardingId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"[EnsureRequirements] START - " +
                $"OnboardingId={volunteerOnboardingId}");

            var requirements =
                await _context.VolunteerOnboardingRequirements
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId)
                    .OrderBy(x =>
                        x.DisplayOrder)
                    .ThenBy(x =>
                        x.VolunteerOnboardingRequirementId)
                    .ToListAsync(cancellationToken);

            var defaults =
                new List<VolunteerOnboardingRequirement>
                {
                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.OfferAcceptance,

                        Title =
                            "Volunteer Offer Accepted",

                        Description =
                            "The volunteer must accept the SwiftCare volunteer offer before continuing with onboarding.",

                        IsRequired = true,
                        IsCompleted = false,
                        DisplayOrder = 1,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.PolicyAcceptance,

                        Title =
                            "Required Policies Accepted",

                        Description =
                            "All required SwiftCare policies must be read and accepted before the volunteer can continue.",

                        IsRequired = true,
                        IsCompleted = false,
                        DisplayOrder = 2,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.CodeOfConduct,

                        Title =
                            "Code of Conduct Accepted",

                        Description =
                            "The volunteer must read and accept the SwiftCare Volunteer Code of Conduct.",

                        IsRequired = true,
                        IsCompleted = false,
                        DisplayOrder = 3,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.IdentityVerification,

                        Title =
                            "Identity Document Verified",

                        Description =
                            "A valid identity document must be uploaded, administrator-approved, and successfully verified through Didit identity verification.",

                        IsRequired = true,
                        IsCompleted = false,
                        DisplayOrder = 4,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.QualificationVerification,

                        Title =
                            "Qualification Documents Verified",

                        Description =
                            "Required qualifications or certificates must be uploaded and verified where applicable.",

                        IsRequired = false,
                        IsCompleted = false,
                        DisplayOrder = 5,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.DocumentUpload,

                        Title =
                            "Supporting Documents Verified",

                        Description =
                            "All additional documents required for this volunteer opportunity must be uploaded and verified.",

                        IsRequired = true,
                        IsCompleted = false,
                        DisplayOrder = 6,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.Contract,

                        Title =
                            "Volunteer Service Agreement Completed",

                        Description =
                            "The Volunteer Service Agreement must be generated, signed, submitted, and approved before final verification.",

                        IsRequired = true,
                        IsCompleted = false,
                        DisplayOrder = 7,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.SupervisorAssignment,

                        Title =
                            "Supervisor Assigned",

                        Description =
                            "A suitable SwiftCare supervisor must be assigned before the volunteer can be activated.",

                        IsRequired = false,
                        IsCompleted = false,
                        DisplayOrder = 10,

                        CreatedDate = now,
                        ModifiedDate = now
                    }
                };

            // --------------------------------------------------------
            // DEDUPLICATE
            // --------------------------------------------------------

            var duplicateGroups =
                requirements
                    .GroupBy(x => x.RequirementType)
                    .Where(g => g.Count() > 1)
                    .ToList();

            foreach (var group in duplicateGroups)
            {
                var canonical =
                    group
                        .OrderByDescending(x => x.IsCompleted)
                        .ThenBy(x => x.VolunteerOnboardingRequirementId)
                        .First();

                var completedDuplicate =
                    group.FirstOrDefault(
                        x => x.IsCompleted);

                if (completedDuplicate != null)
                {
                    canonical.IsCompleted =
                        true;

                    canonical.CompletedDate =
                        completedDuplicate.CompletedDate;

                    canonical.CompletedByAdminId =
                        completedDuplicate.CompletedByAdminId;

                    canonical.ModifiedDate =
                        DateTime.Now;
                }

                var duplicates =
                    group
                        .Where(x =>
                            x.VolunteerOnboardingRequirementId !=
                            canonical.VolunteerOnboardingRequirementId)
                        .ToList();

                foreach (var duplicate in duplicates)
                {
                    Console.WriteLine(
                        $"[EnsureRequirements] Removing duplicate requirement. " +
                        $"Id={duplicate.VolunteerOnboardingRequirementId}, " +
                        $"Type={duplicate.RequirementType}");

                    _context.VolunteerOnboardingRequirements
                        .Remove(duplicate);
                }
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            requirements =
                await _context.VolunteerOnboardingRequirements
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId)
                    .OrderBy(x =>
                        x.DisplayOrder)
                    .ThenBy(x =>
                        x.VolunteerOnboardingRequirementId)
                    .ToListAsync(cancellationToken);

            var existingTypes =
                requirements
                    .Select(x => x.RequirementType)
                    .ToHashSet();

            var missingRequirements =
                defaults
                    .Where(x =>
                        !existingTypes.Contains(
                            x.RequirementType))
                    .ToList();

            if (missingRequirements.Any())
            {
                _context.VolunteerOnboardingRequirements
                    .AddRange(missingRequirements);
            }

            Console.WriteLine(
                $"[EnsureRequirements] END - " +
                $"OnboardingId={volunteerOnboardingId}");
        }

        // ============================================================
        // SYNCHRONIZE AUTOMATIC REQUIREMENTS
        // ============================================================

        private async Task SyncAutomaticRequirementsAsync(
            int volunteerOnboardingId,
            string? adminId,
            CancellationToken cancellationToken)
        {
            await EnsureDefaultRequirementsAsync(
                volunteerOnboardingId,
                DateTime.Now,
                cancellationToken);

            await SyncOfferRequirementAsync(
                volunteerOnboardingId,
                cancellationToken);

            await SyncPolicyRequirementsAsync(
                volunteerOnboardingId,
                cancellationToken);

            await SyncDocumentRequirementsAsync(
                volunteerOnboardingId,
                adminId,
                cancellationToken);
        }

        // ============================================================
        // SYNC OFFER REQUIREMENT
        // ============================================================

        private async Task SyncOfferRequirementAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken)
        {
            var requirement =
                await GetRequirementAsync(
                    volunteerOnboardingId,
                    VolunteerOnboardingRequirementType.OfferAcceptance,
                    cancellationToken);

            if (requirement == null)
            {
                return;
            }

            var acceptedOffer =
                await _context.VolunteerOffers
                    .AnyAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId &&
                            x.Status ==
                            VolunteerOfferStatus.Accepted,
                        cancellationToken);

            if (acceptedOffer)
            {
                MarkRequirementCompleted(
                    requirement,
                    null);
            }
            else
            {
                MarkRequirementIncomplete(
                    requirement);
            }
        }

        // ============================================================
        // SYNC POLICY REQUIREMENTS
        // ============================================================

        private async Task SyncPolicyRequirementsAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken)
        {
            var policyRequirement =
                await GetRequirementAsync(
                    volunteerOnboardingId,
                    VolunteerOnboardingRequirementType.PolicyAcceptance,
                    cancellationToken);

            var codeOfConductRequirement =
                await GetRequirementAsync(
                    volunteerOnboardingId,
                    VolunteerOnboardingRequirementType.CodeOfConduct,
                    cancellationToken);

            var allPoliciesAccepted =
                await HasAcceptedAllRequiredPoliciesAsync(
                    volunteerOnboardingId,
                    cancellationToken);

            if (policyRequirement != null)
            {
                if (allPoliciesAccepted)
                {
                    MarkRequirementCompleted(
                        policyRequirement,
                        null);
                }
                else
                {
                    MarkRequirementIncomplete(
                        policyRequirement);
                }
            }

            if (codeOfConductRequirement != null)
            {
                if (allPoliciesAccepted)
                {
                    MarkRequirementCompleted(
                        codeOfConductRequirement,
                        null);
                }
                else
                {
                    MarkRequirementIncomplete(
                        codeOfConductRequirement);
                }
            }
        }

        // ============================================================
        // SYNC DOCUMENT REQUIREMENTS
        // ============================================================

        private async Task SyncDocumentRequirementsAsync(
            int volunteerOnboardingId,
            string? adminId,
            CancellationToken cancellationToken)
        {
            var requirements =
                await _context.VolunteerOnboardingRequirements
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId)
                    .ToListAsync(cancellationToken);

            var documents =
                await _context.VolunteerOnboardingDocuments
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId)
                    .ToListAsync(cancellationToken);

            var latestDocuments =
                documents
                    .GroupBy(x => x.DocumentType)
                    .Select(group =>
                        group
                            .OrderByDescending(
                                x => x.UploadedDate)
                            .ThenByDescending(
                                x => x.CreatedDate)
                            .First())
                    .ToList();

            // ========================================================
            // IDENTITY DOCUMENT + DIDIT
            // ========================================================

            var identityRequirement =
                requirements.FirstOrDefault(
                    x =>
                        x.RequirementType ==
                        VolunteerOnboardingRequirementType.IdentityVerification);

            if (identityRequirement != null)
            {
                var latestIdentityDocuments =
                    latestDocuments
                        .Where(x =>
                            IsIdentityDocument(
                                x.DocumentType))
                        .ToList();

                var identityDocumentApproved =
                    latestIdentityDocuments.Any() &&
                    latestIdentityDocuments.All(
                        x =>
                            x.Status ==
                            VolunteerOnboardingDocumentStatus.Approved);

                var identityVerification =
                    await _context.VolunteerIdentityVerifications
                        .AsNoTracking()
                        .Where(x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId)
                        .OrderByDescending(
                            x => x.ModifiedDate)
                        .ThenByDescending(
                            x => x.Id)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                var diditVerified =
                    identityVerification != null &&
                    identityVerification.Status ==
                    VolunteerIdentityVerificationStatus.Verified &&
                    identityVerification.DocumentVerified &&
                    identityVerification.LivenessVerified &&
                    identityVerification.FaceMatchVerified;

                Console.WriteLine(
                    $"[SyncDocumentRequirements] " +
                    $"IdentityDocumentApproved={identityDocumentApproved}, " +
                    $"DiditFullyVerified={diditVerified}");

                if (identityDocumentApproved &&
                    diditVerified)
                {
                    MarkRequirementCompleted(
                        identityRequirement,
                        adminId);
                }
                else
                {
                    MarkRequirementIncomplete(
                        identityRequirement);
                }
            }

            // ========================================================
            // QUALIFICATION VERIFICATION
            // ========================================================

            var qualificationRequirement =
                requirements.FirstOrDefault(
                    x =>
                        x.RequirementType ==
                        VolunteerOnboardingRequirementType.QualificationVerification);

            if (qualificationRequirement != null)
            {
                var latestQualificationDocuments =
                    latestDocuments
                        .Where(x =>
                            IsQualificationDocument(
                                x.DocumentType))
                        .ToList();

                var qualificationApproved =
                    latestQualificationDocuments.Any() &&
                    latestQualificationDocuments.All(
                        x =>
                            x.Status ==
                            VolunteerOnboardingDocumentStatus.Approved);

                if (qualificationApproved)
                {
                    MarkRequirementCompleted(
                        qualificationRequirement,
                        adminId);
                }
                else if (qualificationRequirement.IsRequired)
                {
                    MarkRequirementIncomplete(
                        qualificationRequirement);
                }
            }

            // ========================================================
            // SUPPORTING DOCUMENTS
            // ========================================================

            var documentUploadRequirement =
                requirements.FirstOrDefault(
                    x =>
                        x.RequirementType ==
                        VolunteerOnboardingRequirementType.DocumentUpload);

            if (documentUploadRequirement != null)
            {
                var supportingDocuments =
                    latestDocuments
                        .Where(x =>
                            !IsIdentityDocument(x.DocumentType) &&
                            !IsQualificationDocument(x.DocumentType) &&
                            x.DocumentType !=
                            VolunteerOnboardingDocumentType.VolunteerContract)
                        .ToList();

                var allSupportingDocumentsApproved =
                    supportingDocuments.Any() &&
                    supportingDocuments.All(
                        x =>
                            x.Status ==
                            VolunteerOnboardingDocumentStatus.Approved);

                if (allSupportingDocumentsApproved)
                {
                    MarkRequirementCompleted(
                        documentUploadRequirement,
                        adminId);
                }
                else if (documentUploadRequirement.IsRequired)
                {
                    MarkRequirementIncomplete(
                        documentUploadRequirement);
                }
            }

            // ========================================================
            // CONTRACT
            //
            // NEVER synchronize this automatically from generic
            // documents.
            //
            // VolunteerContractService owns this workflow.
            // ========================================================

            Console.WriteLine(
                "[SyncDocumentRequirements] Contract requirement left unchanged.");
        }

        // ============================================================
        // GET REQUIREMENT
        // ============================================================

        private async Task<VolunteerOnboardingRequirement?> GetRequirementAsync(
            int volunteerOnboardingId,
            VolunteerOnboardingRequirementType requirementType,
            CancellationToken cancellationToken)
        {
            return await _context.VolunteerOnboardingRequirements
                .Where(x =>
                    x.VolunteerOnboardingId ==
                    volunteerOnboardingId &&
                    x.RequirementType ==
                    requirementType)
                .OrderByDescending(x => x.IsCompleted)
                .ThenBy(x =>
                    x.VolunteerOnboardingRequirementId)
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        // ============================================================
        // MARK REQUIREMENT COMPLETE
        // ============================================================

        private static void MarkRequirementCompleted(
            VolunteerOnboardingRequirement requirement,
            string? completedByAdminId)
        {
            var now =
                DateTime.Now;

            requirement.IsCompleted =
                true;

            requirement.CompletedDate =
                now;

            if (!string.IsNullOrWhiteSpace(
                    completedByAdminId))
            {
                requirement.CompletedByAdminId =
                    completedByAdminId;
            }

            requirement.ModifiedDate =
                now;
        }

        // ============================================================
        // MARK REQUIREMENT INCOMPLETE
        // ============================================================

        private static void MarkRequirementIncomplete(
            VolunteerOnboardingRequirement requirement)
        {
            // --------------------------------------------------------
            // VERY IMPORTANT:
            //
            // Do not automatically undo a completed contract or other
            // manually completed requirement.
            // --------------------------------------------------------

            if (requirement.IsCompleted)
            {
                return;
            }

            requirement.IsCompleted =
                false;

            requirement.CompletedDate =
                null;

            requirement.CompletedByAdminId =
                null;

            requirement.ModifiedDate =
                DateTime.Now;
        }

        // ============================================================
        // DOCUMENT TYPE HELPERS
        // ============================================================

        private static bool IsIdentityDocument(
            VolunteerOnboardingDocumentType documentType)
        {
            var value =
                documentType
                    .ToString()
                    .Replace("_", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .ToLowerInvariant();

            return
                value.Contains("identity") ||
                value.Contains("passport") ||
                value.Contains("nationalid") ||
                value.Contains("idcard") ||
                value.Contains("identitydocument") ||
                value.Contains("driverlicense") ||
                value.Contains("driverslicense");
        }

        private static bool IsQualificationDocument(
            VolunteerOnboardingDocumentType documentType)
        {
            var value =
                documentType
                    .ToString()
                    .Replace("_", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .ToLowerInvariant();

            return
                value.Contains("qualification") ||
                value.Contains("certificate") ||
                value.Contains("degree") ||
                value.Contains("diploma") ||
                value.Contains("license") ||
                value.Contains("licence");
        }

        // ============================================================
        // SECURE TOKEN GENERATION
        // ============================================================

        private static string GenerateSecureToken()
        {
            var bytes =
                RandomNumberGenerator.GetBytes(48);

            return WebEncoders.Base64UrlEncode(
                bytes);
        }

        // ============================================================
        // TOKEN HASHING
        // ============================================================

        private static string HashToken(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException(
                    "Token cannot be empty.",
                    nameof(token));
            }

            var hash =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(token));

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        // ============================================================
        // CONSTANT-TIME TOKEN COMPARISON
        // ============================================================

        private static bool CryptographicEquals(
            string left,
            string right)
        {
            if (string.IsNullOrWhiteSpace(left) ||
                string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            var leftBytes =
                Encoding.UTF8.GetBytes(left);

            var rightBytes =
                Encoding.UTF8.GetBytes(right);

            return CryptographicOperations.FixedTimeEquals(
                leftBytes,
                rightBytes);
        }

        // ============================================================
        // OFFER REFERENCE
        // ============================================================

        private async Task<string> GenerateOfferReferenceAsync(
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var reference =
                    $"OFF-{DateTime.Now:yyMMdd}-" +
                    Random.Shared.Next(100000, 999999);

                var exists =
                    await _context.VolunteerOffers
                        .AnyAsync(
                            x =>
                                x.OfferReference ==
                                reference,
                            cancellationToken);

                if (!exists)
                {
                    return reference;
                }
            }

            throw new InvalidOperationException(
                "Unable to generate a unique volunteer offer reference.");
        }


      

        // ============================================================
        // DECLINE OFFER - INTERFACE COMPATIBLE VERSION
        // ============================================================

        public async Task<bool> DeclineOfferAsync(
            int volunteerOnboardingId,
            string token,
            string applicantId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (volunteerOnboardingId <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(applicantId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return false;
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                return false;
            }

            if (!string.Equals(
                    onboarding.ApplicantId,
                    applicantId,
                    StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    "The applicant does not belong to this onboarding record.");
            }

            var offer = await _context.VolunteerOffers
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId &&
                        x.Status ==
                        VolunteerOfferStatus.Pending)
                    .OrderByDescending(x => x.IssuedDate)
                    .ThenByDescending(x => x.VolunteerOfferId)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (offer == null)
            {
                return false;
            }

            var result =
                await DeclineOfferAsync(
                    offer.VolunteerOfferId,
                    token,
                    reason,
                    cancellationToken);

            return result.Success;
        }

        // ============================================================
        // ADD DOCUMENT - INTERFACE COMPATIBLE VERSION
        // ============================================================

        public async Task<
            SwiftCare.Models.VolunteerOnboarding.VolunteerOnboardingDocument>
            AddDocumentAsync(
                int volunteerOnboardingId,
                string applicantId,
                VolunteerOnboardingDocumentType documentType,
                string documentName,
                string filePath,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "A file path is required.",
                    nameof(filePath));
            }

            var fileName =
                Path.GetFileName(filePath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName =
                    documentName.Trim();
            }

            return await AddDocumentAsync(
                volunteerOnboardingId,
                applicantId,
                documentType,
                documentName,
                fileName,
                filePath,
                "application/octet-stream",
                1,
                null,
                cancellationToken);
        }

        // ============================================================
        // REVIEW DOCUMENT - INTERFACE COMPATIBLE VERSION
        // ============================================================

        public async Task<bool> ReviewDocumentAsync(
            int documentId,
            string adminId,
            VolunteerOnboardingDocumentStatus status,
            string? reviewNotes = null,
            CancellationToken cancellationToken = default)
        {
            return await ReviewDocumentAsync(
                documentId,
                status,
                adminId,
                reviewNotes,
                cancellationToken);
        }

        // ============================================================
        // FINAL ACTIVATION REQUIREMENTS
        //
        // IMPORTANT:
        // Only requirements that genuinely belong to the onboarding/
        // verification gate should block ReadyForActivation.
        //
        // Orientation and Training are operational/post-onboarding
        // requirements and MUST NOT block final verification or activation.
        // ============================================================

        private static readonly HashSet<VolunteerOnboardingRequirementType>
            FinalActivationBlockingRequirementTypes =
                new()
                {
            VolunteerOnboardingRequirementType.OfferAcceptance,
            VolunteerOnboardingRequirementType.PolicyAcceptance,
            VolunteerOnboardingRequirementType.CodeOfConduct,
            VolunteerOnboardingRequirementType.IdentityVerification,
            VolunteerOnboardingRequirementType.QualificationVerification,
            VolunteerOnboardingRequirementType.DocumentUpload,
            VolunteerOnboardingRequirementType.Contract,
            VolunteerOnboardingRequirementType.SupervisorAssignment
                };

        private static bool IsFinalActivationBlockingRequirement(
            VolunteerOnboardingRequirement requirement)
        {
            return requirement.IsRequired &&
                   FinalActivationBlockingRequirementTypes.Contains(
                       requirement.RequirementType);
        }


    }

    // ================================================================
    // OFFER ACTION RESULT
    // ================================================================

    public class VolunteerOfferActionResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool AllRequiredPoliciesAccepted { get; set; }

        public string? RedirectUrl { get; set; }

        public static VolunteerOfferActionResult Successful(
            string message,
            bool allRequiredPoliciesAccepted = false)
        {
            return new VolunteerOfferActionResult
            {
                Success = true,
                Message = message,
                AllRequiredPoliciesAccepted =
                    allRequiredPoliciesAccepted
            };
        }

        public static VolunteerOfferActionResult Failed(
            string message)
        {
            return new VolunteerOfferActionResult
            {
                Success = false,
                Message = message,
                AllRequiredPoliciesAccepted = false
            };
        }

    }
}