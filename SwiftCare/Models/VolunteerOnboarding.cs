using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class VolunteerOnboarding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerOnboardingId { get; set; }

        public VolunteerOnboardingStatus Status { get; set; }
            = VolunteerOnboardingStatus.NotStarted;

        public DateTime? StartedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool ProfileCompleted { get; set; }

        public bool DocumentsCompleted { get; set; }

        public bool ScreeningCompleted { get; set; }

        public bool AgreementCompleted { get; set; }

        public bool OrientationCompleted { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public string ApplicantId { get; set; }

        [ForeignKey(nameof(ApplicantId))]
        public Applicant? Applicant { get; set; }
    }

    public enum VolunteerOnboardingStatus
    {
        [Display(Name = "Not Started")]
        NotStarted,

        [Display(Name = "In Progress")]
        InProgress,

        [Display(Name = "Completed")]
        Completed,

        [Display(Name = "Failed")]
        Failed
    }
}