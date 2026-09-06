using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class VolunteerTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerTaskId { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime? AssignedDate { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public VolunteerTaskStatus Status { get; set; }
            = VolunteerTaskStatus.Assigned;

        [StringLength(2000)]
        public string? CompletionNotes { get; set; }

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }


        [ForeignKey(nameof(AssignedById))]
        public string? AssignedById { get; set; }
        public UserBaseModel AssignedBy { get; set; }

        public int? VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }
    }

    public enum VolunteerTaskStatus
    {
        [Display(Name = "Assigned")]
        Assigned,

        [Display(Name = "In Progress")]
        InProgress,

        [Display(Name = "Completed")]
        Completed,

        [Display(Name = "Cancelled")]
        Cancelled
    }
}