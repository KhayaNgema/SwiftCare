using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public interface IVolunteerCVParsingEngine
    {
        Task<ParsedCv> ParseAsync(
            string rawText,
            int volunteerApplicationId = 0);
    }
}