using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SwiftCare.Models
{
    public class Volunteer : UserBaseModel
    {

        public string? Bio { get; set; }

        public string? Motivation { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public string? EmergencyContactRelationship { get; set; }


        public VolunteerStatus Status { get; set; }
            = VolunteerStatus.Active;

        public VolunteerArea? PreferredArea { get; set; }


        public ICollection<VolunteerExperience> Experiences { get; set; }
            = new List<VolunteerExperience>();

        public ICollection<VolunteerSkill> Skills { get; set; }
            = new List<VolunteerSkill>();

        public ICollection<VolunteerAvailability> Availability { get; set; }
            = new List<VolunteerAvailability>();

        public ICollection<VolunteerApplication> Applications { get; set; }
            = new List<VolunteerApplication>();

        public ICollection<VolunteerTask> Tasks { get; set; }
            = new List<VolunteerTask>();

        public VolunteerOnboarding? Onboarding { get; set; }
    }

    public enum VolunteerStatus
    {
        [Display(Name = "Active")]
        Active,

        [Display(Name = "Inactive")]
        Inactive,

        [Display(Name = "Suspended")]
        Suspended,

        [Display(Name = "Terminated")]
        Terminated
    }
}