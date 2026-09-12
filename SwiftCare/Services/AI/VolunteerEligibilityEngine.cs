using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerEligibilityResult
    {
        public VolunteerEligibilityStatus Status { get; set; }

        public decimal Score { get; set; }

        public List<string> PassedChecks { get; set; }
            = new();

        public List<string> FailedChecks { get; set; }
            = new();

        public List<string> ReviewItems { get; set; }
            = new();
    }


    public class VolunteerEligibilityEngine
    {
        private readonly SwiftCareDbContext _context;

        public VolunteerEligibilityEngine(
            SwiftCareDbContext context)
        {
            _context = context;
        }


        public async Task<VolunteerEligibilityResult>
            EvaluateAsync(
                VolunteerApplication application)
        {
            var result =
                new VolunteerEligibilityResult();

            // ====================================================
            // LOAD OPPORTUNITY AND APPLICANT
            // ====================================================

            var opportunity =
                application.VolunteerOpportunity;

            var applicant =
                application.Applicant;

            if (opportunity == null || applicant == null)
            {
                var loadedApplication =
                    await _context.VolunteerApplications
                        .Include(x => x.VolunteerOpportunity)
                        .Include(x => x.Applicant)
                        .FirstOrDefaultAsync(x =>
                            x.VolunteerApplicationId ==
                            application.VolunteerApplicationId);

                if (loadedApplication != null)
                {
                    opportunity =
                        loadedApplication.VolunteerOpportunity;

                    applicant =
                        loadedApplication.Applicant;
                }
            }

            if (opportunity == null)
            {
                opportunity =
                    await _context.VolunteerOpportunities
                        .FirstOrDefaultAsync(x =>
                            x.VolunteerOpportunityId ==
                            application.VolunteerOpportunityId);
            }

            if (applicant == null &&
                !string.IsNullOrWhiteSpace(
                    application.ApplicantId))
            {
                applicant =
                    await _context.Users
                        .OfType<Applicant>()
                        .FirstOrDefaultAsync(x =>
                            x.Id ==
                            application.ApplicantId);
            }

            // ====================================================
            // OPPORTUNITY CHECK
            // ====================================================

            if (opportunity == null)
            {
                result.Status =
                    VolunteerEligibilityStatus.NotEligible;

                result.FailedChecks.Add(
                    "Opportunity could not be found.");

                return result;
            }


            int totalChecks = 0;
            int passedChecks = 0;


            // ====================================================
            // CV
            // ====================================================

            totalChecks++;

            if (!string.IsNullOrWhiteSpace(
                    application.CVFilePath))
            {
                passedChecks++;

                result.PassedChecks.Add(
                    "CV has been submitted.");
            }
            else
            {
                result.FailedChecks.Add(
                    "CV has not been submitted.");
            }


            // ====================================================
            // APPLICANT
            // ====================================================

            totalChecks++;

            if (applicant != null)
            {
                passedChecks++;

                result.PassedChecks.Add(
                    "Applicant profile exists.");
            }
            else
            {
                result.FailedChecks.Add(
                    "Applicant profile is missing.");
            }


            // ====================================================
            // DATE OF BIRTH / AGE
            // ====================================================

            totalChecks++;

            if (applicant != null)
            {
                var today =
                    DateTime.Today;

                var dob =
                    applicant.DateOfBirth;

                var age =
                    today.Year - dob.Year;

                if (dob.Date >
                    today.AddYears(-age))
                {
                    age--;
                }

                int? minimumAge =
                    opportunity.MinimumAge;

                if (age >= minimumAge)
                {
                    passedChecks++;

                    result.PassedChecks.Add(
                        $"Applicant meets minimum age requirement ({age}, minimum {minimumAge}).");
                }
                else
                {
                    result.FailedChecks.Add(
                        $"Applicant is below the minimum volunteer age ({age}, minimum {minimumAge}).");
                }
            }
            else
            {
                result.ReviewItems.Add(
                    "Applicant age could not be verified.");
            }


            // ====================================================
            // COVER MESSAGE
            // ====================================================

            totalChecks++;

            if (!string.IsNullOrWhiteSpace(
                    application.CoverMessage))
            {
                passedChecks++;

                result.PassedChecks.Add(
                    "Cover message has been provided.");
            }
            else
            {
                result.FailedChecks.Add(
                    "Cover message is missing.");
            }


            // ====================================================
            // NO APPLICATION QUESTIONS
            // ====================================================
            //
            // Application questions are intentionally not checked.
            //
            // The current volunteer application flow uses:
            //
            // 1. CV
            // 2. Applicant profile
            // 3. Minimum age
            // 4. Cover message
            //
            // There is no VolunteerApplicationAnswers check here.
            // ====================================================


            // ====================================================
            // SCORE
            // ====================================================

            if (totalChecks > 0)
            {
                result.Score =
                    Math.Round(
                        (decimal)passedChecks /
                        totalChecks *
                        100,
                        2);
            }


            // ====================================================
            // FINAL STATUS
            // ====================================================

            if (result.FailedChecks.Count > 0)
            {
                result.Status =
                    VolunteerEligibilityStatus.NotEligible;
            }
            else if (result.ReviewItems.Count > 0)
            {
                result.Status =
                    VolunteerEligibilityStatus.ReviewRequired;
            }
            else
            {
                result.Status =
                    VolunteerEligibilityStatus.Eligible;
            }


            // ====================================================
            // RETURN RESULT
            // ====================================================

            return result;
        }
    }
}