using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SwiftCare.Hubs
{
    public class VolunteerInterviewHub : Hub
    {
        private readonly SwiftCareDbContext _context;

    public VolunteerInterviewHub(SwiftCareDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // JOIN INTERVIEW
        // ============================================================

        public async Task JoinInterview(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                throw new HubException("Invalid interview session.");

            var session = await LoadSessionAsync(sessionToken);

            if (session == null)
                throw new HubException("Interview session not found.");

            var application = session.VolunteerApplication;

            if (application == null)
                throw new HubException("Volunteer application not found.");

            if (application.VolunteerOpportunity == null)
                throw new HubException("Volunteer opportunity not found.");

            // ========================================================
            // SECURITY CHECK
            // ========================================================

            if (application.Status != VolunteerApplicationStatus.Shortlisted)
            {
                throw new HubException(
                    "This interview is not available because the application is not shortlisted.");
            }

            // ========================================================
            // ADD CONNECTION TO SECURE SESSION GROUP
            // ========================================================

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                sessionToken);

            // ========================================================
            // SUSPICIOUS SESSION
            //
            // IMPORTANT:
            // A refresh/reconnect must NOT restart an interview that
            // has already been terminated for suspicious activity.
            // ========================================================

            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                await Clients.Caller.SendAsync(
                    "InterviewSuspicious",
                    new
                    {
                        message =
                            "This interview has been ended and submitted for administrative review.",

                        suspiciousEventCount =
                            session.SuspiciousEventCount,

                        suspiciousActivityDate =
                            session.SuspiciousActivityDate
                    });

                return;
            }

            // ========================================================
            // ALREADY COMPLETED
            // ========================================================

            if (session.IsCompleted)
            {
                await Clients.Caller.SendAsync(
                    "InterviewCompleted",
                    "Thank you. Your interview has already been completed.");

                return;
            }

            // ========================================================
            // START SESSION
            //
            // This only happens for a genuinely new session.
            //
            // IMPORTANT:
            // We NEVER reset CurrentQuestionNumber for an existing
            // started session.
            // ========================================================

            if (!session.IsStarted)
            {
                session.IsStarted = true;
                session.StartedDate = DateTime.Now;
                session.Status = "InProgress";

                if (session.CurrentQuestionNumber <= 0)
                {
                    session.CurrentQuestionNumber = 1;
                }

                await _context.SaveChangesAsync();
            }

            // ========================================================
            // SEND COMPLETE CURRENT INTERVIEW STATE
            //
            // This reconstructs the interview from the database.
            // Therefore refreshing the browser does NOT start a new
            // interview.
            // ========================================================

            await SendInterviewStateAsync(sessionToken);
        }

        // ============================================================
        // SUBMIT ANSWER
        //
        // The browser does NOT send questionId.
        //
        // The server determines the current question from:
        //
        // session.CurrentQuestionNumber
        //
        // This prevents the browser from submitting an answer against
        // another question.
        // ============================================================

        public async Task SubmitAnswer(
            string sessionToken,
            string answer)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                throw new HubException("Invalid interview session.");

            if (string.IsNullOrWhiteSpace(answer))
                throw new HubException("Please provide an answer.");

            answer = answer.Trim();

            // ========================================================
            // MAX ANSWER LENGTH
            // ========================================================

            if (answer.Length > 3000)
            {
                answer = answer.Substring(0, 3000);
            }

            // ========================================================
            // LOAD SESSION
            // ========================================================

            var session = await LoadSessionAsync(sessionToken);

            if (session == null)
                throw new HubException("Interview session not found.");

            var application = session.VolunteerApplication;

            if (application == null)
                throw new HubException("Volunteer application not found.");

            var opportunity = application.VolunteerOpportunity;

            if (opportunity == null)
                throw new HubException("Volunteer opportunity not found.");

            // ========================================================
            // SECURITY
            // ========================================================

            if (application.Status != VolunteerApplicationStatus.Shortlisted)
            {
                throw new HubException(
                    "This interview is no longer available.");
            }

            if (!session.IsStarted)
            {
                throw new HubException(
                    "The interview session has not started.");
            }

            // ========================================================
            // DO NOT ALLOW SUSPICIOUS SESSION TO CONTINUE
            // ========================================================

            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                throw new HubException(
                    "This interview has been ended and submitted for administrative review.");
            }

            if (session.IsCompleted)
            {
                throw new HubException(
                    "This interview has already been completed.");
            }

            // ========================================================
            // GET STABLE QUESTION SET
            // ========================================================

            var questions =
                await GetInterviewQuestionsAsync(
                    opportunity,
                    session.SessionToken);

            if (questions.Count == 0)
            {
                throw new HubException(
                    "No interview questions are available.");
            }

            // ========================================================
            // VALIDATE CURRENT QUESTION
            // ========================================================

            if (session.CurrentQuestionNumber <= 0)
            {
                session.CurrentQuestionNumber = 1;
            }

            if (session.CurrentQuestionNumber > questions.Count)
            {
                await CompleteSessionAsync(session);

                await Clients.Caller.SendAsync(
                    "InterviewCompleted",
                    "Thank you. Your interview has been completed successfully.");

                return;
            }

            var currentQuestion =
                questions[session.CurrentQuestionNumber - 1];

            // ========================================================
            // SAVE / UPDATE ANSWER
            // ========================================================

            var existingAnswer =
                await _context.VolunteerApplicationAnswers
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                            session.VolunteerApplicationId &&
                        x.VolunteerQuestionId ==
                            currentQuestion.VolunteerQuestionId);

            if (existingAnswer == null)
            {
                existingAnswer =
                    new VolunteerApplicationAnswer
                    {
                        VolunteerApplicationId =
                            session.VolunteerApplicationId,

                        VolunteerQuestionId =
                            currentQuestion.VolunteerQuestionId,

                        Answer = answer
                    };

                _context.VolunteerApplicationAnswers.Add(
                    existingAnswer);
            }
            else
            {
                existingAnswer.Answer = answer;
            }

            // ========================================================
            // SEND APPLICANT MESSAGE TO CHAT
            // ========================================================

            await Clients.Group(sessionToken)
                .SendAsync(
                    "ApplicantMessage",
                    new
                    {
                        questionNumber =
                            session.CurrentQuestionNumber,

                        questionId =
                            currentQuestion.VolunteerQuestionId,

                        answer = answer
                    });

            // ========================================================
            // LAST QUESTION?
            // ========================================================

            bool isLastQuestion =
                session.CurrentQuestionNumber >= questions.Count;

            if (isLastQuestion)
            {
                session.CurrentQuestionNumber =
                    questions.Count;

                session.IsCompleted = true;
                session.CompletedDate = DateTime.Now;
                session.Status = "Completed";

                await _context.SaveChangesAsync();

                await Clients.Group(sessionToken)
                    .SendAsync(
                        "InterviewCompleted",
                        "Thank you. Your interview has been completed successfully.");

                return;
            }

            // ========================================================
            // MOVE TO NEXT QUESTION
            // ========================================================

            session.CurrentQuestionNumber++;

            await _context.SaveChangesAsync();

            // ========================================================
            // SEND NEXT BOT QUESTION
            // ========================================================

            await SendCurrentQuestionAsync(sessionToken);
        }

        // ============================================================
        // SEND CURRENT QUESTION
        // ============================================================

        private async Task SendCurrentQuestionAsync(
            string sessionToken)
        {
            var session =
                await LoadSessionAsync(sessionToken);

            if (session == null)
                return;

            if (
                session.IsCompleted ||
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                return;
            }

            var application =
                session.VolunteerApplication;

            if (application == null)
                return;

            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return;

            var questions =
                await GetInterviewQuestionsAsync(
                    opportunity,
                    session.SessionToken);

            if (questions.Count == 0)
            {
                await CompleteSessionAsync(session);

                await Clients.Group(sessionToken)
                    .SendAsync(
                        "InterviewCompleted",
                        "There are no interview questions available.");

                return;
            }

            // ========================================================
            // NORMALIZE QUESTION NUMBER
            // ========================================================

            if (session.CurrentQuestionNumber <= 0)
            {
                session.CurrentQuestionNumber = 1;

                await _context.SaveChangesAsync();
            }

            if (session.CurrentQuestionNumber > questions.Count)
            {
                await CompleteSessionAsync(session);

                await Clients.Group(sessionToken)
                    .SendAsync(
                        "InterviewCompleted",
                        "Thank you. Your interview has been completed.");

                return;
            }

            var question =
                questions[
                    session.CurrentQuestionNumber - 1];

            // ========================================================
            // CHECK IF QUESTION ALREADY HAS AN ANSWER
            // ========================================================

            var existingAnswer =
                await _context.VolunteerApplicationAnswers
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerApplicationId ==
                            session.VolunteerApplicationId &&
                        x.VolunteerQuestionId ==
                            question.VolunteerQuestionId);

            // ========================================================
            // SEND QUESTION
            // ========================================================

            await Clients.Group(sessionToken)
                .SendAsync(
                    "BotQuestion",
                    new
                    {
                        questionId =
                            question.VolunteerQuestionId,

                        questionNumber =
                            session.CurrentQuestionNumber,

                        totalQuestions =
                            questions.Count,

                        question =
                            question.Question,

                        existingAnswer =
                            existingAnswer?.Answer,

                        isLastQuestion =
                            session.CurrentQuestionNumber ==
                            questions.Count
                    });
        }

        // ============================================================
        // SEND COMPLETE INTERVIEW STATE
        //
        // Used when:
        //
        // 1. User first opens interview
        // 2. Browser reconnects
        // 3. User refreshes the page
        //
        // This reconstructs the chat from database answers.
        // ============================================================

        private async Task SendInterviewStateAsync(
            string sessionToken)
        {
            var session =
                await LoadSessionAsync(sessionToken);

            if (session == null)
                return;

            // ========================================================
            // SUSPICIOUS SESSION
            // ========================================================

            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                await Clients.Caller.SendAsync(
                    "InterviewSuspicious",
                    new
                    {
                        message =
                            "This interview has been ended and submitted for administrative review.",

                        suspiciousEventCount =
                            session.SuspiciousEventCount,

                        suspiciousActivityDate =
                            session.SuspiciousActivityDate
                    });

                return;
            }

            // ========================================================
            // COMPLETED SESSION
            // ========================================================

            if (session.IsCompleted)
            {
                await Clients.Caller.SendAsync(
                    "InterviewCompleted",
                    "Thank you. Your interview has already been completed.");

                return;
            }

            var application =
                session.VolunteerApplication;

            if (application == null)
                return;

            var opportunity =
                application.VolunteerOpportunity;

            if (opportunity == null)
                return;

            var questions =
                await GetInterviewQuestionsAsync(
                    opportunity,
                    session.SessionToken);

            if (questions.Count == 0)
            {
                await CompleteSessionAsync(session);

                await Clients.Caller.SendAsync(
                    "InterviewCompleted",
                    "There are no interview questions available.");

                return;
            }

            // ========================================================
            // NORMALIZE CURRENT QUESTION
            //
            // IMPORTANT:
            // We do NOT reset this to 1 unless the database contains
            // an invalid value.
            // ========================================================

            if (session.CurrentQuestionNumber <= 0)
            {
                session.CurrentQuestionNumber = 1;

                await _context.SaveChangesAsync();
            }

            if (session.CurrentQuestionNumber > questions.Count)
            {
                await CompleteSessionAsync(session);

                await Clients.Caller.SendAsync(
                    "InterviewCompleted",
                    "Thank you. Your interview has been completed.");

                return;
            }

            // ========================================================
            // LOAD ANSWERS
            // ========================================================

            var questionIds =
                questions
                    .Select(x => x.VolunteerQuestionId)
                    .ToList();

            var answers =
                await _context.VolunteerApplicationAnswers
                    .Include(x => x.VolunteerQuestion)
                    .Where(x =>
                        x.VolunteerApplicationId ==
                            session.VolunteerApplicationId &&
                        questionIds.Contains(
                            x.VolunteerQuestionId))
                    .ToListAsync();

            // ========================================================
            // BUILD CHAT HISTORY
            // ========================================================

            var history =
                new List<object>();

            for (int i = 0; i < questions.Count; i++)
            {
                var question =
                    questions[i];

                var answer =
                    answers.FirstOrDefault(x =>
                        x.VolunteerQuestionId ==
                        question.VolunteerQuestionId);

                if (
                    answer == null ||
                    string.IsNullOrWhiteSpace(answer.Answer))
                {
                    continue;
                }

                history.Add(
                    new
                    {
                        questionNumber =
                            i + 1,

                        questionId =
                            question.VolunteerQuestionId,

                        question =
                            question.Question,

                        answer =
                            answer.Answer
                    });
            }

            // ========================================================
            // CURRENT QUESTION
            // ========================================================

            var currentQuestion =
                questions[
                    session.CurrentQuestionNumber - 1];

            var currentAnswer =
                answers.FirstOrDefault(x =>
                    x.VolunteerQuestionId ==
                    currentQuestion.VolunteerQuestionId);

            // ========================================================
            // SEND STATE TO CALLER ONLY
            // ========================================================

            await Clients.Caller.SendAsync(
                "InterviewState",
                new
                {
                    applicantName =
                        GetApplicantName(application),

                    opportunityTitle =
                        opportunity.Title,

                    currentQuestionNumber =
                        session.CurrentQuestionNumber,

                    totalQuestions =
                        questions.Count,

                    currentQuestionId =
                        currentQuestion.VolunteerQuestionId,

                    currentQuestion =
                        currentQuestion.Question,

                    existingAnswer =
                        currentAnswer?.Answer,

                    isLastQuestion =
                        session.CurrentQuestionNumber ==
                        questions.Count,

                    history = history
                });
        }

        // ============================================================
        // COMPLETE SESSION
        // ============================================================

        private async Task CompleteSessionAsync(
            VolunteerVoiceInterviewSession session)
        {
            // Never overwrite a suspicious session as Completed.
            if (
                session.Status == "Suspicious" ||
                session.SuspiciousActivityDetected)
            {
                return;
            }

            session.IsCompleted = true;
            session.CompletedDate = DateTime.Now;
            session.Status = "Completed";

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // LOAD SESSION
        // ============================================================

        private async Task<VolunteerVoiceInterviewSession?>
            LoadSessionAsync(
                string sessionToken)
        {
            return await _context
                .VolunteerVoiceInterviewSessions

                .Include(x =>
                    x.VolunteerApplication)

                    .ThenInclude(x =>
                        x!.VolunteerOpportunity)

                .Include(x =>
                    x.VolunteerApplication)

                    .ThenInclude(x =>
                        x!.Applicant)

                .FirstOrDefaultAsync(x =>
                    x.SessionToken == sessionToken);
        }

        // ============================================================
        // QUESTION SELECTION
        //
        // Questions are selected directly from VolunteerQuestions.
        //
        // We DO NOT use VolunteerOpportunityQuestions here because
        // application questions were removed from the opportunity
        // creation/application flow.
        // ============================================================

        private async Task<List<VolunteerQuestion>>
            GetInterviewQuestionsAsync(
                VolunteerOpportunity opportunity,
                string sessionToken)
        {
            var allQuestions =
                await _context.VolunteerQuestions
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.VolunteerQuestionId)
                    .ToListAsync();

            if (!allQuestions.Any())
            {
                return new List<VolunteerQuestion>();
            }

            // ========================================================
            // RANK QUESTIONS
            // ========================================================

            var rankedQuestions =
                allQuestions
                    .Select(question =>
                    {
                        int relevance = 0;

                        // ------------------------------------------------
                        // CATEGORY MATCH
                        // ------------------------------------------------

                        if (
                            opportunity.Category.HasValue &&
                            question.Category.HasValue &&
                            opportunity.Category.Value ==
                            question.Category.Value)
                        {
                            relevance += 100;
                        }

                        // ------------------------------------------------
                        // AREA MATCH
                        // ------------------------------------------------

                        if (
                            opportunity.Area.HasValue &&
                            question.Area.HasValue &&
                            opportunity.Area.Value ==
                            question.Area.Value)
                        {
                            relevance += 100;
                        }

                        // ------------------------------------------------
                        // CATEGORY MATCH WITHOUT AREA
                        // ------------------------------------------------

                        if (
                            opportunity.Category.HasValue &&
                            question.Category.HasValue &&
                            opportunity.Category.Value ==
                            question.Category.Value &&
                            !question.Area.HasValue)
                        {
                            relevance += 25;
                        }

                        return new
                        {
                            Question = question,
                            Relevance = relevance
                        };
                    })

                    // Only relevant questions
                    .Where(x => x.Relevance > 0)

                    .OrderByDescending(x => x.Relevance)

                    .ThenBy(x =>
                        x.Question.DisplayOrder)

                    .ThenBy(x =>
                        x.Question.VolunteerQuestionId)

                    .Take(7)

                    .Select(x => x.Question)

                    .ToList();

            // ========================================================
            // STABLE RANDOMIZATION
            //
            // Same token = same question order.
            //
            // Refreshing/reconnecting therefore does not change
            // the interview questions.
            // ========================================================

            var seed =
                CreateStableRandomSeed(sessionToken);

            var random =
                new Random(seed);

            return rankedQuestions
                .OrderBy(_ => random.Next())
                .ToList();
        }

        // ============================================================
        // STABLE RANDOM SEED
        // ============================================================

        private int CreateStableRandomSeed(
            string sessionToken)
        {
            using var sha =
                SHA256.Create();

            var bytes =
                sha.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        sessionToken ?? string.Empty));

            return BitConverter.ToInt32(
                bytes,
                0);
        }

        // ============================================================
        // APPLICANT NAME
        // ============================================================

        private string GetApplicantName(
            VolunteerApplication application)
        {
            if (application.Applicant == null)
            {
                return "Applicant";
            }

            var firstName =
                application.Applicant.FirstName?
                    .Trim();

            var lastName =
                application.Applicant.LastName?
                    .Trim();

            var fullName =
                $"{firstName} {lastName}"
                    .Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? "Applicant"
                : fullName;
        }
    }

}
