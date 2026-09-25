using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.AI;
using SwiftCare.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerInterviewEvaluationEngine
        : IVolunteerInterviewEvaluationEngine
    {
        private readonly SwiftCareDbContext _context;
        private readonly ISwiftCareLlmService _llmService;
        private readonly ILogger<VolunteerInterviewEvaluationEngine> _logger;

        private const string EvaluationVersion = "2.0";
        private const string PromptVersion = "Foundry-Interview-Evaluation-v2";
        private const string DatasetVersion = "SwiftCare-Interview-Rubric-v2";
        private const string EvaluationProvider = "Microsoft Foundry";
        private const string EvaluationModel = "Phi-4-mini-instruct";

        // ============================================================
        // MAIN CONSTRUCTOR
        // ============================================================

        public VolunteerInterviewEvaluationEngine(
            SwiftCareDbContext context,
            ISwiftCareLlmService llmService,
            ILogger<VolunteerInterviewEvaluationEngine> logger)
        {
            _context = context;
            _llmService = llmService;
            _logger = logger;
        }

        // ============================================================
        // STRUCTURED OUTPUT SCHEMA
        // ============================================================

        private const string InterviewEvaluationSchema = """
        {
          "type": "object",
          "properties": {
            "overallScore": {
              "type": "number",
              "minimum": 0,
              "maximum": 100
            },
            "overallConfidence": {
              "type": "number",
              "minimum": 0,
              "maximum": 100
            },
            "recommendation": {
              "type": "string"
            },
            "explanation": {
              "type": "string"
            },
            "skills": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "skill": {
                    "type": "string"
                  },
                  "foundInProfile": {
                    "type": "boolean"
                  },
                  "profileEvidence": {
                    "type": "string"
                  },
                  "foundInInterview": {
                    "type": "boolean"
                  },
                  "interviewEvidence": {
                    "type": "string"
                  },
                  "score": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 100
                  },
                  "confidence": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 100
                  },
                  "assessment": {
                    "type": "string"
                  },
                  "reasoning": {
                    "type": "string"
                  }
                },
                "required": [
                  "skill",
                  "foundInProfile",
                  "profileEvidence",
                  "foundInInterview",
                  "interviewEvidence",
                  "score",
                  "confidence",
                  "assessment",
                  "reasoning"
                ],
                "additionalProperties": false
              }
            },
            "competencies": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "name": {
                    "type": "string"
                  },
                  "description": {
                    "type": "string"
                  },
                  "score": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 100
                  },
                  "confidence": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 100
                  },
                  "assessment": {
                    "type": "string"
                  },
                  "evidence": {
                    "type": "string"
                  },
                  "reasoning": {
                    "type": "string"
                  },
                  "demonstrated": {
                    "type": "boolean"
                  }
                },
                "required": [
                  "name",
                  "description",
                  "score",
                  "confidence",
                  "assessment",
                  "evidence",
                  "reasoning",
                  "demonstrated"
                ],
                "additionalProperties": false
              }
            }
          },
          "required": [
            "overallScore",
            "overallConfidence",
            "recommendation",
            "explanation",
            "skills",
            "competencies"
          ],
          "additionalProperties": false
        }
        """;

        // ============================================================
        // MAIN EVALUATION
        // ============================================================

        public async Task<VolunteerInterviewEvaluationResult> EvaluateAsync(
            int volunteerApplicationId,
            CancellationToken cancellationToken = default)
        {
            var result = new VolunteerInterviewEvaluationResult
            {
                VolunteerApplicationId = volunteerApplicationId,
                Success = false,
                EvaluationStatus = "Not Evaluated"
            };

            try
            {
                _logger.LogInformation(
                    "[SWIFTCARE INTERVIEW AI] Starting AI evaluation for application {ApplicationId}",
                    volunteerApplicationId);

                // ====================================================
                // LOAD APPLICATION
                // ====================================================

                var application =
                    await _context.VolunteerApplications
                        .Include(x => x.VolunteerOpportunity)
                            .ThenInclude(x => x.RequiredSkills)
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerApplicationId ==
                                volunteerApplicationId,
                            cancellationToken);

                if (application == null)
                {
                    result.ErrorMessage =
                        "Volunteer application could not be found.";

                    return result;
                }

                // ====================================================
                // LOAD APPLICANT
                // ====================================================

                object? applicant = null;

                var applicantNavigation =
                    _context.Entry(application)
                        .Navigations
                        .FirstOrDefault(
                            x =>
                                string.Equals(
                                    x.Metadata.Name,
                                    "Applicant",
                                    StringComparison.OrdinalIgnoreCase));

                if (applicantNavigation != null)
                {
                    await applicantNavigation.LoadAsync(
                        cancellationToken);

                    applicant =
                        applicantNavigation.CurrentValue;
                }

                // ====================================================
                // LOAD INTERVIEW SESSION
                // ====================================================

                var interviewSession =
                    await _context.VolunteerVoiceInterviewSessions
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerApplicationId ==
                                volunteerApplicationId,
                            cancellationToken);

                if (interviewSession == null)
                {
                    result.ErrorMessage =
                        "The candidate does not have a voice interview session.";

                    return result;
                }

                if (!interviewSession.IsCompleted)
                {
                    result.ErrorMessage =
                        "The voice interview has not been completed.";

                    return result;
                }

                // ====================================================
                // LOAD INTERVIEW ANSWERS
                // ====================================================

                var answers =
                    await _context.VolunteerApplicationAnswers
                        .Include(x => x.VolunteerQuestion)
                        .Where(
                            x =>
                                x.VolunteerApplicationId ==
                                volunteerApplicationId)
                        .OrderBy(
                            x =>
                                x.VolunteerQuestion != null
                                    ? x.VolunteerQuestion.DisplayOrder
                                    : int.MaxValue)
                        .ToListAsync(
                            cancellationToken);

                var answeredQuestions =
                    answers
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(
                                    x.Answer))
                        .ToList();

                if (!answeredQuestions.Any())
                {
                    result.ErrorMessage =
                        "The completed interview contains no answers.";

                    return result;
                }

                _logger.LogInformation(
                    "[SWIFTCARE INTERVIEW AI] Interview answers loaded: {Count}",
                    answeredQuestions.Count);

                // ====================================================
                // REQUIRED SKILLS
                // ====================================================

                var requiredSkills =
                    application.VolunteerOpportunity?
                        .RequiredSkills?
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(
                                    x.Skill))
                        .ToList()
                    ?? new List<VolunteerRequiredSkill>();

                _logger.LogInformation(
                    "[SWIFTCARE INTERVIEW AI] Required skills: {Count}",
                    requiredSkills.Count);

                // ====================================================
                // BUILD PROFILE / CV EVIDENCE
                // ====================================================

                var profileEvidence =
                    BuildApplicantProfileEvidence(
                        applicant,
                        application);

                // ====================================================
                // BUILD INTERVIEW TRANSCRIPT
                // ====================================================

                var transcript =
                    BuildTranscript(
                        answeredQuestions);

                // ====================================================
                // BUILD ROLE INFORMATION
                // ====================================================

                var roleTitle =
                    application.VolunteerOpportunity?.Title
                    ?? "Volunteer Position";

                var roleDescription =
                    application.VolunteerOpportunity?.Description
                    ?? string.Empty;

                var requiredSkillText =
                    requiredSkills.Any()
                        ? string.Join(
                            Environment.NewLine,
                            requiredSkills.Select(
                                x =>
                                    $"- {x.Skill.Trim()}"))
                        : "- No specific required skills were configured.";

                // ====================================================
                // FIND / CREATE EVALUATION
                // ====================================================

                var evaluation =
                    await _context.VolunteerInterviewEvaluations
                        .Include(x => x.SkillEvaluations)
                        .Include(x => x.CompetencyEvaluations)
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerApplicationId ==
                                volunteerApplicationId,
                            cancellationToken);

                if (evaluation == null)
                {
                    evaluation =
                        new VolunteerInterviewEvaluation
                        {
                            VolunteerApplicationId =
                                volunteerApplicationId,

                            VolunteerVoiceInterviewSessionId =
                                interviewSession
                                    .VolunteerVoiceInterviewSessionId,

                            CreatedDate =
                                DateTime.Now
                        };

                    _context.VolunteerInterviewEvaluations.Add(
                        evaluation);
                }
                else
                {
                    evaluation.VolunteerVoiceInterviewSessionId =
                        interviewSession.VolunteerVoiceInterviewSessionId;

                    evaluation.LastUpdatedDate =
                        DateTime.Now;

                    if (evaluation.SkillEvaluations.Any())
                    {
                        _context.VolunteerInterviewSkillEvaluations
                            .RemoveRange(
                                evaluation.SkillEvaluations);
                    }

                    if (evaluation.CompetencyEvaluations.Any())
                    {
                        _context.VolunteerInterviewCompetencyEvaluations
                            .RemoveRange(
                                evaluation.CompetencyEvaluations);
                    }

                    evaluation.SkillEvaluations.Clear();
                    evaluation.CompetencyEvaluations.Clear();
                }

                // ====================================================
                // EVALUATION METADATA
                // ====================================================

                evaluation.EvaluationVersion =
                    EvaluationVersion;

                evaluation.EvaluationProvider =
                    EvaluationProvider;

                evaluation.EvaluationModel =
                    EvaluationModel;

                evaluation.EvaluationDatasetVersion =
                    DatasetVersion;

                evaluation.EvaluationPromptVersion =
                    PromptVersion;

                evaluation.EvaluationStatus =
                    "Evaluating";

                evaluation.ErrorMessage =
                    string.Empty;

                // ====================================================
                // BUILD SYSTEM PROMPT
                // ====================================================

                var systemPrompt = """
                    You are the SwiftCare Volunteer Interview Evaluation AI.

                    Your task is to evaluate a volunteer candidate using ONLY
                    the evidence supplied in the evaluation package.

                    You must be objective, evidence-based and conservative.

                    Do NOT invent:
                    - qualifications
                    - skills
                    - employment history
                    - interview statements
                    - experience
                    - certifications
                    - achievements
                    - candidate characteristics

                    If evidence is missing, explicitly say that evidence is missing.

                    Do NOT evaluate or make recommendations based on:
                    - age
                    - date of birth
                    - gender
                    - race
                    - ethnicity
                    - religion
                    - nationality
                    - disability
                    - marital status
                    - sexual orientation
                    - political views
                    - any other protected or irrelevant characteristic.

                    Evaluate only job-relevant evidence.

                    The evaluation has two major evidence sources:

                    1. Candidate profile/application/CV-derived information.
                    2. Completed voice interview transcript.

                    Required role skills should be evaluated individually.

                    The interview should also be evaluated against these
                    behavioural competencies:

                    - Communication
                    - Empathy
                    - Active Listening
                    - Problem Solving
                    - Teamwork
                    - Reliability

                    IMPORTANT SCORING RULE:

                    EVERY SCORE IS A WHOLE-NUMBER PERCENTAGE FROM 0 TO 100.

                    Examples:

                    0 = 0%
                    10 = 10%
                    25 = 25%
                    35 = 35%
                    50 = 50%
                    75 = 75%
                    100 = 100%

                    NEVER return scores using a 0-10 scale.

                    Therefore:

                    3.5 is NOT an acceptable final score.
                    If the underlying assessment would be 35%, return 35.

                    Do not divide percentages by 10.

                    Do not convert 35 into 3.5.

                    Skill scores must be 0-100.
                    Competency scores must be 0-100.
                    Confidence must be 0-100.

                    A keyword appearing in an answer does NOT automatically
                    demonstrate a competency.

                    Evaluate the meaning and context of the candidate's answer.

                    A candidate should receive stronger scores when the evidence
                    demonstrates the actual behaviour or skill.

                    Confidence must represent confidence in the evidence,
                    not confidence that the candidate is a good person.

                    Use these recommendation values only:

                    Strongly Recommended
                    Recommended
                    Review
                    Not Recommended

                    Recommendations must be based only on the supplied
                    job-relevant evidence.

                    IMPORTANT:

                    The application will independently calculate the final
                    overall score from the individual skill and competency
                    scores.

                    Therefore, provide sensible 0-100 scores for every
                    individual skill and competency.

                    Return the requested result as JSON matching the supplied
                    JSON schema exactly.
                    """;

                // ====================================================
                // BUILD USER PROMPT
                // ====================================================

                var userPrompt =
                    BuildEvaluationPrompt(
                        roleTitle,
                        roleDescription,
                        requiredSkillText,
                        profileEvidence,
                        transcript);

                _logger.LogInformation(
                    "[SWIFTCARE INTERVIEW AI] Sending structured evaluation to {Model}",
                    EvaluationModel);

                // ====================================================
                // CALL PHI-4-MINI-INSTRUCT
                // ====================================================

                var rawJson =
                    await _llmService.GenerateStructuredAsync(
                        systemPrompt,
                        userPrompt,
                        InterviewEvaluationSchema,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    throw new InvalidOperationException(
                        "The AI returned an empty evaluation response.");
                }

                _logger.LogInformation(
                    "[SWIFTCARE INTERVIEW AI] Structured AI response received.");

                // ====================================================
                // DESERIALIZE AI RESULT
                // ====================================================

                VolunteerInterviewAiEvaluationResponse? aiResult;

                try
                {
                    aiResult =
                        JsonSerializer.Deserialize<
                            VolunteerInterviewAiEvaluationResponse>(
                            rawJson,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "[SWIFTCARE INTERVIEW AI] Failed to deserialize AI response.");

                    throw new InvalidOperationException(
                        "The AI returned invalid structured evaluation JSON.",
                        ex);
                }

                if (aiResult == null)
                {
                    throw new InvalidOperationException(
                        "The AI evaluation could not be deserialized.");
                }

                // ====================================================
                // NORMALIZE AI RESULT
                // ====================================================

                NormalizeAiResult(
                    aiResult,
                    requiredSkills);

                // ====================================================
                // CREATE SKILL EVALUATIONS
                // ====================================================

                var skillsFoundInProfile = 0;
                var skillsFoundInInterview = 0;

                foreach (var requiredSkill in requiredSkills)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var requiredSkillName =
                        requiredSkill.Skill.Trim();

                    var aiSkill =
                        aiResult.Skills
                            .FirstOrDefault(
                                x =>
                                    string.Equals(
                                        x.Skill?.Trim(),
                                        requiredSkillName,
                                        StringComparison.OrdinalIgnoreCase));

                    if (aiSkill == null)
                    {
                        aiSkill =
                            new VolunteerInterviewAiSkillResult
                            {
                                Skill =
                                    requiredSkillName,

                                FoundInProfile =
                                    false,

                                ProfileEvidence =
                                    "No profile evidence was returned by the AI.",

                                FoundInInterview =
                                    false,

                                InterviewEvidence =
                                    "No interview evidence was returned by the AI.",

                                Score =
                                    0m,

                                Confidence =
                                    0m,

                                Assessment =
                                    "Not Demonstrated",

                                Reasoning =
                                    "The AI did not identify sufficient evidence."
                            };
                    }

                    if (aiSkill.FoundInProfile)
                    {
                        skillsFoundInProfile++;
                    }

                    if (aiSkill.FoundInInterview)
                    {
                        skillsFoundInInterview++;
                    }

                    var combinedEvidence =
                        BuildCombinedEvidence(
                            aiSkill.ProfileEvidence,
                            aiSkill.InterviewEvidence);

                    var skillEvaluation =
                        new VolunteerInterviewSkillEvaluation
                        {
                            VolunteerInterviewEvaluation =
                                evaluation,

                            VolunteerRequiredSkillId =
                                requiredSkill.VolunteerRequiredSkillId,

                            Skill =
                                requiredSkillName,

                            FoundInCv =
                                aiSkill.FoundInProfile,

                            CvEvidence =
                                LimitText(
                                    aiSkill.ProfileEvidence,
                                    4000),

                            FoundInInterview =
                                aiSkill.FoundInInterview,

                            InterviewEvidence =
                                LimitText(
                                    aiSkill.InterviewEvidence,
                                    4000),

                            InterviewScore =
                                NormalizeScore(
                                    aiSkill.Score),

                            Confidence =
                                NormalizeScore(
                                    aiSkill.Confidence),

                            Assessment =
                                LimitText(
                                    aiSkill.Assessment,
                                    50),

                            Evidence =
                                LimitText(
                                    combinedEvidence,
                                    8000),

                            Reasoning =
                                LimitText(
                                    aiSkill.Reasoning,
                                    8000)
                        };

                    evaluation.SkillEvaluations.Add(
                        skillEvaluation);
                }

                // ====================================================
                // CREATE COMPETENCY EVALUATIONS
                // ====================================================

                var expectedCompetencies =
                    new[]
                    {
                        new
                        {
                            Name = "Communication",
                            Description =
                                "Ability to communicate clearly, appropriately and effectively."
                        },

                        new
                        {
                            Name = "Empathy",
                            Description =
                                "Ability to understand and respond appropriately to another person's feelings and needs."
                        },

                        new
                        {
                            Name = "Active Listening",
                            Description =
                                "Ability to listen carefully, understand what is being said and respond appropriately."
                        },

                        new
                        {
                            Name = "Problem Solving",
                            Description =
                                "Ability to identify problems, think through situations and provide practical responses."
                        },

                        new
                        {
                            Name = "Teamwork",
                            Description =
                                "Ability to cooperate with others, contribute positively and work toward shared goals."
                        },

                        new
                        {
                            Name = "Reliability",
                            Description =
                                "Evidence of responsibility, dependability, consistency and commitment."
                        }
                    };

                foreach (var competency in expectedCompetencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var aiCompetency =
                        aiResult.Competencies
                            .FirstOrDefault(
                                x =>
                                    string.Equals(
                                        x.Name?.Trim(),
                                        competency.Name,
                                        StringComparison.OrdinalIgnoreCase));

                    if (aiCompetency == null)
                    {
                        aiCompetency =
                            new VolunteerInterviewAiCompetencyResult
                            {
                                Name =
                                    competency.Name,

                                Description =
                                    competency.Description,

                                Score =
                                    0m,

                                Confidence =
                                    0m,

                                Assessment =
                                    "Not Demonstrated",

                                Evidence =
                                    "No sufficient evidence was returned by the AI.",

                                Reasoning =
                                    "The AI did not identify sufficient evidence.",

                                Demonstrated =
                                    false
                            };
                    }

                    var competencyEvaluation =
                        new VolunteerInterviewCompetencyEvaluation
                        {
                            VolunteerInterviewEvaluation =
                                evaluation,

                            Name =
                                competency.Name,

                            Description =
                                LimitText(
                                    string.IsNullOrWhiteSpace(
                                        aiCompetency.Description)
                                        ? competency.Description
                                        : aiCompetency.Description,
                                    500),

                            Score =
                                NormalizeScore(
                                    aiCompetency.Score),

                            Confidence =
                                NormalizeScore(
                                    aiCompetency.Confidence),

                            Assessment =
                                LimitText(
                                    aiCompetency.Assessment,
                                    50),

                            Evidence =
                                LimitText(
                                    aiCompetency.Evidence,
                                    8000),

                            Reasoning =
                                LimitText(
                                    aiCompetency.Reasoning,
                                    8000),

                            Demonstrated =
                                aiCompetency.Demonstrated
                        };

                    evaluation.CompetencyEvaluations.Add(
                        competencyEvaluation);
                }

                // ====================================================
                // FINAL SCORE CALCULATION
                // ====================================================

                /*
                 * IMPORTANT:
                 *
                 * The AI's overallScore is NOT used.
                 *
                 * We calculate the final score directly from the
                 * individual scores stored above.
                 *
                 * If skills exist:
                 *
                 *     Skill Average =
                 *     Sum of skill scores / number of skills
                 *
                 * If competencies exist:
                 *
                 *     Competency Average =
                 *     Sum of competency scores / number of competencies
                 *
                 * If both exist:
                 *
                 *     Overall =
                 *     (Skill Average + Competency Average) / 2
                 *
                 * If only one exists, that category becomes the
                 * overall score.
                 *
                 * Everything is 0-100.
                 */

                var skillScores =
                    evaluation.SkillEvaluations
                        .Select(x => NormalizeScore(x.InterviewScore))
                        .ToList();

                var competencyScores =
                    evaluation.CompetencyEvaluations
                        .Select(x => NormalizeScore(x.Score))
                        .ToList();

                decimal skillAverage = 0m;
                decimal competencyAverage = 0m;

                if (skillScores.Any())
                {
                    skillAverage =
                        Math.Round(
                            skillScores.Average(),
                            0,
                            MidpointRounding.AwayFromZero);
                }

                if (competencyScores.Any())
                {
                    competencyAverage =
                        Math.Round(
                            competencyScores.Average(),
                            0,
                            MidpointRounding.AwayFromZero);
                }

                decimal overallScore;

                if (skillScores.Any() && competencyScores.Any())
                {
                    overallScore =
                        Math.Round(
                            (skillAverage + competencyAverage) / 2m,
                            0,
                            MidpointRounding.AwayFromZero);
                }
                else if (skillScores.Any())
                {
                    overallScore =
                        skillAverage;
                }
                else if (competencyScores.Any())
                {
                    overallScore =
                        competencyAverage;
                }
                else
                {
                    overallScore = 0m;
                }

                overallScore =
                    NormalizeScore(
                        overallScore);

                // ====================================================
                // FINAL CONFIDENCE
                // ====================================================

                var confidenceValues =
                    evaluation.SkillEvaluations
                        .Select(x => NormalizeScore(x.Confidence))
                        .Concat(
                            evaluation.CompetencyEvaluations
                                .Select(x => NormalizeScore(x.Confidence)))
                        .ToList();

                decimal overallConfidence;

                if (confidenceValues.Any())
                {
                    overallConfidence =
                        Math.Round(
                            confidenceValues.Average(),
                            0,
                            MidpointRounding.AwayFromZero);
                }
                else
                {
                    overallConfidence =
                        NormalizeScore(
                            aiResult.OverallConfidence);
                }

                overallConfidence =
                    NormalizeScore(
                        overallConfidence);

                // ====================================================
                // FINAL RECOMMENDATION
                // ====================================================

                var recommendation =
                    NormalizeRecommendation(
                        aiResult.Recommendation);

                // ====================================================
                // FINAL EXPLANATION
                // ====================================================

                var explanation =
                    string.IsNullOrWhiteSpace(
                        aiResult.Explanation)
                        ? BuildFallbackExplanation(
                            overallScore,
                            overallConfidence,
                            skillAverage,
                            competencyAverage,
                            requiredSkills.Count,
                            skillsFoundInProfile,
                            skillsFoundInInterview)
                        : aiResult.Explanation.Trim();

                // ====================================================
                // SAVE EVALUATION
                // ====================================================

                evaluation.OverallInterviewScore =
                    overallScore;

                evaluation.OverallInterviewConfidence =
                    overallConfidence;

                evaluation.Recommendation =
                    recommendation;

                evaluation.EvaluationStatus =
                    "Evaluated";

                evaluation.Explanation =
                    LimitText(
                        explanation,
                        4000);

                evaluation.EvaluatedDate =
                    DateTime.Now;

                evaluation.LastUpdatedDate =
                    DateTime.Now;

                // ====================================================
                // STORE RAW AI PAYLOAD
                // ====================================================

                evaluation.EvaluationPayloadJson =
                    rawJson;

                // ====================================================
                // SAVE DATABASE
                // ====================================================

                await _context.SaveChangesAsync(
                    cancellationToken);

                // ====================================================
                // RESULT
                // ====================================================

                result.Success = true;

                result.VolunteerInterviewEvaluationId =
                    evaluation.VolunteerInterviewEvaluationId;

                result.OverallInterviewScore =
                    evaluation.OverallInterviewScore;

                result.OverallInterviewConfidence =
                    evaluation.OverallInterviewConfidence;

                result.Recommendation =
                    evaluation.Recommendation;

                result.EvaluationStatus =
                    evaluation.EvaluationStatus;

                result.Explanation =
                    evaluation.Explanation;

                result.RequiredSkillCount =
                    requiredSkills.Count;

                result.RequiredSkillsFoundInCv =
                    skillsFoundInProfile;

                result.RequiredSkillsFoundInInterview =
                    skillsFoundInInterview;

                result.CompetencyCount =
                    evaluation
                        .CompetencyEvaluations
                        .Count;

                _logger.LogInformation(
                    "[SWIFTCARE INTERVIEW AI] Evaluation completed. " +
                    "Application={ApplicationId}, " +
                    "SkillAverage={SkillAverage}, " +
                    "CompetencyAverage={CompetencyAverage}, " +
                    "OverallScore={Score}, " +
                    "Confidence={Confidence}, " +
                    "Recommendation={Recommendation}",
                    volunteerApplicationId,
                    skillAverage,
                    competencyAverage,
                    overallScore,
                    overallConfidence,
                    recommendation);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[SWIFTCARE INTERVIEW AI] Evaluation cancelled for application {ApplicationId}",
                    volunteerApplicationId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE INTERVIEW AI] Evaluation failed for application {ApplicationId}",
                    volunteerApplicationId);

                result.Success = false;

                result.EvaluationStatus =
                    "Failed";

                result.ErrorMessage =
                    ex.Message;

                return result;
            }
        }

        // ============================================================
        // BUILD APPLICANT PROFILE EVIDENCE
        // ============================================================

        private string BuildApplicantProfileEvidence(
            object? applicant,
            VolunteerApplication application)
        {
            var builder =
                new StringBuilder();

            builder.AppendLine(
                "CANDIDATE PROFILE / APPLICATION EVIDENCE");

            builder.AppendLine(
                "========================================");

            // --------------------------------------------------------
            // APPLICATION COVER MESSAGE
            // --------------------------------------------------------

            var coverMessage =
                GetStringProperty(
                    application,
                    "CoverMessage");

            if (!string.IsNullOrWhiteSpace(coverMessage))
            {
                builder.AppendLine(
                    "Application Cover Message:");

                builder.AppendLine(
                    coverMessage.Trim());

                builder.AppendLine();
            }

            // --------------------------------------------------------
            // APPLICANT PROFILE
            // --------------------------------------------------------

            if (applicant == null)
            {
                builder.AppendLine(
                    "Applicant profile information is not available.");

                return builder
                    .ToString()
                    .Trim();
            }

            AddProfileField(
                builder,
                applicant,
                "ProfessionalTitle",
                "Professional Title");

            AddProfileField(
                builder,
                applicant,
                "ProfessionalSummary",
                "Professional Summary");

            AddProfileField(
                builder,
                applicant,
                "CareerObjective",
                "Career Objective");

            AddProfileField(
                builder,
                applicant,
                "City",
                "City");

            AddProfileField(
                builder,
                applicant,
                "Province",
                "Province");

            AddProfileField(
                builder,
                applicant,
                "Country",
                "Country");

            AddProfileField(
                builder,
                applicant,
                "PreferredWorkType",
                "Preferred Work Type");

            AddProfileField(
                builder,
                applicant,
                "PreferredLocation",
                "Preferred Location");

            AddProfileField(
                builder,
                applicant,
                "PreferredWorkLocation",
                "Preferred Work Location");

            AddProfileField(
                builder,
                applicant,
                "Availability",
                "Availability");

            AddProfileField(
                builder,
                applicant,
                "AvailabilityDescription",
                "Availability Description");

            AddProfileField(
                builder,
                applicant,
                "WillingToRelocate",
                "Willing To Relocate");

            AddProfileField(
                builder,
                applicant,
                "LinkedInUrl",
                "LinkedIn");

            AddProfileField(
                builder,
                applicant,
                "PortfolioUrl",
                "Portfolio");

            AddProfileField(
                builder,
                applicant,
                "GitHubUrl",
                "GitHub");

            AddProfileField(
                builder,
                applicant,
                "CurrentCvFileName",
                "CV File");

            AddProfileField(
                builder,
                applicant,
                "CurrentCvFileType",
                "CV File Type");

            AddProfileField(
                builder,
                applicant,
                "CvParsingSuccessful",
                "CV Parsing Successful");

            AddProfileField(
                builder,
                applicant,
                "CvParsingMessage",
                "CV Parsing Message");

            AddProfileField(
                builder,
                applicant,
                "CvLastParsedDate",
                "CV Last Parsed Date");

            return builder
                .ToString()
                .Trim();
        }

        // ============================================================
        // PROFILE FIELD HELPER
        // ============================================================

        private void AddProfileField(
            StringBuilder builder,
            object source,
            string propertyName,
            string displayName)
        {
            var value =
                GetStringProperty(
                    source,
                    propertyName);

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            builder.AppendLine(
                $"{displayName}: {value.Trim()}");
        }

        // ============================================================
        // SAFE PROPERTY READER
        // ============================================================

        private string GetStringProperty(
            object source,
            string propertyName)
        {
            try
            {
                var property =
                    source
                        .GetType()
                        .GetProperty(
                            propertyName,
                            BindingFlags.Public |
                            BindingFlags.Instance |
                            BindingFlags.IgnoreCase);

                if (property == null)
                {
                    return string.Empty;
                }

                var value =
                    property.GetValue(source);

                if (value == null)
                {
                    return string.Empty;
                }

                if (value is bool boolValue)
                {
                    return boolValue
                        ? "Yes"
                        : "No";
                }

                if (value is DateTime dateTime)
                {
                    return dateTime.ToString(
                        "yyyy-MM-dd");
                }

                return value
                    .ToString()
                    ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // ============================================================
        // TRANSCRIPT
        // ============================================================

        private List<InterviewTranscriptEntry> BuildTranscript(
            List<VolunteerApplicationAnswer> answers)
        {
            return answers
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(
                            x.Answer))
                .Select(
                    (x, index) =>
                        new InterviewTranscriptEntry
                        {
                            QuestionNumber =
                                index + 1,

                            Question =
                                x.VolunteerQuestion?
                                    .Question
                                ?? "Interview Question",

                            Answer =
                                x.Answer?
                                    .Trim()
                                ?? string.Empty
                        })
                .ToList();
        }

        // ============================================================
        // BUILD AI PROMPT
        // ============================================================

        private string BuildEvaluationPrompt(
            string roleTitle,
            string roleDescription,
            string requiredSkills,
            string profileEvidence,
            List<InterviewTranscriptEntry> transcript)
        {
            var builder =
                new StringBuilder();

            builder.AppendLine(
                "SWIFTCARE VOLUNTEER INTERVIEW EVALUATION");

            builder.AppendLine(
                "========================================");

            builder.AppendLine();

            builder.AppendLine(
                "ROLE:");

            builder.AppendLine(
                $"Title: {roleTitle}");

            builder.AppendLine(
                $"Description: {roleDescription}");

            builder.AppendLine();

            builder.AppendLine(
                "REQUIRED ROLE SKILLS:");

            builder.AppendLine(
                requiredSkills);

            builder.AppendLine();

            builder.AppendLine(
                profileEvidence);

            builder.AppendLine();

            builder.AppendLine(
                "COMPLETED VOICE INTERVIEW TRANSCRIPT");

            builder.AppendLine(
                "========================================");

            if (!transcript.Any())
            {
                builder.AppendLine(
                    "No interview answers were available.");
            }
            else
            {
                foreach (var entry in transcript)
                {
                    builder.AppendLine(
                        $"Question {entry.QuestionNumber}:");

                    builder.AppendLine(
                        entry.Question);

                    builder.AppendLine(
                        "Candidate Answer:");

                    builder.AppendLine(
                        entry.Answer);

                    builder.AppendLine();
                }
            }

            builder.AppendLine(
                "EVALUATION INSTRUCTIONS");

            builder.AppendLine(
                "======================");

            builder.AppendLine(
                "Evaluate the candidate against the role.");

            builder.AppendLine(
                "For each required skill, determine whether there is " +
                "credible evidence in the supplied profile/application " +
                "information and whether there is credible evidence in " +
                "the voice interview.");

            builder.AppendLine();

            builder.AppendLine(
                "IMPORTANT: Every score must be a whole-number percentage from 0 to 100.");

            builder.AppendLine(
                "For example, 35 means 35%, not 3.5%.");

            builder.AppendLine(
                "Never use a 0-10 scoring scale.");

            builder.AppendLine(
                "Never divide a score by 10.");

            builder.AppendLine();

            builder.AppendLine(
                "Do not treat a keyword alone as proof of competence.");

            builder.AppendLine();

            builder.AppendLine(
                "Evaluate the six behavioural competencies:");

            builder.AppendLine(
                "1. Communication");

            builder.AppendLine(
                "2. Empathy");

            builder.AppendLine(
                "3. Active Listening");

            builder.AppendLine(
                "4. Problem Solving");

            builder.AppendLine(
                "5. Teamwork");

            builder.AppendLine(
                "6. Reliability");

            builder.AppendLine();

            builder.AppendLine(
                "The application will calculate the final overall score " +
                "from the individual skill and competency scores.");

            builder.AppendLine();

            builder.AppendLine(
                "Do not penalize the candidate for information that " +
                "is simply unavailable.");

            builder.AppendLine();

            builder.AppendLine(
                "However, absence of evidence for a required skill " +
                "should result in limited or no support for that skill.");

            builder.AppendLine();

            builder.AppendLine(
                "Return the final result using the supplied JSON schema.");

            return builder
                .ToString()
                .Trim();
        }

        // ============================================================
        // NORMALIZE AI RESULT
        // ============================================================

        private void NormalizeAiResult(
            VolunteerInterviewAiEvaluationResponse aiResult,
            List<VolunteerRequiredSkill> requiredSkills)
        {
            /*
             * IMPORTANT:
             *
             * We intentionally normalize scores here.
             *
             * Correct:
             *
             * 35   -> 35
             * 72   -> 72
             * 100  -> 100
             *
             * If the AI accidentally uses a 0-10 scale:
             *
             * 3.5  -> 35
             * 7.2  -> 72
             * 10   -> 100
             *
             * A value of 0-1 is treated as a fraction:
             *
             * 0.35 -> 35
             * 0.72 -> 72
             *
             * This prevents the system from storing meaningless
             * values such as 3.5 when the intended percentage
             * is 35%.
             */

            aiResult.OverallScore =
                NormalizeScore(
                    aiResult.OverallScore);

            aiResult.OverallConfidence =
                NormalizeScore(
                    aiResult.OverallConfidence);

            aiResult.Skills ??=
                new List<VolunteerInterviewAiSkillResult>();

            aiResult.Competencies ??=
                new List<VolunteerInterviewAiCompetencyResult>();

            foreach (var skill in aiResult.Skills)
            {
                skill.Score =
                    NormalizeScore(
                        skill.Score);

                skill.Confidence =
                    NormalizeScore(
                        skill.Confidence);

                skill.Skill =
                    skill.Skill?.Trim()
                    ?? string.Empty;

                skill.ProfileEvidence =
                    skill.ProfileEvidence?.Trim()
                    ?? string.Empty;

                skill.InterviewEvidence =
                    skill.InterviewEvidence?.Trim()
                    ?? string.Empty;

                skill.Assessment =
                    skill.Assessment?.Trim()
                    ?? "Not Demonstrated";

                skill.Reasoning =
                    skill.Reasoning?.Trim()
                    ?? string.Empty;
            }

            foreach (var competency in aiResult.Competencies)
            {
                competency.Score =
                    NormalizeScore(
                        competency.Score);

                competency.Confidence =
                    NormalizeScore(
                        competency.Confidence);

                competency.Name =
                    competency.Name?.Trim()
                    ?? string.Empty;

                competency.Description =
                    competency.Description?.Trim()
                    ?? string.Empty;

                competency.Assessment =
                    competency.Assessment?.Trim()
                    ?? "Not Demonstrated";

                competency.Evidence =
                    competency.Evidence?.Trim()
                    ?? string.Empty;

                competency.Reasoning =
                    competency.Reasoning?.Trim()
                    ?? string.Empty;
            }

            // Remove AI skill entries that are not actually required
            // by this volunteer opportunity.

            var requiredNames =
                requiredSkills
                    .Select(
                        x =>
                            x.Skill.Trim())
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            aiResult.Skills =
                aiResult.Skills
                    .Where(
                        x =>
                            requiredNames.Contains(
                                x.Skill))
                    .ToList();
        }

        // ============================================================
        // SCORE NORMALIZATION
        // ============================================================

        private decimal NormalizeScore(
            decimal value)
        {
            /*
             * ALL APPLICATION SCORES ARE 0-100.
             *
             * 0.35 -> 35
             * 0.50 -> 50
             * 3.5  -> 35
             * 5    -> 50
             * 7.5  -> 75
             * 10   -> 100
             * 35   -> 35
             * 72   -> 72
             * 100  -> 100
             *
             * Values above 100 are capped at 100.
             */

            if (value < 0m)
            {
                value = 0m;
            }

            // Fractional score such as 0.35 = 35%
            if (value > 0m && value <= 1m)
            {
                value *= 100m;
            }
            // AI accidentally returned a 0-10 score.
            else if (value > 1m && value <= 10m)
            {
                value *= 10m;
            }

            return Math.Clamp(
                Math.Round(
                    value,
                    0,
                    MidpointRounding.AwayFromZero),
                0m,
                100m);
        }

        // ============================================================
        // RECOMMENDATION NORMALIZATION
        // ============================================================

        private string NormalizeRecommendation(
            string? recommendation)
        {
            if (string.IsNullOrWhiteSpace(
                    recommendation))
            {
                return "Review";
            }

            var value =
                recommendation
                    .Trim()
                    .ToLowerInvariant();

            if (value.Contains("not") &&
                value.Contains("recommend"))
            {
                return "Not Recommended";
            }

            if (value.Contains("reject") ||
                value.Contains("unsuitable"))
            {
                return "Not Recommended";
            }

            if (value.Contains("strong"))
            {
                return "Strongly Recommended";
            }

            if (value == "recommended" ||
                value.Contains("recommend"))
            {
                return "Recommended";
            }

            return "Review";
        }

        // ============================================================
        // COMBINED EVIDENCE
        // ============================================================

        private string BuildCombinedEvidence(
            string? profileEvidence,
            string? interviewEvidence)
        {
            var builder =
                new StringBuilder();

            if (!string.IsNullOrWhiteSpace(
                    profileEvidence))
            {
                builder.AppendLine(
                    "Profile/Application Evidence:");

                builder.AppendLine(
                    profileEvidence.Trim());

                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(
                    interviewEvidence))
            {
                builder.AppendLine(
                    "Voice Interview Evidence:");

                builder.AppendLine(
                    interviewEvidence.Trim());
            }

            return builder
                .ToString()
                .Trim();
        }

        // ============================================================
        // FALLBACK EXPLANATION
        // ============================================================

        private string BuildFallbackExplanation(
            decimal overallScore,
            decimal confidence,
            decimal skillAverage,
            decimal competencyAverage,
            int requiredSkillCount,
            int skillsFoundInProfile,
            int skillsFoundInInterview)
        {
            var builder =
                new StringBuilder();

            builder.Append(
                $"The final interview score is {overallScore:0}/100 " +
                $"with evaluation confidence of {confidence:0}%. ");

            if (requiredSkillCount > 0)
            {
                builder.Append(
                    $"The role contains {requiredSkillCount} required skill(s). ");

                builder.Append(
                    $"The calculated required-skill average is " +
                    $"{skillAverage:0}/100. ");

                builder.Append(
                    $"{skillsFoundInProfile} required skill(s) had " +
                    $"supporting profile/application evidence and ");

                builder.Append(
                    $"{skillsFoundInInterview} had supporting interview " +
                    $"evidence. ");
            }

            if (skillAverage > 0m)
            {
                builder.Append(
                    $"The behavioural competency average is " +
                    $"{competencyAverage:0}/100. ");
            }

            builder.Append(
                "The final score is calculated directly from the " +
                "individual evaluation scores and should be considered " +
                "together with the supporting evidence and administrator review.");

            return builder
                .ToString();
        }

        // ============================================================
        // TEXT LIMIT
        // ============================================================

        private string LimitText(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value =
                value.Trim();

            if (value.Length <= maximumLength)
            {
                return value;
            }

            return value.Substring(
                0,
                maximumLength);
        }

        // ============================================================
        // TRANSCRIPT MODEL
        // ============================================================

        private class InterviewTranscriptEntry
        {
            public int QuestionNumber { get; set; }

            public string Question { get; set; } =
                string.Empty;

            public string Answer { get; set; } =
                string.Empty;
        }
    }
}