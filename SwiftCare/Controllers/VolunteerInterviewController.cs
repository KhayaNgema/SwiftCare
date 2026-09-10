using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.ViewModels;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace SwiftCare.Controllers
{
    [AllowAnonymous]
    public class VolunteerInterviewController : Controller
    {
        private readonly SwiftCareDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private const int MaximumInterviewQuestions = 7;

        public VolunteerInterviewController(
            SwiftCareDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // ============================================================
        // START INTERVIEW LANDING PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Start(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Invalid interview link.");

            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x!.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x!.VolunteerOpportunity)
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken == token);

            if (session == null)
                return NotFound("Interview session not found.");

            var application =
                session.VolunteerApplication;

            if (application == null)
                return NotFound("Application not found.");

            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return NotFound("Volunteer opportunity not found.");

            // ========================================================
            // MUST BE SHORTLISTED
            // ========================================================

            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }

            // ========================================================
            // ALREADY COMPLETED
            // ========================================================

            if (session.IsCompleted)
            {
                return RedirectToAction(
                    nameof(Completed),
                    new { token });
            }

            // ========================================================
            // GET RELEVANT QUESTIONS
            // ========================================================

            var questions =
                await GetInterviewQuestionsAsync(
                    session.SessionToken,
                    opportunity);

            if (!questions.Any())
            {
                return View(
                    "Error",
                    "No interview questions have been configured for this volunteer opportunity.");
            }

            var applicantName =
                GetApplicantName(application);

            var model =
                new VolunteerInterviewStartViewModel
                {
                    Token =
                        session.SessionToken,

                    ApplicantName =
                        applicantName,

                    OpportunityTitle =
                        opportunity.Title,

                    QuestionCount =
                        questions.Count,

                    EstimatedMinutes =
                        Math.Max(
                            5,
                            questions.Count * 2),

                    IsStarted =
                        session.IsStarted
                };

            return View(model);
        }

        // ============================================================
        // BEGIN INTERVIEW
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Begin(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Invalid interview link.");

            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .Include(x => x.VolunteerApplication)
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken == token);

            if (session == null)
                return NotFound(
                    "Interview session not found.");

            var application =
                session.VolunteerApplication;

            if (application == null)
                return NotFound(
                    "Application not found.");

            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }

            if (session.IsCompleted)
            {
                return RedirectToAction(
                    nameof(Completed),
                    new { token });
            }

            // ========================================================
            // START SESSION
            // ========================================================

            if (!session.IsStarted)
            {
                session.IsStarted = true;

                session.StartedDate =
                    DateTime.Now;

                session.CurrentQuestionNumber = 1;

                session.Status = "InProgress";

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Interview),
                new { token });
        }

        // ============================================================
        // AZURE SPEECH AUTHORIZATION TOKEN
        //
        // Browser calls:
        //
        // /VolunteerInterview/SpeechToken?token=INTERVIEW_TOKEN
        //
        // The Azure Speech subscription key NEVER goes to the browser.
        //
        // SwiftCare:
        //
        // 1. Validates interview session
        // 2. Validates applicant is shortlisted
        // 3. Requests temporary Azure Speech token
        // 4. Returns temporary token to browser
        //
        // Azure tokens are temporary and Microsoft documents them as
        // valid for 10 minutes.
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> SpeechToken(string token)
        {
            try
            {
                // ============================================================
                // 1. VALIDATE SESSION TOKEN
                // ============================================================

                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid interview session."
                    });
                }

                // ============================================================
                // 2. LOAD INTERVIEW SESSION
                // ============================================================

                var session =
                    await _context.VolunteerVoiceInterviewSessions
                        .Include(x => x.VolunteerApplication)
                        .FirstOrDefaultAsync(
                            x => x.SessionToken == token);

                if (session == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Interview session not found."
                    });
                }

                // ============================================================
                // 3. LOAD APPLICATION
                // ============================================================

                var application =
                    session.VolunteerApplication;

                if (application == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Volunteer application not found."
                    });
                }

                // ============================================================
                // 4. MAKE SURE APPLICANT IS SHORTLISTED
                // ============================================================

                if (application.Status !=
                    VolunteerApplicationStatus.Shortlisted)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message =
                            "This interview is not available because the application is not shortlisted."
                    });
                }

                // ============================================================
                // 5. MAKE SURE INTERVIEW HAS STARTED
                // ============================================================

                if (!session.IsStarted)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Interview session has not started."
                    });
                }

                // ============================================================
                // 6. MAKE SURE INTERVIEW IS NOT COMPLETE
                // ============================================================

                if (session.IsCompleted)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Interview session has already been completed."
                    });
                }

                // ============================================================
                // 7. GET AZURE SPEECH SETTINGS
                // ============================================================

                var speechKey =
                    _configuration["AzureSpeech:Key"];

                var speechRegion =
                    _configuration["AzureSpeech:Region"];

                if (string.IsNullOrWhiteSpace(speechKey))
                {
                    Console.WriteLine(
                        "[SWIFTCARE SPEECH] AzureSpeech:Key is missing.");

                    return StatusCode(500, new
                    {
                        success = false,
                        message =
                            "AzureSpeech:Key is missing from configuration."
                    });
                }

                if (string.IsNullOrWhiteSpace(speechRegion))
                {
                    Console.WriteLine(
                        "[SWIFTCARE SPEECH] AzureSpeech:Region is missing.");

                    return StatusCode(500, new
                    {
                        success = false,
                        message =
                            "AzureSpeech:Region is missing from configuration."
                    });
                }

                speechRegion =
                    speechRegion
                        .Trim()
                        .ToLowerInvariant();

                // ============================================================
                // 8. AZURE SPEECH TOKEN ENDPOINT
                //
                // IMPORTANT:
                // We deliberately do NOT use:
                //
                // https://swiftcare-speech.services.ai.azure.com/
                //
                // for this token request.
                //
                // We use the regional STS endpoint.
                // ============================================================

                var tokenEndpoint =
                    $"https://{speechRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

                Console.WriteLine(
                    "[SWIFTCARE SPEECH] ========================================");

                Console.WriteLine(
                    "[SWIFTCARE SPEECH] Requesting Azure Speech token...");

                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Region: {speechRegion}");

                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Token endpoint: {tokenEndpoint}");

                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Key configured: {!string.IsNullOrWhiteSpace(speechKey)}");

                Console.WriteLine(
                    "[SWIFTCARE SPEECH] ========================================");

                // ============================================================
                // 9. CREATE HTTP CLIENT
                // ============================================================

                using var client =
                    _httpClientFactory.CreateClient();

                // ============================================================
                // 10. CREATE TOKEN REQUEST
                // ============================================================

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        tokenEndpoint);

                request.Headers.TryAddWithoutValidation(
                    "Ocp-Apim-Subscription-Key",
                    speechKey);

                request.Content =
                    new StringContent(
                        string.Empty,
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded");

                // ============================================================
                // 11. CONTACT AZURE
                // ============================================================

                HttpResponseMessage response;

                try
                {
                    response =
                        await client.SendAsync(request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "[SWIFTCARE SPEECH] Azure connection exception:");

                    Console.WriteLine(ex.ToString());

                    return StatusCode(502, new
                    {
                        success = false,
                        message =
                            "SwiftCare could not connect to Azure Speech.",
                        details =
                            ex.Message
                    });
                }

                // ============================================================
                // 12. READ AZURE RESPONSE
                // ============================================================

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Azure HTTP status: {(int)response.StatusCode} {response.StatusCode}");

                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Azure response length: {responseBody?.Length ?? 0}");

                // ============================================================
                // 13. AZURE REJECTED REQUEST
                // ============================================================

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        "[SWIFTCARE SPEECH] Azure token request FAILED.");

                    Console.WriteLine(
                        $"[SWIFTCARE SPEECH] Azure response:");

                    Console.WriteLine(
                        responseBody);

                    return StatusCode(
                        502,
                        new
                        {
                            success = false,

                            message =
                                "Azure Speech rejected the token request.",

                            azureStatus =
                                (int)response.StatusCode,

                            azureReason =
                                response.ReasonPhrase,

                            details =
                                responseBody
                        });
                }

                // ============================================================
                // 14. GET TOKEN
                // ============================================================

                var speechToken =
                    responseBody?.Trim();

                if (string.IsNullOrWhiteSpace(speechToken))
                {
                    Console.WriteLine(
                        "[SWIFTCARE SPEECH] Azure returned an EMPTY token.");

                    return StatusCode(502, new
                    {
                        success = false,
                        message =
                            "Azure Speech returned an empty authentication token."
                    });
                }

                // ============================================================
                // 15. SUCCESS
                // ============================================================

                Console.WriteLine(
                    "[SWIFTCARE SPEECH] Azure Speech token received successfully.");

                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Token length: {speechToken.Length}");

                Console.WriteLine(
                    "[SWIFTCARE SPEECH] ========================================");

                return Json(new
                {
                    success = true,
                    token = speechToken,
                    region = speechRegion
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[SWIFTCARE SPEECH] UNHANDLED SpeechToken ERROR:");

                Console.WriteLine(
                    ex.ToString());

                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "An unexpected error occurred while obtaining the Azure Speech token.",
                    details =
                        ex.Message
                });
            }
        }

        // ============================================================
        // SINGLE-PAGE SIGNALR INTERVIEW
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Interview(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(
                    "Invalid interview link.");

            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x!.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x!.VolunteerOpportunity)
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken == token);

            if (session == null)
                return NotFound(
                    "Interview session not found.");

            var application =
                session.VolunteerApplication;

            if (application == null)
                return NotFound(
                    "Application not found.");

            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return NotFound(
                    "Volunteer opportunity not found.");

            // ========================================================
            // SECURITY
            // ========================================================

            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }

            if (!session.IsStarted)
            {
                return RedirectToAction(
                    nameof(Start),
                    new { token });
            }

            if (session.IsCompleted)
            {
                return RedirectToAction(
                    nameof(Completed),
                    new { token });
            }

            // ========================================================
            // GET QUESTIONS
            // ========================================================

            var questions =
                await GetInterviewQuestionsAsync(
                    session.SessionToken,
                    opportunity);

            if (!questions.Any())
            {
                return View(
                    "Error",
                    "No interview questions are available for this opportunity.");
            }

            // ========================================================
            // NORMALIZE QUESTION POSITION
            // ========================================================

            if (session.CurrentQuestionNumber < 1)
            {
                session.CurrentQuestionNumber = 1;

                await _context.SaveChangesAsync();
            }

            if (session.CurrentQuestionNumber >
                questions.Count)
            {
                session.CurrentQuestionNumber =
                    questions.Count;

                await _context.SaveChangesAsync();
            }

            // ========================================================
            // CURRENT QUESTION
            //
            // SignalR remains the actual interview driver.
            // ========================================================

            var currentQuestion =
                questions[
                    session.CurrentQuestionNumber - 1];

            // ========================================================
            // EXISTING ANSWER
            //
            // Useful if applicant refreshes the page.
            // ========================================================

            var existingAnswer =
                await _context.VolunteerApplicationAnswers
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                            application.VolunteerApplicationId &&
                        x.VolunteerQuestionId ==
                            currentQuestion.VolunteerQuestionId);

            var model =
                new VolunteerInterviewViewModel
                {
                    Token =
                        session.SessionToken,

                    ApplicantName =
                        GetApplicantName(application),

                    OpportunityTitle =
                        opportunity.Title,

                    QuestionNumber =
                        session.CurrentQuestionNumber,

                    TotalQuestions =
                        questions.Count,

                    QuestionId =
                        currentQuestion.VolunteerQuestionId,

                    Question =
                        GetQuestionText(currentQuestion),

                    QuestionType =
                        currentQuestion.QuestionType,

                    ExistingAnswer =
                        existingAnswer?.Answer,

                    IsLastQuestion =
                        session.CurrentQuestionNumber ==
                        questions.Count
                };

            return View(model);
        }

        // ============================================================
        // LEGACY SAVE ANSWER ENDPOINT
        //
        // SignalR now handles saving answers.
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAnswer(
            string token,
            int questionId,
            string answer)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(
                    "Invalid interview link.");

            if (string.IsNullOrWhiteSpace(answer))
                return BadRequest(
                    "Please provide an answer.");

            answer = answer.Trim();

            if (answer.Length > 3000)
            {
                answer =
                    answer.Substring(0, 3000);
            }

            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x =>
                            x!.VolunteerOpportunity)
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken == token);

            if (session == null)
                return NotFound(
                    "Interview session not found.");

            var application =
                session.VolunteerApplication;

            if (application == null)
                return NotFound(
                    "Application not found.");

            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return NotFound(
                    "Volunteer opportunity not found.");

            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }

            if (!session.IsStarted)
                return BadRequest(
                    "The interview has not started.");

            if (session.IsCompleted)
            {
                return Json(new
                {
                    success = true,
                    completed = true
                });
            }

            var questions =
                await GetInterviewQuestionsAsync(
                    session.SessionToken,
                    opportunity);

            if (!questions.Any())
                return BadRequest(
                    "No interview questions are configured.");

            if (session.CurrentQuestionNumber < 1 ||
                session.CurrentQuestionNumber >
                    questions.Count)
            {
                return BadRequest(
                    "Invalid interview question position.");
            }

            var expectedQuestion =
                questions[
                    session.CurrentQuestionNumber - 1];

            // ========================================================
            // NEVER TRUST QUESTION ID FROM BROWSER
            // ========================================================

            if (expectedQuestion.VolunteerQuestionId !=
                questionId)
            {
                return BadRequest(
                    "This is not the current interview question.");
            }

            var existingAnswer =
                await _context.VolunteerApplicationAnswers
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                            application.VolunteerApplicationId &&
                        x.VolunteerQuestionId ==
                            questionId);

            if (existingAnswer == null)
            {
                existingAnswer =
                    new VolunteerApplicationAnswer
                    {
                        VolunteerApplicationId =
                            application.VolunteerApplicationId,

                        VolunteerQuestionId =
                            questionId,

                        Answer =
                            answer
                    };

                _context.VolunteerApplicationAnswers
                    .Add(existingAnswer);
            }
            else
            {
                existingAnswer.Answer =
                    answer;
            }

            // ========================================================
            // LAST QUESTION
            // ========================================================

            if (session.CurrentQuestionNumber >=
                questions.Count)
            {
                session.IsCompleted = true;

                session.CompletedDate =
                    DateTime.Now;

                session.Status =
                    "Completed";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    completed = true
                });
            }

            // ========================================================
            // NEXT QUESTION
            // ========================================================

            session.CurrentQuestionNumber++;

            session.Status =
                "InProgress";

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                completed = false
            });
        }

        // ============================================================
        // COMPLETE INTERVIEW
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(
                    "Invalid interview link.");

            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .Include(x => x.VolunteerApplication)
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken == token);

            if (session == null)
                return NotFound(
                    "Interview session not found.");

            var application =
                session.VolunteerApplication;

            if (application == null)
                return NotFound(
                    "Application not found.");

            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }

            if (!session.IsStarted)
                return BadRequest(
                    "Interview has not started.");

            if (!session.IsCompleted)
            {
                session.IsCompleted = true;

                session.CompletedDate =
                    DateTime.Now;

                session.Status =
                    "Completed";

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Completed),
                new { token });
        }

        // ============================================================
        // COMPLETED PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Completed(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(
                    "Invalid interview link.");

            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x =>
                            x!.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x =>
                            x!.VolunteerOpportunity)
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken == token);

            if (session == null)
                return NotFound(
                    "Interview session not found.");

            var application =
                session.VolunteerApplication;

            if (application == null)
                return NotFound(
                    "Application not found.");

            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }

            var model =
                new VolunteerInterviewCompletedViewModel
                {
                    ApplicantName =
                        GetApplicantName(application),

                    OpportunityTitle =
                        application
                            .VolunteerOpportunity?
                            .Title
                        ?? "Volunteer Opportunity",

                    CompletedDate =
                        session.CompletedDate
                };

            return View(model);
        }

        // ============================================================
        // GET INTERVIEW QUESTIONS
        //
        // Questions come directly from active VolunteerQuestion
        // records and are matched to:
        //
        // 1. Opportunity Category
        // 2. Opportunity Area
        //
        // Maximum = 7
        // ============================================================

        private async Task<List<VolunteerQuestion>>
            GetInterviewQuestionsAsync(
                string sessionToken,
                VolunteerOpportunity opportunity)
        {
            var allQuestions =
                await _context.VolunteerQuestions
                    .Where(q => q.IsActive)
                    .ToListAsync();

            if (!allQuestions.Any())
            {
                return new List<VolunteerQuestion>();
            }

            // ========================================================
            // SCORE QUESTIONS
            // ========================================================

            var relevantQuestions =
                allQuestions
                    .Select(question =>
                    {
                        int relevanceScore = 0;

                        // ------------------------------------------------
                        // CATEGORY MATCH
                        // ------------------------------------------------

                        if (opportunity.Category.HasValue &&
                            question.Category.HasValue &&
                            question.Category.Value ==
                                opportunity.Category.Value)
                        {
                            relevanceScore += 100;
                        }

                        // ------------------------------------------------
                        // AREA MATCH
                        // ------------------------------------------------

                        if (opportunity.Area.HasValue &&
                            question.Area.HasValue &&
                            question.Area.Value ==
                                opportunity.Area.Value)
                        {
                            relevanceScore += 100;
                        }

                        // ------------------------------------------------
                        // GENERAL CATEGORY QUESTION
                        // ------------------------------------------------

                        if (opportunity.Category.HasValue &&
                            question.Category.HasValue &&
                            question.Category.Value ==
                                opportunity.Category.Value &&
                            !question.Area.HasValue)
                        {
                            relevanceScore += 25;
                        }

                        return new
                        {
                            Question = question,
                            Relevance = relevanceScore
                        };
                    })
                    .Where(x =>
                        x.Relevance > 0)
                    .OrderByDescending(x =>
                        x.Relevance)
                    .ToList();

            // ========================================================
            // NO RELEVANT QUESTIONS
            // ========================================================

            if (!relevantQuestions.Any())
            {
                return new List<VolunteerQuestion>();
            }

            // ========================================================
            // STABLE RANDOMIZATION
            // ========================================================

            var randomSeed =
                CreateStableRandomSeed(
                    sessionToken);

            var random =
                new Random(randomSeed);

            var selectedQuestions =
                relevantQuestions
                    .GroupBy(x =>
                        x.Relevance)
                    .OrderByDescending(group =>
                        group.Key)
                    .SelectMany(group =>
                        group
                            .OrderBy(x =>
                                random.Next())
                            .Select(x =>
                                x.Question))
                    .Take(
                        MaximumInterviewQuestions)
                    .ToList();

            return selectedQuestions;
        }

        // ============================================================
        // CREATE STABLE RANDOM SEED
        // ============================================================

        private static int CreateStableRandomSeed(
            string sessionToken)
        {
            using var sha256 =
                SHA256.Create();

            var bytes =
                sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        sessionToken));

            return BitConverter.ToInt32(
                bytes,
                0);
        }

        // ============================================================
        // APPLICANT NAME
        // ============================================================

        private static string GetApplicantName(
            VolunteerApplication application)
        {
            if (application.Applicant == null)
                return "Applicant";

            var firstName =
                application.Applicant
                    .FirstName?
                    .Trim();

            var lastName =
                application.Applicant
                    .LastName?
                    .Trim();

            var fullName =
                $"{firstName} {lastName}"
                    .Trim();

            if (!string.IsNullOrWhiteSpace(
                fullName))
            {
                return fullName;
            }

            return "Applicant";
        }

        // ============================================================
        // QUESTION TEXT
        // ============================================================

        private static string GetQuestionText(
            VolunteerQuestion question)
        {
            return question.Question?
                       .Trim()
                   ?? "Interview question unavailable.";
        }
    }
}