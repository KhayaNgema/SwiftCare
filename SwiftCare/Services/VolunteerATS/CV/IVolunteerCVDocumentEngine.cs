namespace SwiftCare.Services.VolunteerATS.CV
{
    public interface IVolunteerCVDocumentEngine
    {
        bool IsSupported(string fileName);

        string GetDocumentType(string fileName);

        bool IsPdf(string fileName);

        bool IsDocx(string fileName);
    }
}