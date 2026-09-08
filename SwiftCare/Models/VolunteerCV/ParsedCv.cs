using System.ComponentModel.DataAnnotations;

namespace SwiftCare.Models.VolunteerCV
{
    public class ParsedCv
    {
        public int VolunteerApplicationId { get; set; }

        [StringLength(250)]
        public string? FullName { get; set; }

        [StringLength(250)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? PhoneNumber { get; set; }

        public List<string> Skills { get; set; }
            = new();

        public List<CvEducation> Education { get; set; }
            = new();

        public List<CvExperience> Experience { get; set; }
            = new();

        public List<CvCertification> Certifications { get; set; }
            = new();

        public List<string> Languages { get; set; }
            = new();

        public string? RawText { get; set; }

        public bool ExtractionSuccessful { get; set; }

        public string? ExtractionMessage { get; set; }
    }
}