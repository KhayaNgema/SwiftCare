using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerScoringResult
    {
        public decimal QualificationScore { get; set; }

        public decimal ExperienceScore { get; set; }

        public decimal SkillsScore { get; set; }

        public decimal? QuestionScore { get; set; }

        public decimal AvailabilityScore { get; set; }

        public decimal MotivationScore { get; set; }

        public decimal OverallScore { get; set; }

        public bool QuestionsApplicable { get; set; }

        public int MatchedSkills { get; set; }

        public int RequiredSkills { get; set; }

        public int MatchedQualifications { get; set; }

        public int RequiredQualifications { get; set; }

        public int MatchedExperienceRecords { get; set; }

        public int ExperienceRecords { get; set; }

        public int AnsweredQuestions { get; set; }

        public int TotalQuestions { get; set; }
    }

    public class VolunteerScoringEngine
    {
        private readonly SwiftCareDbContext _context;

        public VolunteerScoringEngine(
            SwiftCareDbContext context)
        {
            _context = context
                ?? throw new ArgumentNullException(
                    nameof(context));
        }

        // ============================================================
        // MAIN CALCULATION
        // ============================================================

        public async Task<VolunteerScoringResult>
            CalculateAsync(
                VolunteerApplication application,
                VolunteerEligibilityResult eligibility,
                ParsedCv parsedCv)
        {
            if (application == null)
                throw new ArgumentNullException(
                    nameof(application));

            if (parsedCv == null)
                throw new ArgumentNullException(
                    nameof(parsedCv));

            var result =
                new VolunteerScoringResult();

            // ========================================================
            // QUALIFICATION
            // ========================================================

            result.QualificationScore =
                CalculateQualificationScore(
                    application,
                    parsedCv,
                    result);

            // ========================================================
            // EXPERIENCE
            // ========================================================

            result.ExperienceScore =
                CalculateExperienceScore(
                    application,
                    parsedCv,
                    result);

            // ========================================================
            // SKILLS
            // ========================================================

            result.SkillsScore =
                CalculateSkillsScore(
                    application,
                    parsedCv,
                    result);

            // ========================================================
            // QUESTIONS
            // ========================================================

            var questionResult =
                await CalculateQuestionScoreAsync(
                    application);

            result.QuestionScore =
                questionResult.Score;

            result.QuestionsApplicable =
                questionResult.Applicable;

            result.AnsweredQuestions =
                questionResult.Answered;

            result.TotalQuestions =
                questionResult.Total;

            // ========================================================
            // AVAILABILITY
            // ========================================================

            result.AvailabilityScore =
                CalculateAvailabilityScore(
                    application,
                    eligibility);

            // ========================================================
            // MOTIVATION
            // ========================================================

            result.MotivationScore =
                CalculateMotivationScore(
                    application);

            // ========================================================
            // LOAD SCORING CONFIGURATION
            // ========================================================

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

            // ========================================================
            // FINAL SCORE
            // ========================================================

            decimal weightedTotal = 0;

            decimal activeWeight = 0;

            // Qualification
            weightedTotal +=
                result.QualificationScore *
                configuration.QualificationWeight;

            activeWeight +=
                configuration.QualificationWeight;

            // Experience
            weightedTotal +=
                result.ExperienceScore *
                configuration.ExperienceWeight;

            activeWeight +=
                configuration.ExperienceWeight;

            // Skills
            weightedTotal +=
                result.SkillsScore *
                configuration.SkillsWeight;

            activeWeight +=
                configuration.SkillsWeight;

            // Questions
            if (result.QuestionsApplicable &&
                result.QuestionScore.HasValue)
            {
                weightedTotal +=
                    result.QuestionScore.Value *
                    configuration.QuestionWeight;

                activeWeight +=
                    configuration.QuestionWeight;
            }

            // Availability
            weightedTotal +=
                result.AvailabilityScore *
                configuration.AvailabilityWeight;

            activeWeight +=
                configuration.AvailabilityWeight;

            // Motivation
            weightedTotal +=
                result.MotivationScore *
                configuration.MotivationWeight;

            activeWeight +=
                configuration.MotivationWeight;

            if (activeWeight > 0)
            {
                result.OverallScore =
                    Math.Round(
                        weightedTotal /
                        activeWeight,
                        2);
            }
            else
            {
                result.OverallScore = 0;
            }

            return result;
        }

        // ============================================================
        // QUALIFICATION
        // ============================================================

        private decimal CalculateQualificationScore(
            VolunteerApplication application,
            ParsedCv parsedCv,
            VolunteerScoringResult result)
        {
            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return 0;

            var education =
                parsedCv.Education;

            if (education == null ||
                education.Count == 0)
            {
                result.RequiredQualifications = 1;
                result.MatchedQualifications = 0;

                return 0;
            }

            // --------------------------------------------------------
            // Minimum qualification:
            // Grade 12 / Matric
            //
            // Advantageous:
            // Community Development
            // Youth Development
            // Education
            // Social Sciences
            // Public Administration
            // Psychology
            // --------------------------------------------------------

            bool hasMatric =
                education.Any(x =>
                    ContainsAny(
                        x.Qualification,
                        "grade 12",
                        "matric",
                        "national senior certificate",
                        "national certificate"));

            bool hasRelevantHigherQualification =
                education.Any(x =>
                    ContainsAny(
                        x.Qualification,
                        "diploma",
                        "degree",
                        "certificate",
                        "bachelor",
                        "higher certificate",
                        "advanced certificate") &&
                    ContainsAny(
                        x.Qualification,
                        "community development",
                        "youth development",
                        "education",
                        "social sciences",
                        "public administration",
                        "psychology"));

            // --------------------------------------------------------
            // Matric = baseline.
            //
            // 70 = minimum qualification satisfied
            // 100 = minimum + relevant higher qualification
            // --------------------------------------------------------

            if (hasMatric)
            {
                result.RequiredQualifications = 2;

                result.MatchedQualifications = 1;

                if (hasRelevantHigherQualification)
                {
                    result.MatchedQualifications = 2;

                    return 100;
                }

                return 70;
            }

            // --------------------------------------------------------
            // Higher qualification without identifiable Matric
            // --------------------------------------------------------

            if (hasRelevantHigherQualification)
            {
                result.RequiredQualifications = 2;

                result.MatchedQualifications = 1;

                return 50;
            }

            result.RequiredQualifications = 2;

            result.MatchedQualifications = 0;

            return 0;
        }

        // ============================================================
        // EXPERIENCE
        // ============================================================

        private decimal CalculateExperienceScore(
            VolunteerApplication application,
            ParsedCv parsedCv,
            VolunteerScoringResult result)
        {
            var experience =
                parsedCv.Experience;

            if (experience == null)
            {
                result.ExperienceRecords = 0;
                result.MatchedExperienceRecords = 0;

                return 0;
            }

            result.ExperienceRecords =
                experience.Count;

            if (experience.Count == 0)
            {
                result.MatchedExperienceRecords = 0;

                return 0;
            }

            int relevantRecords = 0;

            foreach (var item in experience)
            {
                var combined =
                    Combine(
                        item.Position,
                        item.Employer,
                        item.Description,
                        item.RawText);

                if (ContainsAny(
                    combined,
                    "community",
                    "youth",
                    "volunteer",
                    "programme",
                    "program",
                    "administrative",
                    "administration",
                    "outreach",
                    "social support",
                    "community development",
                    "participant",
                    "event coordination"))
                {
                    relevantRecords++;
                }
            }

            result.MatchedExperienceRecords =
                relevantRecords;

            // --------------------------------------------------------
            // Experience scoring
            // --------------------------------------------------------

            if (relevantRecords == 0)
                return 0;

            if (relevantRecords == 1)
                return 60;

            if (relevantRecords == 2)
                return 85;

            return 100;
        }

        // ============================================================
        // SKILLS
        // ============================================================

        private decimal CalculateSkillsScore(
            VolunteerApplication application,
            ParsedCv parsedCv,
            VolunteerScoringResult result)
        {
            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return 0;

            // --------------------------------------------------------
            // REQUIRED SKILLS
            // --------------------------------------------------------

            var requiredSkills =
                opportunity.RequiredSkills
                    ?.Where(x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(
                            x.Skill))
                    .Select(x =>
                        NormalizeSkill(
                            x.Skill!))
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList()
                ?? new List<string>();

            // --------------------------------------------------------
            // CANDIDATE SKILLS
            //
            // IMPORTANT:
            // ParsedCv.Skills is ICollection<CvSkill>
            // so we must use x.Skill.
            // --------------------------------------------------------

            var candidateSkills =
                parsedCv.Skills
                    ?.Where(x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(
                            x.Skill))
                    .Select(x =>
                        NormalizeSkill(
                            x.Skill!))
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList()
                ?? new List<string>();

            result.RequiredSkills =
                requiredSkills.Count;

            if (requiredSkills.Count == 0)
            {
                result.MatchedSkills = 0;

                return 0;
            }

            int matched = 0;

            foreach (var requiredSkill in requiredSkills)
            {
                bool found =
                    candidateSkills.Any(
                        candidateSkill =>
                            SkillsMatch(
                                candidateSkill,
                                requiredSkill));

                if (found)
                    matched++;
            }

            result.MatchedSkills =
                matched;

            return Math.Round(
                (decimal)matched /
                requiredSkills.Count *
                100,
                2);
        }

        // ============================================================
        // QUESTIONS
        // ============================================================

        private async Task<QuestionScoreResult>
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

            // --------------------------------------------------------
            // No questions / no answers:
            // criterion is not applicable.
            // --------------------------------------------------------

            if (answers.Count == 0)
            {
                return new QuestionScoreResult
                {
                    Applicable = false,

                    Score = null,

                    Answered = 0,

                    Total = 0
                };
            }

            int answered =
                answers.Count(x =>
                    !string.IsNullOrWhiteSpace(
                        x.Answer));

            return new QuestionScoreResult
            {
                Applicable = true,

                Score =
                    Math.Round(
                        (decimal)answered /
                        answers.Count *
                        100,
                        2),

                Answered = answered,

                Total = answers.Count
            };
        }

        // ============================================================
        // AVAILABILITY
        // ============================================================

        private decimal CalculateAvailabilityScore(
            VolunteerApplication application,
            VolunteerEligibilityResult eligibility)
        {
            if (eligibility == null)
                return 0;

            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return 0;

            if (!opportunity.StartDate.HasValue ||
                !opportunity.EndDate.HasValue)
            {
                return 0;
            }

            if (opportunity.EndDate <
                opportunity.StartDate)
            {
                return 0;
            }

            /*
             * At this stage we don't award availability points
             * simply because an opportunity has a valid date range.
             *
             * This should eventually be connected to the
             * applicant's actual availability.
             */

            return 0;
        }

        // ============================================================
        // MOTIVATION
        // ============================================================

        private decimal CalculateMotivationScore(
            VolunteerApplication application)
        {
            if (string.IsNullOrWhiteSpace(
                    application.CoverMessage))
            {
                return 0;
            }

            var text =
                application.CoverMessage.Trim();

            int score = 0;

            // Volunteer motivation
            if (ContainsAny(
                text,
                "volunteer",
                "volunteering",
                "volunteer with"))
            {
                score += 20;
            }

            // Community
            if (ContainsAny(
                text,
                "community",
                "community development",
                "community support",
                "community outreach"))
            {
                score += 20;
            }

            // Youth
            if (ContainsAny(
                text,
                "youth",
                "young people",
                "youth programme",
                "youth program"))
            {
                score += 20;
            }

            // Contribution / skills
            if (ContainsAny(
                text,
                "skills",
                "experience",
                "contribute",
                "contributing",
                "support"))
            {
                score += 15;
            }

            // Learning / development
            if (ContainsAny(
                text,
                "learn",
                "learning",
                "develop",
                "development",
                "gain experience"))
            {
                score += 15;
            }

            // Reasonable written response
            if (text.Length >= 150)
                score += 10;

            return Math.Min(
                score,
                100);
        }

        // ============================================================
        // SKILL NORMALISATION
        // ============================================================

        private static string NormalizeSkill(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized =
                value
                    .Trim()
                    .ToLowerInvariant();

            normalized =
                normalized
                    .Replace("&", "and")
                    .Replace("/", " ")
                    .Replace("-", " ");

            while (normalized.Contains("  "))
            {
                normalized =
                    normalized.Replace(
                        "  ",
                        " ");
            }

            return normalized.Trim();
        }

        // ============================================================
        // SKILL MATCHING
        // ============================================================

        private static bool SkillsMatch(
            string candidateSkill,
            string requiredSkill)
        {
            if (string.IsNullOrWhiteSpace(
                    candidateSkill) ||
                string.IsNullOrWhiteSpace(
                    requiredSkill))
            {
                return false;
            }

            if (candidateSkill.Equals(
                requiredSkill,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var aliases =
                new Dictionary<string, string[]>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["communication"] =
                        new[]
                        {
                            "communication skills",
                            "verbal communication",
                            "written communication"
                        },

                    ["communication skills"] =
                        new[]
                        {
                            "communication",
                            "verbal communication",
                            "written communication"
                        },

                    ["data capture"] =
                        new[]
                        {
                            "data capturing",
                            "data entry",
                            "data capturing skills"
                        },

                    ["data capturing"] =
                        new[]
                        {
                            "data capture",
                            "data entry"
                        },

                    ["microsoft office"] =
                        new[]
                        {
                            "microsoft word",
                            "microsoft excel",
                            "microsoft powerpoint"
                        },

                    ["organisational skills"] =
                        new[]
                        {
                            "organizational skills",
                            "organisation",
                            "organization"
                        },

                    ["organizational skills"] =
                        new[]
                        {
                            "organisational skills",
                            "organisation",
                            "organization"
                        }
                };

            if (aliases.TryGetValue(
                    requiredSkill,
                    out var alternatives))
            {
                if (alternatives.Any(x =>
                    candidateSkill.Equals(
                        x,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            if (aliases.TryGetValue(
                    candidateSkill,
                    out var candidateAlternatives))
            {
                if (candidateAlternatives.Any(x =>
                    x.Equals(
                        requiredSkill,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================================
        // TEXT HELPERS
        // ============================================================

        private static bool ContainsAny(
            string? text,
            params string[] terms)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (var term in terms)
            {
                if (string.IsNullOrWhiteSpace(term))
                    continue;

                if (text.Contains(
                    term,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Combine(
            params string?[] values)
        {
            return string.Join(
                " ",
                values.Where(x =>
                    !string.IsNullOrWhiteSpace(x)));
        }

        // ============================================================
        // QUESTION RESULT
        // ============================================================

        private sealed class QuestionScoreResult
        {
            public bool Applicable { get; set; }

            public decimal? Score { get; set; }

            public int Answered { get; set; }

            public int Total { get; set; }
        }
    }
}