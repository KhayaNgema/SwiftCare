using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using OnboardingEntity =
    SwiftCare.Models.VolunteerOnboarding.VolunteerOnboarding;

using VolunteerStatus =
    SwiftCare.Models.VolunteerStatus;

namespace SwiftCare.Services.VolunteerActivation
{
    public class VolunteerActivationService : IVolunteerActivationService
    {
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<VolunteerActivationService> _logger;

        private const string VolunteerRole = "Volunteer";

        public VolunteerActivationService(
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<VolunteerActivationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<VolunteerActivationViewModel?> GetActivationViewModelAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            var onboarding =
                await _context.VolunteerOnboardings
                    .AsNoTracking()
                    .Include(x => x.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.VolunteerOpportunity)
                    .Include(x => x.Documents)
                    .Include(x => x.Requirements)
                    .Include(x => x.PolicyAcceptances)
                    .Include(x => x.Offers)
                    .Include(x => x.Volunteer)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerOnboardingId == onboardingId,
                        cancellationToken);

            if (onboarding == null)
                return null;

            var applicant = onboarding.Applicant;

            if (applicant == null)
                return null;

            var application = onboarding.VolunteerApplication;

            if (application == null)
                return null;

            var opportunity = application.VolunteerOpportunity;

            var ready =
                await IsReadyForActivationInternalAsync(
                    onboarding,
                    cancellationToken);

            var supervisors =
                await GetSupervisorsAsync(cancellationToken);

            var departments =
                await GetDepartmentsAsync(cancellationToken);

            var model = new VolunteerActivationViewModel
            {
                VolunteerOnboardingId =
                    onboarding.VolunteerOnboardingId,

                VolunteerApplicationId =
                    onboarding.VolunteerApplicationId,

                ApplicantId =
                    onboarding.ApplicantId,

                ApplicantName =
                    BuildFullName(applicant),

                Email =
                    applicant.Email ?? string.Empty,

                PhoneNumber =
                    applicant.PhoneNumber,

                PositionTitle =
                    opportunity?.Title ?? "Volunteer",

                Bio =
                    GetApplicantBio(applicant),

                Motivation =
                    GetApplicationMotivation(application),

                PreferredArea =
                    GetApplicantPreferredArea(applicant),

                EmergencyContactName =
                    GetApplicantEmergencyContactName(applicant),

                EmergencyContactPhone =
                    GetApplicantEmergencyContactPhone(applicant),

                EmergencyContactRelationship =
                    GetApplicantEmergencyContactRelationship(applicant),

                VolunteerNumber =
                    await GenerateVolunteerNumberAsync(
                        cancellationToken),

                Department =
                    opportunity?.Area?.ToString(),

                SupervisorId = null,

                StartDate =
                    opportunity?.StartDate ?? DateTime.Today,

                EndDate =
                    opportunity?.EndDate,

                Status =
                    VolunteerStatus.Active,

                ReadyForActivation =
                    ready,

                VerificationNotes =
                    onboarding.VerificationNotes,

                OpportunityCity =
                    opportunity?.CityName,

                OpportunityProvince =
                    opportunity?.Province,

                Supervisors =
                    supervisors,

                Departments =
                    departments,

                Documents =
                    onboarding.Documents
                        .OrderByDescending(x => x.UploadedDate)
                        .Select(x =>
                            new VolunteerActivationDocumentViewModel
                            {
                                DocumentId =
                                    x.VolunteerOnboardingDocumentId,

                                DocumentName =
                                    x.DocumentName,

                                FileName =
                                    x.FileName,

                                DocumentType =
                                    x.DocumentType.ToString(),

                                Status =
                                    x.Status.ToString(),

                                UploadedDate =
                                    x.UploadedDate
                            })
                        .ToList()
            };

            return model;
        }

        public async Task<VolunteerActivationResult> ActivateVolunteerAsync(
            VolunteerActivationViewModel model,
            string adminId,
            CancellationToken cancellationToken = default)
        {
            if (model == null)
            {
                return VolunteerActivationResult.Failed(
                    "Activation information was not provided.");
            }

            if (string.IsNullOrWhiteSpace(adminId))
            {
                return VolunteerActivationResult.Failed(
                    "The administrator could not be identified.",
                    model.VolunteerOnboardingId);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var onboarding =
                    await _context.VolunteerOnboardings
                        .Include(x => x.Applicant)
                        .Include(x => x.VolunteerApplication)
                            .ThenInclude(x => x.VolunteerOpportunity)
                        .Include(x => x.Documents)
                        .Include(x => x.Requirements)
                        .Include(x => x.PolicyAcceptances)
                        .Include(x => x.Offers)
                        .Include(x => x.Volunteer)
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerOnboardingId ==
                                model.VolunteerOnboardingId,
                            cancellationToken);

                if (onboarding == null)
                {
                    return VolunteerActivationResult.Failed(
                        "The onboarding record could not be found.",
                        model.VolunteerOnboardingId);
                }

                if (onboarding.Status ==
                    VolunteerOnboardingStatus.Completed)
                {
                    return VolunteerActivationResult.Failed(
                        "This onboarding process has already been completed.",
                        onboarding.VolunteerOnboardingId);
                }

                if (onboarding.Status !=
                    VolunteerOnboardingStatus.ReadyForActivation)
                {
                    return VolunteerActivationResult.Failed(
                        $"This volunteer is not ready for activation. " +
                        $"Current status: {onboarding.Status}.",
                        onboarding.VolunteerOnboardingId);
                }

                if (onboarding.Volunteer != null)
                {
                    return VolunteerActivationResult.Failed(
                        "A Volunteer account has already been linked to this onboarding record.",
                        onboarding.VolunteerOnboardingId);
                }

                var ready =
                    await IsReadyForActivationInternalAsync(
                        onboarding,
                        cancellationToken);

                if (!ready)
                {
                    return VolunteerActivationResult.Failed(
                        "The volunteer has not satisfied all onboarding requirements.",
                        onboarding.VolunteerOnboardingId);
                }

                var applicant = onboarding.Applicant;

                if (applicant == null)
                {
                    return VolunteerActivationResult.Failed(
                        "The applicant account could not be found.",
                        onboarding.VolunteerOnboardingId);
                }

                var application =
                    onboarding.VolunteerApplication;

                if (application == null)
                {
                    return VolunteerActivationResult.Failed(
                        "The volunteer application could not be found.",
                        onboarding.VolunteerOnboardingId);
                }

                if (string.IsNullOrWhiteSpace(model.VolunteerNumber))
                {
                    model.VolunteerNumber =
                        await GenerateVolunteerNumberAsync(
                            cancellationToken);
                }

                model.VolunteerNumber =
                    model.VolunteerNumber.Trim();

                var volunteerNumberExists =
                    await _context.Set<Volunteer>()
                        .AnyAsync(
                            x =>
                                x.VolunteerNumber ==
                                model.VolunteerNumber,
                            cancellationToken);

                if (volunteerNumberExists)
                {
                    return VolunteerActivationResult.Failed(
                        "The volunteer number is already in use.",
                        onboarding.VolunteerOnboardingId);
                }

                /*
                 * StartDate is required by the activation model.
                 * We compare the actual DateTime values rather than
                 * using .Date on nullable DateTime values.
                 */
                if (model.StartDate.Date < DateTime.Today)
                {
                    return VolunteerActivationResult.Failed(
                        "The volunteer start date cannot be in the past.",
                        onboarding.VolunteerOnboardingId);
                }

                /*
                 * EndDate is nullable, therefore HasValue must be
                 * checked before accessing its value.
                 */
                if (model.EndDate.HasValue &&
                    model.EndDate.Value.Date < model.StartDate.Date)
                {
                    return VolunteerActivationResult.Failed(
                        "The volunteer end date cannot be before the start date.",
                        onboarding.VolunteerOnboardingId);
                }

                /*
                 * The Applicant remains intact for application/audit
                 * history.
                 *
                 * A separate Volunteer Identity account is created
                 * using the existing Applicant information.
                 */
                var volunteer =
                    new Volunteer
                    {
                        UserName =
                            applicant.Email,

                        NormalizedUserName =
                            applicant.NormalizedUserName,

                        Email =
                            applicant.Email,

                        NormalizedEmail =
                            applicant.NormalizedEmail,

                        EmailConfirmed =
                            applicant.EmailConfirmed,

                        PhoneNumber =
                            applicant.PhoneNumber,

                        PhoneNumberConfirmed =
                            applicant.PhoneNumberConfirmed,

                        FirstName =
                            applicant.FirstName,

                        LastName =
                            applicant.LastName,

                        DateOfBirth =
                            applicant.DateOfBirth,

                        ProfilePicture =
                            applicant.ProfilePicture,

                        Bio =
                            model.Bio,

                        Motivation =
                            model.Motivation,

                        PreferredArea =
                            model.PreferredArea,

                        EmergencyContactName =
                            model.EmergencyContactName,

                        EmergencyContactPhone =
                            model.EmergencyContactPhone,

                        EmergencyContactRelationship =
                            model.EmergencyContactRelationship,

                        VolunteerNumber =
                            model.VolunteerNumber,

                        VolunteerApplicationId =
                            onboarding.VolunteerApplicationId,

                        VolunteerOnboardingId =
                            onboarding.VolunteerOnboardingId,

                        Status =
                            VolunteerStatus.Active,

                        ActivatedDate =
                            DateTime.Now,

                        OnboardingCompletedDate =
                            DateTime.Now,

                        StartDate =
                            model.StartDate.Date,

                        EndDate =
                            model.EndDate.HasValue
                                ? model.EndDate.Value.Date
                                : null,

                        Department =
                            string.IsNullOrWhiteSpace(model.Department)
                                ? null
                                : model.Department.Trim(),

                        SupervisorId =
                            string.IsNullOrWhiteSpace(model.SupervisorId)
                                ? null
                                : model.SupervisorId.Trim()
                    };

                /*
                 * UserManager requires a unique username.
                 *
                 * Since the Applicant already has the email as a
                 * username in most Identity configurations, generate
                 * a unique username for the Volunteer account.
                 */
                volunteer.UserName =
                    await GenerateVolunteerUsernameAsync(
                        applicant,
                        cancellationToken);

                volunteer.NormalizedUserName =
                    _userManager.NormalizeName(
                        volunteer.UserName);

                /*
                 * Create the Volunteer Identity account.
                 */
                var temporaryPassword =
                    GenerateTemporaryPassword();

                var createResult =
                    await _userManager.CreateAsync(
                        volunteer,
                        temporaryPassword);

                if (!createResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            "; ",
                            createResult.Errors.Select(
                                x => x.Description));

                    _logger.LogError(
                        "[SWIFTCARE ACTIVATION] Failed to create Volunteer Identity account for ApplicantId {ApplicantId}. Errors: {Errors}",
                        applicant.Id,
                        errors);

                    await transaction.RollbackAsync(
                        cancellationToken);

                    return VolunteerActivationResult.Failed(
                        "The Volunteer account could not be created. " +
                        errors,
                        onboarding.VolunteerOnboardingId);
                }

                /*
                 * The Volunteer role must exist in Identity.
                 */
                if (!await _roleManager.RoleExistsAsync(
                        VolunteerRole))
                {
                    _logger.LogError(
                        "[SWIFTCARE ACTIVATION] Required Identity role '{Role}' does not exist.",
                        VolunteerRole);

                    await _userManager.DeleteAsync(
                        volunteer);

                    await transaction.RollbackAsync(
                        cancellationToken);

                    return VolunteerActivationResult.Failed(
                        "The Volunteer Identity role has not been configured. " +
                        "Please create the Volunteer role before activating volunteers.",
                        onboarding.VolunteerOnboardingId);
                }

                var roleResult =
                    await _userManager.AddToRoleAsync(
                        volunteer,
                        VolunteerRole);

                if (!roleResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            "; ",
                            roleResult.Errors.Select(
                                x => x.Description));

                    _logger.LogError(
                        "[SWIFTCARE ACTIVATION] Failed to assign Volunteer role to {VolunteerId}. Errors: {Errors}",
                        volunteer.Id,
                        errors);

                    await _userManager.DeleteAsync(
                        volunteer);

                    await transaction.RollbackAsync(
                        cancellationToken);

                    return VolunteerActivationResult.Failed(
                        "The Volunteer role could not be assigned. " +
                        errors,
                        onboarding.VolunteerOnboardingId);
                }

                /*
                 * Generate a password-reset token so the volunteer
                 * can establish their own password.
                 *
                 * The token is not stored in the database.
                 */
                var passwordResetToken =
                    await _userManager.GeneratePasswordResetTokenAsync(
                        volunteer);

                /*
                 * Email delivery is intentionally kept outside this
                 * service. The controller/application layer can use
                 * the generated password-reset token.
                 *
                 * Do not log or persist the token.
                 */
                _ = passwordResetToken;

                /*
                 * Complete onboarding.
                 */
                onboarding.VolunteerCreatedDate =
                    DateTime.Now;

                onboarding.CompletedDate =
                    DateTime.Now;

                onboarding.VerifiedDate ??=
                    DateTime.Now;

                onboarding.VerifiedByAdminId ??=
                    adminId;

                onboarding.Status =
                    VolunteerOnboardingStatus.Completed;

                onboarding.CurrentStep =
                    7;

                onboarding.ModifiedDate =
                    DateTime.Now;

                onboarding.ModifiedBy =
                    adminId;

                /*
                 * Mark the document submission date if documents
                 * were uploaded.
                 */
                if (!onboarding.DocumentsSubmittedDate.HasValue &&
                    onboarding.Documents.Any())
                {
                    onboarding.DocumentsSubmittedDate =
                        onboarding.Documents
                            .Max(x => x.UploadedDate);
                }

                /*
                 * VolunteerApplication does not contain ModifiedDate
                 * in the current model, so we deliberately do not
                 * attempt to update it here.
                 */

                await _context.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "[SWIFTCARE ACTIVATION] Volunteer activated successfully. " +
                    "VolunteerId: {VolunteerId}, ApplicantId: {ApplicantId}, " +
                    "OnboardingId: {OnboardingId}, VolunteerNumber: {VolunteerNumber}",
                    volunteer.Id,
                    applicant.Id,
                    onboarding.VolunteerOnboardingId,
                    volunteer.VolunteerNumber);

                return VolunteerActivationResult.Succeeded(
                    volunteer,
                    onboarding.VolunteerOnboardingId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "[SWIFTCARE ACTIVATION] Unexpected activation error. " +
                    "OnboardingId: {OnboardingId}",
                    model.VolunteerOnboardingId);

                return VolunteerActivationResult.Failed(
                    "An unexpected error occurred while activating the volunteer.",
                    model.VolunteerOnboardingId);
            }
        }

        public async Task<Volunteer?> GetVolunteerAsync(
            string volunteerId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(volunteerId))
                return null;

            return await _context.Set<Volunteer>()
                .AsNoTracking()
                .Include(x => x.Onboarding)
                .FirstOrDefaultAsync(
                    x => x.Id == volunteerId,
                    cancellationToken);
        }

        public async Task<Volunteer?> GetVolunteerByOnboardingAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<Volunteer>()
                .AsNoTracking()
                .Include(x => x.Onboarding)
                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        onboardingId,
                    cancellationToken);
        }

        private async Task<bool> IsReadyForActivationInternalAsync(
            OnboardingEntity onboarding,
            CancellationToken cancellationToken)
        {
            if (onboarding.Status ==
                VolunteerOnboardingStatus.Completed)
            {
                return true;
            }

            if (onboarding.Status !=
                VolunteerOnboardingStatus.ReadyForActivation)
            {
                return false;
            }

            /*
             * All active required policies must have an accepted
             * policy acceptance for this onboarding.
             */
            var requiredPolicies =
                await _context.VolunteerPolicies
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.IsRequired)
                    .Select(x => x.VolunteerPolicyId)
                    .ToListAsync(cancellationToken);

            var acceptedPolicyIds =
                onboarding.PolicyAcceptances
                    .Where(x => x.Accepted)
                    .Select(x => x.VolunteerPolicyId)
                    .ToHashSet();

            if (requiredPolicies.Any(
                    id => !acceptedPolicyIds.Contains(id)))
            {
                return false;
            }

            /*
             * All required onboarding requirements must be complete.
             */
            var requiredRequirements =
                onboarding.Requirements
                    .Where(x => x.IsRequired)
                    .ToList();

            if (requiredRequirements.Any(
                    x => !x.IsCompleted))
            {
                return false;
            }

            /*
             * No document may still be pending review, rejected,
             * uploaded but unreviewed, or awaiting resubmission.
             */
            var invalidDocuments =
                onboarding.Documents.Any(
                    x =>
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.Rejected ||
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.ResubmissionRequired ||
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.PendingUpload ||
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.Uploaded ||
                        x.Status ==
                            VolunteerOnboardingDocumentStatus.UnderReview);

            if (invalidDocuments)
                return false;

            return true;
        }

        private async Task<List<VolunteerActivationSupervisorViewModel>>
            GetSupervisorsAsync(
                CancellationToken cancellationToken)
        {
            /*
             * Head Nurse / Head Doctor / Administrator users can
             * supervise volunteers.
             */
            var supervisorIds =
                new HashSet<string>();

            var roleNames =
                new[]
                {
                    "Administrator",
                    "Head Nurse",
                    "Head Doctor"
                };

            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(
                        roleName))
                {
                    continue;
                }

                var users =
                    await _userManager.GetUsersInRoleAsync(
                        roleName);

                foreach (var user in users)
                {
                    if (!string.IsNullOrWhiteSpace(user.Id))
                    {
                        supervisorIds.Add(user.Id);
                    }
                }
            }

            if (!supervisorIds.Any())
            {
                return new List<VolunteerActivationSupervisorViewModel>();
            }

            var allUsers =
                await _userManager.Users
                    .Where(x =>
                        supervisorIds.Contains(x.Id))
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync(cancellationToken);

            return allUsers
                .Select(x =>
                    new VolunteerActivationSupervisorViewModel
                    {
                        Id =
                            x.Id,

                        Name =
                            BuildFullName(x)
                    })
                .ToList();
        }

        private async Task<List<string>> GetDepartmentsAsync(
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            return new List<string>
            {
                "Resident Support",
                "Activities and Recreation",
                "Dining Services",
                "Administration",
                "Garden and Outdoor",
                "Events",
                "Transportation",
                "Community Outreach"
            };
        }

        private async Task<string> GenerateVolunteerNumberAsync(
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var number =
                    $"VOL-{DateTime.Now:yyyy}-{Random.Shared.Next(100000, 999999)}";

                var exists =
                    await _context.Set<Volunteer>()
                        .AnyAsync(
                            x =>
                                x.VolunteerNumber == number,
                            cancellationToken);

                if (!exists)
                    return number;
            }

            /*
             * Extremely unlikely fallback.
             */
            var fallback =
                $"VOL-{DateTime.Now:yyyy}-{Guid.NewGuid():N}";

            return fallback
                .Substring(
                    0,
                    Math.Min(
                        30,
                        fallback.Length))
                .ToUpperInvariant();
        }

        private async Task<string> GenerateVolunteerUsernameAsync(
            Applicant applicant,
            CancellationToken cancellationToken)
        {
            var baseUsername =
                !string.IsNullOrWhiteSpace(applicant.Email)
                    ? applicant.Email
                    : $"volunteer.{Guid.NewGuid():N}";

            var username =
                baseUsername;

            var counter =
                1;

            while (
                await _userManager.FindByNameAsync(
                    username) != null)
            {
                var atIndex =
                    baseUsername.IndexOf('@');

                if (atIndex > 0)
                {
                    username =
                        $"{baseUsername[..atIndex]}.{counter}@{baseUsername[(atIndex + 1)..]}";
                }
                else
                {
                    username =
                        $"{baseUsername}.{counter}";
                }

                counter++;
            }

            return username;
        }

        private static string GenerateTemporaryPassword()
        {
            return $"Sc!{Guid.NewGuid():N}A9#";
        }

        private static string BuildFullName(
            UserBaseModel user)
        {
            var name =
                $"{user.FirstName} {user.LastName}"
                    .Trim();

            return string.IsNullOrWhiteSpace(name)
                ? user.Email ?? "Volunteer"
                : name;
        }

        /*
         * These helpers intentionally remain safe because the exact
         * Applicant/Application profile properties have not been
         * confirmed in the current model.
         */
        private static string? GetApplicantBio(
            Applicant applicant)
        {
            return null;
        }

        private static string? GetApplicationMotivation(
            VolunteerApplication application)
        {
            return null;
        }

        private static VolunteerArea? GetApplicantPreferredArea(
            Applicant applicant)
        {
            return null;
        }

        private static string? GetApplicantEmergencyContactName(
            Applicant applicant)
        {
            return null;
        }

        private static string? GetApplicantEmergencyContactPhone(
            Applicant applicant)
        {
            return null;
        }

        private static string? GetApplicantEmergencyContactRelationship(
            Applicant applicant)
        {
            return null;
        }
    }
}
