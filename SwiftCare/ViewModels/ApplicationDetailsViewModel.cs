using System;
using System.Collections.Generic;
using SwiftCare.Models;

namespace SwiftCare.ViewModels.Volunteer
{
    public class ApplicationDetailsViewModel
    {
        // ============================================================
        // APPLICATION
        // ============================================================

        public VolunteerApplication Application { get; set; }
            = null!;

        // ============================================================
        // EVALUATION
        // ============================================================

        public VolunteerApplicationEvaluation? Evaluation { get; set; }

        // ============================================================
        // AUDIT HISTORY
        // ============================================================

        public List<VolunteerApplicationAudit> AuditHistory { get; set; }
            = new List<VolunteerApplicationAudit>();

        // ============================================================
        // HELPER PROPERTIES
        // ============================================================

        public string ApplicantName
        {
            get
            {
                if (Application?.Applicant == null)
                    return "Unknown Applicant";

                var firstName =
                    Application.Applicant.FirstName?.Trim();

                var lastName =
                    Application.Applicant.LastName?.Trim();

                var fullName =
                    $"{firstName} {lastName}".Trim();

                return string.IsNullOrWhiteSpace(fullName)
                    ? "Unknown Applicant"
                    : fullName;
            }
        }

        public string OpportunityTitle
        {
            get
            {
                return Application?.VolunteerOpportunity?.Title
                    ?? "Volunteer Opportunity";
            }
        }

        public decimal ATSScore
        {
            get
            {
                return Application?.ATSScore
                    ?? Evaluation?.OverallScore
                    ?? 0m;
            }
        }

        public int? Ranking
        {
            get
            {
                return Application?.ATSRanking
                    ?? Evaluation?.RankingPosition;
            }
        }

        public bool HasEvaluation
        {
            get
            {
                return Evaluation != null;
            }
        }

        public bool HasCV
        {
            get
            {
                return !string.IsNullOrWhiteSpace(
                    Application?.CVFilePath);
            }
        }
    }
}