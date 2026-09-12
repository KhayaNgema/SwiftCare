using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public interface IVolunteerCVProcessingEngine
    {
        Task<ParsedCv> ProcessAsync(
            Stream documentStream,
            string fileName,
            int volunteerApplicationId = 0);
    }
}