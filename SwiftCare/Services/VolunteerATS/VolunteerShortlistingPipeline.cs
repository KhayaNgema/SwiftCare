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
        // MAIN PIPELINE
        // ============================================================

        public async Task<VolunteerPipelineResult>
            RunAsync(int opportunityId)
        {
            var result =
                new VolunteerPipelineResult();


            // ========================================================
            // COLLECT APPLICATIONS
            // ========================================================

            var applications =
                await _context
                    .VolunteerApplications
                    .Include(x => x.Applicant)
                    .Include(x => x.VolunteerOpportunity)
                    .Where(x =>
                        x.VolunteerOpportunityId ==
                        opportunityId)
                    .ToListAsync();


            result.ApplicationsCollected =
                applications.Count;


            // ========================================================
            // PROCESS EACH APPLICATION
            // ========================================================

            foreach (var application in applications)
            {
                try
                {
                    // =================================================
                    // PIPELINE START
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
                    // CV PROCESSING
                    // =================================================

                    ParsedCv? parsedCv =
                        null;


                    bool cvAvailable =
                        !string.IsNullOrWhiteSpace(
                            application.CVFilePath);


                    if (!cvAvailable)
                    {
                        result.ApplicationsWithoutCV++;


                        await _auditService.LogAsync(
                            application.VolunteerApplicationId,
                            "CV Processing",
                            "No CV file was attached to this application.",
                            "SwiftCare ATS");
                    }
                    else
                    {
                        parsedCv =
                            await ProcessCvAsync(
                                application,
                                result);
                    }


                    // =================================================
                    // ELIGIBILITY
                    // =================================================

                    var eligibility =
                        await _eligibilityEngine
                            .EvaluateAsync(
                                application);


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
                            VolunteerScreeningStatus
                                .Screened;

                        application.Stage =
                            VolunteerApplicationStage
                                .Rejected;


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


                        continue;
                    }


                    // =================================================
                    // ELIGIBILITY RESULT
                    // =================================================

                    if (eligibility.Status ==
                        VolunteerEligibilityStatus.ReviewRequired)
                    {
                        result.ReviewRequired++;
                    }
                    else
                    {
                        result.Eligible++;
                    }


                    // =================================================
                    // CV REQUIRED FOR EVIDENCE-BASED SCORING
                    // =================================================

                    if (parsedCv == null)
                    {
                        /*
                         * We deliberately do NOT manufacture a ParsedCv
                         * here.
                         *
                         * Missing CV evidence must remain missing.
                         */

                        application.ScreeningStatus =
                            VolunteerScreeningStatus
                                .Screened;

                        application.Stage =
                            VolunteerApplicationStage
                                .AIEvaluation;


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

                        noCvEvaluation.QuestionScore = noCvScoring.QuestionScore ?? 0m;

                        noCvEvaluation.AvailabilityScore =
                            noCvScoring.AvailabilityScore;

                        noCvEvaluation.MotivationScore =
                            noCvScoring.MotivationScore;

                        noCvEvaluation.OverallScore =
                            noCvScoring.OverallScore;

                        noCvEvaluation.EvaluatedDate =
                            DateTime.Now;

                        noCvEvaluation.EvaluationVersion =
                            "ATS-1.1";


                        await _context.SaveChangesAsync();


                        await _auditService.LogAsync(
                            application.VolunteerApplicationId,
                            "ATS Review Required",
                            "The application could not receive CV-based scoring because no usable parsed CV was available.",
                            "SwiftCare ATS");


                        continue;
                    }


                    // =================================================
                    // SCORING
                    // =================================================

                    var scoring =
                        await _scoringEngine
                            .CalculateAsync(
                                application,
                                eligibility,
                                parsedCv);


                    application.Stage =
                        VolunteerApplicationStage
                            .AIEvaluation;

                    application.ScreeningStatus =
                        VolunteerScreeningStatus
                            .Screened;


                    // =================================================
                    // CREATE / UPDATE EVALUATION
                    // =================================================

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

                    evaluation.QuestionScore = 
                        scoring.QuestionScore ?? 0m;

                    evaluation.AvailabilityScore =
                        scoring.AvailabilityScore;

                    evaluation.MotivationScore =
                        scoring.MotivationScore;

                    evaluation.OverallScore =
                        scoring.OverallScore;

                    evaluation.EvaluatedDate =
                        DateTime.Now;

                    evaluation.EvaluationVersion =
                        "ATS-1.1";


                    // =================================================
                    // AI EVALUATION
                    // =================================================

                    var ai =
                        await _aiEngine
                            .EvaluateAsync(
                                application,
                                evaluation,
                                parsedCv);


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
                        VolunteerAIRecommendation
                            .StrongShortlist)
                    {
                        result.StrongShortlist++;


                        application.Stage =
                            VolunteerApplicationStage
                                .Shortlisted;


                        application.ShortlistedDate =
                            DateTime.Now;
                    }
                    else if (
                        ai.Recommendation ==
                        VolunteerAIRecommendation
                            .ReviewRecommended)
                    {
                        result.ReviewRecommended++;
                    }
                    else
                    {
                        result.NotRecommended++;
                    }


                    // =================================================
                    // SAVE
                    // =================================================

                    await _context.SaveChangesAsync();


                    // =================================================
                    // AUDIT
                    // =================================================

                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "ATS Evaluation Completed",
                        $"Overall score: {scoring.OverallScore}. " +
                        $"AI recommendation: {ai.Recommendation}.",
                        "SwiftCare ATS");
                }
                catch (Exception)
                {
                    /*
                     * One broken application must not stop the
                     * entire ATS pipeline.
                     */

                    application.ScreeningStatus =
                        VolunteerScreeningStatus
                            .Screened;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "ATS Processing Failed",
                        "An unexpected error occurred while processing this application.",
                        "SwiftCare ATS");


                    await _context.SaveChangesAsync();
                }
            }


            // ========================================================
            // RANK ALL CANDIDATES
            // ========================================================

            await _rankingEngine
                .RankAsync(
                    opportunityId);


            // ========================================================
            // FINAL AUDIT
            // ========================================================

            foreach (var application in applications)
            {
                await _auditService.LogAsync(
                    application.VolunteerApplicationId,
                    "Shortlisting Pipeline Completed",
                    "Application was processed by the SwiftCare ATS pipeline.",
                    "SwiftCare ATS");
            }


            return result;
        }


        // ============================================================
        // CV PROCESSING
        // ============================================================

        private async Task<ParsedCv?>
            ProcessCvAsync(
                VolunteerApplication application,
                VolunteerPipelineResult result)
        {
            try
            {
                var relativePath =
                    application.CVFilePath!
                        .TrimStart(
                            '/',
                            '\\');


                var fullPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        relativePath);


                if (!File.Exists(fullPath))
                {
                    result.CVsFailed++;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        "The uploaded CV file could not be found.",
                        "SwiftCare ATS");


                    return null;
                }


                var fileName =
                    !string.IsNullOrWhiteSpace(
                        application.CVFileName)
                        ? application.CVFileName
                        : Path.GetFileName(
                            fullPath);


                await using var cvStream =
                    File.OpenRead(
                        fullPath);


                var parsedCv =
                    await _cvProcessingEngine
                        .ProcessAsync(
                            cvStream,
                            fileName,
                            application.VolunteerApplicationId);


                if (parsedCv == null)
                {
                    result.CVsFailed++;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        "The CV processing engine returned no parsed CV.",
                        "SwiftCare ATS");


                    return null;
                }


                if (!parsedCv.ExtractionSuccessful)
                {
                    result.CVsFailed++;


                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "CV Processing Failed",
                        parsedCv.ExtractionMessage
                        ?? "The CV could not be extracted or parsed.",
                        "SwiftCare ATS");


                    return null;
                }


                result.CVsProcessed++;


                await _auditService.LogAsync(
                    application.VolunteerApplicationId,
                    "CV Processed",
                    BuildCvProcessingMessage(parsedCv),
                    "SwiftCare ATS");


                return parsedCv;
            }
            catch (Exception)
            {
                result.CVsFailed++;


                await _auditService.LogAsync(
                    application.VolunteerApplicationId,
                    "CV Processing Failed",
                    "An unexpected error occurred while processing the applicant CV.",
                    "SwiftCare ATS");


                return null;
            }
        }


        // ============================================================
        // CREATE / GET EVALUATION
        // ============================================================

        private async Task<VolunteerApplicationEvaluation>
            GetOrCreateEvaluationAsync(
                VolunteerApplication application)
        {
            var evaluation =
                await _context
                    .VolunteerApplicationEvaluations
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                        application.VolunteerApplicationId);


            if (evaluation != null)
                return evaluation;


            evaluation =
                new VolunteerApplicationEvaluation
                {
                    VolunteerApplicationId =
                        application.VolunteerApplicationId
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
                "ATS-1.1";


            await _context.SaveChangesAsync();
        }


        // ============================================================
        // NO-CV RESULT
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

                QuestionScore = 0,

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
                $"Skills: {skills}. " +
                $"Education records: {education}. " +
                $"Experience records: {experience}. " +
                $"Certifications: {certifications}. " +
                $"Languages: {languages}.";
        }
    }
}