using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public interface IVolunteerCVParsingEngine
    {
        Task<ParsedCv> ParseAsync(
            string rawText,
            int volunteerApplicationId);

        Task<ParsedCv> ParseAsync(
            AzureCvExtractionResult azureResult,
            int volunteerApplicationId = 0);
    }
}