using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerAIEvaluationEngine
        : IVolunteerAIEvaluationEngine
    {
        public Task<VolunteerAIResult> EvaluateAsync(
            VolunteerApplication application,
            VolunteerApplicationEvaluation evaluation,
            ParsedCv? parsedCv)
        {
            var result =
                new VolunteerAIResult();

            // ====================================================
            // CV EVIDENCE
            // ====================================================

            bool cvAvailable =
                parsedCv != null &&
                parsedCv.ExtractionSuccessful;

            int cvSkillCount =
                parsedCv?.Skills?.Count ?? 0;

            int cvExperienceCount =
                parsedCv?.Experience?.Count ?? 0;

            int cvEducationCount =
                parsedCv?.Education?.Count ?? 0;

            int cvCertificationCount =
                parsedCv?.Certifications?.Count ?? 0;

            int cvLanguageCount =
                parsedCv?.Languages?.Count ?? 0;

            bool hasExperience =
                cvExperienceCount > 0;

            bool hasEducation =
                cvEducationCount > 0;

            bool hasCertifications =
                cvCertificationCount > 0;

            bool hasSkills =
                cvSkillCount > 0;

            // ====================================================
            // CV EVIDENCE SCORE
            // ====================================================

            int cvEvidenceScore = 0;

            if (cvAvailable)
            {
                // Skills
                if (hasSkills)
                {
                    cvEvidenceScore += 20;
                }

                // Education
                if (hasEducation)
                {
                    cvEvidenceScore += 15;
                }

                // Experience
                if (hasExperience)
                {
                    cvEvidenceScore += 25;
                }

                // Certifications
                if (hasCertifications)
                {
                    cvEvidenceScore += 20;
                }

                // Languages
                if (cvLanguageCount > 0)
                {
                    cvEvidenceScore += 10;
                }

                // Name
                if (!string.IsNullOrWhiteSpace(
                    parsedCv?.FullName))
                {
                    cvEvidenceScore += 5;
                }

                // Email
                if (!string.IsNullOrWhiteSpace(
                    parsedCv?.Email))
                {
                    cvEvidenceScore += 5;
                }
            }

            cvEvidenceScore =
                Math.Min(
                    cvEvidenceScore,
                    100);

            // ====================================================
            // COMBINED ATS + CV ASSESSMENT
            // ====================================================

            /*
             * IMPORTANT:
             *
             * evaluation.OverallScore is decimal.
             *
             * Therefore all calculations here use decimal.
             * The 'm' suffix ensures decimal arithmetic.
             */

            decimal overallScore =
                evaluation.OverallScore;

            decimal combinedScore;

            if (cvAvailable)
            {
                /*
                 * Existing ATS score remains the primary
                 * application score.
                 *
                 * ATS = 75%
                 * CV  = 25%
                 */

                combinedScore =
                    (overallScore * 0.75m) +
                    (cvEvidenceScore * 0.25m);
            }
            else
            {
                combinedScore =
                    overallScore;
            }

            // Keep the final score between 0 and 100.

            combinedScore =
                Math.Clamp(
                    combinedScore,
                    0m,
                    100m);

            // ====================================================
            // BUILD CV STRENGTHS
            // ====================================================

            var strengths =
                new List<string>();

            if (evaluation.OverallScore >= 80m)
            {
                strengths.Add(
                    "Strong overall application score.");
            }
            else if (evaluation.OverallScore >= 60m)
            {
                strengths.Add(
                    "Candidate achieved a reasonable overall application score.");
            }
            else
            {
                strengths.Add(
                    "Application was successfully evaluated.");
            }

            // ====================================================
            // CV STRENGTHS
            // ====================================================

            if (cvAvailable)
            {
                if (hasSkills)
                {
                    strengths.Add(
                        $"CV contains {cvSkillCount} identified skill(s).");
                }

                if (hasExperience)
                {
                    strengths.Add(
                        $"CV contains {cvExperienceCount} identified work experience record(s).");
                }

                if (hasEducation)
                {
                    strengths.Add(
                        $"CV contains {cvEducationCount} identified education record(s).");
                }

                if (hasCertifications)
                {
                    strengths.Add(
                        $"CV contains {cvCertificationCount} identified certification(s).");
                }

                if (cvLanguageCount > 0)
                {
                    strengths.Add(
                        $"CV identifies {cvLanguageCount} language(s).");
                }

                if (!string.IsNullOrWhiteSpace(parsedCv?.FullName))
                {
                    strengths.Add(
                        "Applicant identity information was identified from the CV.");
                }

                if (!string.IsNullOrWhiteSpace(parsedCv?.Email))
                {
                    strengths.Add(
                        "Applicant contact information was identified from the CV.");
                }
            }

            // ====================================================
            // BUILD CV CONCERNS
            // ====================================================

            var concerns =
                new List<string>();

            if (!cvAvailable)
            {
                concerns.Add(
                    "No successfully parsed CV evidence was available.");

                concerns.Add(
                    "Final suitability should be confirmed by an administrator.");
            }
            else
            {
                if (!hasSkills)
                {
                    concerns.Add(
                        "No clear skills were identified from the CV.");
                }

                if (!hasExperience)
                {
                    concerns.Add(
                        "No clear work experience was identified from the CV.");
                }

                if (!hasEducation)
                {
                    concerns.Add(
                        "No clear education information was identified from the CV.");
                }

                if (!hasCertifications)
                {
                    concerns.Add(
                        "No certification information was identified from the CV.");
                }

                concerns.Add(
                    "Final suitability should be confirmed by an administrator.");
            }

            // ====================================================
            // AI RECOMMENDATION
            // ====================================================

            if (combinedScore >= 80m)
            {
                // =================================================
                // STRONG SHORTLIST
                // =================================================

                result.Recommendation =
                    VolunteerAIRecommendation
                        .StrongShortlist;

                /*
                 * Confidence is calculated as an integer because
                 * VolunteerAIResult.Confidence is expected to be
                 * an integer/double-style confidence value.
                 */

                int confidence;

                if (cvAvailable)
                {
                    confidence =
                        80 +
                        (int)(
                            cvEvidenceScore *
                            0.17m);

                    confidence =
                        Math.Min(
                            confidence,
                            97);
                }
                else
                {
                    confidence = 90;
                }

                result.Confidence =
                    confidence;

                result.Strengths =
                    string.Join(
                        "\n",
                        strengths);

                result.Concerns =
                    string.Join(
                        "\n",
                        concerns);

                result.Explanation =
                    cvAvailable
                        ? "The candidate achieved a strong combined ATS assessment supported by information extracted from the applicant's CV."
                        : "The candidate achieved a strong overall score across the configured ATS evaluation criteria.";

                result.RequirementMatches =
                    cvAvailable
                        ? BuildRequirementMatchSummary(
                            parsedCv,
                            combinedScore)
                        : "Core application requirements matched.";
            }
            else if (combinedScore >= 60m)
            {
                // =================================================
                // REVIEW RECOMMENDED
                // =================================================

                result.Recommendation =
                    VolunteerAIRecommendation
                        .ReviewRecommended;

                int confidence;

                if (cvAvailable)
                {
                    confidence =
                        65 +
                        (int)(
                            cvEvidenceScore *
                            0.10m);

                    confidence =
                        Math.Min(
                            confidence,
                            90);
                }
                else
                {
                    confidence = 72;
                }

                result.Confidence =
                    confidence;

                result.Strengths =
                    string.Join(
                        "\n",
                        strengths);

                result.Concerns =
                    string.Join(
                        "\n",
                        concerns);

                result.Explanation =
                    cvAvailable
                        ? "The candidate demonstrates potential based on the ATS application assessment and available CV evidence, but administrator review is recommended."
                        : "The candidate has potential but the available application evidence does not support an immediate strong-shortlist recommendation.";

                result.RequirementMatches =
                    cvAvailable
                        ? BuildRequirementMatchSummary(
                            parsedCv,
                            combinedScore)
                        : "Partial requirement match.";
            }
            else
            {
                // =================================================
                // NOT RECOMMENDED
                // =================================================

                result.Recommendation =
                    VolunteerAIRecommendation
                        .NotRecommended;

                int confidence;

                if (cvAvailable)
                {
                    confidence =
                        70 +
                        (int)(
                            cvEvidenceScore *
                            0.08m);

                    confidence =
                        Math.Min(
                            confidence,
                            90);
                }
                else
                {
                    confidence = 78;
                }

                result.Confidence =
                    confidence;

                result.Strengths =
                    string.Join(
                        "\n",
                        strengths);

                result.Concerns =
                    string.Join(
                        "\n",
                        concerns);

                result.Explanation =
                    cvAvailable
                        ? "The combined ATS assessment and available CV evidence do not currently demonstrate a sufficiently strong match."
                        : "The available application information does not currently demonstrate a sufficiently strong match.";

                result.RequirementMatches =
                    cvAvailable
                        ? BuildRequirementMatchSummary(
                            parsedCv,
                            combinedScore)
                        : "Limited requirement match.";
            }

            // ====================================================
            // RETURN RESULT
            // ====================================================

            return Task.FromResult(result);
        }

        // ========================================================
        // REQUIREMENT MATCH SUMMARY
        // ========================================================

        private string BuildRequirementMatchSummary(
            ParsedCv? parsedCv,
            decimal combinedScore)
        {
            if (parsedCv == null ||
                !parsedCv.ExtractionSuccessful)
            {
                return "CV requirement evidence unavailable.";
            }

            var matches =
                new List<string>();

            // ====================================================
            // SKILLS
            // ====================================================

            if (parsedCv.Skills.Any())
            {
                matches.Add(
                    $"{parsedCv.Skills.Count} skill(s) identified");
            }

            // ====================================================
            // EXPERIENCE
            // ====================================================

            if (parsedCv.Experience.Any())
            {
                matches.Add(
                    $"{parsedCv.Experience.Count} experience record(s) identified");
            }

            // ====================================================
            // EDUCATION
            // ====================================================

            if (parsedCv.Education.Any())
            {
                matches.Add(
                    $"{parsedCv.Education.Count} education record(s) identified");
            }

            // ====================================================
            // CERTIFICATIONS
            // ====================================================

            if (parsedCv.Certifications.Any())
            {
                matches.Add(
                    $"{parsedCv.Certifications.Count} certification(s) identified");
            }

            // ====================================================
            // LANGUAGES
            // ====================================================

            if (parsedCv.Languages.Any())
            {
                matches.Add(
                    $"{parsedCv.Languages.Count} language(s) identified");
            }

            // ====================================================
            // NO STRUCTURED EVIDENCE
            // ====================================================

            if (!matches.Any())
            {
                return
                    "CV was processed, but limited structured requirement evidence was identified.";
            }

            // ====================================================
            // FINAL SUMMARY
            // ====================================================

            return
                "CV evidence: " +
                string.Join(
                    ", ",
                    matches) +
                ".";
        }
    }
}