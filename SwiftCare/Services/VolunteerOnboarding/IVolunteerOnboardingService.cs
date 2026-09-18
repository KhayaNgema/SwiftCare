using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public interface IVolunteerOnboardingService
    {
        // ============================================================
        // CREATE ONBOARDING
        // ============================================================

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding> CreateOnboardingAsync(
            int volunteerApplicationId,
            string? adminId = null,
            CancellationToken cancellationToken = default);


        // ============================================================
        // GET ONBOARDING
        // ============================================================

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingByApplicationAsync(
            int volunteerApplicationId,
            CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingByApplicantAsync(
            string applicantId,
            CancellationToken cancellationToken = default);


        // ============================================================
        // VOLUNTEER OFFER
        // ============================================================

        Task<VolunteerOfferCreationResult> CreateOfferAsync(
            int volunteerOnboardingId,
            DateTime expiryDate,
            string? adminId = null,
            CancellationToken cancellationToken = default);

        Task<bool> AcceptOfferAsync(
            int volunteerOfferId,
            CancellationToken cancellationToken = default);

        Task<bool> DeclineOfferAsync(
            int volunteerOfferId,
            string declineReason,
            CancellationToken cancellationToken = default);

        Task<VolunteerOffer?> GetActiveOfferAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);


        // ============================================================
        // POLICIES
        // ============================================================

        Task<bool> AcceptPolicyAsync(
            int volunteerOnboardingId,
            int volunteerPolicyId,
            string applicantId,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default);

        Task<bool> HasAcceptedAllRequiredPoliciesAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);


        // ============================================================
        // ONBOARDING DOCUMENTS
        // ============================================================

        Task<VolunteerOnboardingDocument> AddDocumentAsync(
            int volunteerOnboardingId,
            string applicantId,
            VolunteerOnboardingDocumentType documentType,
            string documentName,
            string fileName,
            string filePath,
            string contentType,
            long fileSize,
            string? fileHash = null,
            CancellationToken cancellationToken = default);

        Task<bool> ReviewDocumentAsync(
            int documentId,
            VolunteerOnboardingDocumentStatus status,
            string adminId,
            string? rejectionReason = null,
            CancellationToken cancellationToken = default);


        // ============================================================
        // REQUIREMENTS
        // ============================================================

        /// <summary>
        /// Marks a specific onboarding requirement as completed.
        /// Used for requirements completed by a separate workflow,
        /// such as contract completion, orientation, role training,
        /// or supervisor assignment.
        /// </summary>
        Task<bool> CompleteRequirementAsync(
            int volunteerOnboardingId,
            VolunteerOnboardingRequirementType requirementType,
            string? completedByAdminId = null,
            CancellationToken cancellationToken = default);


        // ============================================================
        // ADMIN VERIFICATION
        // ============================================================

        Task<bool> StartVerificationAsync(
            int volunteerOnboardingId,
            string adminId,
            CancellationToken cancellationToken = default);

        Task<bool> CompleteVerificationAsync(
            int volunteerOnboardingId,
            string adminId,
            string? verificationNotes = null,
            CancellationToken cancellationToken = default);

        Task<bool> RejectForCorrectionAsync(
            int volunteerOnboardingId,
            string adminId,
            string reason,
            CancellationToken cancellationToken = default);


        // ============================================================
        // ACTIVATION READINESS
        // ============================================================

        Task<bool> IsReadyForActivationAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);


        // ============================================================
        // CURRENT STATUS
        // ============================================================

        Task<VolunteerOnboardingStatus> GetCurrentStatusAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);
    }
}
