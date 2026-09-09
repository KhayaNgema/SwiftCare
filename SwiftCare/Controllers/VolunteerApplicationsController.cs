using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerATS;
using SwiftCare.ViewModels.Volunteer;

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


                //// ============================================================
                //// SERVER-SIDE PIPELINE CHECK
                //// ============================================================

                //if (opportunity.ShortlistingPipelineRun)
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        alreadyRun = true,
                //        message =
                //            "The shortlisting pipeline has already been run for this opportunity.",
                //        pipelineRunDate =
                //            opportunity.ShortlistingPipelineRunDate
                //    });
                //}


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

                opportunity.ShortlistingPipelineRun = true;

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

    }
}
