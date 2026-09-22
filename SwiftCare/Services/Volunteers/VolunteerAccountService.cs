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
        // Creates the Volunteer profile from the Applicant.
        //
        // Copies:
        //     FirstName
        //     LastName
        //     Email
        //     PhoneNumber
        //     DateOfBirth
        //     ProfilePicture
        //     ProfessionalSummary -> Bio
        //     CareerObjective -> Motivation
        //     ProfessionalTitle -> Department
        //
        // Also:
        //     Generates a unique 6-digit Volunteer Number.
        //     Assigns Supervisor automatically when available.
        //     Links the Volunteer to the onboarding.
        //     Links the completed Didit verification.
        //
        // IMPORTANT:
        //     This does NOT activate the Volunteer.
        //
        // Status:
        //     PendingActivation
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
            // ========================================================

            if (onboarding.Volunteer != null)
            {
                _logger.LogInformation(
                    "[VolunteerAccountService] Volunteer already exists. " +
                    "VolunteerId={VolunteerId}, " +
                    "VolunteerNumber={VolunteerNumber}, " +
                    "OnboardingId={OnboardingId}",
                    onboarding.Volunteer.Id,
                    onboarding.Volunteer.VolunteerNumber,
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
                    "by onboarding ID. " +
                    "VolunteerId={VolunteerId}, " +
                    "VolunteerNumber={VolunteerNumber}",
                    existingVolunteer.Id,
                    existingVolunteer.VolunteerNumber);

                return existingVolunteer;
            }

            // ========================================================
            // APPLICANT REQUIRED
            // ========================================================

            if (onboarding.Applicant == null)
            {
                throw new InvalidOperationException(
                    "The applicant associated with this onboarding record " +
                    "could not be found.");
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
                    "The volunteer application associated with this " +
                    "onboarding record could not be found.");
            }

            // ========================================================
            // OPPORTUNITY REQUIRED
            // ========================================================

            if (application.VolunteerOpportunity == null)
            {
                throw new InvalidOperationException(
                    "The volunteer opportunity associated with this " +
                    "application could not be found.");
            }

            var opportunity =
                application.VolunteerOpportunity;

            // ========================================================
            // AREA
            // ========================================================

            var volunteerArea =
                opportunity.Area;

            // ========================================================
            // FIND ACTIVE SUPERVISOR
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
                    "SupervisorId={SupervisorId}, " +
                    "SupervisorName={FirstName} {LastName}, " +
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
            // COMPLETE SUPERVISOR ASSIGNMENT REQUIREMENT
            //
            // A Supervisor Assignment requirement is considered complete
            // only when an active Supervisor has actually been assigned.
            // ========================================================

            if (supervisor != null)
            {
                var supervisorAssignmentRequirement =
                    await _context.VolunteerOnboardingRequirements
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                volunteerOnboardingId &&
                                x.RequirementType ==
                                VolunteerOnboardingRequirementType.SupervisorAssignment,
                            cancellationToken);

                if (supervisorAssignmentRequirement != null)
                {
                    supervisorAssignmentRequirement.IsCompleted = true;

                    supervisorAssignmentRequirement.CompletedDate =
                        DateTime.Now;

                    supervisorAssignmentRequirement.ModifiedDate =
                        DateTime.Now;


                    _logger.LogInformation(
                        "[VolunteerAccountService] Supervisor Assignment " +
                        "requirement completed. " +
                        "RequirementId={RequirementId}, " +
                        "OnboardingId={OnboardingId}, " +
                        "SupervisorId={SupervisorId}",
                        supervisorAssignmentRequirement.VolunteerOnboardingRequirementId,
                        volunteerOnboardingId,
                        supervisor.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "[VolunteerAccountService] Supervisor Assignment " +
                        "requirement was not found. " +
                        "OnboardingId={OnboardingId}, SupervisorId={SupervisorId}",
                        volunteerOnboardingId,
                        supervisor.Id);
                }
            }

            // ========================================================
            // VALIDATE APPLICANT EMAIL
            // ========================================================

            if (string.IsNullOrWhiteSpace(applicant.Email))
            {
                throw new InvalidOperationException(
                    "The applicant does not have an email address. " +
                    "A Volunteer account cannot be created without an email address.");
            }

            // ========================================================
            // GENERATE UNIQUE 6-DIGIT VOLUNTEER NUMBER
            //
            // Example:
            //
            // 482731
            //
            // Exactly 6 digits.
            // No date is included.
            // ========================================================

            var volunteerNumber =
                await GenerateUniqueVolunteerNumberAsync(
                    cancellationToken);

            _logger.LogInformation(
                "[VolunteerAccountService] Generated Volunteer Number. " +
                "VolunteerNumber={VolunteerNumber}, " +
                "OnboardingId={OnboardingId}",
                volunteerNumber,
                volunteerOnboardingId);

            // ========================================================
            // GENERATE UNIQUE IDENTITY USERNAME
            //
            // We deliberately do NOT copy Applicant.UserName because
            // Identity normally requires UserName to be unique.
            //
            // The Volunteer still receives the Applicant's email.
            // ========================================================

            var volunteerUserName =
                await GenerateUniqueVolunteerUserNameAsync(
                    volunteerNumber,
                    cancellationToken);

            // ========================================================
            // CREATE VOLUNTEER
            // ========================================================

            var now =
                DateTime.Now;

            var volunteer = new Volunteer
            {
                // ====================================================
                // IDENTITY INFORMATION
                // ====================================================

                Email =
                    applicant.Email,

                UserName =
                    volunteerUserName,

                PhoneNumber =
                    applicant.PhoneNumber,

                EmailConfirmed =
                    applicant.EmailConfirmed,

                PhoneNumberConfirmed =
                    applicant.PhoneNumberConfirmed,

                // ====================================================
                // BASIC PROFILE
                // ====================================================

                FirstName =
                    applicant.FirstName,

                LastName =
                    applicant.LastName,

                DateOfBirth =
                    applicant.DateOfBirth,

                ProfilePicture =
                    applicant.ProfilePicture,

                // ====================================================
                // VOLUNTEER PROFILE
                // ====================================================

                Bio =
                    applicant.ProfessionalSummary,

                Motivation =
                    applicant.CareerObjective,

                PreferredArea =
                    volunteerArea,

                // ====================================================
                // VOLUNTEER IDENTIFICATION
                // ====================================================

                VolunteerNumber =
                    volunteerNumber,

                // ====================================================
                // APPLICATION / ONBOARDING
                // ====================================================

                VolunteerApplicationId =
                    onboarding.VolunteerApplicationId,

                VolunteerOnboardingId =
                    onboarding.VolunteerOnboardingId,

                // ====================================================
                // STATUS
                // ====================================================

                Status =
                    VolunteerStatus.Active,

                ActivatedDate =
                    DateTime.Now,

                OnboardingCompletedDate =
                    DateTime.Now,

                // ====================================================
                // DATES
                // ====================================================

                StartDate =
                    null,

                EndDate =
                    null,

                // ====================================================
                // ORGANISATION
                // ====================================================

                Department =
                    applicant.ProfessionalTitle,

                // ====================================================
                // SUPERVISOR
                // ====================================================

                SupervisorId =
                    supervisor?.Id,

                Supervisor =
                    supervisor,

                // ====================================================
                // AUDIT
                // ====================================================

                CreatedDateTime =
                    now,

                CreatedBy =
                    adminId,

                ModifiedDateTime =
                    now,

                ModifiedBy =
                    adminId
            };

            // ========================================================
            // ADD VOLUNTEER
            // ========================================================

            _context.Volunteers.Add(volunteer);

            // ========================================================
            // LINK ONBOARDING
            // ========================================================

            onboarding.Volunteer =
                volunteer;

            onboarding.VolunteerCreatedDate =
                now;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            // ========================================================
            // SAVE VOLUNTEER
            // ========================================================

            await _context.SaveChangesAsync(
                cancellationToken);

            // ========================================================
            // LOG VOLUNTEER CREATION
            // ========================================================

            _logger.LogInformation(
                "[VolunteerAccountService] Volunteer created successfully. " +
                "VolunteerId={VolunteerId}, " +
                "VolunteerNumber={VolunteerNumber}, " +
                "ApplicantId={ApplicantId}, " +
                "ApplicationId={ApplicationId}, " +
                "OnboardingId={OnboardingId}, " +
                "Email={Email}, " +
                "UserName={UserName}, " +
                "Area={Area}, " +
                "SupervisorId={SupervisorId}, " +
                "Status={Status}",
                volunteer.Id,
                volunteer.VolunteerNumber,
                applicant.Id,
                application.VolunteerApplicationId,
                volunteerOnboardingId,
                volunteer.Email,
                volunteer.UserName,
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
                    "VolunteerNumber={VolunteerNumber}, " +
                    "SupervisorId={SupervisorId}, " +
                    "SupervisorName={FirstName} {LastName}, " +
                    "Area={Area}",
                    volunteer.Id,
                    volunteer.VolunteerNumber,
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
                    "VolunteerId={VolunteerId}, " +
                    "VolunteerNumber={VolunteerNumber}, " +
                    "Area={Area}",
                    volunteer.Id,
                    volunteer.VolunteerNumber,
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
                    "to Volunteer. " +
                    "VolunteerId={VolunteerId}, " +
                    "VolunteerNumber={VolunteerNumber}, " +
                    "VerificationId={VerificationId}, " +
                    "ProviderPersonId={ProviderPersonId}",
                    volunteer.Id,
                    volunteer.VolunteerNumber,
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
                "VolunteerNumber={VolunteerNumber}, " +
                "Email={Email}, " +
                "SupervisorId={SupervisorId}, " +
                "Status={Status}",
                volunteer.Id,
                volunteer.VolunteerNumber,
                volunteer.Email,
                volunteer.SupervisorId,
                volunteer.Status);

            return volunteer;
        }

        // ============================================================
        // GENERATE UNIQUE 6-DIGIT VOLUNTEER NUMBER
        //
        // Format:
        //
        //     XXXXXX
        //
        // Example:
        //
        //     482731
        //
        // The number is completely random.
        // It does NOT contain the date.
        //
        // The database is checked before returning the number.
        // ============================================================

        private async Task<string> GenerateUniqueVolunteerNumberAsync(
            CancellationToken cancellationToken)
        {
            while (true)
            {
                // Generates a number from 100000 to 999999.
                // Therefore it will always contain exactly 6 digits.
                var volunteerNumber =
                    Random.Shared
                        .Next(100000, 1000000)
                        .ToString();

                var exists =
                    await _context.Volunteers
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.VolunteerNumber ==
                                volunteerNumber,
                            cancellationToken);

                if (!exists)
                {
                    return volunteerNumber;
                }

                _logger.LogWarning(
                    "[VolunteerAccountService] Generated Volunteer Number " +
                    "already exists. Regenerating. " +
                    "VolunteerNumber={VolunteerNumber}",
                    volunteerNumber);
            }
        }

        // ============================================================
        // GENERATE UNIQUE IDENTITY USERNAME
        //
        // Because Applicant and Volunteer are both Identity users,
        // the Volunteer cannot normally reuse the Applicant's
        // UserName.
        //
        // We therefore generate a unique username for the Volunteer.
        //
        // Email remains the applicant's real email address.
        // ============================================================

        private async Task<string> GenerateUniqueVolunteerUserNameAsync(
            string volunteerNumber,
            CancellationToken cancellationToken)
        {
            var baseUserName =
                volunteerNumber
                    .Replace("-", "")
                    .ToLowerInvariant();

            var userName =
                $"volunteer.{baseUserName}";

            var exists =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.NormalizedUserName ==
                            userName.ToUpperInvariant(),
                        cancellationToken);

            if (!exists)
            {
                return userName;
            }

            var suffix =
                1;

            while (true)
            {
                var candidate =
                    $"{userName}.{suffix}";

                var candidateExists =
                    await _context.Users
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.NormalizedUserName ==
                                candidate.ToUpperInvariant(),
                            cancellationToken);

                if (!candidateExists)
                {
                    return candidate;
                }

                suffix++;
            }
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