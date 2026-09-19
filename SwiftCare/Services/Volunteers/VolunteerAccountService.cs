using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.Volunteers
{
    public class VolunteerAccountService : IVolunteerAccountService
    {
        private readonly SwiftCareDbContext _context;
        private readonly ILogger<VolunteerAccountService> _logger;

        public VolunteerAccountService(
            SwiftCareDbContext context,
            ILogger<VolunteerAccountService> logger)
        {
            _context = context;
            _logger = logger;
        }


        // ============================================================
        // CREATE VOLUNTEER ACCOUNT FROM ONBOARDING
        //
        // IMPORTANT:
        // This creates the Volunteer profile only.
        //
        // It does NOT activate the Volunteer.
        //
        // Status:
        //     PendingActivation
        //
        // ActivatedDate:
        //     null
        //
        // VolunteerNumber:
        //     null
        //
        // Supervisor:
        //     Automatically assigned from the VolunteerOpportunity Area
        //     when an active Supervisor exists for that Area.
        //
        // Final activation remains the responsibility of the
        // VolunteerActivationService.
        // ============================================================

        public async Task<Volunteer> CreateFromOnboardingAsync(
            int volunteerOnboardingId,
            string adminId,
            CancellationToken cancellationToken = default)
        {
            if (volunteerOnboardingId <= 0)
            {
                throw new ArgumentException(
                    "A valid volunteer onboarding ID is required.",
                    nameof(volunteerOnboardingId));
            }

            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
                    nameof(adminId));
            }

            _logger.LogInformation(
                "[VolunteerAccountService] Creating Volunteer account. " +
                "OnboardingId={OnboardingId}, AdminId={AdminId}",
                volunteerOnboardingId,
                adminId);


            // ========================================================
            // LOAD ONBOARDING
            // ========================================================

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Applicant)
                    .Include(x => x.Volunteer)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                throw new InvalidOperationException(
                    "The volunteer onboarding record could not be found.");
            }


            // ========================================================
            // IDEMPOTENCY
            //
            // If the navigation property already contains the
            // Volunteer, return it.
            // ========================================================

            if (onboarding.Volunteer != null)
            {
                _logger.LogInformation(
                    "[VolunteerAccountService] Volunteer already exists. " +
                    "VolunteerId={VolunteerId}, OnboardingId={OnboardingId}",
                    onboarding.Volunteer.Id,
                    volunteerOnboardingId);

                return onboarding.Volunteer;
            }


            // ========================================================
            // EXTRA DATABASE CHECK
            // ========================================================

            var existingVolunteer =
                await _context.Volunteers
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            volunteerOnboardingId,
                        cancellationToken);

            if (existingVolunteer != null)
            {
                _logger.LogInformation(
                    "[VolunteerAccountService] Existing Volunteer found " +
                    "by onboarding ID. VolunteerId={VolunteerId}",
                    existingVolunteer.Id);

                return existingVolunteer;
            }


            // ========================================================
            // APPLICANT REQUIRED
            // ========================================================

            if (onboarding.Applicant == null)
            {
                throw new InvalidOperationException(
                    "The applicant associated with this onboarding record could not be found.");
            }

            var applicant = onboarding.Applicant;


            // ========================================================
            // APPLICATION REQUIRED
            // ========================================================

            var application =
                await _context.VolunteerApplications
                    .Include(x => x.VolunteerOpportunity)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerApplicationId ==
                            onboarding.VolunteerApplicationId,
                        cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException(
                    "The volunteer application associated with this onboarding record could not be found.");
            }


            // ========================================================
            // OPPORTUNITY REQUIRED
            //
            // The VolunteerOpportunity supplies the Area used for
            // automatic Supervisor assignment.
            // ========================================================

            if (application.VolunteerOpportunity == null)
            {
                throw new InvalidOperationException(
                    "The volunteer opportunity associated with this application could not be found.");
            }

            var opportunity =
                application.VolunteerOpportunity;


            // ========================================================
            // AREA
            // ========================================================
            //
            // The opportunity determines the volunteer's initial
            // PreferredArea.
            //
            // Example:
            //
            // Opportunity Area:
            //     ResidentCare
            //
            // Volunteer PreferredArea:
            //     ResidentCare
            //
            // Supervisor Area:
            //     ResidentCare
            //
            // Supervisor:
            //     Thandi Mokoena
            // ========================================================

            var volunteerArea =
                opportunity.Area;


            // ========================================================
            // FIND ACTIVE SUPERVISOR
            // ========================================================
            //
            // We only automatically assign an active Supervisor
            // responsible for the same VolunteerArea.
            //
            // If no Supervisor exists for that Area, the Volunteer
            // will remain without a Supervisor and the required
            // SupervisorAssignment step can be handled by Admin.
            // ========================================================

            var supervisor =
                await _context.Supervisors
                    .Where(s =>
                        s.Area == volunteerArea &&
                        s.IsActive)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .FirstOrDefaultAsync(
                        cancellationToken);


            if (supervisor != null)
            {
                _logger.LogInformation(
                    "[VolunteerAccountService] Matching Supervisor found. " +
                    "SupervisorId={SupervisorId}, SupervisorName={FirstName} {LastName}, " +
                    "Area={Area}",
                    supervisor.Id,
                    supervisor.FirstName,
                    supervisor.LastName,
                    supervisor.Area);
            }
            else
            {
                _logger.LogWarning(
                    "[VolunteerAccountService] No active Supervisor found " +
                    "for Area={Area}. Volunteer will require manual " +
                    "Supervisor assignment.",
                    volunteerArea);
            }


            // ========================================================
            // CREATE VOLUNTEER
            // ========================================================

            var now = DateTime.Now;

            var volunteer = new Volunteer
            {
                // ----------------------------------------------------
                // BASIC PROFILE
                // ----------------------------------------------------

                FirstName = applicant.FirstName,
                LastName = applicant.LastName,
                DateOfBirth = applicant.DateOfBirth,
                ProfilePicture = applicant.ProfilePicture,


                // ----------------------------------------------------
                // VOLUNTEER PROFILE
                // ----------------------------------------------------

                Bio = applicant.ProfessionalSummary,
                Motivation = applicant.CareerObjective,

                // Area comes from the approved opportunity.
                PreferredArea = volunteerArea,


                // ----------------------------------------------------
                // APPLICATION / ONBOARDING LINK
                // ----------------------------------------------------

                VolunteerApplicationId =
                    onboarding.VolunteerApplicationId,

                VolunteerOnboardingId =
                    onboarding.VolunteerOnboardingId,


                // ----------------------------------------------------
                // STATUS
                // ----------------------------------------------------

                Status =
                    VolunteerStatus.PendingActivation,

                ActivatedDate = null,

                OnboardingCompletedDate = null,


                // ----------------------------------------------------
                // VOLUNTEER NUMBER
                //
                // Assigned later by activation.
                // ----------------------------------------------------

                VolunteerNumber = null,


                // ----------------------------------------------------
                // DATES
                // ----------------------------------------------------

                StartDate = null,
                EndDate = null,


                // ----------------------------------------------------
                // ORGANISATION
                // ----------------------------------------------------

                Department =
                    applicant.ProfessionalTitle,


                // ----------------------------------------------------
                // SUPERVISOR
                //
                // Automatically assigned when a matching active
                // Supervisor exists.
                // ----------------------------------------------------

                SupervisorId =
                    supervisor?.Id,

                Supervisor =
                    supervisor,


                // ----------------------------------------------------
                // AUDIT
                // ----------------------------------------------------

                CreatedDateTime = now,
                CreatedBy = adminId,

                ModifiedDateTime = now,
                ModifiedBy = adminId
            };


            // ========================================================
            // ADD VOLUNTEER
            // ========================================================

            _context.Volunteers.Add(volunteer);


            // ========================================================
            // LINK ONBOARDING
            // ========================================================

            onboarding.Volunteer = volunteer;

            onboarding.VolunteerCreatedDate = now;

            onboarding.ModifiedDate = now;
            onboarding.ModifiedBy = adminId;


            // ========================================================
            // SAVE VOLUNTEER
            // ========================================================

            await _context.SaveChangesAsync(
                cancellationToken);


            // ========================================================
            // LOG VOLUNTEER CREATION
            // ========================================================

            _logger.LogInformation(
                "[VolunteerAccountService] Volunteer created. " +
                "VolunteerId={VolunteerId}, ApplicantId={ApplicantId}, " +
                "ApplicationId={ApplicationId}, OnboardingId={OnboardingId}, " +
                "Area={Area}, SupervisorId={SupervisorId}, " +
                "Status={Status}",
                volunteer.Id,
                applicant.Id,
                application.VolunteerApplicationId,
                volunteerOnboardingId,
                volunteer.PreferredArea,
                volunteer.SupervisorId,
                volunteer.Status);


            // ========================================================
            // LOG SUPERVISOR ASSIGNMENT
            // ========================================================

            if (supervisor != null)
            {
                _logger.LogInformation(
                    "[VolunteerAccountService] Supervisor automatically " +
                    "assigned to Volunteer. " +
                    "VolunteerId={VolunteerId}, " +
                    "SupervisorId={SupervisorId}, " +
                    "SupervisorName={FirstName} {LastName}, " +
                    "Area={Area}",
                    volunteer.Id,
                    supervisor.Id,
                    supervisor.FirstName,
                    supervisor.LastName,
                    supervisor.Area);
            }
            else
            {
                _logger.LogWarning(
                    "[VolunteerAccountService] Volunteer created without " +
                    "a Supervisor assignment. " +
                    "VolunteerId={VolunteerId}, Area={Area}",
                    volunteer.Id,
                    volunteer.PreferredArea);
            }


            // ========================================================
            // LINK EXISTING DIDIT VERIFICATION
            //
            // We do NOT store the face image or biometric template.
            //
            // We only associate the already completed provider-side
            // verification with the Volunteer.
            // ========================================================

            var identityVerification =
                await _context.VolunteerIdentityVerifications
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId)
                    .OrderByDescending(
                        x => x.CreatedDate)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (identityVerification != null)
            {
                identityVerification.VolunteerId =
                    volunteer.Id;

                identityVerification.ModifiedDate =
                    now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "[VolunteerAccountService] Didit verification linked " +
                    "to Volunteer. VolunteerId={VolunteerId}, " +
                    "VerificationId={VerificationId}, " +
                    "ProviderPersonId={ProviderPersonId}",
                    volunteer.Id,
                    identityVerification.Id,
                    identityVerification.ProviderPersonId);
            }
            else
            {
                _logger.LogWarning(
                    "[VolunteerAccountService] No identity verification " +
                    "record found for OnboardingId={OnboardingId}",
                    volunteerOnboardingId);
            }


            // ========================================================
            // FINAL LOG
            // ========================================================

            _logger.LogInformation(
                "[VolunteerAccountService] Volunteer account preparation " +
                "completed successfully. " +
                "VolunteerId={VolunteerId}, " +
                "SupervisorId={SupervisorId}",
                volunteer.Id,
                volunteer.SupervisorId);


            return volunteer;
        }


        // ============================================================
        // GET VOLUNTEER BY ONBOARDING
        // ============================================================

        public async Task<Volunteer?> GetByOnboardingIdAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Volunteers
                .Include(x => x.Supervisor)
                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId,
                    cancellationToken);
        }


        // ============================================================
        // CHECK IF VOLUNTEER EXISTS
        // ============================================================

        public async Task<bool> ExistsForOnboardingAsync(
            int volunteerOnboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Volunteers
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        volunteerOnboardingId,
                    cancellationToken);
        }
    }
}