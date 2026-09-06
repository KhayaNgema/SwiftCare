using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class VolunteerScreening
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerScreeningId { get; set; }

        public VolunteerScreeningStatus Status { get; set; }
            = VolunteerScreeningStatus.Pending;

        public DateTime? ScreeningDate { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public string? CompletedById { get; set; }

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }
    }

    public enum VolunteerScreeningStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "In Progress")]
        InProgress,

        [Display(Name = "Passed")]
        Passed,

        [Display(Name = "Failed")]
        Failed
    }
}