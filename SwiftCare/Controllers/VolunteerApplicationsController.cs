using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerATS;
using SwiftCare.Services.VolunteerOnboarding;
using SwiftCare.ViewModels;
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
        private readonly IVolunteerOnboardingService _onboardingService;

        public VolunteerApplicationsController(ILogger<VolunteerApplicationsController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            FileUploadService fileUploadService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            EmailService emailService,
            RandomPasswordGeneratorService randomPasswordGeneratorService,
             VolunteerShortlistingPipeline shortlistingPipeline,
             IVolunteerInterviewEvaluationEngine volunteerInterviewEvaluationEngine,
             IVolunteerOnboardingService onboardingService)
        {
            _logger = logger;
            _onboardingService = onboardingService;
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
                    DateTime.Now;

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
                    .Include(x => x.Applicant)
                    .Include(x => x.VolunteerOpportunity)
                    .FirstOrDefaultAsync(
                        x => x.VolunteerApplicationId == applicationId);

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
                // =========================================================
                // APPROVE
                // =========================================================

                case "approve":

                    application.Status =
                        VolunteerApplicationStatus.Approved;

                    application.Stage =
                        VolunteerApplicationStage.Approved;

                    /*
                     * Save the interview decision first.
                     */
                    await _context.SaveChangesAsync();

                    try
                    {
                        var admin =
                            await _userManager.GetUserAsync(User);

                        if (admin == null)
                        {
                            TempData["Error"] =
                                "The administrator could not be identified.";

                            return RedirectToAction(
                                nameof(InterviewReview),
                                new { applicationId });
                        }

                        if (application.Applicant == null)
                        {
                            TempData["Error"] =
                                "The applicant account could not be found.";

                            return RedirectToAction(
                                nameof(InterviewReview),
                                new { applicationId });
                        }

                        if (string.IsNullOrWhiteSpace(
                            application.Applicant.Email))
                        {
                            TempData["Error"] =
                                "The applicant does not have a valid email address.";

                            return RedirectToAction(
                                nameof(InterviewReview),
                                new { applicationId });
                        }

                        // =================================================
                        // GET OR CREATE ONBOARDING
                        // =================================================

                        var onboarding =
                            await _onboardingService
                                .GetOnboardingByApplicationAsync(
                                    applicationId);

                        if (onboarding == null)
                        {
                            onboarding =
                                await _onboardingService
                                    .CreateOnboardingAsync(
                                        applicationId,
                                        admin.Id);
                        }

                        // =================================================
                        // CHECK FOR EXISTING ACTIVE OFFER
                        // =================================================

                        var existingOffer =
                            await _onboardingService
                                .GetActiveOfferAsync(
                                    onboarding.VolunteerOnboardingId);

                        VolunteerOfferCreationResult offerResult;

                        if (existingOffer != null &&
                            existingOffer.Status ==
                            VolunteerOfferStatus.Pending &&
                            existingOffer.ExpiryDate > DateTime.Now)
                        {
                            /*
                             * There is already a valid offer.
                             *
                             * We cannot recover its raw token because the
                             * database only stores the hash.
                             *
                             * Therefore do not create another offer here.
                             */
                            TempData["Success"] =
                                "Interview decision saved. " +
                                "The applicant already has an active offer.";

                            return RedirectToAction(
                                nameof(InterviewReview),
                                new { applicationId });
                        }

                        // =================================================
                        // CREATE NEW OFFER
                        // =================================================

                        var expiryDate =
                            DateTime.Now.AddDays(7);

                        offerResult =
                            await _onboardingService
                                .CreateOfferAsync(
                                    onboarding.VolunteerOnboardingId,
                                    expiryDate,
                                    admin.Id);

                        if (offerResult == null ||
                            offerResult.Offer == null ||
                            string.IsNullOrWhiteSpace(
                                offerResult.RawAcceptanceToken))
                        {
                            TempData["Error"] =
                                "The applicant was approved, but the volunteer offer could not be created.";

                            return RedirectToAction(
                                nameof(InterviewReview),
                                new { applicationId });
                        }

                        var offer =
                            offerResult.Offer;

                        // =================================================
                        // BUILD SECURE OFFER LINK
                        // =================================================

                        var offerUrl =
                            Url.Action(
                                "Offer",
                                "VolunteerOnboarding",
                                new
                                {
                                    offerId =
                                        offer.VolunteerOfferId,

                                    token =
                                        offerResult.RawAcceptanceToken
                                },
                                Request.Scheme);

                        if (string.IsNullOrWhiteSpace(offerUrl))
                        {
                            TempData["Error"] =
                                "The volunteer offer was created, but the offer link could not be generated.";

                            return RedirectToAction(
                                nameof(InterviewReview),
                                new { applicationId });
                        }

                        // =================================================
                        // APPLICANT DETAILS
                        // =================================================

                        var applicant =
                            application.Applicant;

                        var applicantName =
                            $"{applicant.FirstName} {applicant.LastName}"
                                .Trim();

                        if (string.IsNullOrWhiteSpace(applicantName))
                        {
                            applicantName =
                                "Applicant";
                        }

                        var positionTitle =
                            application.VolunteerOpportunity?.Title
                            ?? offer.PositionTitle
                            ?? "Volunteer";

                        // =================================================
                        // EMAIL
                        // =================================================

                        var emailSubject =
                            "SwiftCare Volunteer Offer";

                        var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>SwiftCare Volunteer Offer</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background:#f4f8f1;
    font-family:Arial,Helvetica,sans-serif;
    color:#254d35;
"">

    <div style=""
        max-width:680px;
        margin:40px auto;
        background:#ffffff;
        border-radius:18px;
        overflow:hidden;
        box-shadow:0 8px 30px rgba(25,75,45,.08);
    "">

        <div style=""
            background:#eaffd0;
            padding:30px;
        "">

            <h1 style=""
                margin:0;
                color:#174d32;
                font-size:28px;
            "">
                SwiftCare
            </h1>

            <p style=""
                margin:8px 0 0;
                color:#426244;
                font-size:15px;
            "">
                Volunteer Opportunity
            </p>

        </div>

        <div style=""
            padding:35px;
        "">

            <h2 style=""
                color:#174d32;
                margin-top:0;
            "">
                Congratulations, {System.Net.WebUtility.HtmlEncode(applicantName)}!
            </h2>

            <p>
                We are pleased to inform you that your interview for the
                <strong>
                    {System.Net.WebUtility.HtmlEncode(positionTitle)}
                </strong>
                volunteer position at SwiftCare has been successful.
            </p>

            <p>
                We would like to offer you the opportunity to continue
                with the SwiftCare volunteer onboarding process.
            </p>

            <div style=""
                background:#f3faeb;
                border-left:4px solid #72b82a;
                padding:18px;
                margin:25px 0;
            "">

                <strong style=""color:#174d32;"">
                    Offer Reference
                </strong>

                <div style=""
                    margin-top:6px;
                    font-size:18px;
                    font-weight:bold;
                    color:#254d35;
                "">
                    {System.Net.WebUtility.HtmlEncode(offer.OfferReference)}
                </div>

                <div style=""
                    margin-top:10px;
                    color:#6c756e;
                    font-size:14px;
                "">
                    Offer expires:
                    {offer.ExpiryDate.ToLocalTime():dd MMM yyyy HH:mm}
                </div>

            </div>

            <p>
                Please review your volunteer offer and continue with the
                onboarding process using the button below.
            </p>

            <div style=""
                text-align:center;
                margin:30px 0;
            "">

                <a href=""{offerUrl}""
                   style=""
                       display:inline-block;
                       background:#72b82a;
                       color:#163d25;
                       text-decoration:none;
                       font-weight:bold;
                       padding:14px 26px;
                       border-radius:10px;
                   "">
                    View My Volunteer Offer
                </a>

            </div>

            <p style=""
                color:#6c756e;
                font-size:14px;
                line-height:1.6;
            "">
                You will be able to accept or decline the offer from
                the secure SwiftCare onboarding page.
            </p>

            <p style=""
                color:#6c756e;
                font-size:14px;
                line-height:1.6;
            "">
                If you did not expect this email or believe you received
                it in error, please contact SwiftCare.
            </p>

            <hr style=""
                border:none;
                border-top:1px solid #edf1ea;
                margin:30px 0;
            "" />

            <p style=""
                margin:0;
                color:#78847b;
                font-size:12px;
            "">
                This is an automated SwiftCare notification.
                Please do not reply directly to this email.
            </p>

        </div>

    </div>

</body>
</html>";

                        await _emailService.SendEmailAsync(
                            applicant.Email,
                            emailSubject,
                            emailBody,
                            "SwiftCare Volunteer Services");

                        _logger.LogInformation(
                            "[SWIFTCARE ATS] Applicant approved and volunteer offer emailed. " +
                            "ApplicationId: {ApplicationId}, ApplicantId: {ApplicantId}, " +
                            "OnboardingId: {OnboardingId}, OfferId: {OfferId}",
                            applicationId,
                            applicant.Id,
                            onboarding.VolunteerOnboardingId,
                            offer.VolunteerOfferId);

                        TempData["Success"] =
                            "Interview decision approved successfully. " +
                            "A volunteer offer has been created and emailed to the applicant.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "[SWIFTCARE ATS] Applicant was approved but volunteer offer/email processing failed. " +
                            "ApplicationId: {ApplicationId}",
                            applicationId);

                        TempData["Error"] =
                            "The applicant was approved, but the volunteer offer email could not be sent. " +
                            "Please check the onboarding record and email configuration.";
                    }

                    break;


                // =========================================================
                // REJECT
                // =========================================================

                case "reject":

                    application.Status =
                        VolunteerApplicationStatus.Rejected;

                    application.Stage =
                        VolunteerApplicationStage.Rejected;

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Interview decision saved successfully.";

                    break;


                // =========================================================
                // REVIEW
                // =========================================================

                case "review":

                    application.Stage =
                        VolunteerApplicationStage.Interview;

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Interview decision saved successfully.";

                    break;


                // =========================================================
                // INVALID
                // =========================================================

                default:

                    TempData["Error"] =
                        "Invalid interview decision.";

                    return RedirectToAction(
                        nameof(InterviewReview),
                        new { applicationId });
            }

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
