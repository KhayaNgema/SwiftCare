using SwiftCare.Models.VolunteerOnboarding;

public interface IVolunteerContractService
{
    Task<VolunteerContract> CreateAndGenerateContractAsync(
        int onboardingId,
        CancellationToken cancellationToken = default);
}