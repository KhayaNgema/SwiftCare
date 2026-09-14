using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    public class VolunteerActivationViewModel
    {
        [Required]
        public int VolunteerOnboardingId { get; set; }

        [Required]
        public int VolunteerApplicationId { get; set; }

        [Required]
        public string ApplicantId { get; set; } = string.Empty;

        public string ApplicantName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string PositionTitle { get; set; } = string.Empty;

        public string? Bio { get; set; }

        public string? Motivation { get; set; }

        public VolunteerArea? PreferredArea { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public string? EmergencyContactRelationship { get; set; }

        [Required]
        [StringLength(30)]
        public string VolunteerNumber { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Department { get; set; }

        [StringLength(450)]
        public string? SupervisorId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public VolunteerStatus Status { get; set; }
            = VolunteerStatus.Active;

        public bool ReadyForActivation { get; set; }

        public string? VerificationNotes { get; set; }

        public string? OpportunityCity { get; set; }

        public string? OpportunityProvince { get; set; }

        public List<VolunteerActivationSupervisorViewModel> Supervisors { get; set; }
            = new();

        public List<string> Departments { get; set; }
            = new();

        public List<VolunteerActivationDocumentViewModel> Documents { get; set; }
            = new();
    }

    public class VolunteerActivationSupervisorViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    public class VolunteerActivationDocumentViewModel
    {
        public int DocumentId { get; set; }

        public string DocumentName { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string DocumentType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; }
    }
}