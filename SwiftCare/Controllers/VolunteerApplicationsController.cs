using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;
using SwiftCare.ViewModels;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerATS;
using SwiftCare.ViewModels.Volunteer;
using System.Security.Cryptography;

namespace SwiftCare.Controllers
{
    public class VolunteerApplicationsController : Controller
    {
        private readonly ILogger<VolunteerApplicationsController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly FileUploadService _fileUploadService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly RandomPasswordGeneratorService _randomPasswordGeneratorService;
        private readonly VolunteerShortlistingPipeline _shortlistingPipeline;
        private readonly IVolunteerInterviewEvaluationEngine _volunteerInterviewEvaluationEngine;

        public VolunteerApplicationsController(ILogger<VolunteerApplicationsController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            FileUploadService fileUploadService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            EmailService emailService,
            RandomPasswordGeneratorService randomPasswordGeneratorService,
             VolunteerShortlistingPipeline shortlistingPipeline,
             IVolunteerInterviewEvaluationEngine volunteerInterviewEvaluationEngine)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _emailService = emailService;
            _randomPasswordGeneratorService = randomPasswordGeneratorService;
            _shortlistingPipeline =
                                  shortlistingPipeline;
            _volunteerInterviewEvaluationEngine = volunteerInterviewEvaluationEngine;
        }
        // ============================================================
        // APPLICATION DETAILS
        // ============================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var application =
                await _context.VolunteerApplications
                    .Include(x => x.Applicant)
                    .Include(x => x.VolunteerOpportunity)
                    .Include(x => x.Evaluation)
                    .Include(x => x.Answers)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            var auditHistory =
                await _context.VolunteerApplicationAudits
                    .AsNoTracking()
                    .Where(x =>
                        x.VolunteerApplicationId == id)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

            var viewModel =
                new ApplicationDetailsViewModel
                {
                    Application = application,
                    Evaluation = application.Evaluation,
                    AuditHistory = auditHistory
                };

            return View(viewModel);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ShortlistManagement()
        {
            var applications = await _context.VolunteerApplications
                .Include(x => x.Applicant)
                .Include(x => x.VolunteerOpportunity)
                .Where(x =>
                    (x.AIRecommendation == VolunteerAIRecommendation.StrongShortlist ||
                     x.AIRecommendation == VolunteerAIRecommendation.ReviewRecommended) &&
                    x.Stage != VolunteerApplicationStage.Rejected)
                .OrderByDescending(x => x.ATSScore)
                .ThenBy(x => x.ATSRanking)
                .ToListAsync();

            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> TrackApplication()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var applications = await _context.VolunteerApplications
                .Include(x => x.VolunteerOpportunity)
                .Where(x => x.ApplicantId == user.Id)
                .OrderByDescending(x => x.AppliedDate)
                .ToListAsync();

            return View(applications);
        }


        [HttpGet]
        public async Task<IActionResult> VolunteerApplications()
        {
            var applications = await _context.VolunteerApplications
                .Include(x => x.VolunteerOpportunity)
                .Include(x => x.Applicant)
                .OrderByDescending(x => x.AppliedDate)
                .ToListAsync();

            return View(applications);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetShortlistingPipelineStatus(int opportunityId)
        {
            var opportunity =
                await _context.VolunteerOpportunities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Volunteer opportunity not found."
                });
            }

            return Json(new
            {
                success = true,

                pipelineRun =
                    opportunity.ShortlistingPipelineRun,

                pipelineRunDate =
                    opportunity.ShortlistingPipelineRunDate,

                pipelineRunBy =
                    opportunity.ShortlistingPipelineRunBy
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunShortlistingPipeline(int opportunityId)
        {
            try
            {
                // ============================================================
                // GET OPPORTUNITY
                // ============================================================

                var opportunity =
                    await _context.VolunteerOpportunities
                        .FirstOrDefaultAsync(x =>
                            x.VolunteerOpportunityId == opportunityId);

                if (opportunity == null)
                {
                    return NotFound();
                }


                // ============================================================
                // SERVER-SIDE PIPELINE CHECK
                // ============================================================

                if (opportunity.ShortlistingPipelineRun)
                {
                    return Json(new
                    {
                        success = false,
                        alreadyRun = true,
                        message =
                            "The shortlisting pipeline has already been run for this opportunity.",
                        pipelineRunDate =
                            opportunity.ShortlistingPipelineRunDate
                    });
                }


                // ============================================================
                // RUN SHORTLISTING PIPELINE
                // ============================================================

                var result =
                    await _shortlistingPipeline
                        .RunAsync(opportunityId);


                // ============================================================
                // MARK PIPELINE AS COMPLETED
                // ONLY AFTER SUCCESSFUL EXECUTION
                // ============================================================

                //opportunity.ShortlistingPipelineRun = true;

                opportunity.ShortlistingPipelineRunDate =
                    DateTime.UtcNow;

                opportunity.ShortlistingPipelineRunBy =
                    User.Identity?.Name;


                await _context.SaveChangesAsync();


                // ============================================================
                // RETURN PIPELINE RESULTS
                // ============================================================

                return Json(new
                {
                    success = true,
                    alreadyRun = false,

                    applicationsCollected =
                        result.ApplicationsCollected,

                    eligible =
                        result.Eligible,

                    reviewRequired =
                        result.ReviewRequired,

                    notEligible =
                        result.NotEligible,

                    strongShortlist =
                        result.StrongShortlist,

                    reviewRecommended =
                        result.ReviewRecommended,

                    notRecommended =
                        result.NotRecommended
                });
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    alreadyRun = false,
                    message =
                        "The shortlisting pipeline could not be completed."
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> InterviewReview(int applicationId)
        {
            // ============================================================
            // 1. LOAD APPLICATION + APPLICANT + OPPORTUNITY + REQUIRED SKILLS
            // ============================================================

            var application = await _context.VolunteerApplications
                .Include(x => x.Applicant)
                .Include(x => x.VolunteerOpportunity)
                    .ThenInclude(x => x.RequiredSkills)
                .FirstOrDefaultAsync(x =>
                    x.VolunteerApplicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }


            // ============================================================
            // 2. CREATE VIEW MODEL
            // ============================================================

            var model = new VolunteerInterviewReviewViewModel
            {
                VolunteerApplicationId =
                    application.VolunteerApplicationId,

                Reference =
                    application.Reference ?? string.Empty,

                AppliedDate =
                    application.AppliedDate,

                Status =
                    application.Status.ToString(),

                Stage =
                    application.Stage.ToString(),

                EligibilityStatus =
                    application.EligibilityStatus.ToString(),

                ScreeningStatus =
                    application.ScreeningStatus.ToString(),

                CoverMessage =
                    application.CoverMessage ?? string.Empty,

                ApplicantId =
                    application.ApplicantId ?? string.Empty,

                ApplicantName =
                    $"{application.Applicant?.FirstName} {application.Applicant?.LastName}"
                        .Trim(),

                ApplicantEmail =
                    application.Applicant?.Email ?? string.Empty,

                ApplicantPhone =
                    application.Applicant?.PhoneNumber ?? string.Empty,

                ApplicantProfilePicture =
                    application.Applicant?.ProfilePicture ?? string.Empty,

                VolunteerOpportunityId =
                    application.VolunteerOpportunityId,

                OpportunityTitle =
                    application.VolunteerOpportunity?.Title ?? string.Empty
            };


            // ============================================================
            // 3. LOAD ATS APPLICATION EVALUATION
            //
            // THIS IS THE DATA THAT POWERS:
            //
            // ATS SCORE
            // ELIGIBILITY
            // EVALUATION DATE
            // EVALUATION VERSION
            // ============================================================

            var atsEvaluation =
                await _context.VolunteerApplicationEvaluations
                    .Where(x =>
                        x.VolunteerApplicationId == applicationId)
                    .OrderByDescending(x => x.EvaluatedDate)
                    .FirstOrDefaultAsync();


            if (atsEvaluation != null)
            {
                // --------------------------------------------------------
                // ATS EVALUATION EXISTS
                // --------------------------------------------------------

                model.HasEvaluation = true;

                model.OverallScore =
                    atsEvaluation.OverallScore;

                model.EligibilityScore =
                    atsEvaluation.EligibilityScore;

                model.EvaluatedDate =
                    atsEvaluation.EvaluatedDate;

                model.EvaluationVersion =
                    atsEvaluation.EvaluationVersion
                    ?? string.Empty;
            }
            else
            {
                // --------------------------------------------------------
                // NO ATS EVALUATION YET
                // --------------------------------------------------------

                model.HasEvaluation = false;

                model.OverallScore = 0m;

                model.EligibilityScore = 0m;

                model.EvaluatedDate = null;

                model.EvaluationVersion = string.Empty;
            }


            // ============================================================
            // 4. LOAD INTERVIEW EVALUATION
            // ============================================================

            var interviewEvaluation =
                await _context.VolunteerInterviewEvaluations

                    .Include(x => x.SkillEvaluations)

                    .Include(x => x.CompetencyEvaluations)

                    .Where(x =>
                        x.VolunteerApplicationId ==
                        applicationId)

                    .FirstOrDefaultAsync();


            if (interviewEvaluation != null)
            {
                // ========================================================
                // INTERVIEW SUMMARY
                // ========================================================

                model.OverallInterviewScore =
                    interviewEvaluation.OverallInterviewScore;

                model.OverallInterviewConfidence =
                    interviewEvaluation.OverallInterviewConfidence;

                model.InterviewRecommendation =
                    interviewEvaluation.Recommendation
                    ?? "Not Evaluated";

                model.InterviewEvaluationStatus =
                    interviewEvaluation.EvaluationStatus
                    ?? "Not Evaluated";

                model.InterviewEvaluatedDate =
                    interviewEvaluation.EvaluatedDate;

                model.InterviewEvaluationVersion =
                    interviewEvaluation.EvaluationVersion
                    ?? string.Empty;


                // ========================================================
                // REQUIRED SKILLS
                // ========================================================

                model.RequiredSkills =
                    interviewEvaluation.SkillEvaluations

                        .OrderBy(x => x.Skill)

                        .Select(x =>
                            new VolunteerInterviewRequiredSkillViewModel
                            {
                                Id =
                                    x.VolunteerRequiredSkillId,

                                Skill =
                                    x.Skill ?? string.Empty,

                                FoundInCv =
                                    x.FoundInCv,

                                CvEvidence =
                                    x.CvEvidence ?? string.Empty,

                                FoundInInterview =
                                    x.FoundInInterview,

                                InterviewEvidence =
                                    x.InterviewEvidence ?? string.Empty,

                                InterviewScore =
                                    x.InterviewScore,

                                Confidence =
                                    x.Confidence,

                                Assessment =
                                    x.Assessment ?? "Not Evaluated",

                                Evidence =
                                    x.Evidence ?? string.Empty,

                                Reasoning =
                                    x.Reasoning ?? string.Empty
                            })

                        .ToList();


                // ========================================================
                // COMPETENCIES
                // ========================================================

                model.InterviewCompetencies =
                    interviewEvaluation
                        .CompetencyEvaluations

                        .OrderBy(x => x.Name)

                        .Select(x =>
                            new VolunteerInterviewCompetencyViewModel
                            {
                                Name =
                                    x.Name ?? string.Empty,

                                Description =
                                    x.Description ?? string.Empty,

                                Score =
                                    x.Score,

                                Confidence =
                                    x.Confidence,

                                Assessment =
                                    x.Assessment ?? "Not Evaluated",

                                Evidence =
                                    x.Evidence ?? string.Empty,

                                Reasoning =
                                    x.Reasoning ?? string.Empty,

                                Demonstrated =
                                    x.Demonstrated
                            })

                        .ToList();


                // ========================================================
                // REQUIRED SKILL COUNTS
                // ========================================================

                model.RequiredSkillCount =
                    model.RequiredSkills.Count;

                model.RequiredSkillsFoundInCv =
                    model.RequiredSkills.Count(
                        x => x.FoundInCv);

                model.RequiredSkillsFoundInInterview =
                    model.RequiredSkills.Count(
                        x => x.FoundInInterview);

                model.RequiredSkillsOverallScore =
                    model.RequiredSkills.Any()
                        ? model.RequiredSkills
                            .Average(x => x.InterviewScore)
                        : 0m;
            }
            else
            {
                // ========================================================
                // NO INTERVIEW EVALUATION YET
                // ========================================================

                model.InterviewRecommendation =
                    "Not Evaluated";

                model.InterviewEvaluationStatus =
                    "Not Evaluated";

                model.OverallInterviewScore =
                    0m;

                model.OverallInterviewConfidence =
                    0m;


                // ========================================================
                // REQUIRED SKILL PLACEHOLDERS
                // ========================================================

                model.RequiredSkills =
                    application.VolunteerOpportunity?
                        .RequiredSkills?

                        .Select(requiredSkill =>
                            new VolunteerInterviewRequiredSkillViewModel
                            {
                                Id =
                                    requiredSkill.VolunteerRequiredSkillId,

                                Skill =
                                    requiredSkill.Skill
                                    ?? string.Empty,

                                FoundInCv =
                                    false,

                                FoundInInterview =
                                    false,

                                InterviewScore =
                                    0m,

                                Confidence =
                                    0m,

                                Assessment =
                                    "Not Evaluated",

                                CvEvidence =
                                    string.Empty,

                                InterviewEvidence =
                                    string.Empty,

                                Evidence =
                                    string.Empty,

                                Reasoning =
                                    string.Empty
                            })

                        .ToList()
                    ?? new List<VolunteerInterviewRequiredSkillViewModel>();


                model.RequiredSkillCount =
                    model.RequiredSkills.Count;


                // ========================================================
                // INTERVIEW COMPETENCY PLACEHOLDERS
                // ========================================================

                model.InterviewCompetencies =
                    new List<VolunteerInterviewCompetencyViewModel>
                    {
                new()
                {
                    Name = "Communication",

                    Description =
                        "Ability to communicate clearly, appropriately and effectively.",

                    Assessment =
                        "Not Evaluated"
                },

                new()
                {
                    Name = "Empathy",

                    Description =
                        "Ability to understand and respond appropriately to another person's feelings and needs.",

                    Assessment =
                        "Not Evaluated"
                },

                new()
                {
                    Name = "Active Listening",

                    Description =
                        "Ability to listen carefully, understand what is being said and respond appropriately.",

                    Assessment =
                        "Not Evaluated"
                },

                new()
                {
                    Name = "Problem Solving",

                    Description =
                        "Ability to identify problems, think through situations and provide practical responses.",

                    Assessment =
                        "Not Evaluated"
                },

                new()
                {
                    Name = "Teamwork",

                    Description =
                        "Ability to cooperate with others, contribute positively and work toward shared goals.",

                    Assessment =
                        "Not Evaluated"
                },

                new()
                {
                    Name = "Reliability",

                    Description =
                        "Evidence of responsibility, dependability, consistency and commitment.",

                    Assessment =
                        "Not Evaluated"
                }
                    };
            }


            // ============================================================
            // 5. LOAD VOICE INTERVIEW SESSION
            // ============================================================

            var interviewSession =
                await _context.VolunteerVoiceInterviewSessions
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId == applicationId);


            if (interviewSession != null)
            {
                model.HasInterviewSession = true;

                model.InterviewStarted =
                    interviewSession.IsStarted;

                model.InterviewCompleted =
                    interviewSession.IsCompleted;

                model.InterviewStartedDate =
                    interviewSession.StartedDate;

                model.InterviewCompletedDate =
                    interviewSession.CompletedDate;

                model.InterviewStatus =
                    interviewSession.Status
                    ?? "Not Started";
            }
            else
            {
                model.HasInterviewSession = false;

                model.InterviewStarted = false;

                model.InterviewCompleted = false;

                model.InterviewStatus =
                    "Not Started";
            }


            // ============================================================
            // 6. LOAD INTERVIEW ANSWERS / TRANSCRIPT
            // ============================================================

            var answers =
                await _context.VolunteerApplicationAnswers

                    .Include(x => x.VolunteerQuestion)

                    .Where(x =>
                        x.VolunteerApplicationId ==
                        applicationId)

                    .OrderBy(x =>
                        x.VolunteerQuestion!.DisplayOrder)

                    .ToListAsync();


            var questionNumber = 1;


            foreach (var answer in answers)
            {
                model.Transcript.Add(
                    new VolunteerInterviewTranscriptItemViewModel
                    {
                        QuestionNumber =
                            questionNumber,

                        QuestionId =
                            answer.VolunteerQuestionId,

                        Question =
                            answer.VolunteerQuestion?.Question
                            ?? string.Empty,

                        Answer =
                            answer.Answer
                            ?? string.Empty
                    });

                questionNumber++;
            }


            // ============================================================
            // 7. INTERVIEW COUNTS
            // ============================================================

            model.InterviewAnsweredCount =
                model.Transcript.Count;


            // If the session exists, use its current question count
            // where possible. Otherwise fall back to the transcript count.

            model.InterviewQuestionCount =
                model.Transcript.Count;


            // ============================================================
            // 8. DECISION AVAILABILITY
            // ============================================================

            model.CanMakeDecision =
                model.InterviewCompleted;


            // ============================================================
            // 9. RETURN REVIEW PAGE
            // ============================================================

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunInterviewEvaluation(
      int applicationId,
      CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    $"[SWIFTCARE INTERVIEW AI] Admin requested evaluation " +
                    $"for application {applicationId}");

                Console.WriteLine(
                    "============================================================");

                if (applicationId <= 0)
                {
                    TempData["Error"] =
                        "Invalid volunteer application ID.";

                    return RedirectToAction(
                        nameof(InterviewReview),
                        new
                        {
                            applicationId
                        });
                }

                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine(
                    "[SWIFTCARE INTERVIEW AI] Calling evaluation engine...");

                var result =
                    await _volunteerInterviewEvaluationEngine
                        .EvaluateAsync(
                            applicationId,
                            cancellationToken);

                Console.WriteLine(
                    $"[SWIFTCARE INTERVIEW AI] Evaluation returned. " +
                    $"Success={result.Success}, " +
                    $"Status={result.EvaluationStatus}");

                if (!result.Success)
                {
                    TempData["Error"] =
                        string.IsNullOrWhiteSpace(
                            result.ErrorMessage)
                            ? "Interview evaluation failed."
                            : result.ErrorMessage;

                    return RedirectToAction(
                        nameof(InterviewReview),
                        new
                        {
                            applicationId
                        });
                }

                TempData["Success"] =
                    $"Interview evaluation completed. " +
                    $"Score: {result.OverallInterviewScore:0.##}/100. " +
                    $"Recommendation: {result.Recommendation}.";

                return RedirectToAction(
                    nameof(InterviewReview),
                    new
                    {
                        applicationId
                    });
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "[SWIFTCARE INTERVIEW AI] Evaluation request was cancelled.");

                TempData["Error"] =
                    "The interview evaluation was cancelled.";

                return RedirectToAction(
                    nameof(InterviewReview),
                    new
                    {
                        applicationId
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[SWIFTCARE INTERVIEW AI] Evaluation failed:");

                Console.WriteLine(
                    ex);

                TempData["Error"] =
                    "Interview evaluation failed: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(InterviewReview),
                    new
                    {
                        applicationId
                    });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveInterviewDecision(
    int applicationId,
    string decision,
    string? decisionNotes)
        {
            var application =
                await _context.VolunteerApplications
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            decision =
                (decision ?? string.Empty)
                .Trim();

            decisionNotes =
                (decisionNotes ?? string.Empty)
                .Trim();

            if (string.IsNullOrWhiteSpace(decision))
            {
                TempData["Error"] =
                    "Please select a decision.";

                return RedirectToAction(
                    nameof(InterviewReview),
                    new { applicationId });
            }

            switch (decision.ToLowerInvariant())
            {
                case "approve":

                    application.Status =
                        VolunteerApplicationStatus.Approved;

                    application.Stage =
                        VolunteerApplicationStage.Approved;

                    break;


                case "reject":

                    application.Status =
                        VolunteerApplicationStatus.Rejected;

                    application.Stage =
                        VolunteerApplicationStage.Rejected;

                    break;


                case "review":

                    application.Stage =
                        VolunteerApplicationStage.Interview;

                    break;


                default:

                    TempData["Error"] =
                        "Invalid interview decision.";

                    return RedirectToAction(
                        nameof(InterviewReview),
                        new { applicationId });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Interview decision saved successfully.";

            return RedirectToAction(
                nameof(InterviewReview),
                new { applicationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShortlistCandidate(int applicationId)
        {
            // ============================================================
            // 1. LOAD APPLICATION
            // ============================================================

            var application = await _context.VolunteerApplications
                .Include(x => x.Applicant)
                .Include(x => x.VolunteerOpportunity)
                .FirstOrDefaultAsync(x =>
                    x.VolunteerApplicationId == applicationId);

            if (application == null)
            {
                TempData["ErrorMessage"] =
                    "The volunteer application could not be found.";

                return RedirectToAction(
                    "Index",
                    "VolunteerApplications");
            }

            // ============================================================
            // 2. PREVENT DUPLICATE SHORTLISTING
            // ============================================================

            if (application.Status == VolunteerApplicationStatus.Shortlisted)
            {
                TempData["ErrorMessage"] =
                    "This applicant has already been shortlisted.";

                return RedirectToAction(
                    "ApplicationDetails",
                    "VolunteerApplications",
                    new
                    {
                        applicationId = applicationId
                    });
            }

            // ============================================================
            // 3. VALIDATE APPLICANT EMAIL
            // ============================================================

            if (application.Applicant == null ||
                string.IsNullOrWhiteSpace(application.Applicant.Email))
            {
                TempData["ErrorMessage"] =
                    "The applicant does not have a valid email address. " +
                    "The candidate was not shortlisted.";

                return RedirectToAction(
                    "ApplicationDetails",
                    "VolunteerApplications",
                    new
                    {
                        applicationId = applicationId
                    });
            }

            // ============================================================
            // 4. MANUAL ADMIN SHORTLIST
            // ============================================================

            application.Status =
                VolunteerApplicationStatus.Shortlisted;

            application.ShortlistedDate = DateTime.Now;

            // ============================================================
            // 5. CHECK FOR EXISTING INTERVIEW SESSION
            // ============================================================

            var interviewSession =
                await _context.VolunteerVoiceInterviewSessions
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId == applicationId);

            // ============================================================
            // 6. CREATE INTERVIEW SESSION IF NEEDED
            // ============================================================

            if (interviewSession == null)
            {
                // Generate a secure random token.
                var tokenBytes =
                    RandomNumberGenerator.GetBytes(32);

                var sessionToken =
                    Convert.ToBase64String(tokenBytes)
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", "");

                interviewSession =
                    new VolunteerVoiceInterviewSession
                    {
                        VolunteerApplicationId =
                            application.VolunteerApplicationId,

                        SessionToken = sessionToken,

                        CreatedDate = DateTime.Now,

                        StartedDate = null,

                        CompletedDate = null,

                        CurrentQuestionNumber = 0,

                        IsStarted = false,

                        IsCompleted = false,

                        Status = "NotStarted"
                    };

                _context.VolunteerVoiceInterviewSessions
                    .Add(interviewSession);
            }

            // ============================================================
            // 7. SAVE SHORTLIST + INTERVIEW SESSION
            // ============================================================

            await _context.SaveChangesAsync();

            // ============================================================
            // 8. BUILD SECURE INTERVIEW URL
            // ============================================================

            var interviewUrl = Url.Action(
                "Start",
                "VolunteerInterview",
                new
                {
                    token = interviewSession.SessionToken
                },
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(interviewUrl))
            {
                TempData["ErrorMessage"] =
                    "The interview session was created, but the interview " +
                    "link could not be generated.";

                return RedirectToAction(
                    "ApplicationDetails",
                    "VolunteerApplications",
                    new
                    {
                        applicationId = applicationId
                    });
            }

            // ============================================================
            // 9. APPLICANT NAME
            // ============================================================

            var applicantName =
                $"{application.Applicant.FirstName} {application.Applicant.LastName}"
                    .Trim();

            if (string.IsNullOrWhiteSpace(applicantName))
            {
                applicantName = "Volunteer Applicant";
            }

            // ============================================================
            // 10. OPPORTUNITY TITLE
            // ============================================================

            var opportunityTitle =
                application.VolunteerOpportunity?.Title
                ?? "SwiftCare Volunteer Opportunity";

            // ============================================================
            // 11. CREATE CUSTOMIZED EMAIL
            // ============================================================

            var subject =
                $"SwiftCare Volunteer Interview – {opportunityTitle}";

            var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>SwiftCare Volunteer Interview</title>
</head>

<body style='margin:0;
             padding:0;
             background-color:#f4f8f5;
             font-family:Arial,Helvetica,sans-serif;
             color:#123d22;'>

    <div style='max-width:680px;
                margin:30px auto;
                background:#ffffff;
                border-radius:18px;
                overflow:hidden;
                box-shadow:0 8px 30px rgba(0,0,0,0.08);'>

        <!-- HEADER -->
        <div style='background:linear-gradient(135deg,#1e6a36,#155c2d,#0d4722);
                    padding:35px 30px;
                    text-align:center;
                    color:white;'>

            <h1 style='margin:0;
                       font-size:30px;
                       font-weight:700;'>
                SwiftCare
            </h1>

            <p style='margin:8px 0 0;
                      font-size:16px;
                      opacity:0.92;'>
                Volunteer Selection Process
            </p>
        </div>

        <!-- CONTENT -->
        <div style='padding:35px 32px;'>

            <h2 style='margin-top:0;
                       color:#155c2d;
                       font-size:24px;'>
                Congratulations, {System.Net.WebUtility.HtmlEncode(applicantName)}!
            </h2>

            <p style='font-size:16px;
                      line-height:1.7;'>
                We are pleased to let you know that you have been
                <strong>shortlisted</strong> for the next stage of the
                SwiftCare volunteer selection process.
            </p>

            <p style='font-size:16px;
                      line-height:1.7;'>
                You have been shortlisted for:
            </p>

            <!-- OPPORTUNITY CARD -->
            <div style='background:#efffce;
                        border-left:5px solid #b6ff2c;
                        padding:18px 20px;
                        border-radius:10px;
                        margin:20px 0;'>

                <div style='font-size:13px;
                            color:#52705d;
                            margin-bottom:6px;'>
                    VOLUNTEER OPPORTUNITY
                </div>

                <div style='font-size:20px;
                            font-weight:bold;
                            color:#155c2d;'>
                    {System.Net.WebUtility.HtmlEncode(opportunityTitle)}
                </div>
            </div>

            <p style='font-size:16px;
                      line-height:1.7;'>
                As the next step, we would like you to complete a
                <strong>short voice-based interview</strong>.
            </p>

            <p style='font-size:16px;
                      line-height:1.7;'>
                During the interview, SwiftCare's virtual interview
                assistant will ask you a series of questions. You will
                answer these questions using your voice, and your responses
                will be securely converted into text and recorded as part
                of your application.
            </p>

            <!-- WHAT TO EXPECT -->
            <div style='margin:28px 0;'>

                <h3 style='color:#155c2d;
                           font-size:18px;'>
                    What to expect
                </h3>

                <p style='font-size:15px;
                          line-height:1.8;
                          margin:6px 0;'>
                    ✓ Questions will be presented one at a time
                </p>

                <p style='font-size:15px;
                          line-height:1.8;
                          margin:6px 0;'>
                    ✓ Questions will be read aloud to you
                </p>

                <p style='font-size:15px;
                          line-height:1.8;
                          margin:6px 0;'>
                    ✓ You will answer using your microphone
                </p>

                <p style='font-size:15px;
                          line-height:1.8;
                          margin:6px 0;'>
                    ✓ Your spoken answers will be converted to text
                </p>

                <p style='font-size:15px;
                          line-height:1.8;
                          margin:6px 0;'>
                    ✓ Please answer naturally and honestly
                </p>

            </div>

            <!-- BUTTON -->
            <div style='text-align:center;
                        margin:35px 0;'>

                <a href='{interviewUrl}'
                   style='display:inline-block;
                          background:#155c2d;
                          color:#ffffff;
                          text-decoration:none;
                          padding:16px 32px;
                          border-radius:10px;
                          font-size:17px;
                          font-weight:bold;'>
                    Start Your Voice Interview
                </a>

            </div>

            <p style='font-size:14px;
                      line-height:1.7;
                      color:#52705d;'>
                Please use the button above to access your secure interview
                session.
            </p>

            <p style='font-size:14px;
                      line-height:1.7;
                      color:#52705d;'>
                We recommend completing the interview in a quiet environment
                with a reliable internet connection and allowing microphone
                access when requested by your browser.
            </p>

            <!-- SECURITY NOTE -->
            <div style='background:#f4f8f5;
                        border-radius:10px;
                        padding:16px 18px;
                        margin-top:25px;'>

                <p style='margin:0;
                          font-size:13px;
                          line-height:1.6;
                          color:#52705d;'>
                    <strong>Important:</strong>
                    This interview link is unique to your application.
                    Please do not share it with another person.
                </p>

            </div>

            <p style='font-size:16px;
                      line-height:1.7;
                      margin-top:30px;'>
                We look forward to hearing from you.
            </p>

            <p style='font-size:16px;
                      line-height:1.7;
                      margin-bottom:0;'>
                Kind regards,<br>
                <strong>SwiftCare Volunteer Team</strong>
            </p>

        </div>

        <!-- FOOTER -->
        <div style='background:#123d22;
                    padding:22px 30px;
                    text-align:center;
                    color:#ffffff;'>

            <p style='margin:0;
                      font-size:13px;
                      opacity:0.9;'>
                SwiftCare
            </p>

            <p style='margin:6px 0 0;
                      font-size:12px;
                      opacity:0.7;'>
                Volunteer Selection &amp; Recruitment
            </p>

        </div>

    </div>

</body>
</html>
";

            // ============================================================
            // 12. SEND EMAIL
            // ============================================================

            try
            {
                await _emailService.SendEmailAsync(
                    application.Applicant.Email,
                    subject,
                    emailBody,
                    "SwiftCare Volunteer Team");

                TempData["SuccessMessage"] =
                    $"Candidate shortlisted successfully. " +
                    $"The voice interview invitation was sent to " +
                    $"{application.Applicant.Email}.";
            }
            catch (Exception ex)
            {
                // The shortlist and interview session have already been saved.
                // Therefore we don't undo the shortlist just because email failed.

                Console.WriteLine(
                    $"[SWIFTCARE EMAIL] Failed to send interview invitation: {ex}");

                TempData["WarningMessage"] =
                    "Candidate was shortlisted and the interview session was " +
                    "created, but the invitation email could not be sent. " +
                    "Please try sending the invitation again.";
            }

            // ============================================================
            // 13. REDIRECT BACK TO APPLICATION DETAILS
            // ============================================================

            return RedirectToAction(
                "ApplicationDetails",
                "VolunteerApplications",
                new
                {
                    id = applicationId
                });
        }

    }
}
