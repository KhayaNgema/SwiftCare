using Microsoft.Extensions.Logging;
using SwiftCare.Services.AI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerATS
{
    /// <summary>
    /// Hangfire background job responsible for running volunteer
    /// interview AI evaluations outside the HTTP request.
    /// </summary>
    public class VolunteerInterviewEvaluationJob
    {
        private readonly IVolunteerInterviewEvaluationEngine _evaluationEngine;
        private readonly ILogger<VolunteerInterviewEvaluationJob> _logger;

        public VolunteerInterviewEvaluationJob(
            IVolunteerInterviewEvaluationEngine evaluationEngine,
            ILogger<VolunteerInterviewEvaluationJob> logger)
        {
            _evaluationEngine = evaluationEngine;
            _logger = logger;
        }

        public async Task RunAsync(int volunteerApplicationId)
        {
            _logger.LogInformation(
                "[SWIFTCARE ATS] Hangfire interview evaluation job started for application {ApplicationId}.",
                volunteerApplicationId);

            try
            {
                var result = await _evaluationEngine.EvaluateAsync(
                    volunteerApplicationId,
                    CancellationToken.None);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[SWIFTCARE ATS] Hangfire interview evaluation completed for application {ApplicationId}. Score={Score}, Confidence={Confidence}, Recommendation={Recommendation}.",
                        volunteerApplicationId,
                        result.OverallInterviewScore,
                        result.OverallInterviewConfidence,
                        result.Recommendation);
                }
                else
                {
                    _logger.LogWarning(
                        "[SWIFTCARE ATS] Hangfire interview evaluation failed for application {ApplicationId}: {Error}",
                        volunteerApplicationId,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE ATS] Hangfire interview evaluation job crashed for application {ApplicationId}.",
                    volunteerApplicationId);

                throw;
            }
        }
    }
}