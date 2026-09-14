using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public class VolunteerContractService
        : IVolunteerContractService
    {
        private readonly SwiftCareDbContext _context;

        private readonly IVolunteerContractGenerationService
            _contractGenerationService;

        private readonly IVolunteerContractPdfGenerator
            _pdfGenerator;

        public VolunteerContractService(
            SwiftCareDbContext context,
            IVolunteerContractGenerationService contractGenerationService,
            IVolunteerContractPdfGenerator pdfGenerator)
        {
            _context = context;

            _contractGenerationService =
                contractGenerationService;

            _pdfGenerator =
                pdfGenerator;
        }

        public async Task<VolunteerContract>
            CreateAndGenerateContractAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            var contract =
                await _contractGenerationService
                    .GenerateContractAsync(
                        onboardingId,
                        cancellationToken);

            contract =
                await _pdfGenerator
                    .GeneratePdfAsync(
                        contract,
                        cancellationToken);

            contract.ModifiedDate =
                DateTime.Now;

            _context.VolunteerContracts.Update(contract);

            await _context.SaveChangesAsync(
                cancellationToken);

            return contract;
        }
    }
}