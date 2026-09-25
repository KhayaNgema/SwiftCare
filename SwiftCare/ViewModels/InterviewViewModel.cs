using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    // ================================================================
    // START VIEW MODEL
    // ================================================================

    public class VolunteerInterviewStartViewModel
    {
        public string Token { get; set; } =
            string.Empty;

        public string ApplicantName { get; set; } =
            "Applicant";

        public string OpportunityTitle { get; set; } =
            string.Empty;

        public int QuestionCount { get; set; }

        public int EstimatedMinutes { get; set; }

        public bool IsStarted { get; set; }

        // ============================================================
        // VIDEO RECORDING
        // ============================================================

        public bool VideoRecordingEnabled { get; set; }

        // ============================================================
        // PROCTORING
        // ============================================================

        public bool ProctoringEnabled { get; set; }
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

        // ============================================================
        // SESSION
        // ============================================================

        public int VolunteerVoiceInterviewSessionId { get; set; }

        public bool IsStarted { get; set; }

        public bool IsCompleted { get; set; }

        // ============================================================
        // VIDEO RECORDING
        // ============================================================

        /// <summary>
        /// Determines whether the interview page should start
        /// camera/video recording.
        /// </summary>
        public bool VideoRecordingEnabled { get; set; }

        /// <summary>
        /// Indicates whether recording has already started.
        /// </summary>
        public bool VideoRecordingStarted { get; set; }

        // ============================================================
        // PROCTORING
        // ============================================================

        /// <summary>
        /// Determines whether browser proctoring is enabled.
        /// </summary>
        public bool ProctoringEnabled { get; set; }

        /// <summary>
        /// Number of tab switches already detected.
        /// </summary>
        public int TabSwitchCount { get; set; }

        /// <summary>
        /// Number of times the browser window lost focus.
        /// </summary>
        public int WindowBlurCount { get; set; }

        /// <summary>
        /// Number of fullscreen exits detected.
        /// </summary>
        public int FullScreenExitCount { get; set; }

        /// <summary>
        /// Number of page leave events detected.
        /// </summary>
        public int PageLeaveCount { get; set; }

        /// <summary>
        /// Number of suspicious events detected.
        /// </summary>
        public int SuspiciousEventCount { get; set; }

        /// <summary>
        /// Indicates that the interview requires administrator review.
        /// </summary>
        public bool RequiresProctoringReview { get; set; }

        /// <summary>
        /// Current proctoring status.
        /// </summary>
        public string ProctoringStatus { get; set; } =
            "NotChecked";
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

        // ============================================================
        // VIDEO
        // ============================================================

        public bool VideoRecordingCompleted { get; set; }

        // ============================================================
        // PROCTORING RESULT
        // ============================================================

        public string ProctoringStatus { get; set; } =
            "NotChecked";

        public int SuspiciousEventCount { get; set; }

        public bool RequiresProctoringReview { get; set; }
    }
}