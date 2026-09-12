using System;
using System.Collections.Generic;

namespace SwiftCare.ViewModels
{
    public class VolunteerInterviewReviewViewModel
    {
        // ============================================================
        // APPLICATION
        // ============================================================

        public int VolunteerApplicationId { get; set; }

        public string Reference { get; set; } = string.Empty;

        public DateTime AppliedDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Stage { get; set; } = string.Empty;

        public string EligibilityStatus { get; set; } = string.Empty;

        public string ScreeningStatus { get; set; } = string.Empty;

        public string CoverMessage { get; set; } = string.Empty;


        // ============================================================
        // APPLICANT
        // ============================================================

        public string ApplicantId { get; set; } = string.Empty;

        public string ApplicantName { get; set; } = "Applicant";

        public string ApplicantEmail { get; set; } = string.Empty;

        public string ApplicantPhone { get; set; } = string.Empty;

        public string ApplicantProfilePicture { get; set; } = string.Empty;


        // ============================================================
        // OPPORTUNITY
        // ============================================================

        public int VolunteerOpportunityId { get; set; }

        public string OpportunityTitle { get; set; } = string.Empty;


        // ============================================================
        // ATS EVALUATION
        // ============================================================

        public bool HasEvaluation { get; set; }

        public decimal OverallScore { get; set; }

        public decimal EligibilityScore { get; set; }

        public DateTime? EvaluatedDate { get; set; }

        public string EvaluationVersion { get; set; } = string.Empty;


        // ============================================================
        // CV
        // ============================================================

        public bool HasParsedCv { get; set; }

        public string CvFileName { get; set; } = string.Empty;

        public string CvFilePath { get; set; } = string.Empty;

        public string CvFullName { get; set; } = string.Empty;

        public string CvEmail { get; set; } = string.Empty;

        public string CvPhone { get; set; } = string.Empty;

        public string ProfessionalTitle { get; set; } = string.Empty;

        public string ProfessionalSummary { get; set; } = string.Empty;

        public string CareerObjective { get; set; } = string.Empty;

        public string CvAddress { get; set; } = string.Empty;

        public string CvCity { get; set; } = string.Empty;

        public string CvProvince { get; set; } = string.Empty;

        public string CvCountry { get; set; } = string.Empty;

        public string LinkedInUrl { get; set; } = string.Empty;

        public string PortfolioUrl { get; set; } = string.Empty;

        public string GitHubUrl { get; set; } = string.Empty;

        public bool CvExtractionSuccessful { get; set; }

        public string CvExtractionMessage { get; set; } = string.Empty;


        // ============================================================
        // CV COLLECTIONS
        // ============================================================

        public List<VolunteerInterviewReviewSkillViewModel> Skills { get; set; }
            = new();

        public List<VolunteerInterviewReviewExperienceViewModel> Experience { get; set; }
            = new();

        public List<VolunteerInterviewReviewEducationViewModel> Education { get; set; }
            = new();

        public List<VolunteerInterviewReviewCertificationViewModel> Certifications { get; set; }
            = new();

        public List<VolunteerInterviewReviewLanguageViewModel> Languages { get; set; }
            = new();


        // ============================================================
        // ROLE REQUIRED SKILLS
        // ============================================================

        public List<VolunteerInterviewRequiredSkillViewModel> RequiredSkills { get; set; }
            = new();

        public int RequiredSkillCount { get; set; }

        public int RequiredSkillsFoundInCv { get; set; }

        public int RequiredSkillsFoundInInterview { get; set; }

        public decimal RequiredSkillsOverallScore { get; set; }


        // ============================================================
        // PERSONAL / INTERVIEW COMPETENCIES
        // ============================================================

        public List<VolunteerInterviewCompetencyViewModel> InterviewCompetencies { get; set; }
            = new();

        public decimal OverallInterviewScore { get; set; }

        public decimal OverallInterviewConfidence { get; set; }

        public string InterviewRecommendation { get; set; } = "Not Evaluated";

        public string InterviewEvaluationStatus { get; set; } = "Not Evaluated";

        public DateTime? InterviewEvaluatedDate { get; set; }

        public string InterviewEvaluationVersion { get; set; } = string.Empty;


        // ============================================================
        // INTERVIEW
        // ============================================================

        public bool HasInterviewSession { get; set; }

        public bool InterviewStarted { get; set; }

        public bool InterviewCompleted { get; set; }

        public string InterviewStatus { get; set; } = "Not Started";

        public DateTime? InterviewStartedDate { get; set; }

        public DateTime? InterviewCompletedDate { get; set; }

        public int InterviewQuestionCount { get; set; }

        public int InterviewAnsweredCount { get; set; }

        public List<VolunteerInterviewTranscriptItemViewModel> Transcript { get; set; }
            = new();


        // ============================================================
        // DECISION
        // ============================================================

        public bool CanMakeDecision { get; set; }

        public string Decision { get; set; } = string.Empty;

        public string DecisionNotes { get; set; } = string.Empty;
    }


    // ================================================================
    // REQUIRED ROLE SKILL
    // ================================================================

    public class VolunteerInterviewRequiredSkillViewModel
    {
        public int Id { get; set; }

        public string Skill { get; set; } = string.Empty;

        // ------------------------------------------------------------
        // CV EVIDENCE
        // ------------------------------------------------------------

        public bool FoundInCv { get; set; }

        public string CvEvidence { get; set; } = string.Empty;


        // ------------------------------------------------------------
        // INTERVIEW EVIDENCE
        // ------------------------------------------------------------

        public bool FoundInInterview { get; set; }

        public string InterviewEvidence { get; set; } = string.Empty;


        // ------------------------------------------------------------
        // AI EVALUATION
        // ------------------------------------------------------------

        public decimal InterviewScore { get; set; }

        public decimal Confidence { get; set; }

        public string Assessment { get; set; } = "Not Evaluated";

        public string Evidence { get; set; } = string.Empty;

        public string Reasoning { get; set; } = string.Empty;
    }


    // ================================================================
    // PERSONAL / INTERVIEW COMPETENCY
    // ================================================================

    public class VolunteerInterviewCompetencyViewModel
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public decimal Confidence { get; set; }

        public string Assessment { get; set; } = "Not Evaluated";

        public string Evidence { get; set; } = string.Empty;

        public string Reasoning { get; set; } = string.Empty;

        public bool Demonstrated { get; set; }
    }


    // ================================================================
    // CV SKILL
    // ================================================================

    public class VolunteerInterviewReviewSkillViewModel
    {
        public string Skill { get; set; } = string.Empty;
    }


    // ================================================================
    // EXPERIENCE
    // ================================================================

    public class VolunteerInterviewReviewExperienceViewModel
    {
        public string Employer { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string StartDate { get; set; } = string.Empty;

        public string EndDate { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }


    // ================================================================
    // EDUCATION
    // ================================================================

    public class VolunteerInterviewReviewEducationViewModel
    {
        public string Institution { get; set; } = string.Empty;

        public string Qualification { get; set; } = string.Empty;

        public string FieldOfStudy { get; set; } = string.Empty;

        public string StartDate { get; set; } = string.Empty;

        public string EndDate { get; set; } = string.Empty;
    }


    // ================================================================
    // CERTIFICATION
    // ================================================================

    public class VolunteerInterviewReviewCertificationViewModel
    {
        public string Name { get; set; } = string.Empty;

        public string IssuingOrganisation { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;
    }


    // ================================================================
    // LANGUAGE
    // ================================================================

    public class VolunteerInterviewReviewLanguageViewModel
    {
        public string Language { get; set; } = string.Empty;
    }


    // ================================================================
    // INTERVIEW TRANSCRIPT
    // ================================================================

    public class VolunteerInterviewTranscriptItemViewModel
    {
        public int QuestionNumber { get; set; }

        public int QuestionId { get; set; }

        public string Question { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;
    }
}