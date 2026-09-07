using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Build.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.ViewModels;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
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

        public VolunteerOpportunitiesController(ILogger<VolunteerOpportunitiesController> logger,
                        SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            FileUploadService fileUploadService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            EmailService emailService,
            RandomPasswordGeneratorService randomPasswordGeneratorService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _emailService = emailService;
            _randomPasswordGeneratorService = randomPasswordGeneratorService;
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

        [HttpGet]
        public async Task<IActionResult> NewOpportunity()
        {
            var viewModel = new NewOpportunityViewModel();

            viewModel.AvailableQuestions =
                await _context.VolunteerQuestions
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.DisplayOrder)
                    .ThenBy(q => q.VolunteerQuestionId)
                    .Select(q => new VolunteerQuestionSelectionViewModel
                    {
                        VolunteerQuestionId = q.VolunteerQuestionId,
                        Question = q.Question,
                        QuestionType = q.QuestionType,
                        IsRequired = q.IsRequired,
                        Category = q.Category,
                        Area = q.Area
                    })
                    .ToListAsync();

            return View(viewModel);
        }


        // =========================================================
        // NEW OPPORTUNITY - POST
        // =========================================================

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
                await LoadAvailableQuestions(viewModel);
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
                await LoadAvailableQuestions(viewModel);
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

                CreatedAt = DateTime.UtcNow,

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
            // ADD SELECTED APPLICATION QUESTIONS
            // =====================================================

            if (viewModel.SelectedQuestionIds != null &&
                viewModel.SelectedQuestionIds.Any())
            {
                var selectedQuestions =
                    await _context.VolunteerQuestions
                        .Where(q =>
                            q.IsActive &&
                            viewModel.SelectedQuestionIds
                                .Contains(q.VolunteerQuestionId))
                        .OrderBy(q => q.DisplayOrder)
                        .ThenBy(q => q.VolunteerQuestionId)
                        .ToListAsync();

                var displayOrder = 1;

                foreach (var question in selectedQuestions)
                {
                    _context.VolunteerOpportunityQuestions.Add(
                        new VolunteerOpportunityQuestion
                        {
                            VolunteerOpportunityId =
                                opportunity.VolunteerOpportunityId,

                            VolunteerQuestionId =
                                question.VolunteerQuestionId,

                            DisplayOrder =
                                displayOrder++
                        }
                    );
                }

                await _context.SaveChangesAsync();
            }


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


        // =========================================================
        // LOAD AVAILABLE QUESTIONS
        // =========================================================

        private async Task LoadAvailableQuestions(
            NewOpportunityViewModel viewModel)
        {
            viewModel.AvailableQuestions =
                await _context.VolunteerQuestions
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.DisplayOrder)
                    .ThenBy(q => q.VolunteerQuestionId)
                    .Select(q => new VolunteerQuestionSelectionViewModel
                    {
                        VolunteerQuestionId = q.VolunteerQuestionId,
                        Question = q.Question,
                        QuestionType = q.QuestionType,
                        IsRequired = q.IsRequired,
                        Category = q.Category,
                        Area = q.Area
                    })
                    .ToListAsync();
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

        [HttpGet]
        public async Task<IActionResult> Apply(int opportunityId)
        {
            var opportunity = await _context.VolunteerOpportunities
                .Include(o => o.ApplicationQuestions)
                    .ThenInclude(q => q.VolunteerQuestion)
                .FirstOrDefaultAsync(o =>
                    o.VolunteerOpportunityId == opportunityId);

            if (opportunity == null)
                return NotFound();

            var viewModel = new VolunteerApplicationViewModel
            {
                VolunteerOpportunityId = opportunity.VolunteerOpportunityId,
                OpportunityTitle = opportunity.Title,

                ApplicationQuestions = opportunity.ApplicationQuestions
                    .Where(q => q.VolunteerQuestion != null)
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new VolunteerQuestionSelectionViewModel
                    {
                        VolunteerQuestionId =
                            q.VolunteerQuestionId,

                        Question =
                            q.VolunteerQuestion!.Question,

                        QuestionType =
                            q.VolunteerQuestion.QuestionType,

                        IsRequired =
                            q.VolunteerQuestion.IsRequired,

                        Category =
                            q.VolunteerQuestion.Category,

                        Area =
                            q.VolunteerQuestion.Area,

                        DisplayOrder =
                            q.DisplayOrder
                    })
                    .ToList()
            };

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            VolunteerApplicationViewModel viewModel)
        {
            var opportunity = await _context.VolunteerOpportunities
                .Include(x => x.ApplicationQuestions)
                    .ThenInclude(x => x.VolunteerQuestion)
                .FirstOrDefaultAsync(x =>
                    x.VolunteerOpportunityId ==
                    viewModel.VolunteerOpportunityId);

            if (opportunity == null)
                return NotFound();

            viewModel.OpportunityTitle = opportunity.Title;

            var assignedQuestions =
                opportunity.ApplicationQuestions
                    .Where(x => x.VolunteerQuestion != null)
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();

            if (viewModel.QuestionAnswers == null)
            {
                viewModel.QuestionAnswers =
                    new Dictionary<int, string>();
            }

            // ============================================================
            // VALIDATE REQUIRED APPLICATION QUESTIONS
            // ============================================================

            foreach (var assignedQuestion in assignedQuestions)
            {
                var question =
                    assignedQuestion.VolunteerQuestion!;

                if (!question.IsRequired)
                    continue;

                var hasAnswer =
                    viewModel.QuestionAnswers.TryGetValue(
                        question.VolunteerQuestionId,
                        out var answer);

                if (!hasAnswer ||
                    string.IsNullOrWhiteSpace(answer))
                {
                    ModelState.AddModelError(
                        $"QuestionAnswers[{question.VolunteerQuestionId}]",
                        "This question requires an answer.");
                }
            }

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
                var allowedExtensions = new[]
                {
            ".pdf",
            ".doc",
            ".docx"
        };

                var extension =
                    Path.GetExtension(viewModel.CV.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "CV",
                        "Please upload a PDF, DOC, or DOCX file.");
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
                viewModel.ApplicationQuestions =
                    assignedQuestions
                        .Select(x =>
                            new VolunteerQuestionSelectionViewModel
                            {
                                VolunteerQuestionId =
                                    x.VolunteerQuestionId,

                                Question =
                                    x.VolunteerQuestion!.Question,

                                QuestionType =
                                    x.VolunteerQuestion.QuestionType,

                                IsRequired =
                                    x.VolunteerQuestion.IsRequired,

                                Category =
                                    x.VolunteerQuestion.Category,

                                Area =
                                    x.VolunteerQuestion.Area,

                                DisplayOrder =
                                    x.DisplayOrder
                            })
                        .ToList();

                viewModel.SelectedQuestionIds =
                    assignedQuestions
                        .Select(x => x.VolunteerQuestionId)
                        .ToList();

                return View(viewModel);
            }

            // ============================================================
            // NORMALISE EMAIL
            // ============================================================

            var email =
                viewModel.Email.Trim().ToLowerInvariant();

            // ============================================================
            // CHECK IF APPLICANT ACCOUNT ALREADY EXISTS
            // ============================================================

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            string applicantId;

            bool accountCreated = false;

            string? generatedPassword = null;

            string? confirmationUrl = null;

            // ============================================================
            // CREATE NEW APPLICANT ACCOUNT
            // ============================================================

            if (existingUser == null)
            {
                var applicant = new Applicant
                {
                    UserName = email,

                    Email = email,

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
                        DateTime.UtcNow,

                    ModifiedDateTime =
                        DateTime.UtcNow,

                    CreatedBy =
                        "Anonymous Application",

                    ModifiedBy =
                        "Anonymous Application"
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

                    viewModel.ApplicationQuestions =
                        assignedQuestions
                            .Select(x =>
                                new VolunteerQuestionSelectionViewModel
                                {
                                    VolunteerQuestionId =
                                        x.VolunteerQuestionId,

                                    Question =
                                        x.VolunteerQuestion!.Question,

                                    QuestionType =
                                        x.VolunteerQuestion.QuestionType,

                                    IsRequired =
                                        x.VolunteerQuestion.IsRequired,

                                    Category =
                                        x.VolunteerQuestion.Category,

                                    Area =
                                        x.VolunteerQuestion.Area,

                                    DisplayOrder =
                                        x.DisplayOrder
                                })
                            .ToList();

                    viewModel.SelectedQuestionIds =
                        assignedQuestions
                            .Select(x => x.VolunteerQuestionId)
                            .ToList();

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
                // DEFAULT ASP.NET CORE IDENTITY EMAIL CONFIRMATION
                // ========================================================


                var confirmationToken =
                    await _userManager.GenerateEmailConfirmationTokenAsync(
                        applicant);

                var encodedConfirmationToken =
                    WebEncoders.Base64UrlEncode(
                        Encoding.UTF8.GetBytes(confirmationToken));

                confirmationUrl =
                    Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new
                        {
                            area = "Identity",
                            userId = applicant.Id,
                            code = encodedConfirmationToken
                        },
                        protocol: Request.Scheme);


                applicantId =
                    applicant.Id;

                accountCreated = true;
            }
            else
            {
                // ========================================================
                // EXISTING ACCOUNT
                // ========================================================

                applicantId =
                    existingUser.Id;
            }

            // ============================================================
            // UPLOAD CV
            // ============================================================

            if (viewModel.CV != null &&
                viewModel.CV.Length > 0)
            {
                cvFilePath =
                    await _fileUploadService.UploadFileAsync(
                        viewModel.CV);
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
            // SAVE APPLICATION QUESTION ANSWERS
            // ============================================================

            foreach (var assignedQuestion in assignedQuestions)
            {
                var question =
                    assignedQuestion.VolunteerQuestion!;

                if (!viewModel.QuestionAnswers.TryGetValue(
                        question.VolunteerQuestionId,
                        out var submittedAnswer))
                {
                    continue;
                }

                var answer =
                    submittedAnswer?.Trim();

                if (string.IsNullOrWhiteSpace(answer))
                    continue;

                _context.VolunteerApplicationAnswers.Add(
                    new VolunteerApplicationAnswer
                    {
                        VolunteerApplicationId =
                            application.VolunteerApplicationId,

                        VolunteerQuestionId =
                            question.VolunteerQuestionId,

                        Answer =
                            answer
                    });
            }

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
            // SEND EMAIL FOR NEW ACCOUNT
            // ============================================================

            if (accountCreated &&
                !string.IsNullOrWhiteSpace(generatedPassword))
            {
                var emailBody =
                    BuildApplicantAccountEmail(
                        viewModel.FirstName.Trim(),
                        viewModel.LastName.Trim(),
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
                    "We created your SwiftCare applicant account " +
                    "and sent your login details to your email.";
            }
            else
            {
                // ========================================================
                // SEND EMAIL FOR EXISTING ACCOUNT
                // ========================================================

                var emailBody =
                    BuildExistingApplicantEmail(
                        viewModel.FirstName.Trim(),
                        opportunity.Title,
                        applicationReference,
                        statusUrl);

                await _emailService.SendEmailAsync(
                    email,
                    "SwiftCare - Volunteer Application Submitted",
                    emailBody,
                    "SwiftCare");

                TempData["SuccessMessage"] =
                    "Your SwiftCare account already exists. " +
                    "Your volunteer application was submitted successfully.";
            }

            // ============================================================
            // REDIRECT TO APPLICATION SUBMITTED PAGE
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
    } 
}
