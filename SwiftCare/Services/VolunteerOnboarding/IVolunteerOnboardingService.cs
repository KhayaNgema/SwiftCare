// IVolunteerOnboardingService.cs

using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public interface IVolunteerOnboardingService
    {
        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding> CreateOnboardingAsync(
            int volunteerApplicationId,
            string? adminId = null,
            CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingByApplicationAsync(
            int volunteerApplicationId,
            CancellationToken cancellationToken = default);

        Task<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding?> GetOnboardingByApplicantAsync(
            string applicantId,
            CancellationToken cancellationToken = default);

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

        Task<bool> IsReadyForActivationAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);

        Task<VolunteerOnboardingStatus> GetCurrentStatusAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);
    }
}