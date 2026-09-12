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
            var result = new VolunteerAIResult();

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
            // COVER MESSAGE
            // ====================================================

            string coverMessage =
                application.CoverMessage?.Trim() ?? string.Empty;

            bool hasCoverMessage =
                !string.IsNullOrWhiteSpace(coverMessage);

            // A meaningful cover message should contain more than
            // just a few characters.
            bool hasMeaningfulCoverMessage =
                coverMessage.Length >= 20;

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
            // COVER MESSAGE EVIDENCE
            // ====================================================

            int coverMessageScore = 0;

            if (hasCoverMessage)
            {
                // Basic evidence that a cover message was supplied.
                coverMessageScore += 40;

                // A meaningful message gets additional evidence.
                if (hasMeaningfulCoverMessage)
                {
                    coverMessageScore += 30;
                }

                // Longer messages can demonstrate more motivation,
                // provided they remain within reasonable limits.
                if (coverMessage.Length >= 50)
                {
                    coverMessageScore += 15;
                }

                if (coverMessage.Length >= 100)
                {
                    coverMessageScore += 15;
                }
            }

            coverMessageScore =
                Math.Min(
                    coverMessageScore,
                    100);

            // ====================================================
            // COMBINED ATS + CV + COVER MESSAGE ASSESSMENT
            // ====================================================

            /*
             * evaluation.OverallScore is the primary ATS score.
             *
             * Existing weighting:
             *
             * ATS              = 70%
             * CV Evidence      = 20%
             * Cover Message    = 10%
             *
             * Questions are intentionally NOT used.
             */

            decimal overallScore =
                evaluation.OverallScore;

            decimal combinedScore;

            if (cvAvailable || hasCoverMessage)
            {
                combinedScore =
                    (overallScore * 0.70m) +
                    (cvEvidenceScore * 0.20m) +
                    (coverMessageScore * 0.10m);
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
            // BUILD STRENGTHS
            // ====================================================

            var strengths =
                new List<string>();

            if (evaluation.OverallScore >= 80m)
            {
                strengths.Add(
                    "Strong overall ATS application score.");
            }
            else if (evaluation.OverallScore >= 60m)
            {
                strengths.Add(
                    "Candidate achieved a reasonable overall ATS application score.");
            }
            else
            {
                strengths.Add(
                    "Application was successfully evaluated.");
            }

            // ====================================================
            // COVER MESSAGE STRENGTH
            // ====================================================

            if (hasMeaningfulCoverMessage)
            {
                strengths.Add(
                    "Candidate provided a meaningful cover message explaining their interest in the opportunity.");
            }
            else if (hasCoverMessage)
            {
                strengths.Add(
                    "Candidate provided a cover message.");
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

                if (!string.IsNullOrWhiteSpace(
                    parsedCv?.FullName))
                {
                    strengths.Add(
                        "Applicant identity information was identified from the CV.");
                }

                if (!string.IsNullOrWhiteSpace(
                    parsedCv?.Email))
                {
                    strengths.Add(
                        "Applicant contact information was identified from the CV.");
                }
            }

            // ====================================================
            // BUILD CONCERNS
            // ====================================================

            var concerns =
                new List<string>();

            if (!hasCoverMessage)
            {
                concerns.Add(
                    "No cover message was provided.");
            }
            else if (!hasMeaningfulCoverMessage)
            {
                concerns.Add(
                    "The cover message is very brief and provides limited evidence of motivation.");
            }

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

                int confidence;

                if (cvAvailable)
                {
                    confidence =
                        80 +
                        (int)(
                            cvEvidenceScore *
                            0.12m);

                    if (hasMeaningfulCoverMessage)
                    {
                        confidence += 3;
                    }

                    confidence =
                        Math.Min(
                            confidence,
                            97);
                }
                else
                {
                    confidence =
                        hasMeaningfulCoverMessage
                            ? 93
                            : 90;
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
                        ? "The candidate achieved a strong combined ATS assessment supported by CV evidence and the submitted cover message."
                        : "The candidate achieved a strong overall ATS assessment supported by the submitted cover message.";

                result.RequirementMatches =
                    BuildRequirementMatchSummary(
                        parsedCv,
                        combinedScore,
                        hasCoverMessage);
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

                    if (hasMeaningfulCoverMessage)
                    {
                        confidence += 3;
                    }

                    confidence =
                        Math.Min(
                            confidence,
                            90);
                }
                else
                {
                    confidence =
                        hasMeaningfulCoverMessage
                            ? 75
                            : 72;
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
                        ? "The candidate demonstrates potential based on the ATS application assessment, CV evidence and available cover message, but administrator review is recommended."
                        : "The candidate has potential based on the available application information, but administrator review is recommended.";

                result.RequirementMatches =
                    BuildRequirementMatchSummary(
                        parsedCv,
                        combinedScore,
                        hasCoverMessage);
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
                    confidence =
                        hasMeaningfulCoverMessage
                            ? 80
                            : 78;
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
                    BuildRequirementMatchSummary(
                        parsedCv,
                        combinedScore,
                        hasCoverMessage);
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
            decimal combinedScore,
            bool hasCoverMessage)
        {
            var matches =
                new List<string>();

            // ====================================================
            // COVER MESSAGE
            // ====================================================

            if (hasCoverMessage)
            {
                matches.Add(
                    "cover message provided");
            }

            // ====================================================
            // CV
            // ====================================================

            if (parsedCv != null &&
                parsedCv.ExtractionSuccessful)
            {
                // =================================================
                // SKILLS
                // =================================================

                if (parsedCv.Skills.Any())
                {
                    matches.Add(
                        $"{parsedCv.Skills.Count} skill(s) identified");
                }

                // =================================================
                // EXPERIENCE
                // =================================================

                if (parsedCv.Experience.Any())
                {
                    matches.Add(
                        $"{parsedCv.Experience.Count} experience record(s) identified");
                }

                // =================================================
                // EDUCATION
                // =================================================

                if (parsedCv.Education.Any())
                {
                    matches.Add(
                        $"{parsedCv.Education.Count} education record(s) identified");
                }

                // =================================================
                // CERTIFICATIONS
                // =================================================

                if (parsedCv.Certifications.Any())
                {
                    matches.Add(
                        $"{parsedCv.Certifications.Count} certification(s) identified");
                }

                // =================================================
                // LANGUAGES
                // =================================================

                if (parsedCv.Languages.Any())
                {
                    matches.Add(
                        $"{parsedCv.Languages.Count} language(s) identified");
                }
            }

            // ====================================================
            // NO STRUCTURED EVIDENCE
            // ====================================================

            if (!matches.Any())
            {
                return
                    "Limited structured application evidence was identified.";
            }

            // ====================================================
            // FINAL SUMMARY
            // ====================================================

            return
                "Application evidence: " +
                string.Join(
                    ", ",
                    matches) +
                ".";
        }
    }
}