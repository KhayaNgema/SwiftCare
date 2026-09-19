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

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        // ============================================================
        // ROSTER
        // ============================================================

        [Required]
        public int VolunteerRosterId { get; set; }

        [ForeignKey(nameof(VolunteerRosterId))]
        public VolunteerRoster? VolunteerRoster { get; set; }

        // ============================================================
        // VOLUNTEER
        // ============================================================

        [Required]
        public string VolunteerId { get; set; } = string.Empty;

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }

        // ============================================================
        // TASK SCHEDULE
        // ============================================================

        [Required]
        public DateTime TaskDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        // ============================================================
        // TASK STATUS
        // ============================================================

        public VolunteerTaskStatus Status { get; set; }
            = VolunteerTaskStatus.Assigned;

        public DateTime? StartedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(2000)]
        public string? CompletionNotes { get; set; }

        // ============================================================
        // ASSIGNMENT
        // ============================================================

        [StringLength(450)]
        public string? AssignedById { get; set; }

        [ForeignKey(nameof(AssignedById))]
        public UserBaseModel? AssignedBy { get; set; }

        // ============================================================
        // OPTIONAL TASK INFORMATION
        // ============================================================

        [StringLength(200)]
        public string? Location { get; set; }

        public VolunteerTaskPriority Priority { get; set; }
            = VolunteerTaskPriority.Normal;

        // ============================================================
        // VOLUNTEER OPPORTUNITY
        // ============================================================

        public int? VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }
    }

    public enum VolunteerTaskStatus
    {
        [Display(Name = "Assigned")]
        Assigned = 1,

        [Display(Name = "In Progress")]
        InProgress = 2,

        [Display(Name = "Completed")]
        Completed = 3,

        [Display(Name = "Cancelled")]
        Cancelled = 4,

        [Display(Name = "Missed")]
        Missed = 5
    }

    public enum VolunteerTaskPriority
    {
        [Display(Name = "Low")]
        Low = 1,

        [Display(Name = "Normal")]
        Normal = 2,

        [Display(Name = "High")]
        High = 3,

        [Display(Name = "Urgent")]
        Urgent = 4
    }
}