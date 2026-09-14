using System.Threading;
using System.Threading.Tasks;
using SwiftCare.Models;
using SwiftCare.ViewModels;

namespace SwiftCare.Services.VolunteerActivation
{
    public interface IVolunteerActivationService
    {
        Task<VolunteerActivationViewModel?> GetActivationViewModelAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        Task<VolunteerActivationResult> ActivateVolunteerAsync(
            VolunteerActivationViewModel model,
            string adminId,
            CancellationToken cancellationToken = default);

        Task<Volunteer?> GetVolunteerAsync(
            string volunteerId,
            CancellationToken cancellationToken = default);

        Task<Volunteer?> GetVolunteerByOnboardingAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);
    }
}