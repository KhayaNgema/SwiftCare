using SwiftCare.Models;

namespace SwiftCare.Services.VolunteerActivation
{
    public class VolunteerActivationResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Volunteer? Volunteer { get; set; }

        public int? VolunteerOnboardingId { get; set; }

        public string? VolunteerUserId { get; set; }

        public static VolunteerActivationResult Failed(
            string message,
            int? onboardingId = null)
        {
            return new VolunteerActivationResult
            {
                Success = false,
                Message = message,
                VolunteerOnboardingId = onboardingId
            };
        }

        public static VolunteerActivationResult Succeeded(
            Volunteer volunteer,
            int onboardingId)
        {
            return new VolunteerActivationResult
            {
                Success = true,
                Message = "Volunteer activated successfully.",
                Volunteer = volunteer,
                VolunteerOnboardingId = onboardingId,
                VolunteerUserId = volunteer.Id
            };
        }
    }
}