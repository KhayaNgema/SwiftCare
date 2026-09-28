using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Build.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerATS.CV;
using SwiftCare.ViewModels;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Twilio.Http;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace SwiftCare.Controllers
{
    public class VolunteerOpportunitiesController : Controller
    {

        private readonly ILogger<VolunteerOpportunitiesController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly FileUploadService _fileUploadService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly RandomPasswordGeneratorService _randomPasswordGeneratorService;
        private readonly IAzureDocumentIntelligenceCvService _azureDocumentIntelligenceCvService;
        private readonly IVolunteerCVParsingEngine _cvParsingEngine;

        public VolunteerOpportunitiesController(ILogger<VolunteerOpportunitiesController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            FileUploadService fileUploadService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            EmailService emailService,
            RandomPasswordGeneratorService randomPasswordGeneratorService,
            IAzureDocumentIntelligenceCvService azureDocumentIntelligenceCvService,
             IVolunteerCVParsingEngine cvParsingEngine)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _emailService = emailService;
            _randomPasswordGeneratorService = randomPasswordGeneratorService;
            _azureDocumentIntelligenceCvService =
                   azureDocumentIntelligenceCvService
                   ?? throw new ArgumentNullException(
                       nameof(azureDocumentIntelligenceCvService));

            _cvParsingEngine =
                cvParsingEngine
                ?? throw new ArgumentNullException(
                    nameof(cvParsingEngine));
        }
        [HttpGet]
        public async Task<IActionResult> ApplicationSubmitted(int applicationId)
        {
            var application = await _context.VolunteerApplications
                .Include(x => x.VolunteerOpportunity)
                .FirstOrDefaultAsync(x =>
                    x.VolunteerApplicationId == applicationId);

            if (application == null)
                return NotFound();

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Volunteers()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VolunteerOpportunities()
        {
            var opportunities = await _context.VolunteerOpportunities
                .Include(x => x.Requirements)
                .Include(x => x.Responsibilities)
                .Include(x => x.RequiredSkills)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(opportunities);
        }


        [HttpGet]
        public async Task<IActionResult> Opportunities()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> OpportunityDetails(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .Include(x => x.Requirements)
                .Include(x => x.Responsibilities)
                .Include(x => x.RequiredSkills)
                .FirstOrDefaultAsync(
                    x => x.VolunteerOpportunityId == opportunityId
                );

            if (opportunity == null)
            {
                return NotFound();
            }

            return View(opportunity);
        }


        [HttpGet]
        public async Task<IActionResult> Opportunity(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .Include(x => x.Requirements)
                .Include(x => x.Responsibilities)
                .Include(x => x.RequiredSkills)
                .Include(o => o.ApplicationQuestions)
                    .ThenInclude(q => q.VolunteerQuestion)
                .FirstOrDefaultAsync(
                    x => x.VolunteerOpportunityId == opportunityId
                );

            if (opportunity == null)
            {
                return NotFound();
            }

            return View(opportunity);
        }

        // =========================================================
        // NEW OPPORTUNITY - GET
        // =========================================================
        [Authorize(Roles = "Human Resources Manager")]
        [HttpGet]
        public IActionResult NewOpportunity()
        {
            var viewModel = new NewOpportunityViewModel();

            return View(viewModel);
        }


        // =========================================================
        // NEW OPPORTUNITY - POST
        // =========================================================
        [Authorize(Roles = "Human Resources Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewOpportunity(
            NewOpportunityViewModel viewModel)
        {
            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }


            // =====================================================
            // DATE VALIDATION
            // =====================================================

            if (viewModel.StartDate.HasValue &&
                viewModel.EndDate.HasValue &&
                viewModel.EndDate.Value.Date <
                viewModel.StartDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(viewModel.EndDate),
                    "The end date cannot be before the start date."
                );
            }


            // =====================================================
            // APPLICATION DEADLINE VALIDATION
            // =====================================================

            if (viewModel.ApplicationDeadline.HasValue &&
                viewModel.StartDate.HasValue &&
                viewModel.ApplicationDeadline.Value.Date >
                viewModel.StartDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(viewModel.ApplicationDeadline),
                    "The application deadline cannot be after the opportunity start date."
                );
            }


            // =====================================================
            // TIME VALIDATION
            // =====================================================

            if (viewModel.StartDate.HasValue &&
                viewModel.EndDate.HasValue &&
                viewModel.StartTime.HasValue &&
                viewModel.EndTime.HasValue &&
                viewModel.StartDate.Value.Date ==
                viewModel.EndDate.Value.Date &&
                viewModel.EndTime.Value <=
                viewModel.StartTime.Value)
            {
                ModelState.AddModelError(
                    nameof(viewModel.EndTime),
                    "The end time must be later than the start time."
                );
            }


            // =====================================================
            // VALIDATE AGAIN
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }


            // =====================================================
            // GET CURRENT USER
            // =====================================================

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }


            // =====================================================
            // UPLOAD POSTER IMAGE
            // =====================================================

            string? posterImagePath = null;

            if (viewModel.PosterImage != null &&
                viewModel.PosterImage.Length > 0)
            {
                posterImagePath =
                    await _fileUploadService.UploadFileAsync(
                        viewModel.PosterImage
                    );
            }


            // =====================================================
            // CREATE OPPORTUNITY
            // =====================================================

            var opportunity = new VolunteerOpportunity
            {
                Title = viewModel.Title.Trim(),

                Description = viewModel.Description.Trim(),

                Category = viewModel.Category,

                Area = viewModel.Area,

                CityName = viewModel.CityName?.Trim(),

                GeoNameId = viewModel.GeoNameId,

                Province = viewModel.Province?.Trim(),

                StartDate = viewModel.StartDate,

                EndDate = viewModel.EndDate,

                StartTime = viewModel.StartTime,

                EndTime = viewModel.EndTime,

                VolunteersNeeded = viewModel.VolunteersNeeded,

                VolunteersAccepted = 0,

                MinimumAge = viewModel.MinimumAge,

                ApplicationDeadline =
                    viewModel.ApplicationDeadline,

                PosterImage = posterImagePath,

                Status = VolunteerOpportunityStatus.Draft,

                CreatedAt = DateTime.Now,

                CreatedById = currentUser.Id
            };


            // =====================================================
            // ADD REQUIREMENTS
            // =====================================================

            if (viewModel.Requirements != null)
            {
                foreach (var requirement in viewModel.Requirements
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    opportunity.Requirements.Add(
                        new VolunteerRequirement
                        {
                            Description = requirement.Trim()
                        }
                    );
                }
            }


            // =====================================================
            // ADD RESPONSIBILITIES
            // =====================================================

            if (viewModel.Responsibilities != null)
            {
                foreach (var responsibility in viewModel.Responsibilities
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    opportunity.Responsibilities.Add(
                        new VolunteerResponsibility
                        {
                            Description = responsibility.Trim()
                        }
                    );
                }
            }


            // =====================================================
            // ADD REQUIRED SKILLS
            // =====================================================

            if (viewModel.RequiredSkills != null)
            {
                foreach (var skill in viewModel.RequiredSkills
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    opportunity.RequiredSkills.Add(
                        new VolunteerRequiredSkill
                        {
                            Skill = skill.Trim()
                        }
                    );
                }
            }


            // =====================================================
            // SAVE OPPORTUNITY
            // =====================================================

            _context.VolunteerOpportunities.Add(opportunity);

            await _context.SaveChangesAsync();


            // =====================================================
            // SUCCESS MESSAGE
            // =====================================================

            TempData["SuccessMessage"] =
                "Volunteer opportunity created successfully.";


            // =====================================================
            // REDIRECT
            // =====================================================

            return RedirectToAction(nameof(VolunteerOpportunities));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .FirstOrDefaultAsync(x => x.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();

            opportunity.Status = VolunteerOpportunityStatus.Published;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Volunteer opportunity published successfully.";

            return RedirectToAction(
                nameof(OpportunityDetails),
                new { opportunityId }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .FirstOrDefaultAsync(x => x.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();

            opportunity.Status = VolunteerOpportunityStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Volunteer opportunity cancelled successfully.";

            return RedirectToAction(
                nameof(OpportunityDetails),
                new { opportunityId }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .FirstOrDefaultAsync(x => x.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();

            opportunity.Status = VolunteerOpportunityStatus.Closed;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Volunteer opportunity closed successfully.";

            return RedirectToAction(
                nameof(OpportunityDetails),
                new { opportunityId }
            );
        }

        // ============================================================
        // APPLY - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Apply(int opportunityId)
        {
            // ========================================================
            // LOAD OPPORTUNITY
            // ========================================================

            var opportunity =
                await _context.VolunteerOpportunities
                    .FirstOrDefaultAsync(o =>
                        o.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();


            // ========================================================
            // CREATE VIEW MODEL
            // ========================================================

            var viewModel =
                new VolunteerApplicationViewModel
                {
                    VolunteerOpportunityId =
                        opportunity.VolunteerOpportunityId,

                    OpportunityTitle =
                        opportunity.Title
                };


            // ========================================================
            // RETURN VIEW
            // ========================================================

            return View(viewModel);
        }


        // ============================================================
        // APPLY - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            VolunteerApplicationViewModel viewModel)
        {
            // ============================================================
            // LOAD OPPORTUNITY
            // ============================================================

            var opportunity =
                await _context.VolunteerOpportunities
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerOpportunityId ==
                        viewModel.VolunteerOpportunityId);

            if (opportunity == null)
                return NotFound();


            // ============================================================
            // SET OPPORTUNITY TITLE
            // ============================================================

            viewModel.OpportunityTitle =
                opportunity.Title;


            // ============================================================
            // VALIDATE CV
            // ============================================================

            string? cvFilePath = null;

            if (viewModel.CV == null ||
                viewModel.CV.Length == 0)
            {
                ModelState.AddModelError(
                    "CV",
                    "Please upload your CV.");
            }
            else
            {
                /*
                 * Azure Document Intelligence Layout supports
                 * PDF and DOCX for this CV workflow.
                 *
                 * We intentionally remove .doc because Azure's
                 * current Layout model does not accept legacy .doc.
                 */

                var allowedExtensions =
                    new[]
                    {
                ".pdf",
                ".docx"
                    };

                var extension =
                    Path.GetExtension(
                        viewModel.CV.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "CV",
                        "Please upload a PDF or DOCX file.");
                }

                const long maxFileSize =
                    5 * 1024 * 1024;

                if (viewModel.CV.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        "CV",
                        "Your CV must be smaller than 5 MB.");
                }
            }


            // ============================================================
            // RETURN FORM IF VALIDATION FAILED
            // ============================================================

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }


            // ============================================================
            // NORMALISE EMAIL
            // ============================================================

            var email =
                viewModel.Email
                    .Trim()
                    .ToLowerInvariant();


            // ============================================================
            // FIND EXISTING ACCOUNT
            // ============================================================

            var existingUser =
                await _userManager
                    .FindByEmailAsync(email);

            string applicantId;

            bool accountCreated = false;

            string? generatedPassword = null;

            string? confirmationUrl = null;


            // ============================================================
            // CREATE APPLICANT ACCOUNT
            // ============================================================

            if (existingUser == null)
            {
                var applicant =
                    new Applicant
                    {
                        UserName =
                            email,

                        Email =
                            email,

                        FirstName =
                            viewModel.FirstName.Trim(),

                        LastName =
                            viewModel.LastName.Trim(),

                        PhoneNumber =
                            viewModel.PhoneNumber.Trim(),

                        DateOfBirth =
                            viewModel.DateOfBirth!.Value,

                        ProfilePicture =
                            "Images/default_profile_image.jpg",

                        CreatedDateTime =
                            DateTime.Now,

                        ModifiedDateTime =
                            DateTime.Now,

                        CreatedBy =
                            "Anonymous Application",

                        ModifiedBy =
                            "Anonymous Application",


                        // ------------------------------------------------
                        // PROFILE FIELDS
                        // ------------------------------------------------

                        ProfessionalTitle =
                            viewModel.ProfessionalTitle?
                                .Trim(),

                        ProfessionalSummary =
                            viewModel.ProfessionalSummary?
                                .Trim(),

                        CareerObjective =
                            viewModel.CareerObjective?
                                .Trim(),

                        Address =
                            viewModel.Address?
                                .Trim(),

                        City =
                            viewModel.City?
                                .Trim(),

                        Province =
                            viewModel.Province?
                                .Trim(),

                        Country =
                            viewModel.Country?
                                .Trim(),

                        PostalCode =
                            viewModel.PostalCode?
                                .Trim(),

                        PreferredWorkType =
                            viewModel.PreferredWorkType?
                                .Trim(),

                        PreferredLocation =
                            viewModel.PreferredLocation?
                                .Trim(),

                        WillingToRelocate =
                            viewModel.WillingToRelocate,

                        AvailableForVolunteerWork =
                            viewModel.AvailableForVolunteerWork,

                        LinkedInUrl =
                            viewModel.LinkedInUrl?
                                .Trim(),

                        PortfolioUrl =
                            viewModel.PortfolioUrl?
                                .Trim(),

                        GitHubUrl =
                            viewModel.GitHubUrl?
                                .Trim(),

                        IsProfileComplete =
                            false,

                        ProfileLastUpdated =
                            DateTime.Now
                    };


                // ========================================================
                // GENERATE TEMPORARY PASSWORD
                // ========================================================

                generatedPassword =
                    _randomPasswordGeneratorService
                        .GenerateRandomPassword(12);


                // ========================================================
                // CREATE IDENTITY ACCOUNT
                // ========================================================

                var createResult =
                    await _userManager.CreateAsync(
                        applicant,
                        generatedPassword);

                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    return View(viewModel);
                }


                // ========================================================
                // ADD APPLICANT ROLE
                // ========================================================

                var roleResult =
                    await _userManager.AddToRoleAsync(
                        applicant,
                        "Applicant");

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    return View(viewModel);
                }


                // ========================================================
                // EMAIL CONFIRMATION
                // ========================================================

                var confirmationToken =
                    await _userManager
                        .GenerateEmailConfirmationTokenAsync(
                            applicant);

                var encodedConfirmationToken =
                    WebEncoders.Base64UrlEncode(
                        Encoding.UTF8.GetBytes(
                            confirmationToken));

                confirmationUrl =
                    Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new
                        {
                            area = "Identity",
                            userId = applicant.Id,
                            code =
                                encodedConfirmationToken
                        },
                        protocol: Request.Scheme);


                applicantId =
                    applicant.Id;

                accountCreated =
                    true;
            }
            else
            {
                // ========================================================
                // EXISTING APPLICANT
                // ========================================================

                applicantId =
                    existingUser.Id;
            }


            // ============================================================
            // GET ACTUAL APPLICANT
            // ============================================================

            var applicantProfile =
                await _userManager.Users
                    .OfType<Applicant>()
                    .FirstOrDefaultAsync(x =>
                        x.Id == applicantId);

            if (applicantProfile == null)
            {
                ModelState.AddModelError(
                    "",
                    "Applicant profile could not be loaded.");

                return View(viewModel);
            }


            // ============================================================
            // UPLOAD CV
            // ============================================================

            if (viewModel.CV != null &&
                viewModel.CV.Length > 0)
            {
                cvFilePath =
                    await _fileUploadService
                        .UploadFileAsync(
                            viewModel.CV);
            }


            // ============================================================
            // AZURE DOCUMENT INTELLIGENCE
            // ============================================================

            ParsedCv? parsedCv = null;

            try
            {
                if (viewModel.CV != null &&
                    viewModel.CV.Length > 0)
                {
                    var azureResult =
                        await _azureDocumentIntelligenceCvService
                            .AnalyzeAsync(
                                viewModel.CV);
                }
            }
            catch (Exception ex)
            {
                /*
                 * Do not destroy the application simply because
                 * CV extraction failed.
                 *
                 * The application can still be submitted and
                 * the CV can be processed again later.
                 */

                parsedCv =
                    new ParsedCv
                    {
                        ExtractionSuccessful =
                            false,

                        ExtractionMessage =
                            "CV extraction failed: " +
                            ex.Message,

                        RawText =
                            null
                    };
            }


            // ============================================================
            // POPULATE APPLICANT PROFILE FROM CV
            // ============================================================

            if (parsedCv != null &&
                parsedCv.ExtractionSuccessful)
            {
                // --------------------------------------------------------
                // PERSONAL INFORMATION
                // --------------------------------------------------------

                /*
                 * We only replace these when Azure actually found
                 * a value.
                 */

                if (!string.IsNullOrWhiteSpace(
                        parsedCv.FullName))
                {
                    var nameParts =
                        parsedCv.FullName
                            .Trim()
                            .Split(
                                ' ',
                                StringSplitOptions.RemoveEmptyEntries);

                    if (nameParts.Length > 0 &&
                        string.IsNullOrWhiteSpace(
                            applicantProfile.FirstName))
                    {
                        applicantProfile.FirstName =
                            nameParts[0];
                    }

                    if (nameParts.Length > 1 &&
                        string.IsNullOrWhiteSpace(
                            applicantProfile.LastName))
                    {
                        applicantProfile.LastName =
                            string.Join(
                                " ",
                                nameParts.Skip(1));
                    }
                }


                if (!string.IsNullOrWhiteSpace(
                        parsedCv.Email))
                {
                    applicantProfile.Email =
                        parsedCv.Email.Trim();
                }


                if (!string.IsNullOrWhiteSpace(
                        parsedCv.PhoneNumber))
                {
                    applicantProfile.PhoneNumber =
                        parsedCv.PhoneNumber.Trim();
                }


                // --------------------------------------------------------
                // PROFILE INFORMATION
                // --------------------------------------------------------

                /*
                 * These values depend on the semantic CV parser
                 * that converts Azure's extracted document text
                 * into ParsedCv.
                 */

                if (string.IsNullOrWhiteSpace(
                        applicantProfile.ProfessionalSummary))
                {
                    applicantProfile.ProfessionalSummary =
                        ExtractProfessionalSummary(parsedCv);
                }


                if (string.IsNullOrWhiteSpace(
                        applicantProfile.ProfessionalTitle))
                {
                    applicantProfile.ProfessionalTitle =
                        ExtractProfessionalTitle(parsedCv);
                }


                // --------------------------------------------------------
                // LOCATION
                // --------------------------------------------------------

                var location =
                    ExtractLocation(parsedCv);

                if (string.IsNullOrWhiteSpace(
                        applicantProfile.City))
                {
                    applicantProfile.City =
                        location.City;
                }

                if (string.IsNullOrWhiteSpace(
                        applicantProfile.Province))
                {
                    applicantProfile.Province =
                        location.Province;
                }

                if (string.IsNullOrWhiteSpace(
                        applicantProfile.Country))
                {
                    applicantProfile.Country =
                        location.Country;
                }


                // --------------------------------------------------------
                // CV METADATA
                // --------------------------------------------------------

                applicantProfile.CurrentCvFileName =
                    Path.GetFileName(
                        viewModel.CV!.FileName);

                applicantProfile.CurrentCvFilePath =
                    cvFilePath;

                applicantProfile.CurrentCvFileType =
                    Path.GetExtension(
                        viewModel.CV.FileName)
                        .ToLowerInvariant();

                applicantProfile.CvUploadedDate =
                    DateTime.Now;

                applicantProfile.CvLastParsedDate =
                    DateTime.Now;

                applicantProfile.CvParsingSuccessful =
                    true;

                applicantProfile.CvParsingMessage =
                    "CV successfully analysed.";

                applicantProfile.ProfileLastUpdated =
                    DateTime.Now;
            }
            else
            {
                applicantProfile.CurrentCvFileName =
                    Path.GetFileName(
                        viewModel.CV!.FileName);

                applicantProfile.CurrentCvFilePath =
                    cvFilePath;

                applicantProfile.CurrentCvFileType =
                    Path.GetExtension(
                        viewModel.CV.FileName)
                        .ToLowerInvariant();

                applicantProfile.CvUploadedDate =
                    DateTime.Now;

                applicantProfile.CvLastParsedDate =
                    DateTime.Now;

                applicantProfile.CvParsingSuccessful =
                    false;

                applicantProfile.CvParsingMessage =
                    parsedCv?.ExtractionMessage ??
                    "CV could not be analysed.";

                applicantProfile.ProfileLastUpdated =
                    DateTime.Now;
            }


            // ============================================================
            // PROFILE COMPLETION
            // ============================================================

            applicantProfile.IsProfileComplete =
                !string.IsNullOrWhiteSpace(
                    applicantProfile.FirstName)
                &&
                !string.IsNullOrWhiteSpace(
                    applicantProfile.LastName)
                &&
                !string.IsNullOrWhiteSpace(
                    applicantProfile.Email)
                &&
                !string.IsNullOrWhiteSpace(
                    applicantProfile.PhoneNumber)
                &&
                !string.IsNullOrWhiteSpace(
                    applicantProfile.CurrentCvFilePath);


            // ============================================================
            // SAVE APPLICANT PROFILE
            // ============================================================

            applicantProfile.ModifiedDateTime =
                DateTime.Now;

            applicantProfile.ModifiedBy =
                "CV Profile Extraction";

            _context.Update(
                applicantProfile);

            await _context.SaveChangesAsync();


            // ============================================================
            // SAVE PARSED CV
            // ============================================================

            if (parsedCv != null)
            {
                parsedCv.VolunteerApplicationId =
                    0;

                /*
                 * IMPORTANT:
                 *
                 * If ParsedCv uses ApplicantId,
                 * set it here:
                 *
                 * parsedCv.ApplicantId =
                 *     applicantProfile.Id;
                 *
                 * The parsed CV should remain the structured
                 * representation of what Azure found in the CV.
                 */

                _context.Add(parsedCv);

                await _context.SaveChangesAsync();
            }


            // ============================================================
            // GENERATE APPLICATION REFERENCE
            // ============================================================

            var applicationReference =
                GenerateApplicationReference();


            // ============================================================
            // CREATE VOLUNTEER APPLICATION
            // ============================================================

            var application =
                new VolunteerApplication
                {
                    VolunteerOpportunityId =
                        opportunity.VolunteerOpportunityId,

                    ApplicantId =
                        applicantId,

                    Reference =
                        applicationReference,

                    CoverMessage =
                        viewModel.CoverMessage.Trim(),

                    AppliedDate =
                        DateTime.Now,

                    Status =
                        VolunteerApplicationStatus.Pending,

                    CVFilePath =
                        cvFilePath,

                    CVFileName =
                        Path.GetFileName(
                            viewModel.CV!.FileName),

                    CVUploadedDate =
                        DateTime.Now
                };


            _context.VolunteerApplications.Add(
                application);

            await _context.SaveChangesAsync();


            // ============================================================
            // TRACK APPLICATION URL
            // ============================================================

            var statusUrl =
                Url.Action(
                    "TrackApplication",
                    "VolunteerApplications",
                    new
                    {
                        applicationId =
                            application.VolunteerApplicationId
                    },
                    Request.Scheme);


            // ============================================================
            // SEND ACCOUNT EMAIL
            // ============================================================

            if (accountCreated &&
                !string.IsNullOrWhiteSpace(
                    generatedPassword))
            {
                var emailBody =
                    BuildApplicantAccountEmail(
                        applicantProfile.FirstName?.Trim()
                            ?? viewModel.FirstName.Trim(),

                        applicantProfile.LastName?.Trim()
                            ?? viewModel.LastName.Trim(),

                        email,

                        generatedPassword,

                        opportunity.Title,

                        application.Reference,

                        statusUrl,

                        confirmationUrl);


                await _emailService.SendEmailAsync(
                    email,
                    "Welcome to SwiftCare - Your Applicant Account",
                    emailBody,
                    "SwiftCare");


                TempData["SuccessMessage"] =
                    "Your application was submitted successfully. " +
                    "Your CV was analysed and your SwiftCare applicant " +
                    "profile was created.";
            }
            else
            {
                var emailBody =
                    BuildExistingApplicantEmail(
                        applicantProfile.FirstName?.Trim()
                            ?? viewModel.FirstName.Trim(),

                        opportunity.Title,

                        application.Reference,

                        statusUrl);


                await _emailService.SendEmailAsync(
                    email,
                    "SwiftCare - Volunteer Application Submitted",
                    emailBody,
                    "SwiftCare");


                TempData["SuccessMessage"] =
                    "Your SwiftCare account already exists. " +
                    "Your volunteer application was submitted successfully " +
                    "and your CV profile was updated.";
            }


            // ============================================================
            // REDIRECT
            // ============================================================

            return RedirectToAction(
                "ApplicationSubmitted",
                new
                {
                    applicationId =
                        application.VolunteerApplicationId
                });
        }


        // ============================================================
        // GENERATE APPLICATION REFERENCE
        // Example: 260907FTHC
        // ============================================================

        private string GenerateApplicationReference()
        {
            const string characters =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var randomPart =
                new string(
                    Enumerable.Range(0, 4)
                        .Select(_ =>
                            characters[
                                Random.Shared.Next(characters.Length)])
                        .ToArray());

            return $"{DateTime.Now:yyMMdd}{randomPart}";
        }


        // ============================================================
        // NEW APPLICANT ACCOUNT EMAIL
        // ============================================================

        private string BuildApplicantAccountEmail(
            string firstName,
            string lastName,
            string email,
            string password,
            string opportunityTitle,
            string applicationReference,
            string? statusUrl,
            string? confirmationUrl)
        {
            var fullName =
                $"{firstName} {lastName}";

            var safeName =
                WebUtility.HtmlEncode(fullName);

            var safeEmail =
                WebUtility.HtmlEncode(email);

            var safePassword =
                WebUtility.HtmlEncode(password);

            var safeOpportunity =
                WebUtility.HtmlEncode(opportunityTitle);

            var safeReference =
                WebUtility.HtmlEncode(applicationReference);

            var safeStatusUrl =
                WebUtility.HtmlEncode(
                    statusUrl ?? "#");

            var safeConfirmationUrl =
                WebUtility.HtmlEncode(
                    confirmationUrl ?? "#");

            return $@"
<!DOCTYPE html>

<html>

<head>

    <meta charset='UTF-8'>

    <meta name='viewport'
          content='width=device-width, initial-scale=1.0'>

    <title>SwiftCare</title>

</head>

<body style='margin:0;
             padding:0;
             background:#eef5f0;
             font-family:Arial,Helvetica,sans-serif;
             color:#173329;'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='background:#eef5f0;
              padding:35px 15px;'>

<tr>

<td align='center'>

<table width='620'
       cellpadding='0'
       cellspacing='0'
       style='max-width:620px;
              width:100%;
              background:#ffffff;
              border-radius:20px;
              overflow:hidden;
              box-shadow:0 12px 35px rgba(23,69,44,0.12);'>

<!-- =========================================================
     HEADER
     ========================================================= -->

<tr>

<td style='background:#176b3a;
           padding:32px;
           text-align:center;'>

<div style='display:inline-block;
            background:#b8ff00;
            color:#0b3f22;
            padding:11px 22px;
            border-radius:12px;
            font-size:22px;
            font-weight:800;
            letter-spacing:1px;'>

    SWIFTCARE

</div>

<p style='margin:14px 0 0;
          color:#ffffff;
          font-size:13px;'>

    Volunteer Care &bull; Community &bull; Compassion

</p>

</td>

</tr>


<!-- =========================================================
     CONTENT
     ========================================================= -->

<tr>

<td style='padding:42px 38px;'>

<h1 style='margin:0 0 12px;
           color:#0f542c;
           font-size:28px;'>

    Welcome to SwiftCare, {safeName}!

</h1>


<p style='font-size:16px;
          line-height:1.7;
          color:#40564b;'>

    Thank you for applying for the

    <strong style='color:#176b3a;'>

        {safeOpportunity}

    </strong>

    volunteer opportunity.

</p>


<p style='font-size:15px;
          line-height:1.7;
          color:#53665d;'>

    Your application has been successfully received,
    and we have created your SwiftCare applicant account.

</p>


<!-- =========================================================
     APPLICATION REFERENCE
     ========================================================= -->

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='margin:28px 0;
              background:#f7faf8;
              border:1px solid #dce9df;
              border-radius:14px;'>

<tr>

<td style='padding:24px;
           text-align:center;'>

<div style='font-size:11px;
            font-weight:bold;
            color:#61736a;
            text-transform:uppercase;
            letter-spacing:1px;'>

    Application Reference

</div>


<div style='margin-top:9px;
            font-family:monospace;
            font-size:24px;
            font-weight:800;
            color:#176b3a;
            letter-spacing:2px;'>

    {safeReference}

</div>


<p style='margin:10px 0 0;
          font-size:12px;
          line-height:1.5;
          color:#61736a;'>

    Please keep this reference safe.
    You can use it when tracking your application.

</p>

</td>

</tr>

</table>


<!-- =========================================================
     LOGIN CARD
     ========================================================= -->

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='margin:28px 0;
              background:#f3f8f4;
              border:1px solid #dce9df;
              border-radius:14px;'>

<tr>

<td style='padding:24px;'>

<div style='font-size:11px;
            font-weight:bold;
            color:#61736a;
            text-transform:uppercase;
            letter-spacing:1px;'>

    Login Email

</div>


<div style='margin-top:7px;
            margin-bottom:20px;
            font-size:16px;
            font-weight:bold;
            color:#173329;'>

    {safeEmail}

</div>


<div style='font-size:11px;
            font-weight:bold;
            color:#61736a;
            text-transform:uppercase;
            letter-spacing:1px;'>

    Temporary Password

</div>


<div style='margin-top:7px;
            padding:14px;
            background:#ffffff;
            border:1px dashed #a9c3b2;
            border-radius:9px;
            font-family:monospace;
            font-size:17px;
            font-weight:bold;
            color:#0f542c;
            letter-spacing:1px;'>

    {safePassword}

</div>

</td>

</tr>

</table>


<!-- =========================================================
     CONFIRM ACCOUNT
     ========================================================= -->

<div style='background:#f3f8f4;
            border:1px solid #dce9df;
            border-radius:14px;
            padding:25px;
            margin:30px 0;
            text-align:center;'>

<div style='font-size:20px;
            font-weight:bold;
            color:#0f542c;
            margin-bottom:10px;'>

    Confirm Your Email Address

</div>


<p style='margin:0 0 20px;
          font-size:14px;
          line-height:1.7;
          color:#53665d;'>

    Please confirm your email address before signing
    in to your new SwiftCare applicant account.

</p>


<a href='{safeConfirmationUrl}'
   style='display:inline-block;
          background:#b8ff00;
          color:#0b3f22;
          text-decoration:none;
          padding:15px 28px;
          border-radius:10px;
          font-size:15px;
          font-weight:bold;'>

    Confirm My Account

</a>

</div>


<!-- =========================================================
     SECURITY
     ========================================================= -->

<div style='background:#fff9e8;
            border-left:4px solid #b8ff00;
            padding:15px 18px;
            margin-bottom:30px;'>

<strong style='color:#0f542c;'>

    Important

</strong>


<p style='margin:7px 0 0;
          font-size:13px;
          line-height:1.6;
          color:#53665d;'>

    This is a temporary password.
    Please keep it secure and change it after signing in.

</p>

</div>


<!-- =========================================================
     TRACK APPLICATION
     ========================================================= -->

<div style='text-align:center;
            margin:32px 0;'>

<a href='{safeStatusUrl}'
   style='display:inline-block;
          background:#176b3a;
          color:#ffffff;
          text-decoration:none;
          padding:16px 30px;
          border-radius:10px;
          font-size:15px;
          font-weight:bold;'>

    Track Application Status

</a>

</div>


<p style='margin-top:30px;
          font-size:14px;
          line-height:1.7;
          color:#53665d;'>

    We appreciate your willingness to support SwiftCare
    and make a difference in the lives of the people
    we serve.

</p>


<p style='margin-top:28px;
          font-size:14px;
          line-height:1.7;
          color:#173329;'>

    Kind regards,<br>

    <strong>SwiftCare Volunteer Team</strong>

</p>

</td>

</tr>


<!-- =========================================================
     FOOTER
     ========================================================= -->

<tr>

<td style='background:#0b3f22;
           padding:24px;
           text-align:center;
           color:#dce9df;
           font-size:12px;
           line-height:1.6;'>

<strong style='color:#b8ff00;
               font-size:16px;'>

    SWIFTCARE

</strong>

<br>

Your community. Your care. Your impact.

</td>

</tr>

</table>

</td>

</tr>

</table>

</body>

</html>";
        }

        //HELPERS

        // =========================================================
        // SEARCH SOUTH AFRICAN CITIES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SearchCities(string? term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
                {
                    return Json(new List<object>());
                }

                var username = _configuration["GeoNames:Username"];

                if (string.IsNullOrWhiteSpace(username))
                {
                    return StatusCode(
                        500,
                        new
                        {
                            message = "GeoNames username is not configured."
                        }
                    );
                }

                var searchTerm = Uri.EscapeDataString(term.Trim());

                var url =
                    $"https://secure.geonames.org/searchJSON" +
                    $"?country=ZA" +
                    $"&featureClass=P" +
                    $"&cities=cities1000" +
                    $"&name_startsWith={searchTerm}" +
                    $"&maxRows=10" +
                    $"&username={Uri.EscapeDataString(username)}";

                var client = _httpClientFactory.CreateClient();

                client.Timeout = TimeSpan.FromSeconds(10);

                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        new
                        {
                            message = "Unable to contact the GeoNames service."
                        }
                    );
                }

                var json = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty(
                        "geonames",
                        out var cities))
                {
                    return Json(new List<object>());
                }

                var results = new List<object>();

                foreach (var city in cities.EnumerateArray())
                {
                    string? name = null;
                    string? province = null;
                    int? geoNameId = null;

                    if (city.TryGetProperty("name", out var nameElement))
                    {
                        name = nameElement.GetString();
                    }

                    if (city.TryGetProperty("adminName1", out var provinceElement))
                    {
                        province = provinceElement.GetString();
                    }

                    if (city.TryGetProperty("geonameId", out var idElement))
                    {
                        if (idElement.TryGetInt32(out var parsedId))
                        {
                            geoNameId = parsedId;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    results.Add(new
                    {
                        cityName = name,
                        geoNameId = geoNameId,
                        province = province
                    });
                }

                return Json(results);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(
                    504,
                    new
                    {
                        message = "The city search service timed out."
                    }
                );
            }
            catch (JsonException)
            {
                return StatusCode(
                    502,
                    new
                    {
                        message = "Invalid response received from the city service."
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Unable to search cities.",
                        error = ex.Message
                    }
                );
            }
        }

        private string BuildExistingApplicantEmail(
            string firstName,
            string opportunityTitle,
            string applicationReference,
            string? statusUrl)
        {
            var safeName =
                WebUtility.HtmlEncode(firstName);

            var safeOpportunity =
                WebUtility.HtmlEncode(opportunityTitle);

            var safeReference =
                WebUtility.HtmlEncode(applicationReference);

            var safeStatusUrl =
                WebUtility.HtmlEncode(statusUrl ?? "#");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport'
          content='width=device-width, initial-scale=1.0'>
    <title>SwiftCare</title>
</head>

<body style='margin:0;
             padding:0;
             background:#eef5f0;
             font-family:Arial,Helvetica,sans-serif;
             color:#173329;'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='background:#eef5f0;
              padding:35px 15px;'>

<tr>
<td align='center'>

<table width='620'
       cellpadding='0'
       cellspacing='0'
       style='max-width:620px;
              width:100%;
              background:#ffffff;
              border-radius:20px;
              overflow:hidden;
              box-shadow:0 12px 35px rgba(23,69,44,0.12);'>

<!-- HEADER -->
<tr>
<td style='background:#176b3a;
           padding:32px;
           text-align:center;'>

<div style='display:inline-block;
            background:#b8ff00;
            color:#0b3f22;
            padding:11px 22px;
            border-radius:12px;
            font-size:22px;
            font-weight:800;
            letter-spacing:1px;'>
    SWIFTCARE
</div>

<p style='margin:14px 0 0;
          color:#ffffff;
          font-size:13px;'>
    Volunteer Care &bull; Community &bull; Compassion
</p>

</td>
</tr>

<!-- CONTENT -->
<tr>
<td style='padding:42px 38px;'>

<h1 style='margin:0 0 15px;
           color:#0f542c;
           font-size:27px;'>
    Application Received, {safeName}
</h1>

<p style='font-size:15px;
          line-height:1.7;
          color:#53665d;'>
    Thank you for applying for the
    <strong style='color:#176b3a;'>
        {safeOpportunity}
    </strong>
    volunteer opportunity.
</p>

<!-- EXISTING ACCOUNT NOTICE -->
<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='margin:28px 0;
              background:#f3f8f4;
              border:1px solid #dce9df;
              border-radius:14px;'>

<tr>
<td style='padding:24px;'>

<div style='font-size:13px;
            line-height:1.7;
            color:#53665d;'>

<strong style='display:block;
               margin-bottom:10px;
               font-size:17px;
               color:#0f542c;'>
    Your SwiftCare account already exists
</strong>

You can continue using your existing SwiftCare
login details to access your account and monitor
your application.

</div>

</td>
</tr>

</table>

<!-- APPLICATION REFERENCE -->
<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='margin:28px 0;
              background:#f7faf8;
              border:1px solid #dce9df;
              border-radius:14px;'>

<tr>
<td style='padding:24px;
           text-align:center;'>

<div style='font-size:11px;
            font-weight:bold;
            color:#61736a;
            text-transform:uppercase;
            letter-spacing:1px;'>
    Application Reference
</div>

<div style='margin-top:9px;
            font-family:monospace;
            font-size:24px;
            font-weight:800;
            color:#176b3a;
            letter-spacing:2px;'>
    {safeReference}
</div>

<p style='margin:10px 0 0;
          font-size:12px;
          line-height:1.5;
          color:#61736a;'>
    Please keep this reference safe.
    You can use it when tracking your application.
</p>

</td>
</tr>

</table>

<!-- TRACK APPLICATION -->
<div style='text-align:center;
            margin:32px 0;'>

<a href='{safeStatusUrl}'
   style='display:inline-block;
          background:#176b3a;
          color:#ffffff;
          text-decoration:none;
          padding:16px 30px;
          border-radius:10px;
          font-size:15px;
          font-weight:bold;'>
    Track Application Status
</a>

</div>

<!-- APPLICATION STATUS -->
<div style='background:#f3f8f4;
            border:1px solid #dce9df;
            border-radius:12px;
            padding:18px;
            margin-top:25px;'>

<p style='margin:0;
          font-size:13px;
          line-height:1.7;
          color:#53665d;'>
    We will review your application and update its
    status through your SwiftCare account.
</p>

</div>

<p style='margin-top:30px;
          font-size:14px;
          line-height:1.7;
          color:#53665d;'>
    We appreciate your willingness to support SwiftCare
    and make a difference in the lives of the people
    we serve.
</p>

<p style='margin-top:28px;
          font-size:14px;
          line-height:1.7;
          color:#173329;'>
    Kind regards,<br>
    <strong>SwiftCare Volunteer Team</strong>
</p>

</td>
</tr>

<!-- FOOTER -->
<tr>
<td style='background:#0b3f22;
           padding:24px;
           text-align:center;
           color:#dce9df;
           font-size:12px;'>

<strong style='color:#b8ff00;
               font-size:16px;'>
    SWIFTCARE
</strong>

<br>

Your community. Your care. Your impact.

</td>
</tr>

</table>

</td>
</tr>

</table>

</body>
</html>";
        }



        private string? ExtractProfessionalSummary(
        ParsedCv parsedCv)
        {
            if (string.IsNullOrWhiteSpace(
                    parsedCv.RawText))
            {
                return null;
            }

            /*
             * We do not blindly copy the entire CV.
             *
             * The semantic CV parser should eventually expose
             * a Summary property. Until then, this method can
             * look for the summary/profile section.
             */

            var lines =
                parsedCv.RawText
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

            var summaryHeaders =
                new[]
                {
            "professional summary",
            "profile",
            "professional profile",
            "personal profile",
            "summary",
            "about me"
                };

            for (var i = 0; i < lines.Count; i++)
            {
                if (!summaryHeaders.Any(header =>
                        lines[i].Equals(
                            header,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var summaryLines =
                    new List<string>();

                for (var j = i + 1;
                     j < lines.Count;
                     j++)
                {
                    var current =
                        lines[j];

                    if (IsCvSectionHeading(current))
                        break;

                    summaryLines.Add(current);

                    if (summaryLines.Count >= 8)
                        break;
                }

                if (summaryLines.Count > 0)
                {
                    return string.Join(
                        " ",
                        summaryLines);
                }
            }

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeCv(
    IFormFile CV,
    CancellationToken cancellationToken)
        {
            if (CV == null || CV.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select a CV."
                });
            }

            try
            {
                // =========================================================
                // AZURE DOCUMENT INTELLIGENCE
                // =========================================================

                var azureResult =
                    await _azureDocumentIntelligenceCvService
                        .AnalyzeAsync(
                            CV,
                            cancellationToken);

                if (!azureResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            azureResult.ErrorMessage
                            ?? "Azure could not analyse the CV."
                    });
                }


                // =========================================================
                // PARSE AZURE RESULT
                // =========================================================

                var parsedCv =
                    await _cvParsingEngine.ParseAsync(
                        azureResult,
                        0);


                // =========================================================
                // RETURN STRUCTURED DATA TO JAVASCRIPT
                // =========================================================

                return Json(new
                {
                    success = true,

                    fullName = parsedCv.FullName,

                    email = parsedCv.Email,

                    phoneNumber = parsedCv.PhoneNumber,

                    professionalTitle =
                        parsedCv.ProfessionalTitle,

                    professionalSummary =
                        parsedCv.ProfessionalSummary,

                    careerObjective =
                        parsedCv.CareerObjective,

                    address =
                        parsedCv.Address,

                    city =
                        parsedCv.City,

                    province =
                        parsedCv.Province,

                    country =
                        parsedCv.Country,

                    linkedInUrl =
                        parsedCv.LinkedInUrl,

                    portfolioUrl =
                        parsedCv.PortfolioUrl,

                    gitHubUrl =
                        parsedCv.GitHubUrl
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message =
                            "An error occurred while analysing your CV: "
                            + ex.Message
                    });
            }
        }
        private string? ExtractProfessionalTitle(
            ParsedCv parsedCv)
        {
            if (parsedCv.Experience == null ||
                parsedCv.Experience.Count == 0)
            {
                return null;
            }

            var latestExperience =
                parsedCv.Experience
                    .FirstOrDefault();

            return latestExperience?
                .Position?
                .Trim();
        }


        private (string? City,
                 string? Province,
                 string? Country)
            ExtractLocation(
                ParsedCv parsedCv)
        {
            if (string.IsNullOrWhiteSpace(
                    parsedCv.RawText))
            {
                return (null, null, null);
            }

            var text =
                parsedCv.RawText;

            string? city = null;
            string? province = null;
            string? country = null;

            if (text.Contains(
                    "Durban",
                    StringComparison.OrdinalIgnoreCase))
            {
                city = "Durban";
            }

            if (text.Contains(
                    "KwaZulu-Natal",
                    StringComparison.OrdinalIgnoreCase))
            {
                province = "KwaZulu-Natal";
            }

            if (text.Contains(
                    "South Africa",
                    StringComparison.OrdinalIgnoreCase))
            {
                country = "South Africa";
            }

            return (
                city,
                province,
                country
            );
        }


        private bool IsCvSectionHeading(
            string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var headings =
                new[]
                {
            "skills",
            "education",
            "experience",
            "work experience",
            "employment history",
            "professional experience",
            "certifications",
            "certificates",
            "languages",
            "projects",
            "references",
            "awards",
            "achievements",
            "qualifications"
                };

            return headings.Any(x =>
                line.Equals(
                    x,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
