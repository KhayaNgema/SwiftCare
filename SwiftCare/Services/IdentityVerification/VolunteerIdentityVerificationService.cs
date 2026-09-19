using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services.IdentityVerification;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SwiftCare.Services.IdentityVerification
{
    public class VolunteerIdentityVerificationService
        : IVolunteerIdentityVerificationService
    {
        private readonly SwiftCareDbContext _context;
        private readonly IDiditService _diditService;
        private readonly ILogger<VolunteerIdentityVerificationService> _logger;

        public VolunteerIdentityVerificationService(
            SwiftCareDbContext context,
            IDiditService diditService,
            ILogger<VolunteerIdentityVerificationService> logger)
        {
            _context = context;
            _diditService = diditService;
            _logger = logger;
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
            // ---------------------------------------------------------
            // LOAD ONBOARDING + APPLICANT
            // ---------------------------------------------------------

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Applicant)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId == onboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                throw new InvalidOperationException(
                    "Volunteer onboarding record could not be found.");
            }

            // ---------------------------------------------------------
            // VALIDATE APPLICANT
            // ---------------------------------------------------------

            if (onboarding.Applicant == null)
            {
                throw new InvalidOperationException(
                    "The applicant associated with this onboarding could not be found.");
            }

            var applicant = onboarding.Applicant;

            if (string.IsNullOrWhiteSpace(applicant.Email))
            {
                throw new InvalidOperationException(
                    "The applicant does not have a valid email address. " +
                    "A Didit verification invitation cannot be sent.");
            }

            // ---------------------------------------------------------
            // LOAD SELECTED DOCUMENT
            // ---------------------------------------------------------

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

            // ---------------------------------------------------------
            // ONLY IDENTITY DOCUMENTS MAY BE USED
            // ---------------------------------------------------------

            if (document.DocumentType !=
                VolunteerOnboardingDocumentType.IdentityDocument)
            {
                throw new InvalidOperationException(
                    "The selected document is not an identity document. " +
                    "Only documents marked as IdentityDocument can be used for Didit identity verification.");
            }

            // ---------------------------------------------------------
            // FIND EXISTING DIDIT VERIFICATION
            // ---------------------------------------------------------

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
            //
            // Do NOT send another email.
            //
            // =========================================================

            if (verification != null &&
                verification.Status ==
                    VolunteerIdentityVerificationStatus.Verified)
            {
                _logger.LogInformation(
                    "Identity verification {VerificationId} is already verified. " +
                    "No new Didit session or email will be sent.",
                    verification.Id);

                return verification;
            }

            // =========================================================
            // EXISTING ACTIVE DIDIT SESSION
            // =========================================================
            //
            // Reuse the existing Didit session.
            //
            // BUT:
            //
            // We now resend the SwiftCare email.
            //
            // This is the important fix.
            //
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
                _logger.LogInformation(
                    "Existing active Didit session {SessionId} found for " +
                    "SwiftCare verification {VerificationId}. " +
                    "The existing session will be reused and the SwiftCare " +
                    "verification email will be sent again.",
                    verification.ProviderSessionId,
                    verification.Id);

                var applicantName =
                    $"{applicant.FirstName} {applicant.LastName}"
                        .Trim();

                var emailSent =
                    await _diditService.ResendVerificationEmailAsync(
                        applicant.Email,
                        applicantName,
                        verification.VerificationUrl!,
                        verification.ProviderSessionId!,
                        cancellationToken);

                if (!emailSent)
                {
                    _logger.LogWarning(
                        "Existing Didit session {SessionId} was reused, " +
                        "but SwiftCare could not resend the verification email " +
                        "to applicant {Email}.",
                        verification.ProviderSessionId,
                        applicant.Email);
                }
                else
                {
                    _logger.LogInformation(
                        "Existing Didit session {SessionId} reused and " +
                        "SwiftCare verification email resent successfully " +
                        "to {Email}.",
                        verification.ProviderSessionId,
                        applicant.Email);
                }

                verification.ModifiedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return verification;
            }

            // =========================================================
            // CREATE OR RESET LOCAL VERIFICATION
            // =========================================================

            if (verification == null)
            {
                // -----------------------------------------------------
                // CREATE NEW LOCAL VERIFICATION
                // -----------------------------------------------------

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

                _context.VolunteerIdentityVerifications
                    .Add(verification);

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Created new SwiftCare identity verification record {VerificationId} " +
                    "for onboarding {OnboardingId}.",
                    verification.Id,
                    onboardingId);
            }
            else
            {
                // =====================================================
                // PREVIOUS SESSION IS NO LONGER ACTIVE
                // =====================================================
                //
                // This includes:
                //
                // Failed
                // Rejected
                // Expired
                // Cancelled
                // RequiresReview
                // Any other non-active status
                //
                // A fresh Didit session will now be created.
                //
                // =====================================================

                _logger.LogInformation(
                    "Existing Didit verification {VerificationId} has status {Status}. " +
                    "A new Didit session will be created.",
                    verification.Id,
                    verification.Status);

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
            // SWIFTCARE CORRELATION REFERENCE
            // =========================================================
            //
            // This is sent to Didit as vendor_data.
            //
            // Example:
            //
            // SWIFTCARE-ID-15
            //
            // =========================================================

            var referenceId =
                $"SWIFTCARE-ID-{verification.Id}";

            // =========================================================
            // CREATE DIDIT REQUEST
            // =========================================================

            var diditRequest =
                new DiditVerificationSessionRequest
                {
                    ReferenceId =
                        referenceId,

                    FirstName =
                        applicant.FirstName ?? string.Empty,

                    LastName =
                        applicant.LastName ?? string.Empty,

                    Email =
                        applicant.Email,

                    DateOfBirth =
                        applicant.DateOfBirth,

                    CountryCode =
                        "ZAF"
                };

            _logger.LogInformation(
                "Starting NEW Didit identity verification session for " +
                "onboarding {OnboardingId}, identity document {IdentityDocumentId}, " +
                "verification {VerificationId}, applicant email {Email}.",
                onboardingId,
                identityDocumentId,
                verification.Id,
                applicant.Email);

            // =========================================================
            // CREATE DIDIT SESSION
            // =========================================================

            var diditResult =
                await _diditService
                    .CreateVerificationSessionAsync(
                        diditRequest,
                        cancellationToken);

            // =========================================================
            // DIDIT SESSION CREATION FAILED
            // =========================================================

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

                _logger.LogError(
                    "Didit verification session creation failed for " +
                    "SwiftCare verification {VerificationId}. Reason: {Reason}",
                    verification.Id,
                    verification.FailureReason);

                return verification;
            }

            // =========================================================
            // STORE DIDIT SESSION ID
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    diditResult.SessionId))
            {
                verification.ProviderSessionId =
                    diditResult.SessionId;
            }

            // =========================================================
            // STORE PROVIDER VERIFICATION ID
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    diditResult.VerificationId))
            {
                verification.ProviderVerificationId =
                    diditResult.VerificationId;
            }

            // =========================================================
            // STORE DIDIT HOSTED VERIFICATION URL
            // =========================================================

            verification.VerificationUrl =
                diditResult.VerificationUrl;

            // =========================================================
            // WAITING FOR APPLICANT
            // =========================================================

            verification.Status =
                VolunteerIdentityVerificationStatus.AwaitingUser;

            verification.VerificationStartedDate =
                verification.VerificationStartedDate ??
                DateTime.Now;

            verification.ModifiedDate =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);

            // =========================================================
            // EMAIL STATUS LOGGING
            // =========================================================
            //
            // The actual email is sent inside DiditService through
            // the existing SwiftCare EmailService.
            //
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    diditResult.ErrorMessage))
            {
                _logger.LogWarning(
                    "Didit session {SessionId} was created for " +
                    "SwiftCare verification {VerificationId}, but DiditService " +
                    "reported an email warning: {Message}",
                    verification.ProviderSessionId,
                    verification.Id,
                    diditResult.ErrorMessage);
            }
            else
            {
                _logger.LogInformation(
                    "Didit verification session {SessionId} created successfully " +
                    "for SwiftCare verification {VerificationId}. " +
                    "SwiftCare sent the verification email to {Email}.",
                    verification.ProviderSessionId,
                    verification.Id,
                    applicant.Email);
            }

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

            // ---------------------------------------------------------
            // LOAD VERIFICATION
            // ---------------------------------------------------------

            var verification =
                await _context
                    .VolunteerIdentityVerifications
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == verificationId,
                        cancellationToken);

            if (verification == null)
            {
                throw new InvalidOperationException(
                    $"Identity verification {verificationId} was not found.");
            }

            // =========================================================
            // STORE DIDIT SESSION ID
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    result.SessionId))
            {
                verification.ProviderSessionId =
                    result.SessionId;
            }

            // =========================================================
            // STORE PROVIDER VERIFICATION ID
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    result.VerificationId))
            {
                verification.ProviderVerificationId =
                    result.VerificationId;
            }

            // =========================================================
            // STORE PROVIDER PERSON ID
            // =========================================================
            //
            // IMPORTANT:
            //
            // Only store a real persistent Didit person identity.
            //
            // Never manufacture one locally.
            //
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    result.PersonId))
            {
                verification.ProviderPersonId =
                    result.PersonId;
            }

            // =========================================================
            // STORE VERIFICATION RESULTS
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
            // DETERMINE SWIFTCARE STATUS
            // =========================================================

            switch (result.Status)
            {
                // -----------------------------------------------------
                // APPROVED
                // -----------------------------------------------------

                case "Approved":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Verified;

                    verification.VerifiedDate =
                        DateTime.Now;

                    verification.FaceIdentityEnrolled =
                        verification.DocumentVerified &&
                        verification.LivenessVerified &&
                        verification.FaceMatchVerified &&
                        !string.IsNullOrWhiteSpace(
                            verification.ProviderPersonId);

                    break;

                // -----------------------------------------------------
                // DECLINED
                // -----------------------------------------------------

                case "Declined":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Rejected;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // IN REVIEW
                // -----------------------------------------------------

                case "In Review":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // IN PROGRESS
                // -----------------------------------------------------

                case "In Progress":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.InProgress;

                    break;

                // -----------------------------------------------------
                // RESUBMITTED
                // -----------------------------------------------------

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

                // -----------------------------------------------------
                // ABANDONED
                // -----------------------------------------------------

                case "Abandoned":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Cancelled;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // EXPIRED
                // -----------------------------------------------------

                case "Expired":

                case "KYC Expired":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Expired;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // UNKNOWN STATUS
                // -----------------------------------------------------

                default:

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    break;
            }

            // =========================================================
            // UPDATE AUDIT DATE
            // =========================================================

            verification.ModifiedDate =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "SwiftCare identity verification {VerificationId} " +
                "updated to {Status}. " +
                "DocumentVerified={DocumentVerified}, " +
                "LivenessVerified={LivenessVerified}, " +
                "FaceMatchVerified={FaceMatchVerified}.",
                verification.Id,
                verification.Status,
                verification.DocumentVerified,
                verification.LivenessVerified,
                verification.FaceMatchVerified);
        }
    }
}
