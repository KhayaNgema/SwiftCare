using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS
{
    public interface IVolunteerAIEvaluationEngine
    {
        Task<VolunteerAIResult> EvaluateAsync(
            VolunteerApplication application,
            VolunteerApplicationEvaluation evaluation,
            ParsedCv? parsedCv);
    }

    public class VolunteerAIResult
    {
        public VolunteerAIRecommendation Recommendation { get; set; }

        public decimal Confidence { get; set; }

        public string Strengths { get; set; } = string.Empty;

        public string Concerns { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public string RequirementMatches { get; set; } = string.Empty;
    }
}