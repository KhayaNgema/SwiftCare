using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using System.Security.Cryptography;
using System.Text;

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
                           cancellationToken)
                       ?? existing;
            }

            if (string.IsNullOrWhiteSpace(application.ApplicantId))
            {
                throw new InvalidOperationException(
                    "The volunteer application does not have an applicant.");
            }

            var now = DateTime.Now;

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
                       cancellationToken)
                   ?? onboarding;
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

            foreach (var previousOffer in previousOffers)
            {
                previousOffer.Status =
                    VolunteerOfferStatus.Cancelled;

                previousOffer.ModifiedDate =
                    DateTime.Now;
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

            var now = DateTime.Now;

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
        // ACCEPT OFFER
        // ============================================================

        public async Task<bool> AcceptOfferAsync(
            int volunteerOfferId,
            CancellationToken cancellationToken = default)
        {
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

            if (now > offer.ExpiryDate)
            {
                offer.Status =
                    VolunteerOfferStatus.Expired;

                offer.ModifiedDate =
                    now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return false;
            }

            var onboarding =
                offer.VolunteerOnboarding;

            if (onboarding == null)
            {
                return false;
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

            return true;
        }


        // ============================================================
        // DECLINE OFFER
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

            if (documentType ==
                    VolunteerOnboardingDocumentType.VolunteerContract &&
                onboarding.Status !=
                    VolunteerOnboardingStatus.ContractPending)
            {
                throw new InvalidOperationException(
                    "The volunteer contract cannot be uploaded until the required onboarding documents have been verified.");
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
                document.RejectionReason =
                    null;
            }

            var now = DateTime.Now;

            document.Status =
                status;

            document.ReviewedDate =
                now;

            document.ReviewedByAdminId =
                adminId;

            document.ModifiedDate =
                now;

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
                }
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            await SyncDocumentRequirementsAsync(
                document.VolunteerOnboardingId,
                adminId,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            // --------------------------------------------------------
            // Only move to ContractPending when all required
            // pre-contract document requirements are complete.
            //
            // Identity verification includes Didit.
            // --------------------------------------------------------

            if (document.VolunteerOnboarding != null &&
                status ==
                    VolunteerOnboardingDocumentStatus.Approved)
            {
                var onboarding =
                    await _context.VolunteerOnboardings
                        .Include(x => x.Requirements)
                        .Include(x => x.Documents)
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                document.VolunteerOnboardingId,
                            cancellationToken);

                if (onboarding != null)
                {
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

                    var hasUnapprovedDocuments =
                        onboarding.Documents.Any(
                            x =>
                                x.Status !=
                                VolunteerOnboardingDocumentStatus.Approved);

                    Console.WriteLine(
                        $"[ReviewDocument] OnboardingId={onboarding.VolunteerOnboardingId}, " +
                        $"RequiredDocumentRequirementsComplete={allRequiredDocumentRequirementsComplete}, " +
                        $"HasUnapprovedDocuments={hasUnapprovedDocuments}");

                    if (allRequiredDocumentRequirementsComplete &&
                        !hasUnapprovedDocuments)
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
                            $"[ReviewDocument] Onboarding {onboarding.VolunteerOnboardingId} " +
                            $"moved to ContractPending.");
                    }
                }
            }

            return true;
        }


        // ============================================================
        // COMPLETE REQUIREMENT
        // ============================================================

        public async Task<bool> CompleteRequirementAsync(
            int volunteerOnboardingId,
            VolunteerOnboardingRequirementType requirementType,
            string? completedByAdminId = null,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine(
                $"[CompleteRequirement] START - " +
                $"OnboardingId={volunteerOnboardingId}, " +
                $"RequirementType={requirementType}");

            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                Console.WriteLine(
                    "[CompleteRequirement] Onboarding not found.");

                return false;
            }

            await EnsureDefaultRequirementsAsync(
                volunteerOnboardingId,
                DateTime.Now,
                cancellationToken);

            var requirement =
                await GetRequirementAsync(
                    volunteerOnboardingId,
                    requirementType,
                    cancellationToken);

            if (requirement == null)
            {
                Console.WriteLine(
                    "[CompleteRequirement] Requirement not found.");

                return false;
            }

            MarkRequirementCompleted(
                requirement,
                completedByAdminId);

            onboarding.ModifiedDate =
                DateTime.Now;

            if (!string.IsNullOrWhiteSpace(completedByAdminId))
            {
                onboarding.ModifiedBy =
                    completedByAdminId;
            }

            if (requirementType ==
                    VolunteerOnboardingRequirementType.Contract &&
                requirement.IsCompleted)
            {
                onboarding.Status =
                    VolunteerOnboardingStatus.AdminVerification;

                onboarding.CurrentStep =
                    5;

                Console.WriteLine(
                    $"[CompleteRequirement] Contract completed. " +
                    $"Onboarding {volunteerOnboardingId} moved to AdminVerification.");
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            Console.WriteLine(
                $"[CompleteRequirement] SUCCESS - " +
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

                // --------------------------------------------------------
                // ENSURE + DEDUPLICATE REQUIREMENTS
                // --------------------------------------------------------

                await EnsureDefaultRequirementsAsync(
                    volunteerOnboardingId,
                    DateTime.Now,
                    cancellationToken);

                // --------------------------------------------------------
                // SYNCHRONIZE AUTOMATIC REQUIREMENTS
                // --------------------------------------------------------

                await SyncAutomaticRequirementsAsync(
                    volunteerOnboardingId,
                    adminId,
                    cancellationToken);

                await _context.SaveChangesAsync(
                    cancellationToken);

                // --------------------------------------------------------
                // Reload requirements.
                // --------------------------------------------------------

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

                // --------------------------------------------------------
                // REQUIRED POLICIES
                // --------------------------------------------------------

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

                // --------------------------------------------------------
                // DIDIT IDENTITY VERIFICATION
                // --------------------------------------------------------

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
                        "[CompleteVerification] No Didit verification found.");

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

                // --------------------------------------------------------
                // REQUIREMENTS
                // --------------------------------------------------------

                // ========================================================
                // DOCUMENT-STAGE REQUIREMENTS
                // ========================================================
                //
                // At this point Didit has already passed.
                //
                // We now need to make sure the requirements that must be
                // completed BEFORE the contract are complete.
                //
                // The Contract requirement is deliberately excluded here.
                // It belongs to the NEXT onboarding stage.
                // ========================================================

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

                Console.WriteLine(
                    $"[CompleteVerification] Incomplete PRE-CONTRACT requirements=" +
                    $"{incompletePreContractRequirements.Count}");

                foreach (var requirement in incompletePreContractRequirements)
                {
                    Console.WriteLine(
                        $"[CompleteVerification] PRE-CONTRACT INCOMPLETE - " +
                        $"Id={requirement.VolunteerOnboardingRequirementId}, " +
                        $"Type={requirement.RequirementType}, " +
                        $"Title={requirement.Title}");
                }

                // ========================================================
                // STOP IF PRE-CONTRACT REQUIREMENTS ARE NOT COMPLETE
                // ========================================================

                if (incompletePreContractRequirements.Any())
                {
                    var names =
                        string.Join(
                            ", ",
                            incompletePreContractRequirements
                                .Select(x => x.Title));

                    Console.WriteLine(
                        $"[CompleteVerification] BLOCKED BEFORE CONTRACT - {names}");

                    throw new InvalidOperationException(
                        $"The following requirements must be completed before the Volunteer Service Agreement: {names}");
                }

                // ========================================================
                // PRE-CONTRACT REQUIREMENTS ARE COMPLETE
                // ========================================================
                //
                // Didit is verified.
                // Identity requirement is complete.
                // Supporting documents are complete.
                //
                // NOW move to ContractPending.
                //
                // DO NOT check the Contract requirement here.
                // DO NOT move to ReadyForActivation.
                // ========================================================

                var contractRequirement =
                    onboarding.Requirements
                        .FirstOrDefault(x =>
                            x.RequirementType ==
                            VolunteerOnboardingRequirementType.Contract);

                if (contractRequirement == null)
                {
                    throw new InvalidOperationException(
                        "The Volunteer Service Agreement requirement could not be found.");
                }

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
                        "[CompleteVerification] PRE-CONTRACT REQUIREMENTS COMPLETE.");

                    Console.WriteLine(
                        "[CompleteVerification] Didit verification complete.");

                    Console.WriteLine(
                        "[CompleteVerification] Contract requirement is still incomplete.");

                    Console.WriteLine(
                        "[CompleteVerification] Moving onboarding to ContractPending.");

                    Console.WriteLine(
                        $"[CompleteVerification] OnboardingId={volunteerOnboardingId}");

                    Console.WriteLine(
                        $"[CompleteVerification] Status={onboarding.Status}");

                    Console.WriteLine(
                        $"[CompleteVerification] Step={onboarding.CurrentStep}");

                    return true;
                }

                // --------------------------------------------------------
                // DOCUMENTS
                // --------------------------------------------------------

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

                // --------------------------------------------------------
                // FINAL STATUS
                // --------------------------------------------------------

                var now = DateTime.Now;

                onboarding.Status =
                    VolunteerOnboardingStatus.ReadyForActivation;

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

            var now = DateTime.Now;

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

            await _context.Entry(onboarding)
                .Collection(x => x.Requirements)
                .LoadAsync(cancellationToken);

            await _context.Entry(onboarding)
                .Collection(x => x.Documents)
                .LoadAsync(cancellationToken);

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
                        x.Status !=
                        VolunteerOnboardingDocumentStatus.Approved))
            {
                return false;
            }

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

            if (identityVerification == null ||
                identityVerification.Status !=
                    VolunteerIdentityVerificationStatus.Verified ||
                !identityVerification.DocumentVerified ||
                !identityVerification.LivenessVerified ||
                !identityVerification.FaceMatchVerified)
            {
                return false;
            }

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
        //
        // IMPORTANT:
        //
        // This method is now SAFE to call repeatedly.
        //
        // It:
        // 1. Creates missing requirement types.
        // 2. Detects duplicate requirement types.
        // 3. Keeps one canonical row.
        // 4. Preserves completion if ANY duplicate was completed.
        // 5. Removes duplicate rows.
        //
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

            Console.WriteLine(
                $"[EnsureRequirements] Existing requirements={requirements.Count}");

            // --------------------------------------------------------
            // Defaults
            // --------------------------------------------------------

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
                            "The Volunteer Service Agreement must be generated, reviewed, and completed before activation.",

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
                            VolunteerOnboardingRequirementType.Orientation,

                        Title =
                            "SwiftCare Orientation Completed",

                        Description =
                            "The volunteer must complete the required SwiftCare orientation before activation.",

                        IsRequired = false,
                        IsCompleted = false,
                        DisplayOrder = 8,

                        CreatedDate = now,
                        ModifiedDate = now
                    },

                    new()
                    {
                        VolunteerOnboardingId =
                            volunteerOnboardingId,

                        RequirementType =
                            VolunteerOnboardingRequirementType.RoleTraining,

                        Title =
                            "Role Training Completed",

                        Description =
                            "The volunteer must complete any training required for their assigned volunteer role.",

                        IsRequired = false,
                        IsCompleted = false,
                        DisplayOrder = 9,

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
            // Deduplicate existing requirements.
            // --------------------------------------------------------

            var duplicateGroups =
                requirements
                    .GroupBy(x => x.RequirementType)
                    .Where(g => g.Count() > 1)
                    .ToList();

            foreach (var group in duplicateGroups)
            {
                Console.WriteLine(
                    $"[EnsureRequirements] DUPLICATE FOUND - " +
                    $"Type={group.Key}, Count={group.Count()}");

                // Prefer a completed record.
                var canonical =
                    group
                        .OrderByDescending(x => x.IsCompleted)
                        .ThenBy(x => x.VolunteerOnboardingRequirementId)
                        .First();

                var duplicates =
                    group
                        .Where(x =>
                            x.VolunteerOnboardingRequirementId !=
                            canonical.VolunteerOnboardingRequirementId)
                        .ToList();

                // Preserve completion from any duplicate.
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

            // --------------------------------------------------------
            // Re-read canonical requirements after deduplication.
            // --------------------------------------------------------

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

            // --------------------------------------------------------
            // Create only genuinely missing requirement types.
            // --------------------------------------------------------

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
                Console.WriteLine(
                    $"[EnsureRequirements] Missing requirements=" +
                    $"{missingRequirements.Count}");

                foreach (var missing in missingRequirements)
                {
                    Console.WriteLine(
                        $"[EnsureRequirements] Creating requirement - " +
                        $"Type={missing.RequirementType}, " +
                        $"Title={missing.Title}");
                }

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
            Console.WriteLine(
                $"[SyncAutomaticRequirements] START - " +
                $"OnboardingId={volunteerOnboardingId}");

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

            Console.WriteLine(
                $"[SyncAutomaticRequirements] END - " +
                $"OnboardingId={volunteerOnboardingId}");
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

            Console.WriteLine(
                $"[SyncOfferRequirement] AcceptedOffer={acceptedOffer}");

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

            Console.WriteLine(
                $"[SyncPolicyRequirements] AllPoliciesAccepted={allPoliciesAccepted}");

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
            Console.WriteLine(
                $"[SyncDocumentRequirements] START - " +
                $"OnboardingId={volunteerOnboardingId}");

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

            Console.WriteLine(
                $"[SyncDocumentRequirements] Requirements={requirements.Count}, " +
                $"Documents={documents.Count}");

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

                Console.WriteLine(
                    $"[SyncDocumentRequirements] IdentityDocuments=" +
                    $"{latestIdentityDocuments.Count}");

                Console.WriteLine(
                    $"[SyncDocumentRequirements] IdentityDocumentApproved=" +
                    $"{identityDocumentApproved}");

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

                if (identityVerification != null)
                {
                    Console.WriteLine(
                        $"[SyncDocumentRequirements] DiditId=" +
                        $"{identityVerification.Id}");

                    Console.WriteLine(
                        $"[SyncDocumentRequirements] DiditStatus=" +
                        $"{identityVerification.Status}");

                    Console.WriteLine(
                        $"[SyncDocumentRequirements] DocumentVerified=" +
                        $"{identityVerification.DocumentVerified}");

                    Console.WriteLine(
                        $"[SyncDocumentRequirements] LivenessVerified=" +
                        $"{identityVerification.LivenessVerified}");

                    Console.WriteLine(
                        $"[SyncDocumentRequirements] FaceMatchVerified=" +
                        $"{identityVerification.FaceMatchVerified}");
                }
                else
                {
                    Console.WriteLine(
                        "[SyncDocumentRequirements] No Didit verification record.");
                }

                Console.WriteLine(
                    $"[SyncDocumentRequirements] DiditFullyVerified=" +
                    $"{diditVerified}");

                if (identityDocumentApproved &&
                    diditVerified)
                {
                    MarkRequirementCompleted(
                        identityRequirement,
                        adminId);

                    Console.WriteLine(
                        "[SyncDocumentRequirements] " +
                        "Identity Verification requirement COMPLETED.");
                }
                else
                {
                    MarkRequirementIncomplete(
                        identityRequirement);

                    Console.WriteLine(
                        "[SyncDocumentRequirements] " +
                        "Identity Verification requirement INCOMPLETE.");
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

                Console.WriteLine(
                    $"[SyncDocumentRequirements] " +
                    $"QualificationApproved={qualificationApproved}");

                // Qualification is optional by default.
                //
                // If qualification documents exist, they must all
                // be approved.
                //
                // If none exist and requirement is optional, do not
                // mark it as required/incomplete.

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

                Console.WriteLine(
                    $"[SyncDocumentRequirements] SupportingDocuments=" +
                    $"{supportingDocuments.Count}");

                var allSupportingDocumentsApproved =
                    supportingDocuments.Any() &&
                    supportingDocuments.All(
                        x =>
                            x.Status ==
                            VolunteerOnboardingDocumentStatus.Approved);

                Console.WriteLine(
                    $"[SyncDocumentRequirements] " +
                    $"AllSupportingDocumentsApproved=" +
                    $"{allSupportingDocumentsApproved}");

                if (allSupportingDocumentsApproved)
                {
                    MarkRequirementCompleted(
                        documentUploadRequirement,
                        adminId);
                }
                else
                {
                    MarkRequirementIncomplete(
                        documentUploadRequirement);
                }
            }

            // ========================================================
            // CONTRACT
            // ========================================================
            //
            // Contract is deliberately NOT synchronized here.
            //
            // Your existing contract workflow is responsible for:
            //
            // Generate contract
            //       ↓
            // Applicant reviews
            //       ↓
            // Applicant signs/accepts
            //       ↓
            // CompleteRequirementAsync(
            //     RequirementType.Contract)
            //
            // ========================================================

            Console.WriteLine(
                "[SyncDocumentRequirements] " +
                "Contract requirement intentionally left unchanged.");

            Console.WriteLine(
                $"[SyncDocumentRequirements] END - " +
                $"OnboardingId={volunteerOnboardingId}");
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
            var now = DateTime.Now;

            requirement.IsCompleted =
                true;

            requirement.CompletedDate =
                now;

            requirement.CompletedByAdminId =
                string.IsNullOrWhiteSpace(completedByAdminId)
                    ? requirement.CompletedByAdminId
                    : completedByAdminId;

            requirement.ModifiedDate =
                now;
        }


        // ============================================================
        // MARK REQUIREMENT INCOMPLETE
        // ============================================================

        private static void MarkRequirementIncomplete(
            VolunteerOnboardingRequirement requirement)
        {
            // Do not touch already-completed requirements unnecessarily.
            //
            // This protects manually completed requirements such as
            // the Contract requirement from unrelated synchronization.
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

            return WebEncoders.Base64UrlEncode(bytes);
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
    }
}