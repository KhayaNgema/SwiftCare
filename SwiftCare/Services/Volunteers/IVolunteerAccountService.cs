using SwiftCare.Models;

namespace SwiftCare.Services.Volunteers
{
    public interface IVolunteerAccountService
    {
        Task<Volunteer> CreateFromOnboardingAsync(
            int volunteerOnboardingId,
            string adminId,
            CancellationToken cancellationToken = default);

        Task<Volunteer?> GetByOnboardingIdAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsForOnboardingAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default);
    }
}