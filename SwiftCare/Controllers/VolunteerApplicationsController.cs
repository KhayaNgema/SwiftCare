using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
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
        private readonly VolunteerShortlistingPipeline
                                                       _shortlistingPipeline;

        public VolunteerApplicationsController(ILogger<VolunteerApplicationsController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            FileUploadService fileUploadService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            EmailService emailService,
            RandomPasswordGeneratorService randomPasswordGeneratorService,
             VolunteerShortlistingPipeline shortlistingPipeline)
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
