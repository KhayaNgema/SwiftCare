namespace SwiftCare.Services.VolunteerATS.CV
{
    public class VolunteerCVDocumentEngine
        : IVolunteerCVDocumentEngine
    {
        private static readonly string[] SupportedExtensions =
        {
            ".pdf",
            ".docx"
        };

        public bool IsSupported(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            return SupportedExtensions.Contains(extension);
        }

        public string GetDocumentType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown";

            var extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "PDF",
                ".docx" => "DOCX",
                _ => "Unknown"
            };
        }

        public bool IsPdf(string fileName)
        {
            return Path.GetExtension(fileName)
                .Equals(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase);
        }

        public bool IsDocx(string fileName)
        {
            return Path.GetExtension(fileName)
                .Equals(
                    ".docx",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}