using System.Collections.Generic;

namespace SwiftCare.Models.AI
{
    public class VolunteerInterviewAiEvaluationResponse
    {
        public decimal OverallScore { get; set; }

        public decimal OverallConfidence { get; set; }

        public string Recommendation { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public List<VolunteerInterviewAiSkillResult> Skills { get; set; }
            = new();

        public List<VolunteerInterviewAiCompetencyResult> Competencies { get; set; }
            = new();
    }

    public class VolunteerInterviewAiSkillResult
    {
        public string Skill { get; set; } = string.Empty;

        public bool FoundInProfile { get; set; }

        public string ProfileEvidence { get; set; } = string.Empty;

        public bool FoundInInterview { get; set; }

        public string InterviewEvidence { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public decimal Confidence { get; set; }

        public string Assessment { get; set; } = string.Empty;

        public string Reasoning { get; set; } = string.Empty;
    }

    public class VolunteerInterviewAiCompetencyResult
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public decimal Confidence { get; set; }

        public string Assessment { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;

        public string Reasoning { get; set; } = string.Empty;

        public bool Demonstrated { get; set; }
    }
}