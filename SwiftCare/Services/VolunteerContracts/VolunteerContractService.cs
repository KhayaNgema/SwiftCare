using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftCare.Data;
using SwiftCare.Models;
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
                "============================================================");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] CreateAndGenerateContractAsync ENTERED. " +
                "OnboardingId={OnboardingId}",
                onboardingId);

            _logger.LogInformation(
                "============================================================");

            // ========================================================
            // LOAD ONBOARDING
            // ========================================================

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

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Onboarding loaded. " +
                "OnboardingId={OnboardingId}, ApplicantId={ApplicantId}",
                onboarding.VolunteerOnboardingId,
                onboarding.ApplicantId);

            // ========================================================
            // VALIDATE APPLICATION
            // ========================================================

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

            // ========================================================
            // IDENTITY VERIFICATION
            // ========================================================

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Checking identity verification. " +
                "OnboardingId={OnboardingId}",
                onboardingId);

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

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Identity verification found. " +
                "VerificationId={VerificationId}, " +
                "Status={Status}, " +
                "Document={Document}, " +
                "Liveness={Liveness}, " +
                "FaceMatch={FaceMatch}, " +
                "FaceEnrollment={FaceEnrollment}",
                identityVerification.Id,
                identityVerification.Status,
                identityVerification.DocumentVerified,
                identityVerification.LivenessVerified,
                identityVerification.FaceMatchVerified,
                identityVerification.FaceIdentityEnrolled);

            if (identityVerification.Status !=
                VolunteerIdentityVerificationStatus.Verified)
            {
                throw new InvalidOperationException(
                    $"Identity verification is not complete. " +
                    $"Current status: {identityVerification.Status}.");
            }

            // ========================================================
            // VERIFY ALL DIDIT IDENTITY CHECKS
            // ========================================================

            if (!identityVerification.DocumentVerified ||
                !identityVerification.LivenessVerified ||
                !identityVerification.FaceMatchVerified ||
                !identityVerification.FaceIdentityEnrolled)
            {
                _logger.LogWarning(
                    "[VOLUNTEER CONTRACT] Contract generation BLOCKED. " +
                    "Identity verification is incomplete. " +
                    "OnboardingId={OnboardingId}, " +
                    "VerificationId={VerificationId}, " +
                    "Document={Document}, " +
                    "Liveness={Liveness}, " +
                    "FaceMatch={FaceMatch}, " +
                    "FaceEnrollment={FaceEnrollment}",
                    onboardingId,
                    identityVerification.Id,
                    identityVerification.DocumentVerified,
                    identityVerification.LivenessVerified,
                    identityVerification.FaceMatchVerified,
                    identityVerification.FaceIdentityEnrolled);

                throw new InvalidOperationException(
                    "Identity verification is marked as verified, " +
                    "but not all required identity verification checks " +
                    "have completed successfully.");
            }

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] All identity verification checks passed. " +
                "OnboardingId={OnboardingId}",
                onboardingId);

            // ========================================================
            // ACCEPTED OFFER
            // ========================================================

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
                _logger.LogWarning(
                    "[VOLUNTEER CONTRACT] No accepted offer found. " +
                    "OnboardingId={OnboardingId}",
                    onboardingId);

                throw new InvalidOperationException(
                    "The volunteer offer has not been accepted.");
            }

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Accepted offer found. " +
                "OfferId={OfferId}, OnboardingId={OnboardingId}",
                acceptedOffer.VolunteerOfferId,
                onboardingId);

            // ========================================================
            // REQUIRED POLICIES
            // ========================================================

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
                    "[VOLUNTEER CONTRACT] Required policies incomplete. " +
                    "OnboardingId={OnboardingId}, MissingCount={MissingCount}",
                    onboardingId,
                    missingPolicies.Count);

                throw new InvalidOperationException(
                    "The applicant has not accepted all required policies.");
            }

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Required policies passed. " +
                "OnboardingId={OnboardingId}",
                onboardingId);

            // ========================================================
            // EXISTING CONTRACT
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
                    "[VOLUNTEER CONTRACT] Existing contract found. " +
                    "ContractId={ContractId}, " +
                    "Reference={Reference}, " +
                    "Status={Status}, " +
                    "GeneratedFileName={GeneratedFileName}, " +
                    "GeneratedFilePath={GeneratedFilePath}",
                    existingContract.VolunteerContractId,
                    existingContract.ContractReference,
                    existingContract.Status,
                    existingContract.GeneratedFileName,
                    existingContract.GeneratedFilePath);

                // ====================================================
                // CHECK EXISTING FILE
                // ====================================================

                var fileExists = false;

                if (!string.IsNullOrWhiteSpace(
                        existingContract.GeneratedFilePath))
                {
                    _logger.LogInformation(
                        "[VOLUNTEER CONTRACT] Checking existing PDF file. " +
                        "ContractId={ContractId}, Path={Path}",
                        existingContract.VolunteerContractId,
                        existingContract.GeneratedFilePath);

                    try
                    {
                        fileExists =
                            await _fileUploadService.FileExistsAsync(
                                existingContract.GeneratedFilePath,
                                cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "[VOLUNTEER CONTRACT] Existing PDF file check failed. " +
                            "ContractId={ContractId}, Path={Path}",
                            existingContract.VolunteerContractId,
                            existingContract.GeneratedFilePath);

                        fileExists = false;
                    }

                    _logger.LogInformation(
                        "[VOLUNTEER CONTRACT] Existing PDF file check complete. " +
                        "ContractId={ContractId}, Exists={Exists}",
                        existingContract.VolunteerContractId,
                        fileExists);
                }

                // ====================================================
                // EXISTING CONTRACT + PDF EXISTS
                // ====================================================

                if (fileExists)
                {
                    _logger.LogInformation(
                        "[VOLUNTEER CONTRACT] Existing PDF is valid. " +
                        "ContractId={ContractId}",
                        existingContract.VolunteerContractId);

                    if (existingContract.Status ==
                        VolunteerContractStatus.Generated)
                    {
                        existingContract.Status =
                            VolunteerContractStatus.AwaitingApplicant;

                        existingContract.ModifiedDate =
                            DateTime.UtcNow;

                        existingContract.ModifiedBy =
                            applicant.Id;

                        await _context.SaveChangesAsync(
                            cancellationToken);

                        _logger.LogInformation(
                            "[VOLUNTEER CONTRACT] Existing contract moved " +
                            "from Generated to AwaitingApplicant. " +
                            "ContractId={ContractId}",
                            existingContract.VolunteerContractId);
                    }

                    return existingContract;
                }

                // ====================================================
                // EXISTING CONTRACT BUT PDF MISSING
                // ====================================================

                _logger.LogWarning(
                    "[VOLUNTEER CONTRACT] Existing contract has NO VALID PDF. " +
                    "REGENERATION STARTING. " +
                    "ContractId={ContractId}, " +
                    "Reference={Reference}, " +
                    "OldPath={OldPath}",
                    existingContract.VolunteerContractId,
                    existingContract.ContractReference,
                    existingContract.GeneratedFilePath);

                existingContract.GeneratedFilePath = null;
                existingContract.GeneratedFileName = null;

                existingContract.ModifiedDate =
                    DateTime.UtcNow;

                existingContract.ModifiedBy =
                    applicant.Id;

                await _context.SaveChangesAsync(
                    cancellationToken);

                // ====================================================
                // REGENERATE PDF
                // ====================================================

                _logger.LogInformation(
                    "[VOLUNTEER CONTRACT] Calling PDF generator for " +
                    "EXISTING contract. " +
                    "ContractId={ContractId}, Reference={Reference}",
                    existingContract.VolunteerContractId,
                    existingContract.ContractReference);

                VolunteerContract repairedContract;

                try
                {
                    repairedContract =
                        await _pdfGenerator.GeneratePdfAsync(
                            existingContract,
                            cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(
                        "[VOLUNTEER CONTRACT] PDF regeneration cancelled. " +
                        "ContractId={ContractId}",
                        existingContract.VolunteerContractId);

                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[VOLUNTEER CONTRACT] PDF regeneration FAILED. " +
                        "ContractId={ContractId}, Reference={Reference}",
                        existingContract.VolunteerContractId,
                        existingContract.ContractReference);

                    throw;
                }

                if (repairedContract == null)
                {
                    throw new InvalidOperationException(
                        "The PDF generator returned a null contract.");
                }

                _logger.LogInformation(
                    "[VOLUNTEER CONTRACT] PDF generator returned for existing contract. " +
                    "ContractId={ContractId}, " +
                    "GeneratedFileName={GeneratedFileName}, " +
                    "GeneratedFilePath={GeneratedFilePath}",
                    repairedContract.VolunteerContractId,
                    repairedContract.GeneratedFileName,
                    repairedContract.GeneratedFilePath);

                if (string.IsNullOrWhiteSpace(
                        repairedContract.GeneratedFilePath))
                {
                    throw new InvalidOperationException(
                        "The volunteer contract PDF generator returned " +
                        "without a generated file path.");
                }

                if (string.IsNullOrWhiteSpace(
                        repairedContract.GeneratedFileName))
                {
                    throw new InvalidOperationException(
                        "The volunteer contract PDF generator returned " +
                        "without a generated filename.");
                }

                // ====================================================
                // PERSIST GENERATED FILE INFORMATION
                // ====================================================

                existingContract.GeneratedFilePath =
                    repairedContract.GeneratedFilePath;

                existingContract.GeneratedFileName =
                    repairedContract.GeneratedFileName;

                existingContract.GeneratedDate =
                    repairedContract.GeneratedDate ??
                    DateTime.UtcNow;

                existingContract.ModifiedDate =
                    DateTime.UtcNow;

                existingContract.ModifiedBy =
                    applicant.Id;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "[VOLUNTEER CONTRACT] Regenerated PDF information persisted. " +
                    "ContractId={ContractId}, " +
                    "Path={Path}, " +
                    "FileName={FileName}",
                    existingContract.VolunteerContractId,
                    existingContract.GeneratedFilePath,
                    existingContract.GeneratedFileName);

                // ====================================================
                // VERIFY ACTUAL FILE EXISTS
                // ====================================================

                var regeneratedFileExists =
                    await _fileUploadService.FileExistsAsync(
                        existingContract.GeneratedFilePath,
                        cancellationToken);

                if (!regeneratedFileExists)
                {
                    _logger.LogError(
                        "[VOLUNTEER CONTRACT] PDF generator returned a path " +
                        "but the file DOES NOT EXIST in storage. " +
                        "ContractId={ContractId}, Path={Path}",
                        existingContract.VolunteerContractId,
                        existingContract.GeneratedFilePath);

                    throw new InvalidOperationException(
                        "The contract PDF path was generated, " +
                        "but the PDF file could not be found in storage.");
                }

                _logger.LogInformation(
                    "[VOLUNTEER CONTRACT] EXISTING CONTRACT PDF REPAIRED SUCCESSFULLY. " +
                    "ContractId={ContractId}, Path={Path}",
                    existingContract.VolunteerContractId,
                    existingContract.GeneratedFilePath);

                return existingContract;
            }

            // ========================================================
            // SNAPSHOT OPPORTUNITY DATA
            // ========================================================

            var requirementsSnapshot =
                BuildRequirementsSnapshot(
                    opportunity.Requirements);

            var responsibilitiesSnapshot =
                BuildResponsibilitiesSnapshot(
                    opportunity.Responsibilities);

            var requiredSkillsSnapshot =
                BuildRequiredSkillsSnapshot(
                    opportunity.RequiredSkills);

            // ========================================================
            // CONTRACT REFERENCE
            // ========================================================

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Generating contract reference. " +
                "OnboardingId={OnboardingId}",
                onboardingId);

            var contractReference =
                await _referenceGenerator.GenerateAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(contractReference))
            {
                throw new InvalidOperationException(
                    "A contract reference could not be generated.");
            }

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract reference generated. " +
                "Reference={Reference}",
                contractReference);

            var now =
                DateTime.UtcNow;

            // ========================================================
            // CREATE CONTRACT
            // ========================================================

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

            // ========================================================
            // SAVE CONTRACT RECORD
            // ========================================================

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Saving contract database record. " +
                "Reference={Reference}",
                contract.ContractReference);

            _context.VolunteerContracts.Add(contract);

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "============================================================");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] CONTRACT DATABASE RECORD CREATED.");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] ContractId={ContractId}",
                contract.VolunteerContractId);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Reference={Reference}",
                contract.ContractReference);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Status={Status}",
                contract.Status);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] GeneratedFileName={FileName}",
                contract.GeneratedFileName);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] GeneratedFilePath={FilePath}",
                contract.GeneratedFilePath);

            _logger.LogInformation(
                "============================================================");

            // ========================================================
            // PDF GENERATION
            // ========================================================

            _logger.LogInformation(
                "============================================================");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] ABOUT TO GENERATE PDF.");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] ContractId={ContractId}",
                contract.VolunteerContractId);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Reference={Reference}",
                contract.ContractReference);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Calling IVolunteerContractPdfGenerator.");

            _logger.LogInformation(
                "============================================================");

            VolunteerContract generatedContract;

            try
            {
                generatedContract =
                    await _pdfGenerator.GeneratePdfAsync(
                        contract,
                        cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[VOLUNTEER CONTRACT] PDF generation was CANCELLED. " +
                    "ContractId={ContractId}, Reference={Reference}",
                    contract.VolunteerContractId,
                    contract.ContractReference);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[VOLUNTEER CONTRACT] PDF GENERATION FAILED. " +
                    "ContractId={ContractId}, Reference={Reference}",
                    contract.VolunteerContractId,
                    contract.ContractReference);

                throw;
            }

            // ========================================================
            // VALIDATE PDF GENERATOR RESULT
            // ========================================================

            if (generatedContract == null)
            {
                _logger.LogError(
                    "[VOLUNTEER CONTRACT] PDF generator returned NULL. " +
                    "ContractId={ContractId}",
                    contract.VolunteerContractId);

                throw new InvalidOperationException(
                    "The volunteer contract PDF generator returned a null contract.");
            }

            _logger.LogInformation(
                "============================================================");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] PDF GENERATOR RETURNED SUCCESSFULLY.");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] ContractId={ContractId}",
                generatedContract.VolunteerContractId);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Reference={Reference}",
                generatedContract.ContractReference);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] GeneratedFileName={FileName}",
                generatedContract.GeneratedFileName);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] GeneratedFilePath={Path}",
                generatedContract.GeneratedFilePath);

            _logger.LogInformation(
                "============================================================");

            // ========================================================
            // VALIDATE GENERATED FILE INFORMATION
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    generatedContract.GeneratedFilePath))
            {
                _logger.LogError(
                    "[VOLUNTEER CONTRACT] PDF generator completed " +
                    "but GeneratedFilePath is EMPTY. " +
                    "ContractId={ContractId}",
                    generatedContract.VolunteerContractId);

                throw new InvalidOperationException(
                    "The volunteer contract PDF was not generated " +
                    "because no generated file path was returned.");
            }

            if (string.IsNullOrWhiteSpace(
                    generatedContract.GeneratedFileName))
            {
                _logger.LogError(
                    "[VOLUNTEER CONTRACT] PDF generator completed " +
                    "but GeneratedFileName is EMPTY. " +
                    "ContractId={ContractId}",
                    generatedContract.VolunteerContractId);

                throw new InvalidOperationException(
                    "The volunteer contract PDF was not generated " +
                    "because no generated filename was returned.");
            }

            // ========================================================
            // COPY GENERATED VALUES BACK TO TRACKED ENTITY
            // ========================================================
            //
            // This is important.
            //
            // The PDF generator may return a VolunteerContract instance
            // containing the generated file values. We explicitly copy
            // those values back onto the tracked contract entity.
            //
            // ========================================================

            contract.GeneratedFilePath =
                generatedContract.GeneratedFilePath;

            contract.GeneratedFileName =
                generatedContract.GeneratedFileName;

            contract.GeneratedDate =
                generatedContract.GeneratedDate ??
                DateTime.UtcNow;

            // Keep the contract in Generated status until we have
            // positively confirmed the actual PDF exists.
            contract.Status =
                VolunteerContractStatus.Generated;

            contract.ModifiedDate =
                DateTime.UtcNow;

            contract.ModifiedBy =
                applicant.Id;

            // ========================================================
            // PERSIST GENERATED PDF INFORMATION
            // ========================================================

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Persisting generated PDF information. " +
                "ContractId={ContractId}, Path={Path}, FileName={FileName}",
                contract.VolunteerContractId,
                contract.GeneratedFilePath,
                contract.GeneratedFileName);

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Generated PDF information saved to database. " +
                "ContractId={ContractId}",
                contract.VolunteerContractId);

            // ========================================================
            // VERIFY ACTUAL FILE EXISTS
            // ========================================================

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Verifying actual PDF exists in storage. " +
                "ContractId={ContractId}, Path={Path}",
                contract.VolunteerContractId,
                contract.GeneratedFilePath);

            bool generatedFileExists;

            try
            {
                generatedFileExists =
                    await _fileUploadService.FileExistsAsync(
                        contract.GeneratedFilePath,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[VOLUNTEER CONTRACT] Storage verification FAILED. " +
                    "ContractId={ContractId}, Path={Path}",
                    contract.VolunteerContractId,
                    contract.GeneratedFilePath);

                throw;
            }

            if (!generatedFileExists)
            {
                _logger.LogError(
                    "[VOLUNTEER CONTRACT] PDF GENERATOR RETURNED A PATH " +
                    "BUT THE ACTUAL FILE DOES NOT EXIST. " +
                    "ContractId={ContractId}, " +
                    "Reference={Reference}, " +
                    "Path={Path}",
                    contract.VolunteerContractId,
                    contract.ContractReference,
                    contract.GeneratedFilePath);

                throw new InvalidOperationException(
                    "The contract PDF generator returned a file path, " +
                    "but the PDF could not be found in storage.");
            }

            // ========================================================
            // PDF EXISTS
            // ========================================================

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] PDF FILE VERIFIED IN STORAGE. " +
                "ContractId={ContractId}, Path={Path}",
                contract.VolunteerContractId,
                contract.GeneratedFilePath);

            // ========================================================
            // MOVE TO AWAITING APPLICANT
            // ========================================================

            contract.Status =
                VolunteerContractStatus.AwaitingApplicant;

            contract.ModifiedDate =
                DateTime.UtcNow;

            contract.ModifiedBy =
                applicant.Id;

            await _context.SaveChangesAsync(
                cancellationToken);

            // ========================================================
            // FINAL SUCCESS
            // ========================================================

            _logger.LogInformation(
                "============================================================");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] CONTRACT GENERATION COMPLETED SUCCESSFULLY.");

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] ContractId={ContractId}",
                contract.VolunteerContractId);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Reference={Reference}",
                contract.ContractReference);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Version={Version}",
                contract.ContractVersion);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Status={Status}",
                contract.Status);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] FileName={FileName}",
                contract.GeneratedFileName);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] FilePath={Path}",
                contract.GeneratedFilePath);

            _logger.LogInformation(
                "============================================================");

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
                DateTime.UtcNow;

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
                DateTime.UtcNow;

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

            contractRequirement.IsCompleted =
                true;

            onboarding.ModifiedDate =
                now;

            onboarding.ModifiedBy =
                adminId;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "[VOLUNTEER CONTRACT] Contract approved successfully. " +
                "ContractId={ContractId}, OnboardingId={OnboardingId}, " +
                "AdminId={AdminId}, RequirementId={RequirementId}",
                contract.VolunteerContractId,
                onboardingId,
                adminId,
                contractRequirement.VolunteerOnboardingRequirementId);

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
                DateTime.UtcNow;

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
                DateTime.UtcNow;

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
        // REQUIREMENTS SNAPSHOT
        // ============================================================

        private static string? BuildRequirementsSnapshot(
            IEnumerable<VolunteerRequirement>? requirements)
        {
            if (requirements == null)
            {
                return null;
            }

            var values =
                requirements
                    .Where(x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(x.Description))
                    .Select(x =>
                        x.Description.Trim())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
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
        // RESPONSIBILITIES SNAPSHOT
        // ============================================================

        private static string? BuildResponsibilitiesSnapshot(
            IEnumerable<VolunteerResponsibility>? responsibilities)
        {
            if (responsibilities == null)
            {
                return null;
            }

            var values =
                responsibilities
                    .Where(x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(x.Description))
                    .Select(x =>
                        x.Description.Trim())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
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
        // REQUIRED SKILLS SNAPSHOT
        // ============================================================

        private static string? BuildRequiredSkillsSnapshot(
            IEnumerable<VolunteerRequiredSkill>? skills)
        {
            if (skills == null)
            {
                return null;
            }

            var values =
                skills
                    .Where(x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(x.Skill))
                    .Select(x =>
                        x.Skill.Trim())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
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
    }
}