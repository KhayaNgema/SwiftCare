using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;
using SwiftCare.Services.VolunteerATS.CV;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerPipelineResult
    {
        public int ApplicationsCollected { get; set; }

        public int Eligible { get; set; }

        public int ReviewRequired { get; set; }

        public int NotEligible { get; set; }

        public int StrongShortlist { get; set; }

        public int ReviewRecommended { get; set; }

        public int NotRecommended { get; set; }

        public int CVsProcessed { get; set; }

        public int CVsFailed { get; set; }

        public int ApplicationsWithoutCV { get; set; }
    }


    public class VolunteerShortlistingPipeline
    {
        private readonly SwiftCareDbContext _context;
        private readonly VolunteerEligibilityEngine _eligibilityEngine;
        private readonly VolunteerScoringEngine _scoringEngine;
        private readonly IVolunteerAIEvaluationEngine _aiEngine;
        private readonly VolunteerRankingEngine _rankingEngine;
        private readonly VolunteerATSAuditService _auditService;
        private readonly IVolunteerCVProcessingEngine _cvProcessingEngine;
        private readonly IWebHostEnvironment _environment;


        public VolunteerShortlistingPipeline(
            SwiftCareDbContext context,
            VolunteerEligibilityEngine eligibilityEngine,
            VolunteerScoringEngine scoringEngine,
            IVolunteerAIEvaluationEngine aiEngine,
            VolunteerRankingEngine rankingEngine,
            VolunteerATSAuditService auditService,
            IVolunteerCVProcessingEngine cvProcessingEngine,
            IWebHostEnvironment environment)
        {
            _context = context;
            _eligibilityEngine = eligibilityEngine;
            _scoringEngine = scoringEngine;
            _aiEngine = aiEngine;
            _rankingEngine = rankingEngine;
            _auditService = auditService;
            _cvProcessingEngine = cvProcessingEngine;
            _environment = environment;
        }


        // ============================================================
        // DEBUG LOGGER
        // ============================================================

        private void Log(string message)
        {
            Console.WriteLine(
                $"[SWIFTCARE ATS] {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}");
        }


        private void LogSeparator()
        {
            Console.WriteLine(
                "================================================================");
        }


        // ============================================================
        // MAIN PIPELINE
        // ============================================================

        public async Task<VolunteerPipelineResult> RunAsync(
            int opportunityId)
        {
            var result = new VolunteerPipelineResult();

            try
            {
                LogSeparator();
                Log("ATS PIPELINE STARTED");
                Log($"Opportunity ID: {opportunityId}");
                Log($"Web Root Path: {_environment.WebRootPath}");
                LogSeparator();


                // ====================================================
                // STEP 1 - LOAD APPLICATIONS
                // ====================================================

                Log("STEP 1: Loading applications...");

                var applications =
                    await _context
                        .VolunteerApplications
                        .Include(x => x.Applicant)
                        .Include(x => x.VolunteerOpportunity)
                        .Where(x =>
                            x.VolunteerOpportunityId == opportunityId)
                        .ToListAsync();


                result.ApplicationsCollected =
                    applications.Count;


                Log(
                    $"STEP 1 COMPLETE: {applications.Count} application(s) found.");


                if (applications.Count == 0)
                {
                    Log("WARNING: No applications were found.");
                    LogSeparator();

                    return result;
                }


                foreach (var app in applications)
                {
                    Log(
                        $"Application found | " +
                        $"ID={app.VolunteerApplicationId} | " +
                        $"ApplicantID={app.ApplicantId} | " +
                        $"CV={app.CVFilePath ?? "NULL"}");
                }


                LogSeparator();


                // ====================================================
                // PROCESS EACH APPLICATION
                // ====================================================

                foreach (var application in applications)
                {
                    try
                    {
                        LogSeparator();

                        Log(
                            $"STARTING APPLICATION {application.VolunteerApplicationId}");

                        Log(
                            $"Applicant ID: {application.ApplicantId}");

                        Log(
                            $"Applicant Name: " +
                            $"{application.Applicant?.FirstName} " +
                            $"{application.Applicant?.LastName}");

                        Log(
                            $"CV Path: {application.CVFilePath}");

                        Log(
                            $"CV File Name: {application.CVFileName}");

                        LogSeparator();


                        // =================================================
                        // STEP 2 - ELIGIBILITY SCREENING
                        // =================================================

                        application.Stage =
                            VolunteerApplicationStage
                                .EligibilityScreening;

                        application.ScreeningStatus =
                            VolunteerScreeningStatus
                                .Screening;


                        await _auditService.LogAsync(
                            application.VolunteerApplicationId,
                            "Eligibility Screening",
                            "Application entered ATS eligibility screening.",
                            "SwiftCare ATS");


                        // =================================================
                        // STEP 3 - CV PROCESSING
                        // =================================================

                        Log("STEP 3: Checking CV availability.");

                        ParsedCv? parsedCv = null;

                        bool cvAvailable =
                            !string.IsNullOrWhiteSpace(
                                application.CVFilePath);


                        if (!cvAvailable)
                        {
                            result.ApplicationsWithoutCV++;

                            Log(
                                "NO CV ATTACHED TO APPLICATION.");


                            await _auditService.LogAsync(
                                application.VolunteerApplicationId,
                                "CV Processing",
                                "No CV file was attached to this application.",
                                "SwiftCare ATS");
                        }
                        else
                        {
                            Log(
                                "CV exists. Starting CV processing engine...");


                            parsedCv =
                                await ProcessCvAsync(
                                    application,
                                    result);


                            if (parsedCv == null)
                            {
                                Log(
                                    "WARNING: ProcessCvAsync returned NULL.");
                            }
                            else
                            {
                                Log(
                                    "ProcessCvAsync returned a ParsedCv object.");

                                Log(
                                    $"ParsedCv ID = {parsedCv.ParsedCvId}");

                                Log(
                                    $"ExtractionSuccessful = " +
                                    $"{parsedCv.ExtractionSuccessful}");

                                Log(
                                    $"Skills = {parsedCv.Skills?.Count ?? 0}");

                                Log(
                                    $"Education = {parsedCv.Education?.Count ?? 0}");

                                Log(
                                    $"Experience = {parsedCv.Experience?.Count ?? 0}");

                                Log(
                                    $"Certifications = " +
                                    $"{parsedCv.Certifications?.Count ?? 0}");

                                Log(
                                    $"Languages = " +
                                    $"{parsedCv.Languages?.Count ?? 0}");
                            }
                        }


                        // =================================================
                        // STEP 4 - ELIGIBILITY
                        // =================================================

                        Log(
                            "STEP 4: Running eligibility engine...");


                        var eligibility =
                            await _eligibilityEngine
                                .EvaluateAsync(application);


                        Log(
                            $"Eligibility result: {eligibility.Status}");

                        Log(
                            $"Eligibility score: {eligibility.Score}");


                        application.EligibilityStatus =
                            eligibility.Status;


                        // =================================================
                        // NOT ELIGIBLE
                        // =================================================

                        if (eligibility.Status ==
                            VolunteerEligibilityStatus.NotEligible)
                        {
                            result.NotEligible++;


                            application.ScreeningStatus =
                                VolunteerScreeningStatus.Screened;

                            application.Stage =
                                VolunteerApplicationStage.Rejected;


                            await SaveEvaluationAsync(
                                application,
                                eligibility,
                                null);


                            await _auditService.LogAsync(
                                application.VolunteerApplicationId,
                                "Eligibility Failed",
                                BuildEligibilityMessage(
                                    eligibility),
                                "SwiftCare ATS");


                            Log(
                                "Application marked Rejected.");

                            continue;
                        }


                        // =================================================
                        // ELIGIBILITY RESULT
                        // =================================================

                        if (eligibility.Status ==
                            VolunteerEligibilityStatus.ReviewRequired)
                        {
                            result.ReviewRequired++;

                            Log(
                                "Eligibility status: REVIEW REQUIRED.");
                        }
                        else
                        {
                            result.Eligible++;

                            Log(
                                "Eligibility status: ELIGIBLE.");
                        }


                        // =================================================
                        // NO USABLE CV
                        // =================================================

                        if (parsedCv == null)
                        {
                            LogSeparator();

                            Log(
                                "NO USABLE PARSED CV AVAILABLE.");

                            Log(
                                "Skipping CV-based scoring.");


                            application.ScreeningStatus =
                                VolunteerScreeningStatus.Screened;

                            application.Stage =
                                VolunteerApplicationStage.AIEvaluation;


                            var noCvScoring =
                                CreateNoCvScoringResult(
                                    application);


                            var noCvEvaluation =
                                await GetOrCreateEvaluationAsync(
                                    application);


                            noCvEvaluation.EligibilityStatus =
                                eligibility.Status;

                            noCvEvaluation.EligibilityScore =
                                eligibility.Score;

                            noCvEvaluation.QualificationScore =
                                noCvScoring.QualificationScore;

                            noCvEvaluation.ExperienceScore =
                                noCvScoring.ExperienceScore;

                            noCvEvaluation.SkillsScore =
                                noCvScoring.SkillsScore;

                            noCvEvaluation.AvailabilityScore =
                                noCvScoring.AvailabilityScore;

                            noCvEvaluation.MotivationScore =
                                noCvScoring.MotivationScore;

                            noCvEvaluation.OverallScore =
                                noCvScoring.OverallScore;

                            noCvEvaluation.EvaluatedDate =
                                DateTime.Now;

                            noCvEvaluation.EvaluationVersion =
                                "01";


                            await _context.SaveChangesAsync();


                            await _auditService.LogAsync(
                                application.VolunteerApplicationId,
                                "ATS Review Required",
                                "The application could not receive CV-based scoring because no usable parsed CV was available.",
                                "SwiftCare ATS");


                            Log(
                                $"Existing/new evaluation updated. " +
                                $"Evaluation ID = " +
                                $"{noCvEvaluation.VolunteerApplicationEvaluationId}");

                            continue;
                        }


                        // =================================================
                        // LOG PARSED CV
                        // =================================================

                        Log(
                            "STEP 5: Parsed CV received.");

                        LogParsedCv(
                            application,
                            parsedCv);


                        // =================================================
                        // STEP 6 - SCORING
                        // =================================================

                        Log(
                            "STEP 6: Starting VolunteerScoringEngine...");


                        var scoring =
                            await _scoringEngine
                                .CalculateAsync(
                                    application,
                                    eligibility,
                                    parsedCv);


                        Log("SCORING COMPLETE.");

                        Log(
                            $"Qualification Score: " +
                            $"{scoring.QualificationScore}");

                        Log(
                            $"Experience Score: " +
                            $"{scoring.ExperienceScore}");

                        Log(
                            $"Skills Score: " +
                            $"{scoring.SkillsScore}");

                        Log(
                            $"Availability Score: " +
                            $"{scoring.AvailabilityScore}");

                        Log(
                            $"Motivation Score: " +
                            $"{scoring.MotivationScore}");

                        Log(
                            $"OVERALL SCORE: " +
                            $"{scoring.OverallScore}");


                        application.Stage =
                            VolunteerApplicationStage.AIEvaluation;

                        application.ScreeningStatus =
                            VolunteerScreeningStatus.Screened;


                        // =================================================
                        // STEP 7 - GET OR CREATE EVALUATION
                        // =================================================

                        Log(
                            "STEP 7: Creating/updating evaluation record.");


                        var evaluation =
                            await GetOrCreateEvaluationAsync(
                                application);


                        evaluation.EligibilityStatus =
                            eligibility.Status;

                        evaluation.EligibilityScore =
                            eligibility.Score;

                        evaluation.QualificationScore =
                            scoring.QualificationScore;

                        evaluation.ExperienceScore =
                            scoring.ExperienceScore;

                        evaluation.SkillsScore =
                            scoring.SkillsScore;

                        evaluation.AvailabilityScore =
                            scoring.AvailabilityScore;

                        evaluation.MotivationScore =
                            scoring.MotivationScore;

                        evaluation.OverallScore =
                            scoring.OverallScore;

                        evaluation.EvaluatedDate =
                            DateTime.Now;

                        evaluation.EvaluationVersion =
                            "01";


                        Log(
                            $"Evaluation populated. " +
                            $"ID={evaluation.VolunteerApplicationEvaluationId}");


                        // =================================================
                        // STEP 8 - AI EVALUATION
                        // =================================================

                        Log(
                            "STEP 8: Starting AI evaluation...");


                        var ai =
                            await _aiEngine
                                .EvaluateAsync(
                                    application,
                                    evaluation,
                                    parsedCv);


                        Log(
                            "AI EVALUATION COMPLETE.");

                        Log(
                            $"AI Recommendation: " +
                            $"{ai.Recommendation}");

                        Log(
                            $"AI Confidence: " +
                            $"{ai.Confidence}");

                        Log(
                            $"AI Explanation: " +
                            $"{ai.Explanation}");


                        // =================================================
                        // SAVE AI RESULT
                        // =================================================

                        evaluation.AIRecommendation =
                            ai.Recommendation;

                        evaluation.AIConfidence =
                            ai.Confidence;

                        evaluation.AIStrengths =
                            ai.Strengths;

                        evaluation.AIConcerns =
                            ai.Concerns;

                        evaluation.AIExplanation =
                            ai.Explanation;

                        evaluation.AIRequirementMatches =
                            ai.RequirementMatches;


                        // =================================================
                        // APPLICATION RESULT
                        // =================================================

                        application.AIRecommendation =
                            ai.Recommendation;

                        application.ATSScore =
                            scoring.OverallScore;

                        application.LastEvaluatedDate =
                            DateTime.Now;


                        // =================================================
                        // RECOMMENDATION
                        // =================================================

                        if (ai.Recommendation ==
                            VolunteerAIRecommendation.StrongShortlist)
                        {
                            result.StrongShortlist++;


                            application.Stage =
                                VolunteerApplicationStage.Shortlisted;

                            application.ShortlistedDate =
                                DateTime.Now;


                            Log(
                                "APPLICATION RESULT: STRONG SHORTLIST.");
                        }
                        else if (
                            ai.Recommendation ==
                            VolunteerAIRecommendation.ReviewRecommended)
                        {
                            result.ReviewRecommended++;

                            Log(
                                "APPLICATION RESULT: REVIEW RECOMMENDED.");
                        }
                        else
                        {
                            result.NotRecommended++;

                            Log(
                                "APPLICATION RESULT: NOT RECOMMENDED.");
                        }


                        // =================================================
                        // STEP 9 - SAVE
                        // =================================================

                        Log(
                            "STEP 9: Saving application/evaluation/database changes.");


                        await _context.SaveChangesAsync();


                        Log(
                            "DATABASE SAVE COMPLETE.");


                        // =================================================
                        // AUDIT
                        // =================================================

                        await _auditService.LogAsync(
                            application.VolunteerApplicationId,
                            "ATS Evaluation Completed",
                            $"Overall score: {scoring.OverallScore}. " +
                            $"Qualification: {scoring.QualificationScore}. " +
                            $"Experience: {scoring.ExperienceScore}. " +
                            $"Skills: {scoring.SkillsScore}. " +
                            $"Motivation: {scoring.MotivationScore}. " +
                            $"AI recommendation: {ai.Recommendation}.",
                            "SwiftCare ATS");


                        Log(
                            $"APPLICATION " +
                            $"{application.VolunteerApplicationId} " +
                            $"COMPLETED SUCCESSFULLY.");

                        LogSeparator();
                    }
                    catch (Exception ex)
                    {
                        LogSeparator();

                        Log(
                            "!!!!!!!! ATS APPLICATION PROCESSING FAILED !!!!!!!!");

                        Log(
                            $"Application ID: " +
                            $"{application.VolunteerApplicationId}");

                        Log(
                            $"Applicant ID: {application.ApplicantId}");

                        Log(
                            $"CV File: {application.CVFilePath}");

                        Log(
                            $"Exception Type: " +
                            $"{ex.GetType().FullName}");

                        Log(
                            $"Exception Message: " +
                            $"{ex.Message}");

                        Log(
                            $"Inner Exception: " +
                            $"{ex.InnerException?.Message}");

                        Log(
                            "FULL EXCEPTION:");

                        Log(
                            ex.ToString());

                        LogSeparator();


                        application.ScreeningStatus =
                            VolunteerScreeningStatus.Screened;


                        try
                        {
                            await _auditService.LogAsync(
                                application.VolunteerApplicationId,
                                "ATS Processing Failed",
                                $"ATS processing failed: {ex.Message}",
                                "SwiftCare ATS");


                            await _context.SaveChangesAsync();
                        }
                        catch (Exception auditEx)
                        {
                            Log(
                                $"Could not save failure audit: " +
                                $"{auditEx}");
                        }
                    }
                }


                // ========================================================
                // STEP 10 - RANK
                // ========================================================

                LogSeparator();

                Log(
                    "STEP 10: Starting candidate ranking...");


                await _rankingEngine
                    .RankAsync(
                        opportunityId);


                Log(
                    "RANKING COMPLETE.");


                // ========================================================
                // STEP 11 - FINAL AUDIT
                // ========================================================

                Log(
                    "STEP 11: Writing final pipeline audit records.");


                foreach (var application in applications)
                {
                    try
                    {
                        await _auditService.LogAsync(
                            application.VolunteerApplicationId,
                            "Shortlisting Pipeline Completed",
                            "Application was processed by the SwiftCare ATS pipeline.",
                            "SwiftCare ATS");
                    }
                    catch (Exception auditEx)
                    {
                        Log(
                            $"Final audit failed for application " +
                            $"{application.VolunteerApplicationId}: " +
                            $"{auditEx.Message}");
                    }
                }


                // ========================================================
                // FINAL RESULT
                // ========================================================

                LogSeparator();

                Log("ATS PIPELINE COMPLETED.");

                Log(
                    $"Applications Collected: " +
                    $"{result.ApplicationsCollected}");

                Log(
                    $"Eligible: {result.Eligible}");

                Log(
                    $"Review Required: " +
                    $"{result.ReviewRequired}");

                Log(
                    $"Not Eligible: " +
                    $"{result.NotEligible}");

                Log(
                    $"CVs Processed: " +
                    $"{result.CVsProcessed}");

                Log(
                    $"CVs Failed: " +
                    $"{result.CVsFailed}");

                Log(
                    $"Applications Without CV: " +
                    $"{result.ApplicationsWithoutCV}");

                Log(
                    $"Strong Shortlist: " +
                    $"{result.StrongShortlist}");

                Log(
                    $"Review Recommended: " +
                    $"{result.ReviewRecommended}");

                Log(
                    $"Not Recommended: " +
                    $"{result.NotRecommended}");

                LogSeparator();


                return result;
            }
            catch (Exception ex)
            {
                LogSeparator();

                Log(
                    "!!!!!!!! ENTIRE ATS PIPELINE FAILED !!!!!!!!");

                Log(
                    $"Opportunity ID: {opportunityId}");

                Log(
                    $"Exception Type: " +
                    $"{ex.GetType().FullName}");

                Log(
                    $"Exception Message: " +
                    $"{ex.Message}");

                Log(
                    $"Inner Exception: " +
                    $"{ex.InnerException?.Message}");

                Log(
                    ex.ToString());

                LogSeparator();

                throw;
            }
        }


        // ============================================================
        // CV PROCESSING
        // ============================================================

        private async Task<ParsedCv?> ProcessCvAsync(
            VolunteerApplication application,
            VolunteerPipelineResult result)
        {
            try
            {
                LogSeparator();

                Log("CV PROCESSING STARTED.");

                Log(
                    $"Application ID: " +
                    $"{application.VolunteerApplicationId}");

                Log(
                    $"CVFilePath: " +
                    $"{application.CVFilePath}");

                Log(
                    $"CVFileName: " +
                    $"{application.CVFileName}");


                if (string.IsNullOrWhiteSpace(
                    application.CVFilePath))
                {
                    Log("CV PATH IS EMPTY.");

                    result.CVsFailed++;

                    return null;
                }


                // ====================================================
                // BUILD FULL FILE PATH
                // ====================================================

                var relativePath =
                    application.CVFilePath
                        .TrimStart('/', '\\');


                var fullPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        relativePath);


                Log(
                    $"WebRootPath: " +
                    $"{_environment.WebRootPath}");

                Log(
                    $"Relative Path: " +
                    $"{relativePath}");

                Log(
                    $"FULL PATH: {fullPath}");

                Log(
                    $"FILE EXISTS: " +
                    $"{File.Exists(fullPath)}");


                if (!File.Exists(fullPath))
                {
                    result.CVsFailed++;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        $"The uploaded CV file could not be found. Expected path: {fullPath}",
                        "SwiftCare ATS");


                    return null;
                }


                var fileName =
                    !string.IsNullOrWhiteSpace(
                        application.CVFileName)
                        ? application.CVFileName
                        : Path.GetFileName(fullPath);


                var extension =
                    Path.GetExtension(fileName)
                        .ToLowerInvariant();


                if (extension != ".pdf" &&
                    extension != ".docx")
                {
                    result.CVsFailed++;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        $"Unsupported CV file type: {extension}. Only PDF and DOCX are supported.",
                        "SwiftCare ATS");


                    return null;
                }


                await using var cvStream =
                    File.OpenRead(fullPath);


                if (cvStream.Length == 0)
                {
                    result.CVsFailed++;

                    return null;
                }


                // ====================================================
                // PROCESS CV
                // ====================================================

                Log(
                    "CALLING IVolunteerCVProcessingEngine.ProcessAsync...");


                var extractedCv =
                    await _cvProcessingEngine
                        .ProcessAsync(
                            cvStream,
                            fileName,
                            application.VolunteerApplicationId);


                Log(
                    "IVolunteerCVProcessingEngine.ProcessAsync RETURNED.");


                if (extractedCv == null)
                {
                    result.CVsFailed++;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        "The CV processing engine returned no ParsedCv object.",
                        "SwiftCare ATS");


                    return null;
                }


                Log(
                    $"Extracted ParsedCv ID: " +
                    $"{extractedCv.ParsedCvId}");


                Log(
                    $"ExtractionSuccessful: " +
                    $"{extractedCv.ExtractionSuccessful}");


                Log(
                    $"RawText Length: " +
                    $"{extractedCv.RawText?.Length ?? 0}");


                LogParsedCv(
                    application,
                    extractedCv);


                // ====================================================
                // EXTRACTION FAILED
                // ====================================================

                if (!extractedCv.ExtractionSuccessful)
                {
                    result.CVsFailed++;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        extractedCv.ExtractionMessage
                        ?? "The CV could not be extracted or parsed.",
                        "SwiftCare ATS");


                    return null;
                }


                // ====================================================
                // CHECK USABLE DATA
                // ====================================================

                if (!HasUsableCvEvidence(extractedCv))
                {
                    result.CVsFailed++;


                    var message =
                        "CV extraction completed but no usable candidate information was produced. " +
                        $"Raw text length: {extractedCv.RawText?.Length ?? 0}, " +
                        $"Skills: {extractedCv.Skills?.Count ?? 0}, " +
                        $"Education: {extractedCv.Education?.Count ?? 0}, " +
                        $"Experience: {extractedCv.Experience?.Count ?? 0}, " +
                        $"Certifications: {extractedCv.Certifications?.Count ?? 0}, " +
                        $"Languages: {extractedCv.Languages?.Count ?? 0}.";


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        message,
                        "SwiftCare ATS");


                    return null;
                }


                // ====================================================
                // UPDATE EXISTING PARSED CV OR CREATE IT ONCE
                // ====================================================

                LogSeparator();

                Log(
                    "UPDATING EXISTING PARSED CV RECORD...");


                var persistedParsedCv =
                    await GetOrCreateParsedCvAsync(
                        application,
                        extractedCv);


                Log(
                    $"ParsedCv persistence complete. " +
                    $"ParsedCv ID = " +
                    $"{persistedParsedCv.ParsedCvId}");


                result.CVsProcessed++;


                await _auditService.LogAsync(
                    application.VolunteerApplicationId,
                    "CV Processed",
                    BuildCvProcessingMessage(
                        persistedParsedCv),
                    "SwiftCare ATS");


                LogSeparator();


                return persistedParsedCv;
            }
            catch (Exception ex)
            {
                result.CVsFailed++;


                LogSeparator();

                Log(
                    "!!!!!!!! CV PROCESSING EXCEPTION !!!!!!!!");

                Log(
                    $"Application ID: " +
                    $"{application.VolunteerApplicationId}");

                Log(
                    $"CV File: " +
                    $"{application.CVFilePath}");

                Log(
                    $"Exception Type: " +
                    $"{ex.GetType().FullName}");

                Log(
                    $"Exception Message: " +
                    $"{ex.Message}");

                Log(
                    $"Inner Exception: " +
                    $"{ex.InnerException?.Message}");

                Log(
                    "FULL EXCEPTION:");

                Log(
                    ex.ToString());

                LogSeparator();


                try
                {
                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        $"CV processing exception: {ex.Message}",
                        "SwiftCare ATS");
                }
                catch (Exception auditEx)
                {
                    Log(
                        $"Could not save CV failure audit: " +
                        $"{auditEx.Message}");
                }


                return null;
            }
        }


        // ============================================================
        // CREATE / UPDATE PARSED CV
        // ============================================================

        private async Task<ParsedCv> GetOrCreateParsedCvAsync(
            VolunteerApplication application,
            ParsedCv incomingParsedCv)
        {
            LogSeparator();

            Log(
                "GET OR CREATE PARSED CV");

            Log(
                $"Application ID: " +
                $"{application.VolunteerApplicationId}");


            var existingParsedCv =
                await _context
                    .Set<ParsedCv>()
                    .Include(x => x.Skills)
                    .Include(x => x.Education)
                    .Include(x => x.Experience)
                    .Include(x => x.Certifications)
                    .Include(x => x.Languages)
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                        application.VolunteerApplicationId);


            if (existingParsedCv == null)
            {
                Log(
                    "NO EXISTING ParsedCv FOUND.");

                Log(
                    "CREATING ParsedCv FOR THE FIRST TIME.");


                incomingParsedCv.ParsedCvId = 0;

                incomingParsedCv.ApplicantId =
                    application.ApplicantId;

                incomingParsedCv.VolunteerApplicationId =
                    application.VolunteerApplicationId;

                incomingParsedCv.ParsedDate =
                    DateTime.Now;


                foreach (var skill in incomingParsedCv.Skills)
                {
                    skill.CvSkillId = 0;
                    skill.ParsedCvId = 0;
                }

                foreach (var education in incomingParsedCv.Education)
                {
                    education.CvEducationId = 0;
                    education.ParsedCvId = 0;
                }

                foreach (var experience in incomingParsedCv.Experience)
                {
                    experience.CvExperienceId = 0;
                    experience.ParsedCvId = 0;
                }

                foreach (var certification in incomingParsedCv.Certifications)
                {
                    certification.CvCertificationId = 0;
                    certification.ParsedCvId = 0;
                }

                foreach (var language in incomingParsedCv.Languages)
                {
                    language.CvLanguageId = 0;
                    language.ParsedCvId = 0;
                }


                _context.Set<ParsedCv>()
                    .Add(incomingParsedCv);


                await _context.SaveChangesAsync();


                Log(
                    $"NEW ParsedCv CREATED. " +
                    $"ID={incomingParsedCv.ParsedCvId}");

                LogSeparator();


                return incomingParsedCv;
            }


            Log(
                "EXISTING ParsedCv FOUND.");

            Log(
                $"Existing ParsedCv ID = " +
                $"{existingParsedCv.ParsedCvId}");

            Log(
                "UPDATING EXISTING RECORD INSTEAD OF CREATING ANOTHER ONE.");


            // ========================================================
            // UPDATE SCALAR PROPERTIES
            // ========================================================

            existingParsedCv.ApplicantId =
                application.ApplicantId;

            existingParsedCv.VolunteerApplicationId =
                application.VolunteerApplicationId;

            existingParsedCv.FullName =
                incomingParsedCv.FullName;

            existingParsedCv.Email =
                incomingParsedCv.Email;

            existingParsedCv.PhoneNumber =
                incomingParsedCv.PhoneNumber;

            existingParsedCv.ProfessionalTitle =
                incomingParsedCv.ProfessionalTitle;

            existingParsedCv.ProfessionalSummary =
                incomingParsedCv.ProfessionalSummary;

            existingParsedCv.CareerObjective =
                incomingParsedCv.CareerObjective;

            existingParsedCv.Address =
                incomingParsedCv.Address;

            existingParsedCv.City =
                incomingParsedCv.City;

            existingParsedCv.Province =
                incomingParsedCv.Province;

            existingParsedCv.Country =
                incomingParsedCv.Country;

            existingParsedCv.LinkedInUrl =
                incomingParsedCv.LinkedInUrl;

            existingParsedCv.PortfolioUrl =
                incomingParsedCv.PortfolioUrl;

            existingParsedCv.GitHubUrl =
                incomingParsedCv.GitHubUrl;

            existingParsedCv.RawText =
                incomingParsedCv.RawText;

            existingParsedCv.ExtractionSuccessful =
                incomingParsedCv.ExtractionSuccessful;

            existingParsedCv.ExtractionMessage =
                incomingParsedCv.ExtractionMessage;

            existingParsedCv.ParsedDate =
                DateTime.Now;


            // ========================================================
            // REMOVE OLD CHILD RECORDS
            // ========================================================

            Log(
                $"Removing old child CV records from ParsedCv " +
                $"{existingParsedCv.ParsedCvId}...");


            if (existingParsedCv.Skills.Any())
            {
                _context.Set<CvSkill>()
                    .RemoveRange(
                        existingParsedCv.Skills);
            }


            if (existingParsedCv.Education.Any())
            {
                _context.Set<CvEducation>()
                    .RemoveRange(
                        existingParsedCv.Education);
            }


            if (existingParsedCv.Experience.Any())
            {
                _context.Set<CvExperience>()
                    .RemoveRange(
                        existingParsedCv.Experience);
            }


            if (existingParsedCv.Certifications.Any())
            {
                _context.Set<CvCertification>()
                    .RemoveRange(
                        existingParsedCv.Certifications);
            }


            if (existingParsedCv.Languages.Any())
            {
                _context.Set<CvLanguage>()
                    .RemoveRange(
                        existingParsedCv.Languages);
            }


            existingParsedCv.Skills.Clear();

            existingParsedCv.Education.Clear();

            existingParsedCv.Experience.Clear();

            existingParsedCv.Certifications.Clear();

            existingParsedCv.Languages.Clear();


            // ========================================================
            // ADD NEW SKILLS
            // ========================================================

            foreach (var skill in incomingParsedCv.Skills)
            {
                var newSkill = new CvSkill
                {
                    ParsedCvId =
                        existingParsedCv.ParsedCvId,

                    Skill =
                        skill.Skill,

                    RawText =
                        skill.RawText
                };


                existingParsedCv.Skills.Add(
                    newSkill);
            }


            // ========================================================
            // ADD NEW EDUCATION
            // ========================================================

            foreach (var education in incomingParsedCv.Education)
            {
                var newEducation = new CvEducation
                {
                    ParsedCvId =
                        existingParsedCv.ParsedCvId,

                    Institution =
                        education.Institution,

                    Qualification =
                        education.Qualification,

                    FieldOfStudy =
                        education.FieldOfStudy,

                    StartDate =
                        education.StartDate,

                    EndDate =
                        education.EndDate,

                    RawText =
                        education.RawText
                };


                existingParsedCv.Education.Add(
                    newEducation);
            }


            // ========================================================
            // ADD NEW EXPERIENCE
            // ========================================================

            foreach (var experience in incomingParsedCv.Experience)
            {
                var newExperience = new CvExperience
                {
                    ParsedCvId =
                        existingParsedCv.ParsedCvId,

                    Employer =
                        experience.Employer,

                    Position =
                        experience.Position,

                    StartDate =
                        experience.StartDate,

                    EndDate =
                        experience.EndDate,

                    Description =
                        experience.Description,

                    RawText =
                        experience.RawText
                };


                existingParsedCv.Experience.Add(
                    newExperience);
            }


            // ========================================================
            // ADD NEW CERTIFICATIONS
            // ========================================================

            foreach (var certification in incomingParsedCv.Certifications)
            {
                var newCertification =
                    new CvCertification
                    {
                        ParsedCvId =
                            existingParsedCv.ParsedCvId,

                        Name =
                            certification.Name,

                        IssuingOrganisation =
                            certification.IssuingOrganisation,

                        Date =
                            certification.Date,

                        RawText =
                            certification.RawText
                    };


                existingParsedCv.Certifications.Add(
                    newCertification);
            }


            // ========================================================
            // ADD NEW LANGUAGES
            // ========================================================

            foreach (var language in incomingParsedCv.Languages)
            {
                var newLanguage = new CvLanguage
                {
                    ParsedCvId =
                        existingParsedCv.ParsedCvId,

                    Language =
                        language.Language,

                    RawText =
                        language.RawText
                };


                existingParsedCv.Languages.Add(
                    newLanguage);
            }


            // ========================================================
            // SAVE UPDATE
            // ========================================================

            await _context.SaveChangesAsync();


            Log(
                $"EXISTING ParsedCv UPDATED SUCCESSFULLY.");

            Log(
                $"ParsedCv ID remains: " +
                $"{existingParsedCv.ParsedCvId}");

            Log(
                $"Skills now: " +
                $"{existingParsedCv.Skills.Count}");

            Log(
                $"Education now: " +
                $"{existingParsedCv.Education.Count}");

            Log(
                $"Experience now: " +
                $"{existingParsedCv.Experience.Count}");

            Log(
                $"Certifications now: " +
                $"{existingParsedCv.Certifications.Count}");

            Log(
                $"Languages now: " +
                $"{existingParsedCv.Languages.Count}");

            LogSeparator();


            return existingParsedCv;
        }


        // ============================================================
        // CHECK PARSED CV
        // ============================================================

        private bool HasUsableCvEvidence(
            ParsedCv parsedCv)
        {
            if (parsedCv == null)
                return false;


            if (!string.IsNullOrWhiteSpace(
                parsedCv.FullName))
            {
                return true;
            }


            if (!string.IsNullOrWhiteSpace(
                parsedCv.Email))
            {
                return true;
            }


            if (!string.IsNullOrWhiteSpace(
                parsedCv.PhoneNumber))
            {
                return true;
            }


            if (parsedCv.Skills != null &&
                parsedCv.Skills.Any(x =>
                    !string.IsNullOrWhiteSpace(x.Skill)))
            {
                return true;
            }


            if (parsedCv.Education != null &&
                parsedCv.Education.Any(x =>
                    !string.IsNullOrWhiteSpace(x.Qualification) ||
                    !string.IsNullOrWhiteSpace(x.Institution)))
            {
                return true;
            }


            if (parsedCv.Experience != null &&
                parsedCv.Experience.Any(x =>
                    !string.IsNullOrWhiteSpace(x.Position) ||
                    !string.IsNullOrWhiteSpace(x.Employer) ||
                    !string.IsNullOrWhiteSpace(x.Description)))
            {
                return true;
            }


            if (parsedCv.Certifications != null &&
                parsedCv.Certifications.Any(x =>
                    !string.IsNullOrWhiteSpace(x.Name)))
            {
                return true;
            }


            if (parsedCv.Languages != null &&
                parsedCv.Languages.Any(x =>
                    !string.IsNullOrWhiteSpace(x.Language)))
            {
                return true;
            }


            if (!string.IsNullOrWhiteSpace(
                parsedCv.RawText) &&
                parsedCv.RawText.Trim().Length >= 30)
            {
                return true;
            }


            return false;
        }


        // ============================================================
        // DEBUG PARSED CV
        // ============================================================

        private void LogParsedCv(
            VolunteerApplication application,
            ParsedCv parsedCv)
        {
            LogSeparator();

            Log("========== PARSED CV RESULT ==========");

            Log(
                $"Application ID: " +
                $"{application.VolunteerApplicationId}");

            Log(
                $"ParsedCv ID: " +
                $"{parsedCv.ParsedCvId}");

            Log(
                $"Full Name: " +
                $"{parsedCv.FullName}");

            Log(
                $"Email: " +
                $"{parsedCv.Email}");

            Log(
                $"Phone: " +
                $"{parsedCv.PhoneNumber}");

            Log(
                $"Professional Title: " +
                $"{parsedCv.ProfessionalTitle}");

            Log(
                $"Professional Summary: " +
                $"{parsedCv.ProfessionalSummary}");

            Log(
                $"Career Objective: " +
                $"{parsedCv.CareerObjective}");

            Log(
                $"Address: " +
                $"{parsedCv.Address}");

            Log(
                $"City: " +
                $"{parsedCv.City}");

            Log(
                $"Province: " +
                $"{parsedCv.Province}");

            Log(
                $"Country: " +
                $"{parsedCv.Country}");

            Log(
                $"LinkedIn: " +
                $"{parsedCv.LinkedInUrl}");

            Log(
                $"Portfolio: " +
                $"{parsedCv.PortfolioUrl}");

            Log(
                $"GitHub: " +
                $"{parsedCv.GitHubUrl}");

            Log(
                $"Extraction Successful: " +
                $"{parsedCv.ExtractionSuccessful}");

            Log(
                $"Extraction Message: " +
                $"{parsedCv.ExtractionMessage}");

            Log(
                $"Raw Text Length: " +
                $"{parsedCv.RawText?.Length ?? 0}");

            Log(
                $"Skills Count: " +
                $"{parsedCv.Skills?.Count ?? 0}");

            Log(
                $"Education Count: " +
                $"{parsedCv.Education?.Count ?? 0}");

            Log(
                $"Experience Count: " +
                $"{parsedCv.Experience?.Count ?? 0}");

            Log(
                $"Certifications Count: " +
                $"{parsedCv.Certifications?.Count ?? 0}");

            Log(
                $"Languages Count: " +
                $"{parsedCv.Languages?.Count ?? 0}");


            if (parsedCv.Skills != null)
            {
                foreach (var skill in parsedCv.Skills)
                {
                    Log(
                        $"SKILL => {skill.Skill}");
                }
            }


            if (parsedCv.Education != null)
            {
                foreach (var education in parsedCv.Education)
                {
                    Log(
                        $"EDUCATION => " +
                        $"Qualification={education.Qualification} | " +
                        $"Institution={education.Institution} | " +
                        $"Field={education.FieldOfStudy} | " +
                        $"Start={education.StartDate} | " +
                        $"End={education.EndDate}");
                }
            }


            if (parsedCv.Experience != null)
            {
                foreach (var experience in parsedCv.Experience)
                {
                    Log(
                        $"EXPERIENCE => " +
                        $"Position={experience.Position} | " +
                        $"Employer={experience.Employer} | " +
                        $"Start={experience.StartDate} | " +
                        $"End={experience.EndDate}");

                    Log(
                        $"EXPERIENCE DESCRIPTION => " +
                        $"{experience.Description}");
                }
            }


            if (parsedCv.Certifications != null)
            {
                foreach (var certification in parsedCv.Certifications)
                {
                    Log(
                        $"CERTIFICATION => " +
                        $"Name={certification.Name} | " +
                        $"Organisation={certification.IssuingOrganisation} | " +
                        $"Date={certification.Date}");
                }
            }


            if (parsedCv.Languages != null)
            {
                foreach (var language in parsedCv.Languages)
                {
                    Log(
                        $"LANGUAGE => {language.Language}");
                }
            }


            if (!string.IsNullOrWhiteSpace(
                parsedCv.RawText))
            {
                var rawText =
                    parsedCv.RawText.Trim();

                var previewLength =
                    Math.Min(
                        rawText.Length,
                        1000);

                Log(
                    $"RAW TEXT PREVIEW => " +
                    $"{rawText.Substring(0, previewLength)}");
            }


            Log("========== END PARSED CV ==========");

            LogSeparator();
        }


        // ============================================================
        // CREATE / GET EVALUATION
        // ============================================================

        private async Task<VolunteerApplicationEvaluation>
            GetOrCreateEvaluationAsync(
                VolunteerApplication application)
        {
            Log(
                $"Loading evaluation for application " +
                $"{application.VolunteerApplicationId}...");


            var evaluation =
                await _context
                    .VolunteerApplicationEvaluations
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                        application.VolunteerApplicationId);


            if (evaluation != null)
            {
                Log(
                    "EXISTING EVALUATION FOUND.");

                Log(
                    $"Evaluation ID = " +
                    $"{evaluation.VolunteerApplicationEvaluationId}");

                Log(
                    "Updating this evaluation instead of creating another one.");


                return evaluation;
            }


            Log(
                "NO EXISTING EVALUATION FOUND.");

            Log(
                "Creating evaluation for the first time.");


            evaluation =
                new VolunteerApplicationEvaluation
                {
                    VolunteerApplicationId =
                        application.VolunteerApplicationId,

                    EvaluationVersion = "01",

                    EvaluatedDate = DateTime.Now
                };


            _context
                .VolunteerApplicationEvaluations
                .Add(evaluation);


            return evaluation;
        }


        // ============================================================
        // SAVE EVALUATION
        // ============================================================

        private async Task SaveEvaluationAsync(
            VolunteerApplication application,
            VolunteerEligibilityResult eligibility,
            VolunteerScoringResult? scoring)
        {
            Log(
                $"Saving evaluation for application " +
                $"{application.VolunteerApplicationId}...");


            var evaluation =
                await GetOrCreateEvaluationAsync(
                    application);


            evaluation.EligibilityStatus =
                eligibility.Status;

            evaluation.EligibilityScore =
                eligibility.Score;

            evaluation.OverallScore =
                scoring?.OverallScore ?? 0;

            evaluation.EvaluatedDate =
                DateTime.Now;

            evaluation.EvaluationVersion =
                "01";


            await _context.SaveChangesAsync();


            Log(
                $"Evaluation saved/updated. " +
                $"ID={evaluation.VolunteerApplicationEvaluationId}");
        }


        // ============================================================
        // NO CV RESULT
        // ============================================================

        private VolunteerScoringResult
            CreateNoCvScoringResult(
                VolunteerApplication application)
        {
            return new VolunteerScoringResult
            {
                QualificationScore = 0,

                ExperienceScore = 0,

                SkillsScore = 0,

                AvailabilityScore = 0,

                MotivationScore = 0,

                OverallScore = 0
            };
        }


        // ============================================================
        // ELIGIBILITY MESSAGE
        // ============================================================

        private string BuildEligibilityMessage(
            VolunteerEligibilityResult eligibility)
        {
            if (eligibility.FailedChecks == null ||
                eligibility.FailedChecks.Count == 0)
            {
                return
                    "The application did not meet the required eligibility criteria.";
            }


            return string.Join(
                " ",
                eligibility.FailedChecks);
        }


        // ============================================================
        // CV MESSAGE
        // ============================================================

        private string BuildCvProcessingMessage(
            ParsedCv parsedCv)
        {
            var skills =
                parsedCv.Skills?.Count ?? 0;

            var education =
                parsedCv.Education?.Count ?? 0;

            var experience =
                parsedCv.Experience?.Count ?? 0;

            var certifications =
                parsedCv.Certifications?.Count ?? 0;

            var languages =
                parsedCv.Languages?.Count ?? 0;


            return
                $"CV successfully processed. " +
                $"Name: {parsedCv.FullName ?? "Unknown"}. " +
                $"Skills: {skills}. " +
                $"Education records: {education}. " +
                $"Experience records: {experience}. " +
                $"Certifications: {certifications}. " +
                $"Languages: {languages}.";
        }
    }
}