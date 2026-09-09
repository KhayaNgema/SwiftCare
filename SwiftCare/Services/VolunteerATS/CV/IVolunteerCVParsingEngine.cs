using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public interface IVolunteerCVParsingEngine
    {
        // Existing raw-text fallback parser
        Task<ParsedCv> ParseAsync(
            string rawText,
            int volunteerApplicationId = 0);

        // Primary parser for Azure Document Intelligence
        Task<ParsedCv> ParseAsync(
            AzureCvExtractionResult azureResult,
            int volunteerApplicationId = 0);
    }
}