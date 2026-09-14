using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public class VolunteerContractReferenceGenerator
    {
        private readonly SwiftCareDbContext _context;

        public VolunteerContractReferenceGenerator(
            SwiftCareDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAsync(
            CancellationToken cancellationToken = default)
        {
            var datePart =
                DateTime.Now.ToString("yyMMdd");

            for (var attempt = 0; attempt < 50; attempt++)
            {
                var randomPart =
                    Random.Shared.Next(1000, 9999);

                var reference =
                    $"SC-CON-{datePart}-{randomPart}";

                var exists =
                    await _context.VolunteerContracts
                        .AnyAsync(
                            x =>
                                x.ContractReference ==
                                reference,
                            cancellationToken);

                if (!exists)
                {
                    return reference;
                }
            }

            return
                $"SC-CON-{datePart}-{Guid.NewGuid():N}"
                    .Substring(0, 20)
                    .ToUpperInvariant();
        }
    }
}