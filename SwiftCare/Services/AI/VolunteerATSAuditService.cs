using SwiftCare.Data;
using SwiftCare.Models;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerATSAuditService
    {
        private readonly SwiftCareDbContext _context;

        public VolunteerATSAuditService(
            SwiftCareDbContext context)
        {
            _context = context;
        }


        public async Task LogAsync(
            int applicationId,
            string action,
            string? description = null,
            string? performedBy = null)
        {
            var audit =
                new VolunteerApplicationAudit
                {
                    VolunteerApplicationId =
                        applicationId,

                    Action =
                        action,

                    Description =
                        description,

                    PerformedBy =
                        performedBy,

                    CreatedDate =
                        DateTime.Now
                };

            _context
                .VolunteerApplicationAudits
                .Add(audit);

            await _context.SaveChangesAsync();
        }
    }
}