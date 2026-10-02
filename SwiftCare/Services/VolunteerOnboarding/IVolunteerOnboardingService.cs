using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public interface IVolunteerOnboardingService
    {
        // ============================================================
        // ONBOARDING
        // ============================================================

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding>
            CreateOnboardingAsync(
                int volunteerApplicationId,
                string? createdBy = null,
                CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?>
            GetOnboardingAsync(
                int volunteerOnboardingId,
                CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?>
            GetOnboardingByApplicationAsync(
                int volunteerApplicationId,
                CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?>
            GetOnboardingByApplicantAsync(
                string applicantId,
                CancellationToken cancellationToken = default);


        // ============================================================
        // OFFER
        // ============================================================

        Task<VolunteerOfferCreationResult>
            CreateOfferAsync(
                int volunteerOnboardingId,
                DateTime expiryDate,
                string createdBy,
                CancellationToken cancellationToken = default);

        Task<VolunteerOfferActionResult> AcceptOfferAsync(
            int volunteerOfferId,
            string token,
            CancellationToken cancellationToken = default);

        Task<VolunteerOfferActionResult> DeclineOfferAsync(
            int volunteerOfferId,
            string token,
            string declineReason,
            CancellationToken cancellationToken = default);

        Task<VolunteerOffer?>
            GetActiveOfferAsync(
                int volunteerOnboardingId,
                CancellationToken cancellationToken = default);


        // ============================================================
        // POLICIES
        // ============================================================

        Task<bool>
            AcceptPolicyAsync(
                int volunteerOnboardingId,
                int policyId,
                string applicantId,
                string? ipAddress = null,
                string? userAgent = null,
                CancellationToken cancellationToken = default);

        Task<bool>
            HasAcceptedAllRequiredPoliciesAsync(
                int volunteerOnboardingId,
                CancellationToken cancellationToken = default);


        // ============================================================
        // DOCUMENTS
        // ============================================================

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboardingDocument>
            AddDocumentAsync(
                int volunteerOnboardingId,
                string applicantId,
                VolunteerOnboardingDocumentType documentType,
                string documentName,
                string filePath,
                CancellationToken cancellationToken = default);

        Task<bool>
            ReviewDocumentAsync(
                int documentId,
                string adminId,
                VolunteerOnboardingDocumentStatus status,
                string? reviewNotes = null,
                CancellationToken cancellationToken = default);


        // ============================================================
        // REQUIREMENTS
        // ============================================================

        Task
            SyncDocumentRequirementsAsync(
                int volunteerOnboardingId,
                string? adminId = null,
                CancellationToken cancellationToken = default);

        Task<bool>
            CompleteRequirementAsync(
                int volunteerOnboardingId,
                VolunteerOnboardingRequirementType requirementType,
                string adminId,
                string? notes = null,
                CancellationToken cancellationToken = default);


        // ============================================================
        // VERIFICATION
        // ============================================================

        Task<bool>
            StartVerificationAsync(
                int volunteerOnboardingId,
                string adminId,
                CancellationToken cancellationToken = default);

        Task<bool>
            CompleteVerificationAsync(
                int volunteerOnboardingId,
                string adminId,
                string? verificationNotes = null,
                CancellationToken cancellationToken = default);

        Task<bool>
            RejectForCorrectionAsync(
                int volunteerOnboardingId,
                string adminId,
                string reason,
                CancellationToken cancellationToken = default);


        // ============================================================
        // ACTIVATION
        // ============================================================

        Task<bool>
            IsReadyForActivationAsync(
                int volunteerOnboardingId,
                CancellationToken cancellationToken = default);

        Task<VolunteerOnboardingStatus>
            GetCurrentStatusAsync(
                int volunteerOnboardingId,
                CancellationToken cancellationToken = default);
    }
}