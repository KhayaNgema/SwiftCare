using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    public class NewOpportunityViewModel
    {
        [Required(ErrorMessage = "Opportunity title is required.")]
        [StringLength(
            150,
            MinimumLength = 3,
            ErrorMessage = "The title must be between 3 and 150 characters."
        )]
        [Display(Name = "Opportunity Title")]
        public string Title { get; set; } = string.Empty;


        [Required(ErrorMessage = "Opportunity description is required.")]
        [StringLength(
            3000,
            MinimumLength = 10,
            ErrorMessage = "The description must be between 10 and 3000 characters."
        )]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public VolunteerCategory? Category { get; set; }


        [Required(ErrorMessage = "Please select an area.")]
        [Display(Name = "Volunteer Area")]
        public VolunteerArea? Area { get; set; }


        // =========================================================
        // CITY
        // =========================================================

        [Required(ErrorMessage = "Please select a city.")]
        [StringLength(150)]
        [Display(Name = "City")]
        public string? CityName { get; set; }


        public int? GeoNameId { get; set; }


        [StringLength(100)]
        [Display(Name = "Province")]
        public string? Province { get; set; }


        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }


        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }


        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }


        [Required(ErrorMessage = "End time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan? EndTime { get; set; }


        [Required(ErrorMessage = "Please specify the number of volunteers needed.")]
        [Range(
            1,
            1000,
            ErrorMessage = "The number of volunteers needed must be between 1 and 1000."
        )]
        [Display(Name = "Volunteers Needed")]
        public int? VolunteersNeeded { get; set; }


        [Required(ErrorMessage = "Minimum age is required.")]
        [Range(
            14,
            100,
            ErrorMessage = "The minimum age must be between 14 and 100."
        )]
        [Display(Name = "Minimum Age")]
        public int? MinimumAge { get; set; }


        [Required(ErrorMessage = "Application deadline is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Application Deadline")]
        public DateTime? ApplicationDeadline { get; set; }


        // =========================================================
        // POSTER IMAGE UPLOAD
        // =========================================================

        [Display(Name = "Opportunity Poster")]
        public IFormFile? PosterImage { get; set; }


        // =========================================================
        // REQUIREMENTS
        // =========================================================

        public List<string> Requirements { get; set; }
            = new List<string>();


        // =========================================================
        // RESPONSIBILITIES
        // =========================================================

        public List<string> Responsibilities { get; set; }
            = new List<string>();


        // =========================================================
        // REQUIRED SKILLS
        // =========================================================

        public List<string> RequiredSkills { get; set; }
            = new List<string>();


        // =========================================================
        // SELECTED APPLICATION QUESTIONS
        // =========================================================

        public List<int> SelectedQuestionIds { get; set; }
            = new List<int>();


        // =========================================================
        // AVAILABLE APPLICATION QUESTIONS
        // =========================================================

        public List<VolunteerQuestionSelectionViewModel>
            AvailableQuestions
        { get; set; }
            = new List<VolunteerQuestionSelectionViewModel>();
    }


    // =========================================================
    // VOLUNTEER QUESTION SELECTION VIEW MODEL
    // =========================================================

    public class VolunteerQuestionSelectionViewModel
    {
        public int VolunteerQuestionId { get; set; }


        public string Question { get; set; }
            = string.Empty;


        public VolunteerQuestionType QuestionType { get; set; }


        public bool IsRequired { get; set; }


        public VolunteerCategory? Category { get; set; }


        public VolunteerArea? Area { get; set; }

        public int? DisplayOrder { get; set; }
    }
}