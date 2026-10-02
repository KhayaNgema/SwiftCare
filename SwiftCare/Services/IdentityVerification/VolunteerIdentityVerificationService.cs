using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services.VolunteerOnboarding;

namespace SwiftCare.Services.IdentityVerification
{
    public class VolunteerIdentityVerificationService
        : IVolunteerIdentityVerificationService
    {
        private readonly SwiftCareDbContext _context;
        private readonly IDiditService _diditService;
        private readonly ILogger<VolunteerIdentityVerificationService> _logger;
        private readonly IVolunteerOnboardingService _volunteerOnboardingService;

        public VolunteerIdentityVerificationService(
            SwiftCareDbContext context,
            IVolunteerOnboardingService volunteerOnboardingService,
            IDiditService diditService,
            ILogger<VolunteerIdentityVerificationService> logger)
        {
            _context = context;
            _diditService = diditService;
            _logger = logger;
            _volunteerOnboardingService = volunteerOnboardingService;
        }

        // =============================================================
        // START IDENTITY VERIFICATION
        // =============================================================

        public async Task<VolunteerIdentityVerification>
            StartIdentityVerificationAsync(
                int onboardingId,
                int identityDocumentId,
                CancellationToken cancellationToken = default)
        {
            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Applicant)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                throw new InvalidOperationException(
                    "Volunteer onboarding record could not be found.");
            }

            if (onboarding.Applicant == null)
            {
                throw new InvalidOperationException(
                    "The applicant associated with this onboarding could not be found.");
            }

            var applicant =
                onboarding.Applicant;

            if (string.IsNullOrWhiteSpace(applicant.Email))
            {
                throw new InvalidOperationException(
                    "The applicant does not have a valid email address.");
            }

            var document =
                await _context.VolunteerOnboardingDocuments
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingDocumentId ==
                                identityDocumentId &&
                            x.VolunteerOnboardingId ==
                                onboardingId,
                        cancellationToken);

            if (document == null)
            {
                throw new InvalidOperationException(
                    "The selected identity document could not be found.");
            }

            if (document.DocumentType !=
                VolunteerOnboardingDocumentType.IdentityDocument)
            {
                throw new InvalidOperationException(
                    "The selected document is not an identity document.");
            }

            var verification =
                await _context.VolunteerIdentityVerifications
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                                onboardingId &&
                            x.IdentityDocumentId ==
                                identityDocumentId &&
                            x.Provider == "Didit",
                        cancellationToken);

            // =========================================================
            // ALREADY VERIFIED
            // =========================================================

            if (verification != null &&
                verification.Status ==
                    VolunteerIdentityVerificationStatus.Verified)
            {
                _logger.LogInformation(
                    "Identity verification {VerificationId} is already verified. " +
                    "No new Didit session will be created.",
                    verification.Id);

                return verification;
            }

            // =========================================================
            // EXISTING ACTIVE SESSION
            // =========================================================

            if (verification != null &&
                !string.IsNullOrWhiteSpace(
                    verification.ProviderSessionId) &&
                !string.IsNullOrWhiteSpace(
                    verification.VerificationUrl) &&
                (
                    verification.Status ==
                        VolunteerIdentityVerificationStatus.InProgress ||

                    verification.Status ==
                        VolunteerIdentityVerificationStatus.AwaitingUser ||

                    verification.Status ==
                        VolunteerIdentityVerificationStatus.FaceVerificationInProgress
                ))
            {
                var applicantName =
                    $"{applicant.FirstName} {applicant.LastName}"
                        .Trim();

                await _diditService
                    .ResendVerificationEmailAsync(
                        applicant.Email,
                        applicantName,
                        verification.VerificationUrl!,
                        verification.ProviderSessionId!,
                        cancellationToken);

                verification.ModifiedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return verification;
            }

            // =========================================================
            // CREATE LOCAL VERIFICATION
            // =========================================================

            if (verification == null)
            {
                verification =
                    new VolunteerIdentityVerification
                    {
                        VolunteerOnboardingId =
                            onboardingId,

                        VolunteerId =
                            applicant.Id,

                        IdentityDocumentId =
                            identityDocumentId,

                        Provider =
                            "Didit",

                        Status =
                            VolunteerIdentityVerificationStatus.InProgress,

                        DocumentVerified =
                            false,

                        LivenessVerified =
                            false,

                        FaceMatchVerified =
                            false,

                        FaceIdentityEnrolled =
                            false,

                        VerificationStartedDate =
                            DateTime.Now,

                        CreatedDate =
                            DateTime.Now,

                        ModifiedDate =
                            DateTime.Now
                    };

                _context.VolunteerIdentityVerifications.Add(
                    verification);

                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            else
            {
                // =====================================================
                // RESET PREVIOUS FAILED / EXPIRED SESSION
                // =====================================================

                verification.Status =
                    VolunteerIdentityVerificationStatus.InProgress;

                verification.DocumentVerified =
                    false;

                verification.LivenessVerified =
                    false;

                verification.FaceMatchVerified =
                    false;

                verification.FaceIdentityEnrolled =
                    false;

                verification.FailureReason =
                    null;

                verification.ReviewNotes =
                    null;

                verification.VerifiedDate =
                    null;

                verification.ProviderSessionId =
                    null;

                verification.ProviderVerificationId =
                    null;

                verification.ProviderPersonId =
                    null;

                verification.VerificationUrl =
                    null;

                verification.VerificationStartedDate =
                    DateTime.Now;

                verification.ModifiedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            // =========================================================
            // IMPORTANT
            //
            // This vendor_data identifies the SwiftCare identity.
            //
            // Didit Face Search uses the verified identity associated
            // with this session. Do NOT use a temporary login vendor
            // reference here.
            // =========================================================

            var referenceId =
                $"SWIFTCARE-ID-{verification.Id}";

            var diditRequest =
                new DiditVerificationSessionRequest
                {
                    ReferenceId =
                        referenceId,

                    FirstName =
                        applicant.FirstName ??
                        string.Empty,

                    LastName =
                        applicant.LastName ??
                        string.Empty,

                    Email =
                        applicant.Email,

                    DateOfBirth =
                        applicant.DateOfBirth,

                    CountryCode =
                        "ZAF"
                };

            _logger.LogInformation(
                "Creating Didit biometric enrollment verification. " +
                "VerificationId={VerificationId}, " +
                "VendorData={VendorData}.",
                verification.Id,
                referenceId);

            var diditResult =
                await _diditService
                    .CreateVerificationSessionAsync(
                        diditRequest,
                        cancellationToken);

            if (!diditResult.Success)
            {
                verification.Status =
                    VolunteerIdentityVerificationStatus.Failed;

                verification.FailureReason =
                    diditResult.ErrorMessage ??
                    "Didit verification session could not be created.";

                verification.ModifiedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return verification;
            }

            if (!string.IsNullOrWhiteSpace(
                    diditResult.SessionId))
            {
                verification.ProviderSessionId =
                    diditResult.SessionId;
            }

            if (!string.IsNullOrWhiteSpace(
                    diditResult.VerificationId))
            {
                verification.ProviderVerificationId =
                    diditResult.VerificationId;
            }

            verification.VerificationUrl =
                diditResult.VerificationUrl;

            verification.Status =
                VolunteerIdentityVerificationStatus.AwaitingUser;

            verification.VerificationStartedDate =
                verification.VerificationStartedDate ??
                DateTime.Now;

            verification.ModifiedDate =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);

            return verification;
        }

        // =============================================================
        // GET CURRENT VERIFICATION
        // =============================================================

        public async Task<VolunteerIdentityVerification?>
            GetVerificationAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context
                .VolunteerIdentityVerifications

                .Include(x =>
                    x.IdentityDocument)

                .Include(x =>
                    x.VolunteerOnboarding)

                .Include(x =>
                    x.Volunteer)

                .Where(x =>
                    x.VolunteerOnboardingId ==
                    onboardingId)

                .OrderByDescending(x =>
                    x.CreatedDate)

                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        // =============================================================
        // PROCESS DIDIT RESULT
        // =============================================================

        // =============================================================
        // PROCESS DIDIT RESULT
        // =============================================================

        public async Task ProcessDiditResultAsync(
            int verificationId,
            DiditVerificationResult result,
            CancellationToken cancellationToken = default)
        {
            if (result == null)
            {
                throw new ArgumentNullException(
                    nameof(result));
            }

            var verification =
                await _context
                    .VolunteerIdentityVerifications
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            verificationId,
                        cancellationToken);

            if (verification == null)
            {
                throw new InvalidOperationException(
                    $"Identity verification {verificationId} was not found.");
            }

            // =========================================================
            // UPDATE DIDIT IDENTIFIERS
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    result.SessionId))
            {
                verification.ProviderSessionId =
                    result.SessionId;
            }

            if (!string.IsNullOrWhiteSpace(
                    result.VerificationId))
            {
                verification.ProviderVerificationId =
                    result.VerificationId;
            }

            if (!string.IsNullOrWhiteSpace(
                    result.PersonId))
            {
                verification.ProviderPersonId =
                    result.PersonId;
            }

            // =========================================================
            // UPDATE VERIFICATION FLAGS
            // =========================================================

            verification.DocumentVerified =
                result.DocumentVerified;

            verification.LivenessVerified =
                result.LivenessVerified;

            verification.FaceMatchVerified =
                result.FaceMatchVerified;

            verification.FailureReason =
                result.FailureReason;

            // =========================================================
            // UPDATE STATUS
            // =========================================================

            switch (result.Status)
            {
                case "Approved":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Verified;

                    verification.VerifiedDate =
                        DateTime.Now;

                    verification.FaceIdentityEnrolled =
                        verification.DocumentVerified &&
                        verification.LivenessVerified &&
                        verification.FaceMatchVerified;

                    break;

                case "Declined":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Rejected;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                case "In Review":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                case "In Progress":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.InProgress;

                    break;

                case "Resubmitted":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.AwaitingUser;

                    verification.DocumentVerified =
                        false;

                    verification.LivenessVerified =
                        false;

                    verification.FaceMatchVerified =
                        false;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    verification.FailureReason =
                        null;

                    break;

                case "Abandoned":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Cancelled;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                case "Expired":

                case "KYC Expired":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Expired;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                default:

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    break;
            }

            verification.ModifiedDate =
                DateTime.Now;

            // =========================================================
            // SAVE DIDIT RESULT FIRST
            // =========================================================

            await _context.SaveChangesAsync(
                cancellationToken);

            // =========================================================
            // SYNC ONBOARDING REQUIREMENTS
            //
            // This allows SyncDocumentRequirementsAsync() to evaluate:
            //
            // 1. Identity document approved by admin
            // 2. Didit status = Verified
            // 3. DocumentVerified = true
            // 4. LivenessVerified = true
            // 5. FaceMatchVerified = true
            // 6. FaceIdentityEnrolled = true
            //
            // Only when ALL required conditions are true will the
            // IdentityVerification requirement be marked completed.
            // =========================================================

            await _volunteerOnboardingService
                .SyncDocumentRequirementsAsync(
                    verification.VolunteerOnboardingId,
                    null,
                    cancellationToken);

            // =========================================================
            // SAVE REQUIREMENT UPDATE
            // =========================================================

            await _context.SaveChangesAsync(
                cancellationToken);

            // =========================================================
            // LOG
            // =========================================================

            _logger.LogInformation(
                "Identity verification {VerificationId} updated. " +
                "Status={Status}, " +
                "Document={Document}, " +
                "Liveness={Liveness}, " +
                "FaceMatch={FaceMatch}, " +
                "DiditFaceSearchEnrollment={Enrolled}. " +
                "Onboarding requirements synchronized.",
                verification.Id,
                verification.Status,
                verification.DocumentVerified,
                verification.LivenessVerified,
                verification.FaceMatchVerified,
                verification.FaceIdentityEnrolled);
        }
    }
}