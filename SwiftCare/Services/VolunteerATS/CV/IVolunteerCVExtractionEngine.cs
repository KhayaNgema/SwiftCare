namespace SwiftCare.Services.VolunteerATS.CV
{
    public interface IVolunteerCVExtractionEngine
    {
        Task<string> ExtractTextAsync(
            Stream documentStream,
            string fileName);
    }
}