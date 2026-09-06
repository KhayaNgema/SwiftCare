using System.ComponentModel.DataAnnotations;

namespace SwiftCare.ViewModels
{
    public class VolunteerApplicationViewModel
    {
        public int VolunteerOpportunityId { get; set; }

        [Required(ErrorMessage = "Please provide a cover message.")]
        [StringLength(
            2000,
            MinimumLength = 20,
            ErrorMessage = "Your cover message must be between 20 and 2000 characters."
        )]


        [Display(Name = "Why would you like to volunteer?")]
        public string CoverMessage { get; set; } = string.Empty;

        public List<VolunteerResponsibilityViewModel> Responsibilities { get; set; }
            = new();

        public List<VolunteerRequirementViewModel> Requirements { get; set; }
            = new();

        public List<VolunteerRequiredSkillViewModel> RequiredSkills { get; set; }
            = new();
    }

    public class VolunteerResponsibilityViewModel
    {
        public int VolunteerResponsibilityId { get; set; }

        public string Description { get; set; } = string.Empty;
    }

    public class VolunteerRequirementViewModel
    {
        public int VolunteerRequirementId { get; set; }

        public string Description { get; set; } = string.Empty;
    }

    public class VolunteerRequiredSkillViewModel
    {
        public int VolunteerRequiredSkillId { get; set; }

        public string Skill { get; set; } = string.Empty;
    }
}