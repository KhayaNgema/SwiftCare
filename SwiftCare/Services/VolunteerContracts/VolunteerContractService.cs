using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services.VolunteerOnboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private const long MaxContractFileSize =
            25 * 1024 * 1024;

        private const string RequiredContractContentType =
            "application/pdf";

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
        // CREATE + GENERATE CONTRACT
        // ============================================================

        public async Task<VolunteerContract> CreateAndGenerateContractAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Starting contract generation. " +
                "OnboardingId={OnboardingId}",
                onboardingId);

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.VolunteerOpportunity)
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
                    "[VOLUNTEER CONTRACT] Onboarding not found. " +
                    "OnboardingId={OnboardingId}",
                    onboardingId);

                throw new InvalidOperationException(
                    "The volunteer onboarding record could not be found.");
            }

            if (onboarding.VolunteerApplication == null)
            {
                throw new InvalidOperationException(
                    "The volunteer application could not be found.");
            }

            if (onboarding.VolunteerApplication.VolunteerOpportunity == null)
            {
                throw new InvalidOperationException(
                    "The volunteer opportunity could not be found.");
            }

            if (onboarding.Applicant == null)
            {
                throw new InvalidOperationException(
                    "The applicant could not be found.");
            }

            var opportunity =
                onboarding.VolunteerApplication.VolunteerOpportunity;

            var applicant =
                onboarding.Applicant;

            // --------------------------------------------------------
            // DIDIT VERIFICATION
            // --------------------------------------------------------

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
                    "[VOLUNTEER CONTRACT] No identity verification found. " +
                    "OnboardingId={OnboardingId}",
                    onboardingId);

                throw new InvalidOperationException(
                    "Identity verification has not been completed.");
            }

            if (identityVerification.Status !=
                VolunteerIdentityVerificationStatus.Verified)
            {
                throw new InvalidOperationException(
                    $"Identity verification is not complete. " +
                    $"Current status: {identityVerification.Status}.");
            }

            if (!identityVerification.DocumentVerified ||
                !identityVerification.LivenessVerified ||
                !identityVerification.FaceMatchVerified)
            {
                throw new InvalidOperationException(
                    "Identity verification is marked as verified, " +
                    "but the document, liveness, and face verification " +
                    "checks have not all completed successfully.");
            }

            // --------------------------------------------------------
            // ACCEPTED OFFER
            // --------------------------------------------------------

            var acceptedOffer =
                onboarding.Offers?
                    .Where(x =>
                        x.Status ==
                        VolunteerOfferStatus.Accepted)
                    .OrderByDescending(x => x.AcceptedDate)
                    .ThenByDescending(x => x.CreatedDate)
                    .FirstOrDefault();

            if (acceptedOffer == null)
            {
                throw new InvalidOperationException(
                    "The volunteer offer has not been accepted.");
            }

            // --------------------------------------------------------
            // REQUIRED POLICIES
            // --------------------------------------------------------

            var requiredPolicies =
                await _context.VolunteerPolicies
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.IsRequired)
                    .Select(x => x.VolunteerPolicyId)
                    .ToListAsync(cancellationToken);

            var acceptedPolicyIds =
                onboarding.PolicyAcceptances?
                    .Where(x => x.Accepted)
                    .Select(x => x.VolunteerPolicyId)
                    .ToHashSet()
                ?? new HashSet<int>();

            var missingPolicies =
                requiredPolicies
                    .Where(x =>
                        !acceptedPolicyIds.Contains(x))
                    .ToList();

            if (missingPolicies.Any())
            {
                _logger.LogWarning(
                    "[VOLUNTEER CONTRACT] Required policies are incomplete. " +
                    "OnboardingId={OnboardingId}, MissingCount={MissingCount}",
                    onboardingId,
                    missingPolicies.Count);

                throw new InvalidOperationException(
                    "The applicant has not accepted all required policies.");
            }

            // --------------------------------------------------------
            // EXISTING CONTRACT
            // --------------------------------------------------------

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
                    "[VOLUNTEER CONTRACT] Existing contract found. " +
                    "ContractId={ContractId}, Status={Status}, " +
                    "GeneratedFilePath={GeneratedFilePath}",
                    existingContract.VolunteerContractId,
                    existingContract.Status,
                    existingContract.GeneratedFilePath);

                // ----------------------------------------------------
                // Existing generated file is still valid.
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        existingContract.GeneratedFilePath))
                {
                    if (existingContract.Status ==
                        VolunteerContractStatus.Generated)
                    {
                        existingContract.Status =
                            VolunteerContractStatus.AwaitingApplicant;

                        existingContract.ModifiedDate =
                            DateTime.Now;

                        existingContract.ModifiedBy =
                            applicant.Id;

                        await _context.SaveChangesAsync(
                            cancellationToken);
                    }

                    return existingContract;
                }

                // ----------------------------------------------------
                // Existing contract record but generated file missing.
                // Repair the PDF instead of creating another contract.
                // ----------------------------------------------------

                _logger.LogWarning(
                    "[VOLUNTEER CONTRACT] Existing contract has no " +
                    "generated file. Regenerating. ContractId={ContractId}",
                    existingContract.VolunteerContractId);

                var repairedContract =
                    await _pdfGenerator.GeneratePdfAsync(
                        existingContract,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(
                        repairedContract.GeneratedFilePath))
                {
                    throw new InvalidOperationException(
                        "The volunteer contract PDF could not be generated.");
                }

                return repairedContract;
            }

            // --------------------------------------------------------
            // SNAPSHOT OPPORTUNITY DATA
            // --------------------------------------------------------

            var requirementsSnapshot =
                BuildSnapshot(
                    opportunity.Requirements);

            var responsibilitiesSnapshot =
                BuildSnapshot(
                    opportunity.Responsibilities);

            var requiredSkillsSnapshot =
                BuildSnapshot(
                    opportunity.RequiredSkills);

            // --------------------------------------------------------
            // CONTRACT REFERENCE
            // --------------------------------------------------------

            var contractReference =
                await _referenceGenerator.GenerateAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(contractReference))
            {
                throw new InvalidOperationException(
                    "A contract reference could not be generated.");
            }

            var now =
                DateTime.Now;

            // --------------------------------------------------------
            // CREATE CONTRACT
            // --------------------------------------------------------

            var contract =
                new VolunteerContract
                {
                    VolunteerOnboardingId =
                        onboarding.VolunteerOnboardingId,

                    VolunteerApplicationId =
                        onboarding.VolunteerApplicationId,

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
                        opportunity.Title,

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
                        requirementsSnapshot,

                    ResponsibilitiesSnapshot =
                        responsibilitiesSnapshot,

                    RequiredSkillsSnapshot =
                        requiredSkillsSnapshot,

                    AdditionalClausesSnapshot =
                        null,

                    Status =
                        VolunteerContractStatus.Generated,

                    GeneratedDate =
                        now,

                    CreatedDate =
                        now,

                    ModifiedDate =
                        now,

                    CreatedBy =
                        applicant.Id,

                    ModifiedBy =
                        applicant.Id
                };

            _context.VolunteerContracts.Add(contract);

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract record created. " +
                "ContractId={ContractId}, Reference={ContractReference}, " +
                "OnboardingId={OnboardingId}",
                contract.VolunteerContractId,
                contract.ContractReference,
                onboardingId);

            // --------------------------------------------------------
            // GENERATE PDF
            // --------------------------------------------------------

            contract =
                await _pdfGenerator.GeneratePdfAsync(
                    contract,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(
                    contract.GeneratedFilePath))
            {
                throw new InvalidOperationException(
                    "The volunteer contract PDF was not generated.");
            }

            if (string.IsNullOrWhiteSpace(
                    contract.GeneratedFileName))
            {
                throw new InvalidOperationException(
                    "The generated volunteer contract filename is missing.");
            }

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract generated successfully. " +
                "ContractId={ContractId}, Reference={ContractReference}, " +
                "FilePath={FilePath}, Status={Status}",
                contract.VolunteerContractId,
                contract.ContractReference,
                contract.GeneratedFilePath,
                contract.Status);

            return contract;
        }

        // ============================================================
        // GET CONTRACT BY ONBOARDING
        // ============================================================

        public async Task<VolunteerContract?> GetContractByOnboardingAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()
                .Where(x =>
                    x.VolunteerOnboardingId ==
                    onboardingId)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.VolunteerContractId)
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        // ============================================================
        // GET CONTRACT
        // ============================================================

        public async Task<VolunteerContract?> GetContractAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await GetContractByOnboardingAsync(
                onboardingId,
                cancellationToken);
        }

        // ============================================================
        // GET CONTRACT BY ID
        // ============================================================

        public async Task<VolunteerContract?> GetContractByIdAsync(
            int contractId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.VolunteerContractId ==
                        contractId,
                    cancellationToken);
        }

        // ============================================================
        // CONTRACT EXISTS
        // ============================================================

        public async Task<bool> ContractExistsAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        onboardingId,
                    cancellationToken);
        }

        // ============================================================
        // UPLOAD SIGNED CONTRACT
        // ============================================================

        public async Task<VolunteerContract> UploadCompletedContractAsync(
            int onboardingId,
            IFormFile file,
            string applicantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(applicantId))
            {
                throw new ArgumentException(
                    "An applicant ID is required.",
                    nameof(applicantId));
            }

            if (file == null ||
                file.Length <= 0)
            {
                throw new ArgumentException(
                    "A signed contract file is required.",
                    nameof(file));
            }

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Signed contract upload started. " +
                "OnboardingId={OnboardingId}, ApplicantId={ApplicantId}, " +
                "FileName={FileName}, FileSize={FileSize}",
                onboardingId,
                applicantId,
                file.FileName,
                file.Length);

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

            if (!string.Equals(
                    onboarding.ApplicantId,
                    applicantId,
                    StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    "You are not authorised to upload a contract for this onboarding.");
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
                    "A generated volunteer contract could not be found.");
            }

            if (contract.Status !=
                    VolunteerContractStatus.AwaitingApplicant &&
                contract.Status !=
                    VolunteerContractStatus.CorrectionRequired)
            {
                throw new InvalidOperationException(
                    $"The signed contract cannot be uploaded while the " +
                    $"contract is in status '{contract.Status}'.");
            }

            ValidateContractFile(file);

            var signedFilePath =
                await _fileUploadService.UploadFileAsync(
                    file);

            if (string.IsNullOrWhiteSpace(signedFilePath))
            {
                throw new InvalidOperationException(
                    "The signed contract could not be uploaded.");
            }

            var now =
                DateTime.Now;

            contract.SignedFileName =
                Path.GetFileName(file.FileName);

            contract.SignedFilePath =
                signedFilePath;

            contract.SignedDate =
                now;

            contract.Status =
                VolunteerContractStatus.Submitted;

            contract.RejectionReason =
                null;

            contract.ReviewedDate =
                null;

            contract.ReviewedByAdminId =
                null;

            contract.ModifiedDate =
                now;

            contract.ModifiedBy =
                applicantId;

            // --------------------------------------------------------
            // The contract has now been submitted.
            // Record the onboarding submission date.
            // --------------------------------------------------------

            onboarding.ContractSubmittedDate =
                now;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                applicantId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Signed contract submitted. " +
                "ContractId={ContractId}, OnboardingId={OnboardingId}, " +
                "ApplicantId={ApplicantId}, Status={Status}",
                contract.VolunteerContractId,
                onboardingId,
                applicantId,
                contract.Status);

            return contract;
        }

        // ============================================================
        // RESUBMIT SIGNED CONTRACT
        // ============================================================

        public async Task<VolunteerContract> ResubmitContractAsync(
            int onboardingId,
            IFormFile file,
            string applicantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(applicantId))
            {
                throw new ArgumentException(
                    "An applicant ID is required.",
                    nameof(applicantId));
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

            if (contract.Status !=
                VolunteerContractStatus.CorrectionRequired)
            {
                throw new InvalidOperationException(
                    "This contract does not currently require correction.");
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

        public async Task<VolunteerContract> ApproveContractAsync(
            int onboardingId,
            string adminId,
            string? reviewNotes = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
                    nameof(adminId));
            }

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract approval started. " +
                "OnboardingId={OnboardingId}, AdminId={AdminId}",
                onboardingId,
                adminId);

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

            if (contract.Status !=
                    VolunteerContractStatus.Submitted &&
                contract.Status !=
                    VolunteerContractStatus.UnderReview)
            {
                throw new InvalidOperationException(
                    $"The contract cannot be approved while it is in " +
                    $"status '{contract.Status}'.");
            }

            if (string.IsNullOrWhiteSpace(
                    contract.SignedFilePath))
            {
                throw new InvalidOperationException(
                    "The applicant has not uploaded a signed contract.");
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Requirements)
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

            var contractRequirement =
                onboarding.Requirements
                    .FirstOrDefault(x =>
                        x.RequirementType ==
                        VolunteerOnboardingRequirementType.Contract);

            if (contractRequirement == null)
            {
                throw new InvalidOperationException(
                    "The Volunteer Service Agreement requirement could not be found.");
            }

            var now =
                DateTime.Now;

            // --------------------------------------------------------
            // APPROVE CONTRACT
            // --------------------------------------------------------

            contract.Status =
                VolunteerContractStatus.Approved;

            contract.ReviewedByAdminId =
                adminId;

            contract.ReviewedDate =
                now;

            contract.RejectionReason =
                null;

            contract.ModifiedDate =
                now;

            contract.ModifiedBy =
                adminId;

            // --------------------------------------------------------
            // COMPLETE CONTRACT REQUIREMENT
            // --------------------------------------------------------

            contractRequirement.IsCompleted =
                true;

            // --------------------------------------------------------
            // IMPORTANT:
            //
            // We deliberately do NOT move the onboarding directly
            // to ReadyForActivation here.
            //
            // Contract approval only completes the Contract
            // requirement.
            //
            // Final verification must still be performed by the
            // administrator through CompleteVerificationAsync().
            // --------------------------------------------------------

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract approved successfully. " +
                "ContractId={ContractId}, OnboardingId={OnboardingId}, " +
                "AdminId={AdminId}, RequirementId={RequirementId}, " +
                "RequirementCompleted={RequirementCompleted}",
                contract.VolunteerContractId,
                onboardingId,
                adminId,
                contractRequirement.VolunteerOnboardingRequirementId,
                contractRequirement.IsCompleted);

            return contract;
        }

        // ============================================================
        // REQUEST CORRECTION
        // ============================================================

        public async Task<VolunteerContract> RequestCorrectionAsync(
            int onboardingId,
            string adminId,
            string correctionReason,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
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

            if (contract.Status !=
                    VolunteerContractStatus.Submitted &&
                contract.Status !=
                    VolunteerContractStatus.UnderReview)
            {
                throw new InvalidOperationException(
                    $"A correction cannot be requested while the contract " +
                    $"is in status '{contract.Status}'.");
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Requirements)
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

            var contractRequirement =
                onboarding.Requirements
                    .FirstOrDefault(x =>
                        x.RequirementType ==
                        VolunteerOnboardingRequirementType.Contract);

            if (contractRequirement == null)
            {
                throw new InvalidOperationException(
                    "The Volunteer Service Agreement requirement could not be found.");
            }

            var now =
                DateTime.Now;

            contract.Status =
                VolunteerContractStatus.CorrectionRequired;

            contract.ReviewedByAdminId =
                adminId;

            contract.ReviewedDate =
                now;

            contract.RejectionReason =
                correctionReason.Trim();

            contract.ModifiedDate =
                now;

            contract.ModifiedBy =
                adminId;

            // --------------------------------------------------------
            // Ensure the contract requirement remains incomplete.
            // --------------------------------------------------------

            contractRequirement.IsCompleted =
                false;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract correction requested. " +
                "ContractId={ContractId}, OnboardingId={OnboardingId}, " +
                "AdminId={AdminId}",
                contract.VolunteerContractId,
                onboardingId,
                adminId);

            return contract;
        }

        // ============================================================
        // REJECT CONTRACT
        // ============================================================

        public async Task<VolunteerContract> RejectContractAsync(
            int onboardingId,
            string adminId,
            string rejectionReason,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new ArgumentException(
                    "An administrator ID is required.",
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

            if (contract.Status !=
                    VolunteerContractStatus.Submitted &&
                contract.Status !=
                    VolunteerContractStatus.UnderReview &&
                contract.Status !=
                    VolunteerContractStatus.CorrectionRequired)
            {
                throw new InvalidOperationException(
                    $"The contract cannot be rejected while it is in " +
                    $"status '{contract.Status}'.");
            }

            var onboarding =
                await _context.VolunteerOnboardings
                    .Include(x => x.Requirements)
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

            var contractRequirement =
                onboarding.Requirements
                    .FirstOrDefault(x =>
                        x.RequirementType ==
                        VolunteerOnboardingRequirementType.Contract);

            if (contractRequirement == null)
            {
                throw new InvalidOperationException(
                    "The Volunteer Service Agreement requirement could not be found.");
            }

            var now =
                DateTime.Now;

            contract.Status =
                VolunteerContractStatus.Rejected;

            contract.ReviewedByAdminId =
                adminId;

            contract.ReviewedDate =
                now;

            contract.RejectionReason =
                rejectionReason.Trim();

            contract.ModifiedDate =
                now;

            contract.ModifiedBy =
                adminId;

            // --------------------------------------------------------
            // A rejected contract must never satisfy the requirement.
            // --------------------------------------------------------

            contractRequirement.IsCompleted =
                false;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract rejected. " +
                "ContractId={ContractId}, OnboardingId={OnboardingId}, " +
                "AdminId={AdminId}",
                contract.VolunteerContractId,
                onboardingId,
                adminId);

            return contract;
        }

        // ============================================================
        // CONTRACT COMPLETED
        // ============================================================

        public async Task<bool> IsContractCompletedAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        onboardingId &&
                        x.Status ==
                        VolunteerContractStatus.Approved,
                    cancellationToken);
        }

        // ============================================================
        // CONTRACT AWAITING REVIEW
        // ============================================================

        public async Task<bool> IsContractAwaitingReviewAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()
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
        // CONTRACT CORRECTION REQUIRED
        // ============================================================

        public async Task<bool> IsContractCorrectionRequiredAsync(
            int onboardingId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VolunteerContracts
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.VolunteerOnboardingId ==
                        onboardingId &&
                        x.Status ==
                        VolunteerContractStatus.CorrectionRequired,
                    cancellationToken);
        }

        // ============================================================
        // FILE VALIDATION
        // ============================================================

        private static void ValidateContractFile(
            IFormFile file)
        {
            if (file == null ||
                file.Length <= 0)
            {
                throw new ArgumentException(
                    "The contract file is empty.");
            }

            if (file.Length >
                MaxContractFileSize)
            {
                throw new InvalidOperationException(
                    "The signed contract may not exceed 25 MB.");
            }

            var extension =
                Path.GetExtension(
                    file.FileName);

            if (!string.Equals(
                    extension,
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The signed contract must be a PDF file.");
            }

            if (!string.Equals(
                    file.ContentType,
                    RequiredContractContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The signed contract must be uploaded as a PDF.");
            }
        }

        // ============================================================
        // BUILD SNAPSHOT
        // ============================================================

        private static string? BuildSnapshot<T>(
            IEnumerable<T>? items)
        {
            if (items == null)
            {
                return null;
            }

            var values =
                items
                    .Select(GetSnapshotValue)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (!values.Any())
            {
                return null;
            }

            return string.Join(
                Environment.NewLine,
                values.Select(
                    x => $"• {x}"));
        }

        // ============================================================
        // SNAPSHOT VALUE
        // ============================================================

        private static string? GetSnapshotValue<T>(
            T item)
        {
            if (item == null)
            {
                return null;
            }

            var type =
                item.GetType();

            var propertyNames =
                new[]
                {
                    "Description",
                    "Requirement",
                    "Responsibility",
                    "Name",
                    "Title",
                    "Text",
                    "Value"
                };

            foreach (var propertyName in propertyNames)
            {
                var property =
                    type.GetProperty(
                        propertyName);

                if (property == null)
                {
                    continue;
                }

                var value =
                    property.GetValue(item);

                if (value == null)
                {
                    continue;
                }

                var stringValue =
                    Convert.ToString(value);

                if (!string.IsNullOrWhiteSpace(
                        stringValue))
                {
                    return stringValue;
                }
            }

            return null;
        }
    }
}