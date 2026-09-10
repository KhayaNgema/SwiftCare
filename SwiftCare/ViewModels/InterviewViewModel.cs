using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    // ================================================================
    // START VIEW MODEL
    // ================================================================

    public class VolunteerInterviewStartViewModel
    {
        public string Token { get; set; } = string.Empty;

        public string ApplicantName { get; set; } =
            "Applicant";

        public string OpportunityTitle { get; set; } =
            string.Empty;

        public int QuestionCount { get; set; }

        public int EstimatedMinutes { get; set; }

        public bool IsStarted { get; set; }
    }

    // ================================================================
    // INTERVIEW VIEW MODEL
    // ================================================================

    public class VolunteerInterviewViewModel
    {
        public string Token { get; set; } =
            string.Empty;

        public string ApplicantName { get; set; } =
            "Applicant";

        public string OpportunityTitle { get; set; } =
            string.Empty;

        public int QuestionNumber { get; set; }

        public int TotalQuestions { get; set; }

        public int QuestionId { get; set; }

        public string Question { get; set; } =
            string.Empty;

        public VolunteerQuestionType? QuestionType
        {
            get;
            set;
        }

        public string? ExistingAnswer { get; set; }

        public bool IsLastQuestion { get; set; }
    }

    // ================================================================
    // COMPLETED VIEW MODEL
    // ================================================================

    public class VolunteerInterviewCompletedViewModel
    {
        public string ApplicantName { get; set; } =
            "Applicant";

        public string OpportunityTitle { get; set; } =
            string.Empty;

        public DateTime? CompletedDate { get; set; }
    }

}

