using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class VolunteerInterviewProctoringEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerInterviewProctoringEventId { get; set; }


        // ============================================================
        // INTERVIEW SESSION
        // ============================================================

        public int VolunteerVoiceInterviewSessionId { get; set; }

        [ForeignKey(nameof(VolunteerVoiceInterviewSessionId))]
        public VolunteerVoiceInterviewSession?
            VolunteerVoiceInterviewSession
        { get; set; }


        // ============================================================
        // EVENT
        // ============================================================

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty;


        [StringLength(1000)]
        public string? Description { get; set; }


        // ============================================================
        // INTERVIEW POSITION
        // ============================================================

        public int QuestionNumber { get; set; }


        // ============================================================
        // TIMING
        // ============================================================

        public DateTime EventDate { get; set; } = DateTime.Now;


        // ============================================================
        // CLIENT INFORMATION
        // ============================================================

        [StringLength(100)]
        public string? PageVisibilityState { get; set; }

        public bool? PageHidden { get; set; }


        [StringLength(500)]
        public string? UserAgent { get; set; }


        // ============================================================
        // UNFOCUSED TIME
        // ============================================================

        public int UnfocusedSeconds { get; set; }
    }
}