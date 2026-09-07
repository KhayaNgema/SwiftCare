using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerScoringResult
    {
        public decimal QualificationScore { get; set; }

        public decimal ExperienceScore { get; set; }

        public decimal SkillsScore { get; set; }

        public decimal QuestionScore { get; set; }

        public decimal AvailabilityScore { get; set; }

        public decimal MotivationScore { get; set; }

        public decimal OverallScore { get; set; }
    }


    public class VolunteerScoringEngine
    {
        private readonly SwiftCareDbContext _context;

        public VolunteerScoringEngine(
            SwiftCareDbContext context)
        {
            _context = context;
        }


        public async Task<VolunteerScoringResult>
            CalculateAsync(
                VolunteerApplication application,
                VolunteerEligibilityResult eligibility)
        {
            var result =
                new VolunteerScoringResult();


            // ====================================================
            // QUALIFICATION
            // ====================================================

            result.QualificationScore =
                CalculateQualificationScore(
                    application);


            // ====================================================
            // EXPERIENCE
            // ====================================================

            result.ExperienceScore =
                CalculateExperienceScore(
                    application);


            // ====================================================
            // SKILLS
            // ====================================================

            result.SkillsScore =
                CalculateSkillsScore(
                    application);


            // ====================================================
            // APPLICATION QUESTIONS
            // ====================================================

            result.QuestionScore =
                await CalculateQuestionScoreAsync(
                    application);


            // ====================================================
            // AVAILABILITY
            // ====================================================

            result.AvailabilityScore =
                CalculateAvailabilityScore(
                    application);


            // ====================================================
            // MOTIVATION
            // ====================================================

            result.MotivationScore =
                CalculateMotivationScore(
                    application);


            // ====================================================
            // WEIGHTS
            // ====================================================

            var configuration =
                await _context
                    .VolunteerOpportunityScoringConfigurations
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerOpportunityId ==
                        application.VolunteerOpportunityId);


            if (configuration == null)
            {
                configuration =
                    new VolunteerOpportunityScoringConfiguration
                    {
                        VolunteerOpportunityId =
                            application.VolunteerOpportunityId,

                        QualificationWeight = 25,

                        ExperienceWeight = 20,

                        SkillsWeight = 15,

                        QuestionWeight = 20,

                        AvailabilityWeight = 10,

                        MotivationWeight = 10,

                        CreatedDate = DateTime.Now,

                        ModifiedDate = DateTime.Now
                    };

                _context
                    .VolunteerOpportunityScoringConfigurations
                    .Add(configuration);

                await _context.SaveChangesAsync();
            }


            // ====================================================
            // FINAL SCORE
            // ====================================================

            result.OverallScore =
                Math.Round(
                    (
                        result.QualificationScore *
                        configuration.QualificationWeight

                        +

                        result.ExperienceScore *
                        configuration.ExperienceWeight

                        +

                        result.SkillsScore *
                        configuration.SkillsWeight

                        +

                        result.QuestionScore *
                        configuration.QuestionWeight

                        +

                        result.AvailabilityScore *
                        configuration.AvailabilityWeight

                        +

                        result.MotivationScore *
                        configuration.MotivationWeight

                    ) / 100,
                    2);


            return result;
        }


        // ========================================================
        // QUALIFICATION
        // ========================================================

        private decimal CalculateQualificationScore(
            VolunteerApplication application)
        {
            if (application.Applicant == null)
                return 0;

            return 75;
        }


        // ========================================================
        // EXPERIENCE
        // ========================================================

        private decimal CalculateExperienceScore(
            VolunteerApplication application)
        {
            if (string.IsNullOrWhiteSpace(
                    application.CoverMessage))
            {
                return 30;
            }

            var message =
                application.CoverMessage
                    .ToLowerInvariant();

            int score = 40;

            string[] experienceTerms =
            {
                "experience",
                "volunteer",
                "nursing",
                "care",
                "resident",
                "elderly",
                "community",
                "support"
            };

            foreach (var term in experienceTerms)
            {
                if (message.Contains(term))
                {
                    score += 7;
                }
            }

            return Math.Min(score, 100);
        }


        // ========================================================
        // SKILLS
        // ========================================================

        private decimal CalculateSkillsScore(
                     VolunteerApplication application)
        {
            if (application.VolunteerOpportunity == null)
                return 50;

            var skills =
                application.VolunteerOpportunity.RequiredSkills;

            if (skills == null || skills.Count == 0)
                return 75;

            if (string.IsNullOrWhiteSpace(
                application.CoverMessage))
                return 30;

            var applicationText =
                application.CoverMessage.ToLowerInvariant();

            int matched = 0;

            foreach (var skill in skills)
            {
                if (skill == null)
                    continue;

                if (string.IsNullOrWhiteSpace(skill.Skill))
                    continue;

                if (applicationText.Contains(
                    skill.Skill.ToLowerInvariant()))
                {
                    matched++;
                }
            }

            return Math.Round(
                (decimal)matched / skills.Count * 100,
                2);
        }


        // ========================================================
        // QUESTIONS
        // ========================================================

        private async Task<decimal>
            CalculateQuestionScoreAsync(
                VolunteerApplication application)
        {
            var answers =
                await _context
                    .VolunteerApplicationAnswers
                    .Where(x =>
                        x.VolunteerApplicationId ==
                        application.VolunteerApplicationId)
                    .ToListAsync();

            if (answers.Count == 0)
                return 0;

            int answered = answers.Count(x =>
                !string.IsNullOrWhiteSpace(
                    x.Answer));

            return Math.Round(
                (decimal)answered /
                answers.Count *
                100,
                2);
        }


        // ========================================================
        // AVAILABILITY
        // ========================================================

        private decimal CalculateAvailabilityScore(
            VolunteerApplication application)
        {
            if (application.VolunteerOpportunity == null)
                return 50;

            if (application.VolunteerOpportunity.StartDate == null ||
                application.VolunteerOpportunity.EndDate == null)
            {
                return 75;
            }

            return 80;
        }


        // ========================================================
        // MOTIVATION
        // ========================================================

        private decimal CalculateMotivationScore(
            VolunteerApplication application)
        {
            if (string.IsNullOrWhiteSpace(
                    application.CoverMessage))
            {
                return 0;
            }

            var length =
                application.CoverMessage.Trim().Length;

            if (length >= 500)
                return 100;

            if (length >= 300)
                return 90;

            if (length >= 150)
                return 80;

            if (length >= 75)
                return 65;

            return 40;
        }
    }
}