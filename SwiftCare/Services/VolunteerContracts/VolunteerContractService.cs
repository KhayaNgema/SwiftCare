using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerOnboarding;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerContracts
{
    public class VolunteerContractService : IVolunteerContractService
    {
        private readonly SwiftCareDbContext _context;
        private readonly VolunteerContractReferenceGenerator _referenceGenerator;
        private readonly FileUploadService _fileUploadService;
        private readonly IVolunteerContractPdfGenerator _pdfGenerator;
        private readonly ILogger<VolunteerContractService> _logger;

        public VolunteerContractService(
            SwiftCareDbContext context,
            VolunteerContractReferenceGenerator referenceGenerator,
            FileUploadService fileUploadService,
            IVolunteerContractPdfGenerator pdfGenerator,
            ILogger<VolunteerContractService> logger)
        {
            _context = context;
            _referenceGenerator = referenceGenerator;
            _fileUploadService = fileUploadService;
            _pdfGenerator = pdfGenerator;
            _logger = logger;
        }

        // ============================================================
        // CREATE + GENERATE VOLUNTEER SERVICE AGREEMENT
        // ============================================================

        public async Task<VolunteerContract>
            CreateAndGenerateContractAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Starting contract generation for OnboardingId {OnboardingId}.",
                onboardingId);

            // ========================================================
            // LOAD ONBOARDING
            // ========================================================

            var onboarding =
                await _context.VolunteerOnboardings

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
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Onboarding {OnboardingId} was not found.",
                    onboardingId);

                throw new InvalidOperationException(
                    "The volunteer onboarding record could not be found.");
            }

            // ========================================================
            // APPLICATION
            // ========================================================

            if (onboarding.VolunteerApplication == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Onboarding {OnboardingId} has no volunteer application.",
                    onboardingId);

                throw new InvalidOperationException(
                    "The volunteer application could not be found.");
            }

            var application =
                onboarding.VolunteerApplication;

            // ========================================================
            // OPPORTUNITY
            // ========================================================

            if (application.VolunteerOpportunity == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Application {ApplicationId} has no volunteer opportunity.",
                    application.VolunteerApplicationId);

                throw new InvalidOperationException(
                    "The volunteer opportunity could not be found.");
            }

            var opportunity =
                application.VolunteerOpportunity;

            // ========================================================
            // APPLICANT
            // ========================================================

            if (onboarding.Applicant == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Onboarding {OnboardingId} has no applicant.",
                    onboardingId);

                throw new InvalidOperationException(
                    "The applicant could not be found.");
            }

            // ========================================================
            // IDENTITY VERIFICATION
            // ========================================================
            //
            // Contract generation is only allowed after Didit has
            // successfully completed:
            //
            // Document verification
            // Liveness verification
            // Face match verification
            //
            // ========================================================

            var identityVerification =
                await _context.VolunteerIdentityVerifications
                    .AsNoTracking()
                    .Where(x =>
                        x.VolunteerOnboardingId ==
                        onboardingId)
                    .OrderByDescending(x => x.VerifiedDate)
                    .ThenByDescending(x => x.ModifiedDate)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (identityVerification == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] No identity verification exists for OnboardingId {OnboardingId}.",
                    onboardingId);

                throw new InvalidOperationException(
                    "Identity verification must be completed before the volunteer service agreement can be generated.");
            }

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Identity verification found. " +
                "VerificationId={VerificationId}, Status={Status}, " +
                "DocumentVerified={DocumentVerified}, " +
                "LivenessVerified={LivenessVerified}, " +
                "FaceMatchVerified={FaceMatchVerified}.",
                identityVerification.Id,
                identityVerification.Status,
                identityVerification.DocumentVerified,
                identityVerification.LivenessVerified,
                identityVerification.FaceMatchVerified);

            if (identityVerification.Status !=
                    VolunteerIdentityVerificationStatus.Verified ||
                !identityVerification.DocumentVerified ||
                !identityVerification.LivenessVerified ||
                !identityVerification.FaceMatchVerified)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Contract generation blocked. " +
                    "OnboardingId={OnboardingId}, Status={Status}, " +
                    "Document={Document}, Liveness={Liveness}, FaceMatch={FaceMatch}.",
                    onboardingId,
                    identityVerification.Status,
                    identityVerification.DocumentVerified,
                    identityVerification.LivenessVerified,
                    identityVerification.FaceMatchVerified);

                throw new InvalidOperationException(
                    "Identity verification must be completed successfully before the volunteer service agreement can be generated.");
            }

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Identity verification requirements passed for OnboardingId {OnboardingId}.",
                onboardingId);

            // ========================================================
            // ACCEPTED OFFER
            // ========================================================

            var acceptedOffer =
                onboarding.Offers
                    .Where(x =>
                        x.Status ==
                        VolunteerOfferStatus.Accepted)
                    .OrderByDescending(x => x.AcceptedDate)
                    .FirstOrDefault();

            if (acceptedOffer == null)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] No accepted offer exists for OnboardingId {OnboardingId}.",
                    onboardingId);

                throw new InvalidOperationException(
                    "A volunteer offer must be accepted before a contract can be generated.");
            }

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Accepted offer {OfferId} found for OnboardingId {OnboardingId}.",
                acceptedOffer.VolunteerOfferId,
                onboardingId);

            // ========================================================
            // REQUIRED POLICIES
            // ========================================================

            var requiredPolicies =
                await _context.VolunteerPolicies
                    .Where(x =>
                        x.IsActive &&
                        x.IsRequired)
                    .ToListAsync(
                        cancellationToken);

            var acceptedPolicyIds =
                onboarding.PolicyAcceptances
                    .Where(x => x.Accepted)
                    .Select(x => x.VolunteerPolicyId)
                    .ToHashSet();

            var missingPolicies =
                requiredPolicies
                    .Where(x =>
                        !acceptedPolicyIds.Contains(
                            x.VolunteerPolicyId))
                    .ToList();

            if (missingPolicies.Any())
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Contract generation blocked. " +
                    "{MissingPolicyCount} required policies are not accepted. " +
                    "OnboardingId={OnboardingId}.",
                    missingPolicies.Count,
                    onboardingId);

                throw new InvalidOperationException(
                    "All required volunteer policies must be accepted before the contract can be generated.");
            }

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] All required policies accepted for OnboardingId {OnboardingId}.",
                onboardingId);

            // ========================================================
            // CHECK EXISTING CONTRACT
            // ========================================================
            //
            // IMPORTANT:
            //
            // If a contract already exists but its GeneratedFilePath
            // is missing, DO NOT simply return it.
            //
            // This repairs the exact situation we currently have:
            //
            // ContractId=3
            // ContractReference=SC-CON-260918-7435
            // GeneratedFilePath=NULL
            //
            // ========================================================

            var existingContract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (existingContract != null)
            {
                _logger.LogInformation(
                    "[SWIFTCARE CONTRACT] Existing contract found. " +
                    "ContractId={ContractId}, Reference={ContractReference}, " +
                    "Status={Status}, GeneratedFileName={GeneratedFileName}, " +
                    "GeneratedFilePath={GeneratedFilePath}.",
                    existingContract.VolunteerContractId,
                    existingContract.ContractReference,
                    existingContract.Status,
                    existingContract.GeneratedFileName,
                    existingContract.GeneratedFilePath);

                // ----------------------------------------------------
                // EXISTING PDF ALREADY AVAILABLE
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        existingContract.GeneratedFilePath))
                {
                    _logger.LogInformation(
                        "[SWIFTCARE CONTRACT] Existing contract already has a generated PDF. " +
                        "ContractId={ContractId}, Path={GeneratedFilePath}. " +
                        "No duplicate contract will be generated.",
                        existingContract.VolunteerContractId,
                        existingContract.GeneratedFilePath);

                    if (existingContract.Status ==
                        VolunteerContractStatus.Generated)
                    {
                        existingContract.Status =
                            VolunteerContractStatus.AwaitingApplicant;

                        existingContract.ModifiedDate =
                            DateTime.UtcNow;

                        existingContract.ModifiedBy =
                            onboarding.ApplicantId;

                        await _context.SaveChangesAsync(
                            cancellationToken);
                    }

                    return existingContract;
                }

                // ----------------------------------------------------
                // EXISTING CONTRACT BUT PDF PATH IS MISSING
                // ----------------------------------------------------
                //
                // THIS IS THE REPAIR PATH FOR YOUR CURRENT CONTRACT #3.
                //
                // ----------------------------------------------------

                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Existing contract has no generated PDF path. " +
                    "The PDF will now be generated. " +
                    "ContractId={ContractId}, Reference={ContractReference}, " +
                    "OnboardingId={OnboardingId}.",
                    existingContract.VolunteerContractId,
                    existingContract.ContractReference,
                    onboardingId);

                var repairedContract =
                    await _pdfGenerator.GeneratePdfAsync(
                        existingContract,
                        cancellationToken);

                if (repairedContract == null)
                {
                    throw new InvalidOperationException(
                        "The volunteer service agreement PDF generator returned no contract.");
                }

                if (string.IsNullOrWhiteSpace(
                        repairedContract.GeneratedFilePath))
                {
                    _logger.LogError(
                        "[SWIFTCARE CONTRACT] PDF generator completed but GeneratedFilePath is still empty. " +
                        "ContractId={ContractId}, Reference={ContractReference}.",
                        repairedContract.VolunteerContractId,
                        repairedContract.ContractReference);

                    throw new InvalidOperationException(
                        "The volunteer service agreement PDF was generated but its file path was not saved.");
                }

                _logger.LogInformation(
                    "[SWIFTCARE CONTRACT] Existing contract repaired successfully. " +
                    "ContractId={ContractId}, Reference={ContractReference}, " +
                    "GeneratedFileName={GeneratedFileName}, " +
                    "GeneratedFilePath={GeneratedFilePath}.",
                    repairedContract.VolunteerContractId,
                    repairedContract.ContractReference,
                    repairedContract.GeneratedFileName,
                    repairedContract.GeneratedFilePath);

                return repairedContract;
            }

            // ========================================================
            // SNAPSHOT REQUIREMENTS
            // ========================================================

            var requirementsSnapshot =
                string.Join(
                    Environment.NewLine,
                    opportunity.Requirements?
                        .Where(x => x != null)
                        .Select(
                            x =>
                                GetRequirementOrResponsibilityText(
                                    x!))
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                    ?? Enumerable.Empty<string>());

            // ========================================================
            // SNAPSHOT RESPONSIBILITIES
            // ========================================================

            var responsibilitiesSnapshot =
                string.Join(
                    Environment.NewLine,
                    opportunity.Responsibilities?
                        .Where(x => x != null)
                        .Select(
                            x =>
                                GetRequirementOrResponsibilityText(
                                    x!))
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                    ?? Enumerable.Empty<string>());

            // ========================================================
            // SNAPSHOT REQUIRED SKILLS
            // ========================================================

            var requiredSkillsSnapshot =
                string.Join(
                    Environment.NewLine,
                    opportunity.RequiredSkills?
                        .Where(x => x != null)
                        .Select(x => x.Skill)
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                    ?? Enumerable.Empty<string>());

            // ========================================================
            // LOG SNAPSHOT INFORMATION
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Opportunity {OpportunityId} contains " +
                "{RequirementCount} requirements, " +
                "{ResponsibilityCount} responsibilities and " +
                "{SkillCount} required skills.",
                opportunity.VolunteerOpportunityId,
                opportunity.Requirements?.Count ?? 0,
                opportunity.Responsibilities?.Count ?? 0,
                opportunity.RequiredSkills?.Count ?? 0);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Requirements snapshot: {RequirementsSnapshot}",
                string.IsNullOrWhiteSpace(
                    requirementsSnapshot)
                    ? "[NONE]"
                    : requirementsSnapshot);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Responsibilities snapshot: {ResponsibilitiesSnapshot}",
                string.IsNullOrWhiteSpace(
                    responsibilitiesSnapshot)
                    ? "[NONE]"
                    : responsibilitiesSnapshot);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Required skills snapshot: {RequiredSkillsSnapshot}",
                string.IsNullOrWhiteSpace(
                    requiredSkillsSnapshot)
                    ? "[NONE]"
                    : requiredSkillsSnapshot);

            // ========================================================
            // CONTRACT REFERENCE
            // ========================================================

            var contractReference =
                await _referenceGenerator.GenerateAsync(
                    cancellationToken);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Generated contract reference {ContractReference} for OnboardingId {OnboardingId}.",
                contractReference,
                onboardingId);

            // ========================================================
            // CREATE CONTRACT
            // ========================================================

            var now =
                DateTime.UtcNow;

            var contract =
                new VolunteerContract
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
                        opportunity.Title ??
                        "Volunteer Position",

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
                        string.IsNullOrWhiteSpace(
                            requirementsSnapshot)
                            ? null
                            : requirementsSnapshot,

                    ResponsibilitiesSnapshot =
                        string.IsNullOrWhiteSpace(
                            responsibilitiesSnapshot)
                            ? null
                            : responsibilitiesSnapshot,

                    RequiredSkillsSnapshot =
                        string.IsNullOrWhiteSpace(
                            requiredSkillsSnapshot)
                            ? null
                            : requiredSkillsSnapshot,

                    Status =
                        VolunteerContractStatus.AwaitingApplicant,

                    GeneratedDate =
                        now,

                    CreatedDate =
                        now,

                    ModifiedDate =
                        now,

                    CreatedBy =
                        onboarding.ApplicantId,

                    ModifiedBy =
                        onboarding.ApplicantId
                };

            // ========================================================
            // SAVE CONTRACT FIRST
            // ========================================================
            //
            // We save first so the contract has its database ID.
            //
            // Then the PDF generator can populate:
            //
            // GeneratedFileName
            // GeneratedFilePath
            //
            // ========================================================

            _context.VolunteerContracts.Add(
                contract);

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Contract database record created. " +
                "ContractId={ContractId}, Reference={ContractReference}, " +
                "OnboardingId={OnboardingId}.",
                contract.VolunteerContractId,
                contract.ContractReference,
                onboardingId);

            // ========================================================
            // GENERATE PDF
            // ========================================================
            //
            // This was the missing operation in the old service.
            //
            // VolunteerContractPdfGenerator:
            //
            // 1. Builds the contract content.
            // 2. Loads VolunteerServiceAgreement.html.
            // 3. Populates placeholders.
            // 4. Creates the QuestPDF document.
            // 5. Writes the PDF to:
            //
            //    UploadedFiles/
            //        VolunteerContracts/
            //            Generated/
            //
            // 6. Sets GeneratedFileName.
            // 7. Sets GeneratedFilePath.
            // 8. Saves the contract.
            //
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Starting PDF generation. " +
                "ContractId={ContractId}, Reference={ContractReference}.",
                contract.VolunteerContractId,
                contract.ContractReference);

            var generatedContract =
                await _pdfGenerator.GeneratePdfAsync(
                    contract,
                    cancellationToken);

            if (generatedContract == null)
            {
                _logger.LogError(
                    "[SWIFTCARE CONTRACT] PDF generator returned null. " +
                    "ContractId={ContractId}, Reference={ContractReference}.",
                    contract.VolunteerContractId,
                    contract.ContractReference);

                throw new InvalidOperationException(
                    "The volunteer service agreement PDF could not be generated.");
            }

            // ========================================================
            // VERIFY GENERATED PDF PATH
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    generatedContract.GeneratedFilePath))
            {
                _logger.LogError(
                    "[SWIFTCARE CONTRACT] PDF generator returned a contract " +
                    "without GeneratedFilePath. " +
                    "ContractId={ContractId}, Reference={ContractReference}.",
                    generatedContract.VolunteerContractId,
                    generatedContract.ContractReference);

                throw new InvalidOperationException(
                    "The volunteer service agreement PDF was generated but its file path was not saved.");
            }

            if (string.IsNullOrWhiteSpace(
                    generatedContract.GeneratedFileName))
            {
                _logger.LogError(
                    "[SWIFTCARE CONTRACT] PDF generator returned a contract " +
                    "without GeneratedFileName. " +
                    "ContractId={ContractId}, Reference={ContractReference}.",
                    generatedContract.VolunteerContractId,
                    generatedContract.ContractReference);

                throw new InvalidOperationException(
                    "The volunteer service agreement PDF was generated but its file name was not saved.");
            }

            // ========================================================
            // FINAL LOG
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Volunteer Service Agreement generated successfully. " +
                "ContractId={ContractId}, Reference={ContractReference}, " +
                "OnboardingId={OnboardingId}, Status={Status}, " +
                "GeneratedFileName={GeneratedFileName}, " +
                "GeneratedFilePath={GeneratedFilePath}.",
                generatedContract.VolunteerContractId,
                generatedContract.ContractReference,
                onboardingId,
                generatedContract.Status,
                generatedContract.GeneratedFileName,
                generatedContract.GeneratedFilePath);

            return generatedContract;
        }

        // ============================================================
        // GET CONTRACT BY ONBOARDING
        // ============================================================

        public async Task<VolunteerContract?>
            GetContractByOnboardingAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()

                .Include(x => x.VolunteerApplication)

                .Include(x => x.VolunteerOpportunity)

                .Include(x => x.VolunteerOffer)

                .Include(x => x.Applicant)

                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        onboardingId,
                    cancellationToken);
        }

        // ============================================================
        // GET CONTRACT
        // ============================================================

        public async Task<VolunteerContract?>
            GetContractAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        onboardingId,
                    cancellationToken);
        }

        // ============================================================
        // GET CONTRACT BY ID
        // ============================================================

        public async Task<VolunteerContract?>
            GetContractByIdAsync(
                int contractId,
                CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()

                .Include(x => x.VolunteerApplication)

                .Include(x => x.VolunteerOpportunity)

                .Include(x => x.VolunteerOffer)

                .Include(x => x.Applicant)

                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerContractId ==
                        contractId,
                    cancellationToken);
        }

        // ============================================================
        // CONTRACT EXISTS
        // ============================================================

        public async Task<bool>
            ContractExistsAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        onboardingId,
                    cancellationToken);
        }

        // ============================================================
        // UPLOAD COMPLETED CONTRACT
        // ============================================================

        public async Task<VolunteerContract>
            UploadCompletedContractAsync(
                int onboardingId,
                IFormFile file,
                string applicantId,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(applicantId))
            {
                throw new ArgumentException(
                    "Applicant ID is required.",
                    nameof(applicantId));
            }

            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException(
                    "Please select a completed contract to upload.");
            }

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Applicant {ApplicantId} uploading completed contract for OnboardingId {OnboardingId}. File={FileName}, Size={FileSize}.",
                applicantId,
                onboardingId,
                file.FileName,
                file.Length);

            // ========================================================
            // LOAD ONBOARDING
            // ========================================================

            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                throw new InvalidOperationException(
                    "The volunteer onboarding record could not be found.");
            }

            // ========================================================
            // OWNERSHIP
            // ========================================================

            if (onboarding.ApplicantId != applicantId)
            {
                _logger.LogWarning(
                    "[SWIFTCARE CONTRACT] Unauthorized contract upload attempt. " +
                    "OnboardingId={OnboardingId}, ApplicantId={ApplicantId}.",
                    onboardingId,
                    applicantId);

                throw new UnauthorizedAccessException(
                    "You are not authorized to upload this contract.");
            }

            // ========================================================
            // LOAD CONTRACT
            // ========================================================

            var contract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (contract == null)
            {
                throw new InvalidOperationException(
                    "A volunteer service agreement has not yet been generated.");
            }

            // ========================================================
            // VALID STATUS
            // ========================================================

            if (contract.Status !=
                    VolunteerContractStatus.AwaitingApplicant &&
                contract.Status !=
                    VolunteerContractStatus.CorrectionRequired)
            {
                throw new InvalidOperationException(
                    "This contract is not currently available for submission.");
            }

            // ========================================================
            // VALIDATE FILE
            // ========================================================

            ValidateContractFile(file);

            // ========================================================
            // UPLOAD THROUGH FILE UPLOAD SERVICE
            // ========================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Uploading completed contract through FileUploadService. " +
                "OnboardingId={OnboardingId}, File={FileName}.",
                onboardingId,
                file.FileName);

            var signedFilePath =
                await _fileUploadService.UploadFileAsync(
                    file);

            // ========================================================
            // UPDATE CONTRACT
            // ========================================================

            contract.SignedFileName =
                Path.GetFileName(
                    file.FileName);

            contract.SignedFilePath =
                signedFilePath;

            contract.SignedDate =
                DateTime.UtcNow;

            contract.Status =
                VolunteerContractStatus.Submitted;

            contract.RejectionReason =
                null;

            contract.ReviewedDate =
                null;

            contract.ReviewedByAdminId =
                null;

            contract.ModifiedDate =
                DateTime.UtcNow;

            contract.ModifiedBy =
                applicantId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Contract {ContractReference} submitted successfully. " +
                "OnboardingId={OnboardingId}, ApplicantId={ApplicantId}, File={SignedFilePath}.",
                contract.ContractReference,
                onboardingId,
                applicantId,
                signedFilePath);

            return contract;
        }

        // ============================================================
        // RESUBMIT CONTRACT
        // ============================================================

        public async Task<VolunteerContract>
            ResubmitContractAsync(
                int onboardingId,
                IFormFile file,
                string applicantId,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(applicantId))
            {
                throw new ArgumentException(
                    "Applicant ID is required.",
                    nameof(applicantId));
            }

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Contract resubmission requested. " +
                "OnboardingId={OnboardingId}, ApplicantId={ApplicantId}.",
                onboardingId,
                applicantId);

            var contract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (contract == null)
            {
                throw new InvalidOperationException(
                    "The volunteer service agreement could not be found.");
            }

            if (contract.Status !=
                VolunteerContractStatus.CorrectionRequired)
            {
                throw new InvalidOperationException(
                    "This contract is not currently awaiting correction.");
            }

            return await UploadCompletedContractAsync(
                onboardingId,
                file,
                applicantId,
                cancellationToken);
        }

        // ============================================================
        // APPROVE CONTRACT
        // ============================================================

        public async Task<VolunteerContract>
            ApproveContractAsync(
                int onboardingId,
                string adminId,
                string? reviewNotes = null,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "Administrator ID is required.",
                    nameof(adminId));
            }

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Administrator {AdminId} attempting to approve contract for OnboardingId {OnboardingId}.",
                adminId,
                onboardingId);

            var contract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (contract == null)
            {
                throw new InvalidOperationException(
                    "The volunteer contract could not be found.");
            }

            // ========================================================
            // VALID STATUS
            // ========================================================

            if (contract.Status !=
                    VolunteerContractStatus.Submitted &&
                contract.Status !=
                    VolunteerContractStatus.UnderReview)
            {
                throw new InvalidOperationException(
                    "Only a submitted contract can be approved.");
            }

            // ========================================================
            // SIGNED FILE MUST EXIST
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    contract.SignedFilePath))
            {
                throw new InvalidOperationException(
                    "The completed contract has not been uploaded.");
            }

            // ========================================================
            // APPROVE
            // ========================================================

            contract.Status =
                VolunteerContractStatus.Approved;

            contract.ReviewedByAdminId =
                adminId;

            contract.ReviewedDate =
                DateTime.UtcNow;

            contract.RejectionReason =
                null;

            contract.ModifiedDate =
                DateTime.UtcNow;

            contract.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Contract {ContractReference} approved successfully. " +
                "OnboardingId={OnboardingId}, AdminId={AdminId}.",
                contract.ContractReference,
                onboardingId,
                adminId);

            return contract;
        }

        // ============================================================
        // REQUEST CORRECTION
        // ============================================================

        public async Task<VolunteerContract>
            RequestCorrectionAsync(
                int onboardingId,
                string adminId,
                string correctionReason,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "Administrator ID is required.",
                    nameof(adminId));
            }

            if (string.IsNullOrWhiteSpace(correctionReason))
            {
                throw new ArgumentException(
                    "A correction reason is required.",
                    nameof(correctionReason));
            }

            var contract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (contract == null)
            {
                throw new InvalidOperationException(
                    "The volunteer contract could not be found.");
            }

            // ========================================================
            // VALID STATUS
            // ========================================================

            if (contract.Status !=
                    VolunteerContractStatus.Submitted &&
                contract.Status !=
                    VolunteerContractStatus.UnderReview)
            {
                throw new InvalidOperationException(
                    "Only a submitted contract can be returned for correction.");
            }

            // ========================================================
            // REQUEST CORRECTION
            // ========================================================

            contract.Status =
                VolunteerContractStatus.CorrectionRequired;

            contract.ReviewedByAdminId =
                adminId;

            contract.ReviewedDate =
                DateTime.UtcNow;

            contract.RejectionReason =
                correctionReason.Trim();

            contract.ModifiedDate =
                DateTime.UtcNow;

            contract.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogWarning(
                "[SWIFTCARE CONTRACT] Correction requested for Contract {ContractReference}. " +
                "OnboardingId={OnboardingId}, AdminId={AdminId}, Reason={Reason}.",
                contract.ContractReference,
                onboardingId,
                adminId,
                correctionReason);

            return contract;
        }

        // ============================================================
        // REJECT CONTRACT
        // ============================================================

        public async Task<VolunteerContract>
            RejectContractAsync(
                int onboardingId,
                string adminId,
                string rejectionReason,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "Administrator ID is required.",
                    nameof(adminId));
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                throw new ArgumentException(
                    "A rejection reason is required.",
                    nameof(rejectionReason));
            }

            var contract =
                await _context.VolunteerContracts
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            onboardingId,
                        cancellationToken);

            if (contract == null)
            {
                throw new InvalidOperationException(
                    "The volunteer contract could not be found.");
            }

            // ========================================================
            // VALID STATUS
            // ========================================================

            if (contract.Status !=
                    VolunteerContractStatus.Submitted &&
                contract.Status !=
                    VolunteerContractStatus.UnderReview &&
                contract.Status !=
                    VolunteerContractStatus.CorrectionRequired)
            {
                throw new InvalidOperationException(
                    "This contract cannot currently be rejected.");
            }

            // ========================================================
            // REJECT
            // ========================================================

            contract.Status =
                VolunteerContractStatus.Rejected;

            contract.ReviewedByAdminId =
                adminId;

            contract.ReviewedDate =
                DateTime.UtcNow;

            contract.RejectionReason =
                rejectionReason.Trim();

            contract.ModifiedDate =
                DateTime.UtcNow;

            contract.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogWarning(
                "[SWIFTCARE CONTRACT] Contract {ContractReference} rejected. " +
                "OnboardingId={OnboardingId}, AdminId={AdminId}, Reason={Reason}.",
                contract.ContractReference,
                onboardingId,
                adminId,
                rejectionReason);

            return contract;
        }

        // ============================================================
        // CONTRACT COMPLETED?
        // ============================================================

        public async Task<bool>
            IsContractCompletedAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                            onboardingId &&
                        x.Status ==
                            VolunteerContractStatus.Approved,
                    cancellationToken);
        }

        // ============================================================
        // CONTRACT AWAITING REVIEW?
        // ============================================================

        public async Task<bool>
            IsContractAwaitingReviewAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                            onboardingId &&
                        (
                            x.Status ==
                                VolunteerContractStatus.Submitted ||
                            x.Status ==
                                VolunteerContractStatus.UnderReview
                        ),
                    cancellationToken);
        }

        // ============================================================
        // CORRECTION REQUIRED?
        // ============================================================

        public async Task<bool>
            IsContractCorrectionRequiredAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                            onboardingId &&
                        x.Status ==
                            VolunteerContractStatus.CorrectionRequired,
                    cancellationToken);
        }

        // ============================================================
        // EXTRACT ACTUAL REQUIREMENT / RESPONSIBILITY TEXT
        // ============================================================

        private static string?
            GetRequirementOrResponsibilityText(
                object item)
        {
            if (item == null)
            {
                return null;
            }

            var itemType =
                item.GetType();

            // --------------------------------------------------------
            // DESCRIPTION
            // --------------------------------------------------------

            var descriptionProperty =
                itemType.GetProperty(
                    "Description",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (descriptionProperty != null)
            {
                var value =
                    descriptionProperty
                        .GetValue(item)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // --------------------------------------------------------
            // REQUIREMENT
            // --------------------------------------------------------

            var requirementProperty =
                itemType.GetProperty(
                    "Requirement",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (requirementProperty != null)
            {
                var value =
                    requirementProperty
                        .GetValue(item)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // --------------------------------------------------------
            // RESPONSIBILITY
            // --------------------------------------------------------

            var responsibilityProperty =
                itemType.GetProperty(
                    "Responsibility",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (responsibilityProperty != null)
            {
                var value =
                    responsibilityProperty
                        .GetValue(item)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // --------------------------------------------------------
            // NAME
            // --------------------------------------------------------

            var nameProperty =
                itemType.GetProperty(
                    "Name",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (nameProperty != null)
            {
                var value =
                    nameProperty
                        .GetValue(item)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // --------------------------------------------------------
            // TITLE
            // --------------------------------------------------------

            var titleProperty =
                itemType.GetProperty(
                    "Title",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (titleProperty != null)
            {
                var value =
                    titleProperty
                        .GetValue(item)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // --------------------------------------------------------
            // TEXT
            // --------------------------------------------------------

            var textProperty =
                itemType.GetProperty(
                    "Text",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (textProperty != null)
            {
                var value =
                    textProperty
                        .GetValue(item)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            // --------------------------------------------------------
            // VALUE
            // --------------------------------------------------------

            var valueProperty =
                itemType.GetProperty(
                    "Value",
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (valueProperty != null)
            {
                var value =
                    valueProperty
                        .GetValue(item)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        // ============================================================
        // VALIDATE CONTRACT FILE
        // ============================================================

        private static void ValidateContractFile(
            IFormFile file)
        {
            const long maxFileSize =
                25 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                throw new InvalidOperationException(
                    "The completed contract cannot exceed 25 MB.");
            }

            var extension =
                Path.GetExtension(
                    file.FileName)
                    .ToLowerInvariant();

            if (extension != ".pdf")
            {
                throw new InvalidOperationException(
                    "The completed volunteer contract must be uploaded as a PDF file.");
            }

            if (!string.Equals(
                    file.ContentType,
                    "application/pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The uploaded contract must be a PDF file.");
            }
        }
    }
}