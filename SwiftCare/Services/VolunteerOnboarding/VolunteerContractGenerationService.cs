using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public class VolunteerContractGenerationService
    : IVolunteerContractGenerationService
    {
        private readonly SwiftCareDbContext _context;
        private readonly VolunteerContractReferenceGenerator _referenceGenerator;
        private readonly ILogger<VolunteerContractGenerationService> _logger;

    public VolunteerContractGenerationService(
        SwiftCareDbContext context,
        VolunteerContractReferenceGenerator referenceGenerator,
        ILogger<VolunteerContractGenerationService> logger)
        {
            _context = context;
            _referenceGenerator = referenceGenerator;
            _logger = logger;
        }

        public async Task<VolunteerContract> GenerateContractAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            var onboarding = await _context.VolunteerOnboardings
                .Include(x => x.Applicant)

                .Include(x => x.VolunteerApplication)
                    .ThenInclude(x => x.VolunteerOpportunity)
                        .ThenInclude(x => x.Requirements)

                .Include(x => x.VolunteerApplication)
                    .ThenInclude(x => x.VolunteerOpportunity)
                        .ThenInclude(x => x.Responsibilities)

                .Include(x => x.VolunteerApplication)
                    .ThenInclude(x => x.VolunteerOpportunity)
                        .ThenInclude(x => x.RequiredSkills)

                .Include(x => x.Offers)

                .Include(x => x.PolicyAcceptances)
                    .ThenInclude(x => x.VolunteerPolicy)

                .FirstOrDefaultAsync(
                    x => x.VolunteerOnboardingId == onboardingId,
                    cancellationToken);

            if (onboarding == null)
            {
                throw new InvalidOperationException(
                    "The volunteer onboarding record could not be found.");
            }

            if (onboarding.VolunteerApplication == null)
            {
                throw new InvalidOperationException(
                    "The volunteer application could not be found.");
            }

            var application = onboarding.VolunteerApplication;

            if (application.VolunteerOpportunity == null)
            {
                throw new InvalidOperationException(
                    "The volunteer opportunity could not be found.");
            }

            if (onboarding.Applicant == null)
            {
                throw new InvalidOperationException(
                    "The applicant could not be found.");
            }

            // ---------------------------------------------------------
            // ACCEPTED OFFER
            // ---------------------------------------------------------

            var acceptedOffer = onboarding.Offers
                .Where(x => x.Status == VolunteerOfferStatus.Accepted)
                .OrderByDescending(x => x.AcceptedDate)
                .FirstOrDefault();

            if (acceptedOffer == null)
            {
                throw new InvalidOperationException(
                    "A volunteer offer must be accepted before a contract can be generated.");
            }

            // ---------------------------------------------------------
            // REQUIRED POLICIES
            // ---------------------------------------------------------

            var requiredPolicies = await _context.VolunteerPolicies
                .Where(x => x.IsActive && x.IsRequired)
                .ToListAsync(cancellationToken);

            var acceptedPolicyIds = onboarding.PolicyAcceptances
                .Where(x => x.Accepted)
                .Select(x => x.VolunteerPolicyId)
                .ToHashSet();

            var missingPolicies = requiredPolicies
                .Where(x => !acceptedPolicyIds.Contains(x.VolunteerPolicyId))
                .ToList();

            if (missingPolicies.Any())
            {
                throw new InvalidOperationException(
                    "All required volunteer policies must be accepted before the contract can be generated.");
            }

            // ---------------------------------------------------------
            // PREVENT DUPLICATE ACTIVE CONTRACTS
            // ---------------------------------------------------------

            var existingContract = await _context.VolunteerContracts
                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerOnboardingId == onboardingId &&
                        x.Status != VolunteerContractStatus.Cancelled,
                    cancellationToken);

            if (existingContract != null)
            {
                return existingContract;
            }

            var opportunity = application.VolunteerOpportunity;

            // ---------------------------------------------------------
            // SNAPSHOT REQUIREMENTS
            //
            // IMPORTANT:
            // Do NOT call x.ToString() here.
            //
            // x.ToString() produces:
            // SwiftCare.Models.VolunteerRequirement
            //
            // Instead, GetRequirementOrResponsibilityText()
            // extracts the actual stored text.
            // ---------------------------------------------------------

            var requirementsSnapshot = string.Join(
                Environment.NewLine,
                opportunity.Requirements
                    .Where(x => x != null)
                    .Select(x => GetRequirementOrResponsibilityText(x!))
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            // ---------------------------------------------------------
            // SNAPSHOT RESPONSIBILITIES
            //
            // IMPORTANT:
            // Do NOT call x.ToString() here.
            //
            // x.ToString() produces:
            // SwiftCare.Models.VolunteerResponsibility
            //
            // Instead, extract the actual responsibility text.
            // ---------------------------------------------------------

            var responsibilitiesSnapshot = string.Join(
                Environment.NewLine,
                opportunity.Responsibilities
                    .Where(x => x != null)
                    .Select(x => GetRequirementOrResponsibilityText(x!))
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            // ---------------------------------------------------------
            // SNAPSHOT REQUIRED SKILLS
            // ---------------------------------------------------------

            var requiredSkillsSnapshot = string.Join(
                Environment.NewLine,
                opportunity.RequiredSkills
                    .Where(x => x != null)
                    .Select(x => x.Skill)
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            // ---------------------------------------------------------
            // LOG SNAPSHOT INFORMATION
            // ---------------------------------------------------------

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Opportunity {OpportunityId} has {RequirementCount} requirements, {ResponsibilityCount} responsibilities and {SkillCount} required skills.",
                opportunity.VolunteerOpportunityId,
                opportunity.Requirements?.Count ?? 0,
                opportunity.Responsibilities?.Count ?? 0,
                opportunity.RequiredSkills?.Count ?? 0);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Requirements snapshot: {RequirementsSnapshot}",
                string.IsNullOrWhiteSpace(requirementsSnapshot)
                    ? "[NONE]"
                    : requirementsSnapshot);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Responsibilities snapshot: {ResponsibilitiesSnapshot}",
                string.IsNullOrWhiteSpace(responsibilitiesSnapshot)
                    ? "[NONE]"
                    : responsibilitiesSnapshot);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Required skills snapshot: {RequiredSkillsSnapshot}",
                string.IsNullOrWhiteSpace(requiredSkillsSnapshot)
                    ? "[NONE]"
                    : requiredSkillsSnapshot);

            // ---------------------------------------------------------
            // CONTRACT REFERENCE
            // ---------------------------------------------------------

            var contractReference =
                await _referenceGenerator.GenerateAsync(
                    cancellationToken);

            // ---------------------------------------------------------
            // CREATE CONTRACT
            // ---------------------------------------------------------

            var contract = new VolunteerContract
            {
                VolunteerOnboardingId =
                    onboarding.VolunteerOnboardingId,

                VolunteerApplicationId =
                    application.VolunteerApplicationId,

                VolunteerOpportunityId =
                    opportunity.VolunteerOpportunityId,

                VolunteerOfferId =
                    acceptedOffer.VolunteerOfferId,

                ApplicantId =
                    onboarding.ApplicantId,

                ContractReference =
                    contractReference,

                ContractVersion =
                    "1.0",

                PositionTitle =
                    opportunity.Title ?? "Volunteer Position",

                RoleDescription =
                    opportunity.Description,

                Category =
                    opportunity.Category,

                Area =
                    opportunity.Area,

                CityName =
                    opportunity.CityName,

                Province =
                    opportunity.Province,

                StartDate =
                    opportunity.StartDate,

                EndDate =
                    opportunity.EndDate,

                StartTime =
                    opportunity.StartTime,

                EndTime =
                    opportunity.EndTime,

                MinimumAge =
                    opportunity.MinimumAge,

                RequirementsSnapshot =
                    string.IsNullOrWhiteSpace(requirementsSnapshot)
                        ? null
                        : requirementsSnapshot,

                ResponsibilitiesSnapshot =
                    string.IsNullOrWhiteSpace(responsibilitiesSnapshot)
                        ? null
                        : responsibilitiesSnapshot,

                RequiredSkillsSnapshot =
                    string.IsNullOrWhiteSpace(requiredSkillsSnapshot)
                        ? null
                        : requiredSkillsSnapshot,

                Status =
                    VolunteerContractStatus.Draft,

                CreatedDate =
                    DateTime.Now,

                ModifiedDate =
                    DateTime.Now
            };

            _context.VolunteerContracts.Add(contract);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Contract {ContractReference} created for onboarding {OnboardingId}.",
                contract.ContractReference,
                onboardingId);

            return contract;
        }

        // =============================================================
        // EXTRACT ACTUAL REQUIREMENT / RESPONSIBILITY TEXT
        // =============================================================
        //
        // This prevents:
        //
        // SwiftCare.Models.VolunteerResponsibility
        //
        // and:
        //
        // SwiftCare.Models.VolunteerRequirement
        //
        // from appearing in the contract.
        //
        // The method checks the likely text properties used by the
        // SwiftCare requirement/responsibility models.
        // =============================================================

        private static string? GetRequirementOrResponsibilityText(
            object item)
        {
            if (item == null)
            {
                return null;
            }

            var itemType = item.GetType();

            // ---------------------------------------------------------
            // MOST LIKELY PROPERTY: Description
            // ---------------------------------------------------------

            var descriptionProperty =
                itemType.GetProperty(
                    "Description",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (descriptionProperty != null)
            {
                var value =
                    descriptionProperty.GetValue(item)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // ---------------------------------------------------------
            // ALTERNATIVE: Requirement
            // ---------------------------------------------------------

            var requirementProperty =
                itemType.GetProperty(
                    "Requirement",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (requirementProperty != null)
            {
                var value =
                    requirementProperty.GetValue(item)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // ---------------------------------------------------------
            // ALTERNATIVE: Responsibility
            // ---------------------------------------------------------

            var responsibilityProperty =
                itemType.GetProperty(
                    "Responsibility",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (responsibilityProperty != null)
            {
                var value =
                    responsibilityProperty.GetValue(item)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // ---------------------------------------------------------
            // ALTERNATIVE: Name
            // ---------------------------------------------------------

            var nameProperty =
                itemType.GetProperty(
                    "Name",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (nameProperty != null)
            {
                var value =
                    nameProperty.GetValue(item)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // ---------------------------------------------------------
            // ALTERNATIVE: Title
            // ---------------------------------------------------------

            var titleProperty =
                itemType.GetProperty(
                    "Title",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (titleProperty != null)
            {
                var value =
                    titleProperty.GetValue(item)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // ---------------------------------------------------------
            // ALTERNATIVE: Text
            // ---------------------------------------------------------

            var textProperty =
                itemType.GetProperty(
                    "Text",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (textProperty != null)
            {
                var value =
                    textProperty.GetValue(item)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // ---------------------------------------------------------
            // ALTERNATIVE: Value
            // ---------------------------------------------------------

            var valueProperty =
                itemType.GetProperty(
                    "Value",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (valueProperty != null)
            {
                var value =
                    valueProperty.GetValue(item)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // ---------------------------------------------------------
            // NOTHING FOUND
            // ---------------------------------------------------------

            return null;
        }

        public async Task<VolunteerContract?> GetContractAsync(
            int contractId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .Include(x => x.VolunteerApplication)
                .Include(x => x.VolunteerOpportunity)
                .Include(x => x.VolunteerOffer)
                .Include(x => x.Applicant)
                .FirstOrDefaultAsync(
                    x => x.VolunteerContractId == contractId,
                    cancellationToken);
        }

        public async Task<VolunteerContract?> GetContractByOnboardingAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .Include(x => x.VolunteerApplication)
                .Include(x => x.VolunteerOpportunity)
                .Include(x => x.VolunteerOffer)
                .Include(x => x.Applicant)
                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerOnboardingId == onboardingId &&
                        x.Status != VolunteerContractStatus.Cancelled,
                    cancellationToken);
        }
    }

}
