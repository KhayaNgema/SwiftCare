using SwiftCare.Models;

namespace SwiftCare.Services.VolunteerATS
{
    public interface IVolunteerInterviewEvaluationEngine
    {
        Task<VolunteerInterviewEvaluationResult> EvaluateAsync(
            int volunteerApplicationId,
            CancellationToken cancellationToken = default);
    }

    public class VolunteerInterviewEvaluationResult
    {
        public bool Success { get; set; }

        public int VolunteerApplicationId { get; set; }

        public int VolunteerInterviewEvaluationId { get; set; }

        public decimal OverallInterviewScore { get; set; }

        public decimal OverallInterviewConfidence { get; set; }

        public string Recommendation { get; set; } =
            "Not Evaluated";

        public string EvaluationStatus { get; set; } =
            "Not Evaluated";

        public string Explanation { get; set; } =
            string.Empty;

        public int RequiredSkillCount { get; set; }

        public int RequiredSkillsFoundInCv { get; set; }

        public int RequiredSkillsFoundInInterview { get; set; }

        public int CompetencyCount { get; set; }

        public string ErrorMessage { get; set; } =
            string.Empty;
    }
}