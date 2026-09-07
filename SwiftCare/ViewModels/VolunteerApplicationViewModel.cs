using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    public class VolunteerApplicationViewModel
    {
        public int VolunteerOpportunityId { get; set; }

        public string OpportunityTitle { get; set; } = string.Empty;


        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(255)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;


        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }


        [Required(ErrorMessage = "Please provide a cover message.")]
        [StringLength(2000, MinimumLength = 20,
            ErrorMessage = "Your cover message must be between 20 and 2000 characters.")]
        [Display(Name = "Why would you like to volunteer?")]
        public string CoverMessage { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please upload your CV.")]
        [Display(Name = "CV")]
        public IFormFile? CV { get; set; }


        /*
         * Questions assigned to this application.
         * These are the question IDs coming from the
         * opportunity's ApplicationQuestions.
         */
        public List<int> SelectedQuestionIds { get; set; }
            = new List<int>();


        /*
         * Questions displayed on the application page.
         */
        public List<VolunteerQuestionSelectionViewModel>
            ApplicationQuestions
        { get; set; }
            = new List<VolunteerQuestionSelectionViewModel>();


        /*
         * Answers submitted by the volunteer.
         *
         * Key   = VolunteerQuestionId
         * Value = Answer
         */
        public Dictionary<int, string> QuestionAnswers { get; set; }
            = new Dictionary<int, string>();
    }

}