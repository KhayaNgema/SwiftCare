using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public class VolunteerOnboardingService : IVolunteerOnboardingService
    {
        private readonly SwiftCareDbContext _context;

        public VolunteerOnboardingService(
            SwiftCareDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CREATE ONBOARDING
        // =========================================================

        public async Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding> CreateOnboardingAsync(
            int volunteerApplicationId,
            string? adminId = null,
            CancellationToken cancellationToken = default)
        {
            var application = await _context.VolunteerApplications
                .Include(x => x.VolunteerOpportunity)
                .FirstOrDefaultAsync(
                    x => x.VolunteerApplicationId == volunteerApplicationId,
                    cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException(
                    "The volunteer application could not be found.");
            }

            var existing = await _context.VolunteerOnboardings
                .FirstOrDefaultAsync(
                    x => x.VolunteerApplicationId == volunteerApplicationId,
                    cancellationToken);

            if (existing != null)
            {
                return existing;
            }

            if (string.IsNullOrWhiteSpace(application.ApplicantId))
            {
                throw new InvalidOperationException(
                    "The volunteer application does not have an applicant.");
            }

            var onboarding =
                new SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding
                {
                    VolunteerApplicationId =
                        application.VolunteerApplicationId,

                    ApplicantId =
                        application.ApplicantId,

                    Status =
                        VolunteerOnboardingStatus.OfferPending,

                    CurrentStep = 1,

                    CreatedDate =
                        DateTime.Now,

                    ModifiedDate =
                        DateTime.Now,

                    CreatedBy =
                        adminId
                };

            _context.VolunteerOnboardings.Add(onboarding);

            await _context.SaveChangesAsync(cancellationToken);

            return onboarding;
        }

        // =========================================================
        // GET ONBOARDING
        // =========================================================

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

        // =========================================================
        // GET ONBOARDING BY APPLICATION
        // =========================================================

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
                .FirstOrDefaultAsync(
                    x => x.VolunteerApplicationId == volunteerApplicationId,
                    cancellationToken);
        }

        // =========================================================
        // GET ONBOARDING BY APPLICANT
        // =========================================================

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

        // =========================================================
        // CREATE OFFER
        // =========================================================

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

            // =====================================================
            // CANCEL PREVIOUS PENDING OFFERS
            // =====================================================

            var previousOffers =
                await _context.VolunteerOffers
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                            volunteerOnboardingId &&
                        x.Status ==
                            VolunteerOfferStatus.Pending)
                    .ToListAsync(cancellationToken);

            foreach (var previousOffer in previousOffers)
            {
                previousOffer.Status =
                    VolunteerOfferStatus.Cancelled;

                previousOffer.ModifiedDate =
                    DateTime.Now;
            }

            // =====================================================
            // GENERATE ONE RAW TOKEN
            // =====================================================

            var rawAcceptanceToken =
                GenerateSecureToken();

            // =====================================================
            // HASH THAT EXACT SAME TOKEN
            // =====================================================

            var tokenHash =
                HashToken(rawAcceptanceToken);

            // =====================================================
            // GENERATE OFFER REFERENCE
            // =====================================================

            var offerReference =
                await GenerateOfferReferenceAsync(
                    cancellationToken);

            // =====================================================
            // CREATE OFFER
            // =====================================================

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
                        DateTime.Now,

                    ExpiryDate =
                        expiryDate,

                    Status =
                        VolunteerOfferStatus.Pending,

                    // IMPORTANT:
                    // Only the HASH is stored in the database.
                    AcceptanceTokenHash =
                        tokenHash,

                    TokenExpiryDate =
                        expiryDate,

                    CreatedByAdminId =
                        adminId,

                    CreatedDate =
                        DateTime.Now,

                    ModifiedDate =
                        DateTime.Now
                };

            _context.VolunteerOffers.Add(offer);

            // =====================================================
            // UPDATE ONBOARDING
            // =====================================================

            onboarding.OfferSentDate =
                DateTime.Now;

            onboarding.Status =
                VolunteerOnboardingStatus.OfferPending;

            onboarding.CurrentStep = 1;

            onboarding.ModifiedDate =
                DateTime.Now;

            onboarding.ModifiedBy =
                adminId;

            // =====================================================
            // SAVE
            // =====================================================

            await _context.SaveChangesAsync(
                cancellationToken);

            // =====================================================
            // RETURN BOTH:
            //
            // 1. Offer
            // 2. RAW TOKEN
            //
            // The raw token is NEVER stored in the database.
            // =====================================================

            return new VolunteerOfferCreationResult
            {
                Offer =
                    offer,

                RawAcceptanceToken =
                    rawAcceptanceToken
            };
        }

        // =========================================================
        // ACCEPT OFFER
        // =========================================================

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

        // =========================================================
        // DECLINE OFFER
        // =========================================================

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

            offer.Status =
                VolunteerOfferStatus.Declined;

            offer.DeclinedDate =
                DateTime.Now;

            offer.DeclineReason =
                declineReason;

            offer.ModifiedDate =
                DateTime.Now;

            var onboarding =
                offer.VolunteerOnboarding;

            if (onboarding != null)
            {
                onboarding.Status =
                    VolunteerOnboardingStatus.OfferDeclined;

                onboarding.OfferDeclinedDate =
                    DateTime.Now;

                onboarding.OfferDeclineReason =
                    declineReason;

                onboarding.ModifiedDate =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // GET ACTIVE OFFER
        // =========================================================

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
                            DateTime.Now,
                    cancellationToken);
        }

        // =========================================================
        // ACCEPT POLICY
        // =========================================================

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

            if (policy == null)
            {
                return false;
            }

            if (!policy.IsRequired)
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

            if (existing != null)
            {
                existing.Accepted = true;

                existing.AcceptedDate =
                    DateTime.Now;

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

                        Accepted = true,

                        AcceptedDate =
                            DateTime.Now,

                        PolicyVersion =
                            policy.Version,

                        IpAddress =
                            ipAddress,

                        UserAgent =
                            userAgent,

                        CreatedDate =
                            DateTime.Now
                    });
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            if (await HasAcceptedAllRequiredPoliciesAsync(
                    volunteerOnboardingId,
                    cancellationToken))
            {
                onboarding.Status =
                    VolunteerOnboardingStatus.ContractPending;

                onboarding.CurrentStep = 3;

                onboarding.PoliciesAcceptedDate =
                    DateTime.Now;

                onboarding.ModifiedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            return true;
        }

        // =========================================================
        // CHECK REQUIRED POLICIES
        // =========================================================

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

        // =========================================================
        // ADD DOCUMENT
        // =========================================================

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
                    DateTime.Now;
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
                        DateTime.Now,

                    FileHash =
                        fileHash,

                    CreatedDate =
                        DateTime.Now,

                    ModifiedDate =
                        DateTime.Now
                };

            _context.VolunteerOnboardingDocuments.Add(document);

            if (documentType ==
                VolunteerOnboardingDocumentType.VolunteerContract)
            {
                onboarding.ContractSubmittedDate =
                    DateTime.Now;
            }

            onboarding.Status =
                VolunteerOnboardingStatus.DocumentsPending;

            onboarding.CurrentStep = 4;

            onboarding.ModifiedDate =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);

            return document;
        }

        // =========================================================
        // REVIEW DOCUMENT
        // =========================================================

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
                return false;
            }

            if (status ==
                    VolunteerOnboardingDocumentStatus.Rejected ||
                status ==
                    VolunteerOnboardingDocumentStatus.ResubmissionRequired)
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    throw new ArgumentException(
                        "A reason is required when rejecting or requesting resubmission.");
                }

                document.RejectionReason =
                    rejectionReason.Trim();
            }
            else
            {
                document.RejectionReason = null;
            }

            document.Status =
                status;

            document.ReviewedDate =
                DateTime.Now;

            document.ReviewedByAdminId =
                adminId;

            document.ModifiedDate =
                DateTime.Now;

            if (document.VolunteerOnboarding != null &&
                (status ==
                    VolunteerOnboardingDocumentStatus.Rejected ||
                 status ==
                    VolunteerOnboardingDocumentStatus.ResubmissionRequired))
            {
                document.VolunteerOnboarding.Status =
                    VolunteerOnboardingStatus.RejectedForCorrection;

                document.VolunteerOnboarding.ModifiedDate =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // START VERIFICATION
        // =========================================================

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

            onboarding.Status =
                VolunteerOnboardingStatus.AdminVerification;

            onboarding.CurrentStep = 5;

            onboarding.VerificationStartedDate ??=
                DateTime.Now;

            onboarding.ModifiedDate =
                DateTime.Now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // COMPLETE VERIFICATION
        // =========================================================

        public async Task<bool> CompleteVerificationAsync(
            int volunteerOnboardingId,
            string adminId,
            string? verificationNotes = null,
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
                    .Include(x => x.Documents)
                    .Include(x => x.Requirements)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                return false;
            }

            if (!await HasAcceptedAllRequiredPoliciesAsync(
                    volunteerOnboardingId,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    "The applicant has not accepted all required policies.");
            }

            var incompleteRequirements =
                onboarding.Requirements
                    .Where(x =>
                        x.IsRequired &&
                        !x.IsCompleted)
                    .ToList();

            if (incompleteRequirements.Any())
            {
                throw new InvalidOperationException(
                    "There are still incomplete onboarding requirements.");
            }

            var requiredDocumentRequirements =
                onboarding.Requirements
                    .Where(x =>
                        x.IsRequired &&
                        (
                            x.RequirementType ==
                                VolunteerOnboardingRequirementType.DocumentUpload ||

                            x.RequirementType ==
                                VolunteerOnboardingRequirementType.IdentityVerification ||

                            x.RequirementType ==
                                VolunteerOnboardingRequirementType.QualificationVerification ||

                            x.RequirementType ==
                                VolunteerOnboardingRequirementType.Contract
                        ))
                    .ToList();

            if (requiredDocumentRequirements.Any())
            {
                var hasRejectedDocuments =
                    onboarding.Documents.Any(x =>
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.Rejected ||

                        x.Status ==
                            VolunteerOnboardingDocumentStatus.ResubmissionRequired);

                if (hasRejectedDocuments)
                {
                    throw new InvalidOperationException(
                        "One or more onboarding documents require correction.");
                }

                var hasPendingDocuments =
                    onboarding.Documents.Any(x =>
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.PendingUpload ||

                        x.Status ==
                            VolunteerOnboardingDocumentStatus.Uploaded ||

                        x.Status ==
                            VolunteerOnboardingDocumentStatus.UnderReview);

                if (hasPendingDocuments)
                {
                    throw new InvalidOperationException(
                        "One or more onboarding documents have not been approved.");
                }
            }

            onboarding.Status =
                VolunteerOnboardingStatus.ReadyForActivation;

            onboarding.CurrentStep = 6;

            onboarding.VerifiedDate =
                DateTime.Now;

            onboarding.VerifiedByAdminId =
                adminId;

            onboarding.VerificationNotes =
                string.IsNullOrWhiteSpace(verificationNotes)
                    ? null
                    : verificationNotes.Trim();

            onboarding.ModifiedDate =
                DateTime.Now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // REJECT FOR CORRECTION
        // =========================================================

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

            onboarding.Status =
                VolunteerOnboardingStatus.RejectedForCorrection;

            onboarding.VerificationNotes =
                reason.Trim();

            onboarding.ModifiedDate =
                DateTime.Now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // READY FOR ACTIVATION
        // =========================================================

        public async Task<bool> IsReadyForActivationAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
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
                return false;
            }

            if (onboarding.Status !=
                VolunteerOnboardingStatus.ReadyForActivation)
            {
                return false;
            }

            if (!await HasAcceptedAllRequiredPoliciesAsync(
                    volunteerOnboardingId,
                    cancellationToken))
            {
                return false;
            }

            if (onboarding.Requirements.Any(
                    x =>
                        x.IsRequired &&
                        !x.IsCompleted))
            {
                return false;
            }

            if (onboarding.Documents.Any(
                    x =>
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.Rejected ||

                        x.Status ==
                            VolunteerOnboardingDocumentStatus.ResubmissionRequired))
            {
                return false;
            }

            return true;
        }

        // =========================================================
        // CURRENT STATUS
        // =========================================================

        public async Task<VolunteerOnboardingStatus>
            GetCurrentStatusAsync(
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

        // =========================================================
        // SECURE TOKEN GENERATION
        // =========================================================

        private static string GenerateSecureToken()
        {
            var bytes =
                RandomNumberGenerator.GetBytes(48);

            // URL-safe Base64.
            //
            // This avoids:
            // +
            // /
            // =
            //
            // appearing in the token.
            return WebEncoders.Base64UrlEncode(bytes);
        }

        // =========================================================
        // TOKEN HASHING
        // =========================================================

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

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        // =========================================================
        // OFFER REFERENCE
        // =========================================================

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
    }
}