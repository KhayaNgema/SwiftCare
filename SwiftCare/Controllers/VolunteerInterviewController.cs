using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.ViewModels;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SwiftCare.Controllers
{
    [AllowAnonymous]
    public class VolunteerInterviewController : Controller
    {
        private readonly SwiftCareDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly FileUploadService _fileUploadService;

        private const int MaximumInterviewQuestions = 7;

        /*
         * IMPORTANT:
         *
         * The browser and server use the same threshold.
         *
         * 1 event = warning
         * 2 events = warning
         * 3 events = interview ended for administrative review
         */
        private const int SuspiciousEventThreshold = 3;


        public VolunteerInterviewController(
            SwiftCareDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            FileUploadService fileUploadService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
        }

        [Authorize(Roles = "Human Resources Manager")]
        [HttpGet]
        public async Task<IActionResult> Interviews()
        {
            var interviews =
                await _context.VolunteerVoiceInterviewSessions
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.Applicant)
                    .Include(x => x.VolunteerApplication)
                        .ThenInclude(x => x.VolunteerOpportunity)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

            return View(interviews);
        }

        // ============================================================
        // ERROR VIEW HELPER
        // ============================================================

        private IActionResult InterviewError(string message)
        {
            return View(
                "Error",
                new ErrorViewModel
                {
                    Message = message,
                    RequestId = HttpContext.TraceIdentifier
                });
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


            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }


            // ========================================================
            // ALREADY SUSPICIOUS
            //
            // DO NOT RESTART.
            // ========================================================

            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                return InterviewError(
                    "This interview has already been ended and submitted for administrative review.");
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


            var questions =
                await GetInterviewQuestionsAsync(
                    session.SessionToken,
                    opportunity);


            if (!questions.Any())
            {
                return InterviewError(
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


            // ========================================================
            // BLOCK SUSPICIOUS SESSION
            // ========================================================

            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                return BadRequest(
                    "This interview has already been ended and submitted for administrative review.");
            }


            if (session.IsCompleted)
            {
                return RedirectToAction(
                    nameof(Completed),
                    new { token });
            }


            if (!session.IsStarted)
            {
                session.IsStarted = true;

                session.StartedDate =
                    DateTime.Now;

                session.CurrentQuestionNumber = 1;

                session.Status =
                    "InProgress";

                session.LastActivityDate =
                    DateTime.Now;

                session.LastActivityType =
                    "InterviewStarted";

                await _context.SaveChangesAsync();
            }


            return RedirectToAction(
                nameof(Interview),
                new { token });
        }


        // ============================================================
        // AZURE SPEECH AUTHORIZATION TOKEN
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> SpeechToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid interview session."
                    });
                }


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


                if (!session.IsStarted)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Interview session has not started."
                    });
                }


                // ====================================================
                // BLOCK SUSPICIOUS
                // ====================================================

                if (
                    session.Status == "Suspicious" ||
                    session.SuspiciousActivityDetected)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message =
                            "This interview has already been ended and submitted for administrative review."
                    });
                }


                if (session.IsCompleted)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Interview session has already been completed."
                    });
                }


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


                using var client =
                    _httpClientFactory.CreateClient();


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

                    Console.WriteLine(
                        ex.ToString());

                    return StatusCode(502, new
                    {
                        success = false,
                        message =
                            "SwiftCare could not connect to Azure Speech.",
                        details =
                            ex.Message
                    });
                }


                var responseBody =
                    await response.Content.ReadAsStringAsync();


                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Azure HTTP status: {(int)response.StatusCode} {response.StatusCode}");

                Console.WriteLine(
                    $"[SWIFTCARE SPEECH] Azure response length: {responseBody?.Length ?? 0}");


                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        "[SWIFTCARE SPEECH] Azure token request FAILED.");

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


            if (application.Status !=
                VolunteerApplicationStatus.Shortlisted)
            {
                return Forbid();
            }


            // ========================================================
            // BLOCK SUSPICIOUS SESSION
            // ========================================================

            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                return InterviewError(
                    "This interview has already been ended and submitted for administrative review.");
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


            var questions =
                await GetInterviewQuestionsAsync(
                    session.SessionToken,
                    opportunity);


            if (!questions.Any())
            {
                return InterviewError(
                    "No interview questions are available for this opportunity.");
            }


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


            var currentQuestion =
                questions[
                    session.CurrentQuestionNumber - 1];


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


            answer =
                answer.Trim();


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


            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                return BadRequest(
                    "This interview has been ended and submitted for administrative review.");
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


            if (
                session.CurrentQuestionNumber < 1 ||
                session.CurrentQuestionNumber >
                    questions.Count)
            {
                return BadRequest(
                    "Invalid interview question position.");
            }


            var expectedQuestion =
                questions[
                    session.CurrentQuestionNumber - 1];


            if (
                expectedQuestion.VolunteerQuestionId !=
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


            if (
                session.CurrentQuestionNumber >=
                questions.Count)
            {
                session.IsCompleted = true;

                session.CompletedDate =
                    DateTime.Now;

                session.Status =
                    "Completed";

                session.LastActivityDate =
                    DateTime.Now;

                session.LastActivityType =
                    "InterviewCompleted";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    completed = true
                });
            }


            session.CurrentQuestionNumber++;

            session.Status =
                "InProgress";

            session.LastActivityDate =
                DateTime.Now;

            session.LastActivityType =
                "AnswerSaved";

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


            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                return BadRequest(
                    "This interview has been ended and submitted for administrative review.");
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

                session.LastActivityDate =
                    DateTime.Now;

                session.LastActivityType =
                    "InterviewCompleted";

                await _context.SaveChangesAsync();
            }


            return RedirectToAction(
                nameof(Completed),
                new { token });
        }


        // ============================================================
        // SAVE PROCTORING EVENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProctoringEvent(
            [FromBody] ProctoringEventRequest request)
        {
            if (
                request == null ||
                string.IsNullOrWhiteSpace(request.Token)
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid interview session."
                });
            }


            return await ProcessProctoringEventAsync(
                request);
        }


        // ============================================================
        // BEACON PROCTORING EVENT
        // ============================================================

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProctoringEventBeacon()
        {
            try
            {
                using var reader =
                    new StreamReader(
                        Request.Body);

                var body =
                    await reader.ReadToEndAsync();


                if (
                    string.IsNullOrWhiteSpace(body)
                )
                {
                    return BadRequest();
                }


                var request =
                    JsonSerializer.Deserialize<ProctoringEventRequest>(
                        body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });


                if (
                    request == null ||
                    string.IsNullOrWhiteSpace(
                        request.Token)
                )
                {
                    return BadRequest();
                }


                var result =
                    await ProcessProctoringEventAsync(
                        request);


                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[SWIFTCARE PROCTORING] Beacon error:");

                Console.WriteLine(
                    ex.ToString());

                return BadRequest();
            }
        }


        // ============================================================
        // PROCESS PROCTORING EVENT
        // ============================================================

        private async Task<IActionResult>
            ProcessProctoringEventAsync(
                ProctoringEventRequest request)
        {
            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken ==
                        request.Token);


            if (session == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Interview session not found."
                });
            }


            if (!session.IsStarted)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Interview has not started."
                });
            }


            if (session.IsCompleted)
            {
                return Json(new
                {
                    success = true,
                    ignored = true,
                    message =
                        "Interview has already completed."
                });
            }


            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected
            )
            {
                return Json(new
                {
                    success = true,
                    suspicious = true,
                    ignored = true,
                    message =
                        "Interview has already been submitted for administrative review.",
                    suspiciousEventCount =
                        session.SuspiciousEventCount
                });
            }


            // ========================================================
            // NORMALIZE EVENT
            // ========================================================

            var eventType =
                string.IsNullOrWhiteSpace(
                    request.EventType)
                    ? "Unknown"
                    : request.EventType.Trim();


            var allowedEventTypes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "TabSwitch",
                    "WindowBlur",
                    "FullScreenExit",
                    "PageLeave",
                    "CameraStopped",
                    "CameraPermissionDenied"
                };


            if (!allowedEventTypes.Contains(eventType))
            {
                eventType =
                    "Unknown";
            }


            // ========================================================
            // UPDATE EVENT COUNTERS
            // ========================================================

            session.LastProctoringEventType =
                eventType;

            session.SuspiciousEventCount++;

            session.LastActivityDate =
                DateTime.Now;

            session.LastActivityType =
                eventType;


            switch (eventType)
            {
                case "TabSwitch":

                    session.TabSwitchCount++;

                    break;


                case "WindowBlur":

                    session.WindowBlurCount++;

                    break;


                case "FullScreenExit":

                    session.FullScreenExitCount++;

                    break;


                case "PageLeave":

                    session.PageLeaveCount++;

                    break;


                case "CameraStopped":

                    session.CameraStoppedCount++;

                    break;
            }


            // ========================================================
            // CREATE EVENT HISTORY RECORD
            // ========================================================

            var proctoringEvent =
                new VolunteerInterviewProctoringEvent
                {
                    VolunteerVoiceInterviewSessionId =
                        session.VolunteerVoiceInterviewSessionId,

                    EventType =
                        eventType,

                    Description =
                        GetProctoringEventDescription(
                            eventType),

                    QuestionNumber =
                        request.QuestionNumber,

                    EventDate =
                        DateTime.Now,

                    PageVisibilityState =
                        request.PageVisibilityState,

                    PageHidden =
                        request.PageHidden,

                    UserAgent =
                        Request.Headers["User-Agent"]
                            .FirstOrDefault(),

                    UnfocusedSeconds =
                        request.UnfocusedSeconds
                };


            _context.VolunteerInterviewProctoringEvents
                .Add(proctoringEvent);


            // ========================================================
            // AUTHORITATIVE THRESHOLD
            // ========================================================

            if (
                session.SuspiciousEventCount >=
                SuspiciousEventThreshold
            )
            {
                session.SuspiciousActivityDetected =
                    true;

                session.SuspiciousActivityDate =
                    DateTime.Now;

                session.Status =
                    "Suspicious";

                session.IncompleteReason =
                    "Interview integrity threshold reached.";

                session.LastActivityType =
                    "Suspicious Activity Threshold Reached";
            }


            await _context.SaveChangesAsync();


            var suspicious =
                session.SuspiciousActivityDetected;


            Console.WriteLine(
                $"[SWIFTCARE PROCTORING] Event={eventType} " +
                $"Count={session.SuspiciousEventCount} " +
                $"Suspicious={suspicious}");


            return Json(new
            {
                success = true,

                suspicious =
                    suspicious,

                eventId =
                    proctoringEvent
                        .VolunteerInterviewProctoringEventId,

                suspiciousEventCount =
                    session.SuspiciousEventCount,

                tabSwitchCount =
                    session.TabSwitchCount,

                windowBlurCount =
                    session.WindowBlurCount,

                fullScreenExitCount =
                    session.FullScreenExitCount,

                pageLeaveCount =
                    session.PageLeaveCount,

                cameraStoppedCount =
                    session.CameraStoppedCount
            });
        }


        // ============================================================
        // PROCTORING EVENT DESCRIPTION
        // ============================================================

        private static string
            GetProctoringEventDescription(
                string eventType)
        {
            return eventType switch
            {
                "TabSwitch" =>
                    "The interview page became hidden or another browser tab/window became active.",

                "WindowBlur" =>
                    "The interview browser window lost focus while the interview page remained visible.",

                "FullScreenExit" =>
                    "Fullscreen mode was exited during the interview.",

                "PageLeave" =>
                    "The applicant attempted to leave the interview page.",

                "CameraStopped" =>
                    "The interview camera stream stopped during the interview.",

                "CameraPermissionDenied" =>
                    "Camera permission was denied or unavailable during the interview.",

                _ =>
                    "An interview integrity event was detected."
            };
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
                        GetApplicantName(
                            application),

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
        // ============================================================

        private async Task<List<VolunteerQuestion>>
            GetInterviewQuestionsAsync(
                string sessionToken,
                VolunteerOpportunity opportunity)
        {
            var allQuestions =
                await _context.VolunteerQuestions
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.DisplayOrder)
                    .ThenBy(q => q.VolunteerQuestionId)
                    .ToListAsync();


            if (!allQuestions.Any())
            {
                return new List<VolunteerQuestion>();
            }


            var relevantQuestions =
                allQuestions
                    .Select(question =>
                    {
                        int relevanceScore = 0;


                        if (
                            opportunity.Category.HasValue &&
                            question.Category.HasValue &&
                            question.Category.Value ==
                                opportunity.Category.Value
                        )
                        {
                            relevanceScore += 100;
                        }


                        if (
                            opportunity.Area.HasValue &&
                            question.Area.HasValue &&
                            question.Area.Value ==
                                opportunity.Area.Value
                        )
                        {
                            relevanceScore += 100;
                        }


                        if (
                            opportunity.Category.HasValue &&
                            question.Category.HasValue &&
                            question.Category.Value ==
                                opportunity.Category.Value &&
                            !question.Area.HasValue
                        )
                        {
                            relevanceScore += 25;
                        }


                        return new
                        {
                            Question =
                                question,

                            Relevance =
                                relevanceScore
                        };
                    })
                    .Where(x =>
                        x.Relevance > 0)
                    .OrderByDescending(x =>
                        x.Relevance)
                    .ToList();


            if (!relevantQuestions.Any())
            {
                return new List<VolunteerQuestion>();
            }


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

        private static int
            CreateStableRandomSeed(
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

        private static string
            GetApplicantName(
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

        private static string
            GetQuestionText(
                VolunteerQuestion question)
        {
            return question.Question?
                       .Trim()
                   ?? "Interview question unavailable.";
        }


        // ============================================================
        // SUBMIT INCOMPLETE INTERVIEW
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            SubmitIncompleteInterview(
                [FromBody]
                SubmitIncompleteInterviewRequest request)
        {
            if (
                request == null ||
                string.IsNullOrWhiteSpace(
                    request.Token)
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Invalid interview session."
                });
            }


            var session =
                await _context.VolunteerVoiceInterviewSessions
                    .FirstOrDefaultAsync(x =>
                        x.SessionToken ==
                        request.Token);


            if (session == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Interview session not found."
                });
            }


            // ========================================================
            // DO NOT TURN A NORMAL COMPLETION INTO SUSPICIOUS
            // ========================================================

            if (session.IsCompleted)
            {
                return Json(new
                {
                    success = true,
                    alreadyCompleted = true
                });
            }


            // ========================================================
            // PERSIST FINAL PROCTORING SUMMARY
            // ========================================================

            session.SuspiciousActivityDetected =
                true;

            session.SuspiciousActivityDate =
                DateTime.Now;

            session.LastProctoringEventType =
                string.IsNullOrWhiteSpace(
                    request.EventType)
                    ? session.LastProctoringEventType
                    : request.EventType.Trim();


            session.SuspiciousEventCount =
                Math.Max(
                    session.SuspiciousEventCount,
                    request.SuspiciousEventCount);


            session.TabSwitchCount =
                Math.Max(
                    session.TabSwitchCount,
                    request.TabSwitchCount);


            session.WindowBlurCount =
                Math.Max(
                    session.WindowBlurCount,
                    request.WindowBlurCount);


            session.FullScreenExitCount =
                Math.Max(
                    session.FullScreenExitCount,
                    request.FullScreenExitCount);


            session.PageLeaveCount =
                Math.Max(
                    session.PageLeaveCount,
                    request.PageLeaveCount);


            session.CameraStoppedCount =
                Math.Max(
                    session.CameraStoppedCount,
                    request.CameraStoppedCount);


            session.TotalUnfocusedSeconds =
                Math.Max(
                    session.TotalUnfocusedSeconds,
                    request.TotalUnfocusedSeconds);


            session.VideoDurationSeconds =
                Math.Max(
                    session.VideoDurationSeconds,
                    request.VideoDurationSeconds);


            session.IncompleteReason =
                string.IsNullOrWhiteSpace(
                    request.Reason)
                    ? "Interview ended due to interview integrity activity."
                    : request.Reason.Trim();


            session.Status =
                "Suspicious";


            session.LastActivityDate =
                DateTime.Now;


            session.LastActivityType =
                "Suspicious Interview Submitted";


            // ========================================================
            // THIS IS NOT A NORMAL COMPLETION
            // ========================================================

            session.IsCompleted =
                false;


            session.CompletedDate =
                null;


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,

                message =
                    "Interview submitted for administrative review.",

                sessionId =
                    session.VolunteerVoiceInterviewSessionId
            });
        }


        // ============================================================
        // UPLOAD INTERVIEW VIDEO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadInterviewVideo(
            [FromForm] string token,
            [FromForm] IFormFile? video,
            [FromForm] int durationSeconds = 0)
            {
            try
            {
                // ============================================================
                // 1. VALIDATE TOKEN
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
                // 2. VALIDATE VIDEO
                // ============================================================

                if (video == null || video.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Interview video was not received."
                    });
                }


                // ============================================================
                // 3. LOAD INTERVIEW SESSION
                // ============================================================

                var session =
                    await _context.VolunteerVoiceInterviewSessions
                        .FirstOrDefaultAsync(x =>
                            x.SessionToken == token);

                if (session == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Interview session not found."
                    });
                }


                // ============================================================
                // 4. MAKE SURE INTERVIEW STARTED
                // ============================================================

                if (!session.IsStarted)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Interview has not started."
                    });
                }


                // ============================================================
                // 5. MAXIMUM VIDEO SIZE
                // ============================================================

                const long maximumVideoSize =
                    250L * 1024L * 1024L;

                if (video.Length > maximumVideoSize)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Interview video is too large."
                    });
                }


                // ============================================================
                // 6. DETERMINE FILE EXTENSION
                // ============================================================

                var originalFileName =
                    Path.GetFileName(video.FileName);

                var extension =
                    Path.GetExtension(originalFileName);

                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".webm";
                }

                extension =
                    extension.ToLowerInvariant();

                // Only allow expected video formats
                var allowedExtensions = new[]
                {
            ".webm",
            ".mp4",
            ".ogg",
            ".ogv"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    extension = ".webm";
                }


                // ============================================================
                // 7. CREATE SAFE FILE NAME
                // ============================================================

                var safeFileName =
                    $"VolunteerInterview_" +
                    $"{session.VolunteerVoiceInterviewSessionId}_" +
                    $"{Guid.NewGuid():N}" +
                    extension;


                // ============================================================
                // 8. COPY VIDEO INTO MEMORY
                // ============================================================

                await using var memoryStream =
                    new MemoryStream();

                await video.CopyToAsync(memoryStream);


                // ============================================================
                // 9. SAVE VIDEO
                // ============================================================

                var savedPath =
                    await _fileUploadService.SaveFileAsync(
                        memoryStream.ToArray(),
                        safeFileName);


                // ============================================================
                // 10. VALIDATE SAVED PATH
                // ============================================================

                if (string.IsNullOrWhiteSpace(savedPath))
                {
                    Console.WriteLine(
                        "[SWIFTCARE VIDEO] ERROR: " +
                        "SaveFileAsync returned an empty path.");

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "The interview video could not be saved."
                    });
                }


                // ============================================================
                // 11. UPDATE SESSION VIDEO INFORMATION
                // ============================================================

                session.VideoFilePath =
                    savedPath;

                session.VideoMimeType =
                    string.IsNullOrWhiteSpace(video.ContentType)
                        ? "video/webm"
                        : video.ContentType;

                session.VideoDurationSeconds =
                    Math.Max(0, durationSeconds);


                if (!session.VideoStartedDate.HasValue)
                {
                    session.VideoStartedDate =
                        session.StartedDate
                        ?? DateTime.Now;
                }


                session.VideoCompletedDate =
                    DateTime.Now;

                session.LastActivityDate =
                    DateTime.Now;

                session.LastActivityType =
                    "VideoUploaded";


                // ============================================================
                // 12. SAVE DATABASE CHANGES
                // ============================================================

                await _context.SaveChangesAsync();


                // ============================================================
                // 13. DEBUG LOGGING
                // ============================================================

                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "[SWIFTCARE VIDEO] VIDEO UPLOAD SUCCESS");

                Console.WriteLine(
                    $"Session ID       : " +
                    $"{session.VolunteerVoiceInterviewSessionId}");

                Console.WriteLine(
                    $"File name        : " +
                    $"{safeFileName}");

                Console.WriteLine(
                    $"Saved path       : " +
                    $"{savedPath}");

                Console.WriteLine(
                    $"MIME type        : " +
                    $"{session.VideoMimeType}");

                Console.WriteLine(
                    $"Duration         : " +
                    $"{session.VideoDurationSeconds}");

                Console.WriteLine(
                    $"Video completed  : " +
                    $"{session.VideoCompletedDate}");

                Console.WriteLine(
                    "============================================================");


                // ============================================================
                // 14. RETURN SUCCESS
                // ============================================================

                return Json(new
                {
                    success = true,

                    videoPath =
                        savedPath,

                    durationSeconds =
                        session.VideoDurationSeconds
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "[SWIFTCARE VIDEO] VIDEO UPLOAD ERROR");

                Console.WriteLine(
                    ex.ToString());

                Console.WriteLine(
                    "============================================================");

                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "An error occurred while saving the interview video."
                });
            }
        }
    }
}


// ====================================================================
// PROCTORING EVENT REQUEST
// ====================================================================

public class ProctoringEventRequest
{
    public string Token { get; set; } =
        string.Empty;


    public string EventType { get; set; } =
        string.Empty;


    public int QuestionNumber { get; set; }


    public string? Timestamp { get; set; }


    public string? PageVisibilityState { get; set; }


    public bool? PageHidden { get; set; }


    public int UnfocusedSeconds { get; set; }
}


// ====================================================================
// SUBMIT INCOMPLETE INTERVIEW REQUEST
// ====================================================================

public class SubmitIncompleteInterviewRequest
{
    public string Token { get; set; } =
        string.Empty;


    public string Reason { get; set; } =
        string.Empty;


    public string EventType { get; set; } =
        string.Empty;


    public int QuestionNumber { get; set; }


    public int TabSwitchCount { get; set; }


    public int WindowBlurCount { get; set; }


    public int FullScreenExitCount { get; set; }


    public int PageLeaveCount { get; set; }


    public int CameraStoppedCount { get; set; }


    public int SuspiciousEventCount { get; set; }


    public int TotalUnfocusedSeconds { get; set; }


    public bool VideoUploaded { get; set; }


    public int VideoDurationSeconds { get; set; }
}