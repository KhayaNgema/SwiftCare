using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SwiftCare.ViewModels
{
    public class VolunteerApplicationViewModel
    {
        // ============================================================
        // OPPORTUNITY
        // ============================================================

        public int VolunteerOpportunityId { get; set; }

        public string OpportunityTitle { get; set; } = string.Empty;


        // ============================================================
        // PERSONAL INFORMATION
        // ============================================================

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage =
                "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage =
                "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(
            ErrorMessage =
                "Please enter a valid email address.")]
        [StringLength(255)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(
            ErrorMessage =
                "Please enter a valid phone number.")]
        [StringLength(30)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;


        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }


        // ============================================================
        // PROFILE INFORMATION
        // These can be supplied by Azure CV extraction or edited
        // by the applicant.
        // ============================================================

        [StringLength(150)]
        [Display(Name = "Professional Title")]
        public string? ProfessionalTitle { get; set; }


        [StringLength(3000)]
        [Display(Name = "Professional Summary")]
        public string? ProfessionalSummary { get; set; }


        [StringLength(3000)]
        [Display(Name = "Career Objective")]
        public string? CareerObjective { get; set; }


        // ============================================================
        // LOCATION
        // ============================================================

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }


        [StringLength(100)]
        [Display(Name = "City")]
        public string? City { get; set; }


        [StringLength(100)]
        [Display(Name = "Province")]
        public string? Province { get; set; }


        [StringLength(100)]
        [Display(Name = "Country")]
        public string? Country { get; set; }


        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }


        // ============================================================
        // PREFERENCES
        // ============================================================

        [StringLength(100)]
        [Display(Name = "Preferred Work Type")]
        public string? PreferredWorkType { get; set; }


        [StringLength(250)]
        [Display(Name = "Preferred Location")]
        public string? PreferredLocation { get; set; }


        [Display(Name = "Willing to Relocate")]
        public bool WillingToRelocate { get; set; }


        [Display(Name = "Available for Volunteer Work")]
        public bool AvailableForVolunteerWork { get; set; }


        // ============================================================
        // PROFESSIONAL LINKS
        // ============================================================

        [StringLength(500)]
        [Url]
        [Display(Name = "LinkedIn")]
        public string? LinkedInUrl { get; set; }


        [StringLength(500)]
        [Url]
        [Display(Name = "Portfolio")]
        public string? PortfolioUrl { get; set; }


        [StringLength(500)]
        [Url]
        [Display(Name = "GitHub")]
        public string? GitHubUrl { get; set; }


        // ============================================================
        // COVER MESSAGE
        // ============================================================

        [Required(
            ErrorMessage =
                "Please provide a cover message.")]
        [StringLength(
            2000,
            MinimumLength = 20,
            ErrorMessage =
                "Your cover message must be between 20 and 2000 characters.")]
        [Display(
            Name =
                "Why would you like to volunteer?")]
        public string CoverMessage { get; set; } = string.Empty;


        // ============================================================
        // CV
        // ============================================================

        [Required(
            ErrorMessage =
                "Please upload your CV.")]
        [Display(Name = "CV")]
        public IFormFile? CV { get; set; }


        // ============================================================
        // QUESTIONS
        // ============================================================

        public List<int> SelectedQuestionIds { get; set; }
            = new List<int>();


        public List<VolunteerQuestionSelectionViewModel>
            ApplicationQuestions
        { get; set; }
            = new List<VolunteerQuestionSelectionViewModel>();


        public Dictionary<int, string> QuestionAnswers { get; set; }
            = new Dictionary<int, string>();
    }
}