using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS
{
    // ================================================================
    // VOLUNTEER SCORING RESULT
    // ================================================================
    //
    // APPLICATION QUESTIONS HAVE BEEN COMPLETELY REMOVED.
    //
    // The scoring result now contains only:
    //
    // - Qualification
    // - Experience
    // - Skills
    // - Availability
    // - Motivation / Cover Message
    // - Overall Score
    //
    // ================================================================

    public class VolunteerScoringResult
    {
        public decimal QualificationScore { get; set; }

        public decimal ExperienceScore { get; set; }

        public decimal SkillsScore { get; set; }

        public decimal AvailabilityScore { get; set; }

        public decimal MotivationScore { get; set; }

        public decimal OverallScore { get; set; }

        public int MatchedSkills { get; set; }

        public int RequiredSkills { get; set; }

        public int MatchedQualifications { get; set; }

        public int RequiredQualifications { get; set; }

        public int MatchedExperienceRecords { get; set; }

        public int ExperienceRecords { get; set; }
    }


    // ================================================================
    // VOLUNTEER SCORING ENGINE
    // ================================================================

    public class VolunteerScoringEngine
    {
        private readonly SwiftCareDbContext _context;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

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
            {
                throw new ArgumentNullException(
                    nameof(application));
            }

            if (parsedCv == null)
            {
                throw new ArgumentNullException(
                    nameof(parsedCv));
            }


            // ========================================================
            // LOAD OPPORTUNITY + REQUIRED SKILLS
            // ========================================================

            var opportunity =
                await _context
                    .VolunteerOpportunities
                    .Include(x => x.RequiredSkills)
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerOpportunityId ==
                        application.VolunteerOpportunityId);


            if (opportunity == null)
            {
                throw new InvalidOperationException(
                    $"Volunteer opportunity " +
                    $"{application.VolunteerOpportunityId} " +
                    $"could not be found.");
            }


            // Make sure the application points to the fully loaded
            // opportunity so all scoring methods can use it.

            application.VolunteerOpportunity =
                opportunity;


            Console.WriteLine();

            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "[SWIFTCARE ATS] SCORING ENGINE STARTED");

            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Application ID: " +
                $"{application.VolunteerApplicationId}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Opportunity ID: " +
                $"{opportunity.VolunteerOpportunityId}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Opportunity: " +
                $"{opportunity.Title}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] RequiredSkills records loaded: " +
                $"{opportunity.RequiredSkills?.Count ?? 0}");

            Console.WriteLine(
                "============================================================");


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


            Console.WriteLine(
                $"[SWIFTCARE ATS] Qualification Score: " +
                $"{result.QualificationScore}");


            // ========================================================
            // EXPERIENCE
            // ========================================================

            result.ExperienceScore =
                CalculateExperienceScore(
                    application,
                    parsedCv,
                    result);


            Console.WriteLine(
                $"[SWIFTCARE ATS] Experience Score: " +
                $"{result.ExperienceScore}");


            // ========================================================
            // SKILLS
            // ========================================================

            result.SkillsScore =
                CalculateSkillsScore(
                    application,
                    parsedCv,
                    result);


            Console.WriteLine(
                $"[SWIFTCARE ATS] Skills Score: " +
                $"{result.SkillsScore}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Skills Matched: " +
                $"{result.MatchedSkills}/" +
                $"{result.RequiredSkills}");


            // ========================================================
            // AVAILABILITY
            // ========================================================

            result.AvailabilityScore =
                CalculateAvailabilityScore(
                    application,
                    eligibility);


            Console.WriteLine(
                $"[SWIFTCARE ATS] Availability Score: " +
                $"{result.AvailabilityScore}");


            // ========================================================
            // MOTIVATION
            // ========================================================
            //
            // Motivation is calculated from the CoverMessage.
            //
            // Application questions are NOT used.
            //
            // ========================================================

            result.MotivationScore =
                CalculateMotivationScore(
                    application);


            Console.WriteLine(
                $"[SWIFTCARE ATS] Motivation / Cover Message Score: " +
                $"{result.MotivationScore}");


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
                Console.WriteLine(
                    "[SWIFTCARE ATS] No scoring configuration found.");

                Console.WriteLine(
                    "[SWIFTCARE ATS] Creating default scoring configuration.");


                configuration =
                    new VolunteerOpportunityScoringConfiguration
                    {
                        VolunteerOpportunityId =
                            application.VolunteerOpportunityId,

                        QualificationWeight = 25,

                        ExperienceWeight = 20,

                        SkillsWeight = 15,

                        // ------------------------------------------------
                        // QUESTIONS REMOVED
                        // ------------------------------------------------
                        //
                        // No QuestionWeight is used anymore.
                        //
                        // ------------------------------------------------

                        AvailabilityWeight = 10,

                        MotivationWeight = 30,

                        CreatedDate = DateTime.Now,

                        ModifiedDate = DateTime.Now
                    };


                _context
                    .VolunteerOpportunityScoringConfigurations
                    .Add(configuration);


                await _context.SaveChangesAsync();
            }


            // ========================================================
            // FINAL WEIGHTED SCORE
            // ========================================================

            decimal weightedTotal = 0;

            decimal activeWeight = 0;


            // --------------------------------------------------------
            // QUALIFICATION
            // --------------------------------------------------------

            weightedTotal +=
                result.QualificationScore *
                configuration.QualificationWeight;

            activeWeight +=
                configuration.QualificationWeight;


            // --------------------------------------------------------
            // EXPERIENCE
            // --------------------------------------------------------

            weightedTotal +=
                result.ExperienceScore *
                configuration.ExperienceWeight;

            activeWeight +=
                configuration.ExperienceWeight;


            // --------------------------------------------------------
            // SKILLS
            // --------------------------------------------------------

            weightedTotal +=
                result.SkillsScore *
                configuration.SkillsWeight;

            activeWeight +=
                configuration.SkillsWeight;


            // --------------------------------------------------------
            // AVAILABILITY
            // --------------------------------------------------------

            weightedTotal +=
                result.AvailabilityScore *
                configuration.AvailabilityWeight;

            activeWeight +=
                configuration.AvailabilityWeight;


            // --------------------------------------------------------
            // MOTIVATION / COVER MESSAGE
            // --------------------------------------------------------

            weightedTotal +=
                result.MotivationScore *
                configuration.MotivationWeight;

            activeWeight +=
                configuration.MotivationWeight;


            // ========================================================
            // CALCULATE OVERALL
            // ========================================================

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


            // ========================================================
            // FINAL LOG
            // ========================================================

            Console.WriteLine();

            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "[SWIFTCARE ATS] FINAL SCORING RESULT");

            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Qualification: " +
                $"{result.QualificationScore}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Experience: " +
                $"{result.ExperienceScore}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Skills: " +
                $"{result.SkillsScore}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Availability: " +
                $"{result.AvailabilityScore}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Motivation / Cover Message: " +
                $"{result.MotivationScore}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Overall: " +
                $"{result.OverallScore}");

            Console.WriteLine(
                "============================================================");


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
            {
                return 0;
            }


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
            // MINIMUM QUALIFICATION
            // --------------------------------------------------------

            bool hasMatric =
                education.Any(x =>
                    ContainsAny(
                        x.Qualification,

                        "grade 12",

                        "matric",

                        "national senior certificate",

                        "national certificate"));


            // --------------------------------------------------------
            // RELEVANT HIGHER QUALIFICATION
            // --------------------------------------------------------

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

                        "community work",

                        "youth development",

                        "youth work",

                        "education",

                        "teaching",

                        "social sciences",

                        "social science",

                        "public administration",

                        "psychology",

                        "human services",

                        "caregiving",

                        "healthcare",

                        "nursing",

                        "gerontology",

                        "elderly care",

                        "social care"));


            // --------------------------------------------------------
            // RELEVANT CERTIFICATION
            // --------------------------------------------------------

            bool hasRelevantCertification =
                parsedCv.Certifications != null &&
                parsedCv.Certifications.Any(x =>
                    ContainsAny(
                        x.Name,

                        "first aid",

                        "CPR",

                        "caregiving",

                        "elderly care",

                        "community development",

                        "community care",

                        "social care",

                        "healthcare",

                        "child care",

                        "disability support",

                        "mental health first aid",

                        "resident care",

                        "basic life support"));


            // --------------------------------------------------------
            // MATRIC + RELEVANT HIGHER QUALIFICATION
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


                if (hasRelevantCertification)
                {
                    return 85;
                }


                return 70;
            }


            // --------------------------------------------------------
            // RELEVANT HIGHER QUALIFICATION WITHOUT MATRIC
            // --------------------------------------------------------

            if (hasRelevantHigherQualification)
            {
                result.RequiredQualifications = 2;

                result.MatchedQualifications = 1;

                return 65;
            }


            // --------------------------------------------------------
            // RELEVANT CERTIFICATION ONLY
            // --------------------------------------------------------

            if (hasRelevantCertification)
            {
                result.RequiredQualifications = 2;

                result.MatchedQualifications = 1;

                return 45;
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


            var opportunity =
                application.VolunteerOpportunity;


            string opportunityText =
                Combine(
                    opportunity?.Title,
                    opportunity?.Description);


            // ========================================================
            // OPPORTUNITY RESPONSIBILITY CATEGORIES
            // ========================================================

            var categories =
                new Dictionary<string, decimal>(
                    StringComparer.OrdinalIgnoreCase);


            if (ContainsAny(
                opportunityText,
                "companion",
                "companionship",
                "talking",
                "conversation"))
            {
                categories["companionship"] = 20;
            }


            if (ContainsAny(
                opportunityText,
                "elderly",
                "resident",
                "senior",
                "elderly support"))
            {
                categories["resident support"] = 20;
            }


            if (ContainsAny(
                opportunityText,
                "activity",
                "activities",
                "recreation",
                "games",
                "arts",
                "crafts",
                "entertainment"))
            {
                categories["activities"] = 15;
            }


            if (ContainsAny(
                opportunityText,
                "communication",
                "listen",
                "listening",
                "conversation"))
            {
                categories["communication"] = 15;
            }


            if (ContainsAny(
                opportunityText,
                "empathy",
                "patience",
                "compassion",
                "kindness"))
            {
                categories["empathy"] = 10;
            }


            if (ContainsAny(
                opportunityText,
                "teamwork",
                "team",
                "staff",
                "collaborate"))
            {
                categories["teamwork"] = 10;
            }


            if (ContainsAny(
                opportunityText,
                "organise",
                "organize",
                "organisation",
                "organization",
                "prepare",
                "coordination"))
            {
                categories["organisation"] = 10;
            }


            if (ContainsAny(
                opportunityText,
                "record",
                "records",
                "attendance",
                "administration",
                "data"))
            {
                categories["records"] = 10;
            }


            if (ContainsAny(
                opportunityText,
                "community",
                "outreach",
                "community support"))
            {
                categories["community"] = 10;
            }


            if (ContainsAny(
                opportunityText,
                "safe",
                "safety",
                "boundaries",
                "confidentiality",
                "privacy"))
            {
                categories["safety"] = 10;
            }


            // If opportunity text did not identify categories,
            // use sensible general volunteer categories.

            if (categories.Count == 0)
            {
                categories["community"] = 20;

                categories["communication"] = 20;

                categories["teamwork"] = 20;

                categories["organisation"] = 20;

                categories["support"] = 20;
            }


            decimal totalCategoryWeight =
                categories.Values.Sum();


            int relevantRecords = 0;

            decimal relevanceTotal = 0;


            // ========================================================
            // SCORE EACH EXPERIENCE RECORD
            // ========================================================

            foreach (var item in experience)
            {
                var combined =
                    Combine(
                        item.Position,
                        item.Employer,
                        item.Description,
                        item.RawText);


                if (string.IsNullOrWhiteSpace(combined))
                {
                    Console.WriteLine(
                        "[SWIFTCARE ATS] Experience record " +
                        "contains no usable text.");

                    continue;
                }


                decimal matchedWeight = 0;


                foreach (var category in categories)
                {
                    bool matched =
                        category.Key switch
                        {
                            "companionship" =>
                                ContainsAny(
                                    combined,
                                    "companionship",
                                    "companion",
                                    "conversation",
                                    "talking",
                                    "social interaction"),

                            "resident support" =>
                                ContainsAny(
                                    combined,
                                    "elderly",
                                    "resident",
                                    "senior",
                                    "elderly support",
                                    "resident support",
                                    "elderly care"),

                            "activities" =>
                                ContainsAny(
                                    combined,
                                    "activity",
                                    "activities",
                                    "recreation",
                                    "recreational",
                                    "games",
                                    "puzzles",
                                    "reading",
                                    "arts",
                                    "crafts",
                                    "events"),

                            "communication" =>
                                ContainsAny(
                                    combined,
                                    "communication",
                                    "communicate",
                                    "listening",
                                    "active listening",
                                    "conversation"),

                            "empathy" =>
                                ContainsAny(
                                    combined,
                                    "empathy",
                                    "empathetic",
                                    "compassion",
                                    "patient",
                                    "patience",
                                    "kindness"),

                            "teamwork" =>
                                ContainsAny(
                                    combined,
                                    "teamwork",
                                    "team",
                                    "staff",
                                    "worked with",
                                    "collaborated"),

                            "organisation" =>
                                ContainsAny(
                                    combined,
                                    "organisation",
                                    "organization",
                                    "organised",
                                    "organized",
                                    "prepare",
                                    "prepared",
                                    "coordination"),

                            "records" =>
                                ContainsAny(
                                    combined,
                                    "records",
                                    "record",
                                    "attendance",
                                    "participant lists",
                                    "administration",
                                    "data"),

                            "community" =>
                                ContainsAny(
                                    combined,
                                    "community",
                                    "outreach",
                                    "community support",
                                    "community programme",
                                    "community program"),

                            "safety" =>
                                ContainsAny(
                                    combined,
                                    "safety",
                                    "safe",
                                    "boundaries",
                                    "confidentiality",
                                    "privacy",
                                    "instructions"),

                            "support" =>
                                ContainsAny(
                                    combined,
                                    "support",
                                    "help",
                                    "assist",
                                    "assisted"),

                            _ => false
                        };


                    if (matched)
                    {
                        matchedWeight +=
                            category.Value;
                    }
                }


                decimal relevance =
                    totalCategoryWeight > 0
                        ? matchedWeight /
                          totalCategoryWeight *
                          100
                        : 0;


                // ----------------------------------------------------
                // ACTION BONUS
                // ----------------------------------------------------

                if (ContainsAny(
                    combined,
                    "provided",
                    "assisted",
                    "supported",
                    "helped",
                    "organised",
                    "organized",
                    "coordinated",
                    "worked",
                    "encouraged",
                    "maintained",
                    "prepared",
                    "communicated"))
                {
                    relevance += 5;
                }


                // ----------------------------------------------------
                // DESCRIPTION DEPTH BONUS
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                    item.Description))
                {
                    if (item.Description.Length >= 150)
                    {
                        relevance += 5;
                    }
                    else if (item.Description.Length >= 75)
                    {
                        relevance += 3;
                    }
                }


                relevance =
                    Math.Min(
                        relevance,
                        100);


                Console.WriteLine(
                    $"[SWIFTCARE ATS] Experience: " +
                    $"{item.Position} | " +
                    $"{item.Employer} | " +
                    $"Relevance={relevance:0.##}");


                if (relevance >= 40)
                {
                    relevantRecords++;
                }


                relevanceTotal +=
                    relevance;
            }


            result.MatchedExperienceRecords =
                relevantRecords;


            if (experience.Count == 0)
            {
                return 0;
            }


            // ========================================================
            // BASE EXPERIENCE SCORE
            // ========================================================

            decimal averageRelevance =
                relevanceTotal /
                experience.Count;


            // ========================================================
            // DEPTH BONUS
            // ========================================================

            decimal depthBonus = 0;


            if (relevantRecords >= 2)
            {
                depthBonus += 2;
            }


            if (relevantRecords >= 3)
            {
                depthBonus += 4;
            }


            if (relevantRecords >= 4)
            {
                depthBonus += 5;
            }


            decimal finalScore =
                Math.Min(
                    averageRelevance +
                    depthBonus,
                    100);


            return Math.Round(
                finalScore,
                2);
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
            {
                Console.WriteLine(
                    "[SWIFTCARE ATS] Skills: " +
                    "Opportunity is NULL.");

                result.RequiredSkills = 0;

                result.MatchedSkills = 0;

                return 0;
            }


            // ========================================================
            // REQUIRED SKILLS
            //
            // VolunteerOpportunity.RequiredSkills is:
            //
            // ICollection<VolunteerRequiredSkill>
            //
            // We therefore extract x.Skill.
            // ========================================================

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
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList()
                ?? new List<string>();


            // ========================================================
            // CANDIDATE SKILLS
            // ========================================================

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
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList()
                ?? new List<string>();


            // ========================================================
            // RESULT COUNTS
            // ========================================================

            result.RequiredSkills =
                requiredSkills.Count;


            // ========================================================
            // DEBUG LOGGING
            // ========================================================

            Console.WriteLine();

            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "[SWIFTCARE ATS] SKILLS COMPARISON DEBUG");

            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Opportunity: " +
                $"{opportunity.Title}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] RequiredSkills records: " +
                $"{opportunity.RequiredSkills?.Count ?? 0}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Required skill count: " +
                $"{requiredSkills.Count}");


            Console.WriteLine(
                "[SWIFTCARE ATS] REQUIRED SKILLS:");

            foreach (var requiredSkill in requiredSkills)
            {
                Console.WriteLine(
                    $"[SWIFTCARE ATS] REQUIRED => " +
                    $"{requiredSkill}");
            }


            Console.WriteLine();

            Console.WriteLine(
                $"[SWIFTCARE ATS] Candidate skill count: " +
                $"{candidateSkills.Count}");


            Console.WriteLine(
                "[SWIFTCARE ATS] CANDIDATE SKILLS:");

            foreach (var candidateSkill in candidateSkills)
            {
                Console.WriteLine(
                    $"[SWIFTCARE ATS] CANDIDATE => " +
                    $"{candidateSkill}");
            }


            // ========================================================
            // NO REQUIRED SKILLS
            // ========================================================

            if (requiredSkills.Count == 0)
            {
                result.MatchedSkills = 0;

                Console.WriteLine();

                Console.WriteLine(
                    "[SWIFTCARE ATS] WARNING: " +
                    "NO REQUIRED SKILLS FOUND FOR OPPORTUNITY.");

                Console.WriteLine(
                    "[SWIFTCARE ATS] Skills Score: 0");

                Console.WriteLine(
                    "============================================================");

                return 0;
            }


            // ========================================================
            // NO CANDIDATE SKILLS
            // ========================================================

            if (candidateSkills.Count == 0)
            {
                result.MatchedSkills = 0;

                Console.WriteLine();

                Console.WriteLine(
                    "[SWIFTCARE ATS] WARNING: " +
                    "CV CONTAINS NO PARSED SKILLS.");

                Console.WriteLine(
                    $"[SWIFTCARE ATS] Skills Matched: " +
                    $"0/{requiredSkills.Count}");

                Console.WriteLine(
                    "[SWIFTCARE ATS] Skills Score: 0");

                Console.WriteLine(
                    "============================================================");

                return 0;
            }


            // ========================================================
            // MATCH REQUIRED SKILLS
            // ========================================================

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
                {
                    matched++;

                    Console.WriteLine(
                        $"[SWIFTCARE ATS] SKILL MATCH => " +
                        $"{requiredSkill}");
                }
                else
                {
                    Console.WriteLine(
                        $"[SWIFTCARE ATS] SKILL MISS => " +
                        $"{requiredSkill}");
                }
            }


            result.MatchedSkills =
                matched;


            decimal score =
                (decimal)matched /
                requiredSkills.Count *
                100;


            score =
                Math.Clamp(
                    score,
                    0,
                    100);


            score =
                Math.Round(
                    score,
                    2);


            Console.WriteLine();

            Console.WriteLine(
                $"[SWIFTCARE ATS] Skills Matched: " +
                $"{matched}/{requiredSkills.Count}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Skills Score: " +
                $"{score}");

            Console.WriteLine(
                "============================================================");


            return score;
        }


        // ============================================================
        // AVAILABILITY
        // ============================================================

        private decimal CalculateAvailabilityScore(
            VolunteerApplication application,
            VolunteerEligibilityResult eligibility)
        {
            if (eligibility == null)
            {
                return 0;
            }


            var opportunity =
                application.VolunteerOpportunity;


            if (opportunity == null)
            {
                return 0;
            }


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
             * We do not award availability points simply because
             * the opportunity has a valid date range.
             *
             * This should eventually be connected to the
             * applicant's actual availability.
             */

            return 0;
        }


        // ============================================================
        // MOTIVATION / COVER MESSAGE
        // ============================================================
        //
        // The CoverMessage is now the only applicant-written response
        // used by the scoring engine.
        //
        // NO application questions are used.
        //
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


            // --------------------------------------------------------
            // VOLUNTEERING
            // --------------------------------------------------------

            if (ContainsAny(
                text,
                "volunteer",
                "volunteering",
                "volunteer with"))
            {
                score += 20;
            }


            // --------------------------------------------------------
            // COMMUNITY
            // --------------------------------------------------------

            if (ContainsAny(
                text,
                "community",
                "community development",
                "community support",
                "community outreach"))
            {
                score += 20;
            }


            // --------------------------------------------------------
            // ELDERLY / RESIDENT SUPPORT
            // --------------------------------------------------------

            if (ContainsAny(
                text,
                "elderly",
                "resident",
                "residents",
                "senior",
                "elderly care",
                "companionship",
                "support residents"))
            {
                score += 20;
            }


            // --------------------------------------------------------
            // CONTRIBUTION / SKILLS
            // --------------------------------------------------------

            if (ContainsAny(
                text,
                "skills",
                "experience",
                "contribute",
                "contributing",
                "support",
                "communication",
                "compassion",
                "patience"))
            {
                score += 15;
            }


            // --------------------------------------------------------
            // LEARNING / DEVELOPMENT
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // RESPONSE QUALITY
            // --------------------------------------------------------

            if (text.Length >= 150)
            {
                score += 10;
            }


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
            {
                return string.Empty;
            }


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


            // --------------------------------------------------------
            // EXACT MATCH
            // --------------------------------------------------------

            if (candidateSkill.Equals(
                requiredSkill,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            // --------------------------------------------------------
            // CONTAINS MATCH
            // --------------------------------------------------------

            if (candidateSkill.Contains(
                    requiredSkill,
                    StringComparison.OrdinalIgnoreCase) ||
                requiredSkill.Contains(
                    candidateSkill,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            // --------------------------------------------------------
            // ALIASES
            // --------------------------------------------------------

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

                    ["active listening"] =
                        new[]
                        {
                            "listening",
                            "good listener"
                        },

                    ["elderly support"] =
                        new[]
                        {
                            "elderly resident support",
                            "elderly care",
                            "senior support",
                            "senior care"
                        },

                    ["resident support"] =
                        new[]
                        {
                            "elderly resident support",
                            "resident care",
                            "elderly support"
                        },

                    ["companionship"] =
                        new[]
                        {
                            "companion",
                            "resident companionship"
                        },

                    ["recreation"] =
                        new[]
                        {
                            "recreational activities",
                            "recreational support"
                        },

                    ["activity coordination"] =
                        new[]
                        {
                            "activities coordination",
                            "activity planning",
                            "activities planning"
                        },

                    ["organisation"] =
                        new[]
                        {
                            "organization",
                            "organisational skills",
                            "organizational skills",
                            "organising",
                            "organizing"
                        },

                    ["organisational skills"] =
                        new[]
                        {
                            "organisation",
                            "organization",
                            "organizational skills"
                        },

                    ["organizational skills"] =
                        new[]
                        {
                            "organisation",
                            "organization",
                            "organisational skills"
                        },

                    ["teamwork"] =
                        new[]
                        {
                            "team work",
                            "team player"
                        },

                    ["problem solving"] =
                        new[]
                        {
                            "problem-solving"
                        },

                    ["reliability"] =
                        new[]
                        {
                            "reliable",
                            "dependability",
                            "dependable"
                        },

                    ["patience"] =
                        new[]
                        {
                            "patient"
                        },

                    ["empathy"] =
                        new[]
                        {
                            "empathetic",
                            "compassion",
                            "compassionate"
                        },

                    ["interpersonal skills"] =
                        new[]
                        {
                            "interpersonal",
                            "people skills"
                        },

                    ["time management"] =
                        new[]
                        {
                            "timekeeping"
                        },

                    ["arts and crafts"] =
                        new[]
                        {
                            "arts crafts",
                            "crafts"
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
                        }
                };


            // --------------------------------------------------------
            // REQUIRED -> CANDIDATE
            // --------------------------------------------------------

            if (aliases.TryGetValue(
                requiredSkill,
                out var alternatives))
            {
                if (alternatives.Any(
                    x =>
                        candidateSkill.Equals(
                            NormalizeSkill(x),
                            StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }


            // --------------------------------------------------------
            // CANDIDATE -> REQUIRED
            // --------------------------------------------------------

            if (aliases.TryGetValue(
                candidateSkill,
                out var candidateAlternatives))
            {
                if (candidateAlternatives.Any(
                    x =>
                        requiredSkill.Equals(
                            NormalizeSkill(x),
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
            {
                return false;
            }


            foreach (var term in terms)
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    continue;
                }


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
    }
}