using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerRankingEngine
    {
        private readonly SwiftCareDbContext _context;

        public VolunteerRankingEngine(
            SwiftCareDbContext context)
        {
            _context = context;
        }


        public async Task RankAsync(
            int opportunityId)
        {
            var evaluations =
                await _context
                    .VolunteerApplicationEvaluations
                    .Include(x =>
                        x.VolunteerApplication)
                    .Where(x =>
                        x.VolunteerApplication != null &&
                        x.VolunteerApplication
                            .VolunteerOpportunityId ==
                        opportunityId &&
                        x.EligibilityStatus !=
                            VolunteerEligibilityStatus.NotEligible)
                    .OrderByDescending(x =>
                        x.OverallScore)
                    .ThenByDescending(x =>
                        x.AIConfidence)
                    .ToListAsync();


            int rank = 1;

            foreach (var evaluation in evaluations)
            {
                evaluation.RankingPosition =
                    rank;

                if (evaluation.AIRecommendation ==
                    VolunteerAIRecommendation.NotEvaluated)
                {
                    if (evaluation.OverallScore >= 80)
                    {
                        evaluation.AIRecommendation =
                            VolunteerAIRecommendation
                                .StrongShortlist;
                    }
                    else if (evaluation.OverallScore >= 60)
                    {
                        evaluation.AIRecommendation =
                            VolunteerAIRecommendation
                                .ReviewRecommended;
                    }
                    else
                    {
                        evaluation.AIRecommendation =
                            VolunteerAIRecommendation
                                .NotRecommended;
                    }
                }

                var application =
                    evaluation.VolunteerApplication!;

                application.ATSRanking =
                    rank;

                application.ATSScore =
                    evaluation.OverallScore;

                application.AIRecommendation =
                    evaluation.AIRecommendation;

                if (evaluation.AIRecommendation ==
                    VolunteerAIRecommendation.StrongShortlist)
                {
                    application.Stage =
                        VolunteerApplicationStage.Shortlisted;

                    application.ShortlistedDate =
                        DateTime.Now;
                }

                rank++;
            }

            await _context.SaveChangesAsync();
        }
    }
}