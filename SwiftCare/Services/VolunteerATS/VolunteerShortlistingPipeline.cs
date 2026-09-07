using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;

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
    }


    public class VolunteerShortlistingPipeline
    {
        private readonly SwiftCareDbContext _context;
        private readonly VolunteerEligibilityEngine _eligibilityEngine;
        private readonly VolunteerScoringEngine _scoringEngine;
        private readonly IVolunteerAIEvaluationEngine _aiEngine;
        private readonly VolunteerRankingEngine _rankingEngine;
        private readonly VolunteerATSAuditService _auditService;


        public VolunteerShortlistingPipeline(
            SwiftCareDbContext context,
            VolunteerEligibilityEngine eligibilityEngine,
            VolunteerScoringEngine scoringEngine,
            IVolunteerAIEvaluationEngine aiEngine,
            VolunteerRankingEngine rankingEngine,
            VolunteerATSAuditService auditService)
        {
            _context = context;
            _eligibilityEngine = eligibilityEngine;
            _scoringEngine = scoringEngine;
            _aiEngine = aiEngine;
            _rankingEngine = rankingEngine;
            _auditService = auditService;
        }


        public async Task<VolunteerPipelineResult>
            RunAsync(int opportunityId)
        {
            var result =
                new VolunteerPipelineResult();


            // ====================================================
            // COLLECT APPLICATIONS
            // ====================================================

            var applications =
                await _context
                    .VolunteerApplications
                    .Include(x =>
                        x.Applicant)
                    .Include(x =>
                        x.VolunteerOpportunity)
                    .Where(x =>
                        x.VolunteerOpportunityId ==
                        opportunityId)
                    .ToListAsync();


            result.ApplicationsCollected =
                applications.Count;


            // ====================================================
            // PROCESS EACH APPLICATION
            // ====================================================

            foreach (var application in applications)
            {
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
                // ELIGIBILITY
                // =================================================

                var eligibility =
                    await _eligibilityEngine
                        .EvaluateAsync(application);


                application.EligibilityStatus =
                    eligibility.Status;


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
                        null,
                        null);

                    await _auditService.LogAsync(
                        application.VolunteerApplicationId,
                        "Eligibility Failed",
                        string.Join(
                            " ",
                            eligibility.FailedChecks),
                        "SwiftCare ATS");

                    continue;
                }


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
                // SCORING
                // =================================================

                var scoring =
                    await _scoringEngine
                        .CalculateAsync(
                            application,
                            eligibility);


                application.Stage =
                    VolunteerApplicationStage
                        .AIEvaluation;


                application.ScreeningStatus =
                    VolunteerScreeningStatus.Screened;


                // =================================================
                // CREATE / UPDATE EVALUATION
                // =================================================

                var evaluation =
                    await _context
                        .VolunteerApplicationEvaluations
                        .FirstOrDefaultAsync(x =>
                            x.VolunteerApplicationId ==
                            application.VolunteerApplicationId);


                if (evaluation == null)
                {
                    evaluation =
                        new VolunteerApplicationEvaluation
                        {
                            VolunteerApplicationId =
                                application.VolunteerApplicationId
                        };

                    _context
                        .VolunteerApplicationEvaluations
                        .Add(evaluation);
                }


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
                    scoring.QuestionScore;

                evaluation.AvailabilityScore =
                    scoring.AvailabilityScore;

                evaluation.MotivationScore =
                    scoring.MotivationScore;

                evaluation.OverallScore =
                    scoring.OverallScore;

                evaluation.EvaluatedDate =
                    DateTime.Now;

                evaluation.EvaluationVersion =
                    "ATS-1.0";


                // =================================================
                // AI EVALUATION
                // =================================================

                var ai =
                    await _aiEngine
                        .EvaluateAsync(
                            application,
                            evaluation);


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
                else if (ai.Recommendation ==
                    VolunteerAIRecommendation
                        .ReviewRecommended)
                {
                    result.ReviewRecommended++;
                }
                else
                {
                    result.NotRecommended++;
                }


                await _context.SaveChangesAsync();


                await _auditService.LogAsync(
                    application.VolunteerApplicationId,
                    "ATS Evaluation Completed",
                    $"Overall score: {scoring.OverallScore}. " +
                    $"AI recommendation: {ai.Recommendation}.",
                    "SwiftCare ATS");
            }


            // ====================================================
            // RANK ALL CANDIDATES
            // ====================================================

            await _rankingEngine.RankAsync(
                opportunityId);


            // ====================================================
            // FINAL AUDIT
            // ====================================================

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


        private async Task SaveEvaluationAsync(
            VolunteerApplication application,
            VolunteerEligibilityResult eligibility,
            VolunteerScoringResult? scoring,
            VolunteerAIResult? ai)
        {
            var evaluation =
                await _context
                    .VolunteerApplicationEvaluations
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                        application.VolunteerApplicationId);


            if (evaluation == null)
            {
                evaluation =
                    new VolunteerApplicationEvaluation
                    {
                        VolunteerApplicationId =
                            application.VolunteerApplicationId
                    };

                _context
                    .VolunteerApplicationEvaluations
                    .Add(evaluation);
            }


            evaluation.EligibilityStatus =
                eligibility.Status;

            evaluation.EligibilityScore =
                eligibility.Score;

            evaluation.OverallScore =
                scoring?.OverallScore ?? 0;

            evaluation.EvaluatedDate =
                DateTime.Now;

            evaluation.EvaluationVersion =
                "ATS-1.0";


            await _context.SaveChangesAsync();
        }
    }
}