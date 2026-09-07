using SwiftCare.Models;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerAIEvaluationEngine
        : IVolunteerAIEvaluationEngine
    {
        public Task<VolunteerAIResult> EvaluateAsync(
            VolunteerApplication application,
            VolunteerApplicationEvaluation evaluation)
        {
            var result =
                new VolunteerAIResult();


            // ====================================================
            // CURRENT BASELINE EVALUATION
            // ====================================================

            if (evaluation.OverallScore >= 80)
            {
                result.Recommendation =
                    VolunteerAIRecommendation
                        .StrongShortlist;

                result.Confidence = 90;

                result.Strengths =
                    "Strong overall application score.\n" +
                    "Meets the core eligibility requirements.\n" +
                    "Application contains relevant information.";

                result.Concerns =
                    "Final suitability should be confirmed by an administrator.";

                result.Explanation =
                    "The candidate achieved a strong overall score " +
                    "across the configured ATS evaluation criteria.";

                result.RequirementMatches =
                    "Core application requirements matched.";
            }
            else if (evaluation.OverallScore >= 60)
            {
                result.Recommendation =
                    VolunteerAIRecommendation
                        .ReviewRecommended;

                result.Confidence = 72;

                result.Strengths =
                    "Candidate meets a reasonable portion of the evaluation criteria.";

                result.Concerns =
                    "Some areas require administrator review.";

                result.Explanation =
                    "The candidate has potential but the available " +
                    "application evidence does not support an immediate " +
                    "strong-shortlist recommendation.";

                result.RequirementMatches =
                    "Partial requirement match.";
            }
            else
            {
                result.Recommendation =
                    VolunteerAIRecommendation
                        .NotRecommended;

                result.Confidence = 78;

                result.Strengths =
                    "Application was successfully evaluated.";

                result.Concerns =
                    "Overall application score is below the shortlist threshold.";

                result.Explanation =
                    "The available application information does not " +
                    "currently demonstrate a sufficiently strong match.";

                result.RequirementMatches =
                    "Limited requirement match.";
            }


            return Task.FromResult(result);
        }
    }
}