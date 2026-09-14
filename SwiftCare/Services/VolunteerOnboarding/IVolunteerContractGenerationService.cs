using System.Threading;
using System.Threading.Tasks;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public interface IVolunteerContractGenerationService
    {
        Task<VolunteerContract> GenerateContractAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        Task<VolunteerContract?> GetContractAsync(
            int contractId,
            CancellationToken cancellationToken = default);

        Task<VolunteerContract?> GetContractByOnboardingAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);
    }
}