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

            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
            {
                opportunity =
                    await _context.VolunteerOpportunities
                        .FirstOrDefaultAsync(x =>
                            x.VolunteerOpportunityId ==
                            application.VolunteerOpportunityId);
            }

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

            if (application.Applicant != null)
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

            if (application.Applicant != null)
            {
                var today = DateTime.Today;

                var dob =
                    application.Applicant.DateOfBirth;

                var age =
                    today.Year - dob.Year;

                if (dob.Date >
                    today.AddYears(-age))
                {
                    age--;
                }

                if (age >= 18)
                {
                    passedChecks++;

                    result.PassedChecks.Add(
                        $"Applicant meets minimum age requirement ({age}).");
                }
                else
                {
                    result.FailedChecks.Add(
                        $"Applicant is below the minimum volunteer age ({age}).");
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
            // APPLICATION ANSWERS
            // ====================================================

            totalChecks++;

            var answerCount =
                await _context.VolunteerApplicationAnswers
                    .CountAsync(x =>
                        x.VolunteerApplicationId ==
                        application.VolunteerApplicationId);

            if (answerCount > 0)
            {
                passedChecks++;

                result.PassedChecks.Add(
                    $"{answerCount} application answer(s) found.");
            }
            else
            {
                result.ReviewItems.Add(
                    "No application answers were found.");
            }


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

            return result;
        }
    }
}