using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class VolunteerVoiceInterviewSession
    {
        [Key]
        public int VolunteerVoiceInterviewSessionId { get; set; }

        public int VolunteerApplicationId { get; set; }

        [ForeignKey(nameof(VolunteerApplicationId))]
        public VolunteerApplication? VolunteerApplication { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionToken { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? StartedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public int CurrentQuestionNumber { get; set; }

        public bool IsStarted { get; set; }

        public bool IsCompleted { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "NotStarted";


        // ============================================================
        // VIDEO RECORDING
        // ============================================================

        [StringLength(1000)]
        public string? VideoFilePath { get; set; }

        [StringLength(100)]
        public string? VideoMimeType { get; set; }

        public DateTime? VideoStartedDate { get; set; }

        public DateTime? VideoCompletedDate { get; set; }

        public int VideoDurationSeconds { get; set; }


        // ============================================================
        // PROCTORING / INTERVIEW INTEGRITY
        // ============================================================

        public bool SuspiciousActivityDetected { get; set; }

        public DateTime? SuspiciousActivityDate { get; set; }

        [StringLength(100)]
        public string? LastProctoringEventType { get; set; }

        public int SuspiciousEventCount { get; set; }

        public int TabSwitchCount { get; set; }

        public int WindowBlurCount { get; set; }

        public int FullScreenExitCount { get; set; }

        public int PageLeaveCount { get; set; }

        public int CameraStoppedCount { get; set; }

        public int TotalUnfocusedSeconds { get; set; }

        [StringLength(1000)]
        public string? IncompleteReason { get; set; }


        // ============================================================
        // NAVIGATION / RESUME STATE
        // ============================================================

        public DateTime? LastActivityDate { get; set; }

        [StringLength(100)]
        public string? LastActivityType { get; set; }


        // ============================================================
        // PROCTORING EVENTS
        // ============================================================

        public ICollection<VolunteerInterviewProctoringEvent>
            ProctoringEvents
        { get; set; }
                = new List<VolunteerInterviewProctoringEvent>();
    }
}