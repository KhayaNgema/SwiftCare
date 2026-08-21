using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class MaintenanceRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }


        // ============================================================
        // EQUIPMENT
        // ============================================================

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey(nameof(EquipmentId))]
        public virtual OurEquipment OurEquipment { get; set; }


        // ============================================================
        // ASSIGNED TO - MAINTENANCE PERSON
        // ============================================================

        public string? AssignedToId { get; set; }

        [ForeignKey(nameof(AssignedToId))]
        public virtual MaintenancePerson? AssignedTo { get; set; }


        // ============================================================
        // LOGGED BY - NURSE
        // ============================================================

        [Required]
        public string LoggedById { get; set; }

        [ForeignKey(nameof(LoggedById))]
        public virtual Nurse LoggedBy { get; set; }


        // ============================================================
        // PRIORITY
        // ============================================================

        [Required]
        public MaintenancePriority Priority { get; set; }
            = MaintenancePriority.Medium;


        // ============================================================
        // MESSAGE / PROBLEM DESCRIPTION
        // ============================================================

        [Required]
        [StringLength(2000)]
        public string Message { get; set; }


        public string ReferenceNumber { get; set; }

        // ============================================================
        // REQUEST STATUS
        // ============================================================

        [Required]
        public MaintenanceRequestStatus RequestStatus { get; set; }
            = MaintenanceRequestStatus.Pending;


        // ============================================================
        // TIME TRACKING
        // ============================================================

        [Required]
        public DateTime LoggedAt { get; set; }
            = DateTime.Now;

        public DateTime? ResolvedAt { get; set; }


        // ============================================================
        // RESOLVED BY
        // ============================================================

        public string? ResolvedById { get; set; }

        [ForeignKey(nameof(ResolvedById))]
        public virtual MaintenancePerson? ResolvedBy { get; set; }


        // ============================================================
        // EQUIPMENT CONDITION BEFORE MAINTENANCE
        // ============================================================

        [StringLength(500)]
        public string? EquipmentConditionImageBefore { get; set; }

        // ============================================================
        // MAINTENANCE WORK PERFORMED
        // ============================================================

        [StringLength(3000)]
        public string? WorkPerformed { get; set; }
        // ============================================================
        // EQUIPMENT CONDITION AFTER MAINTENANCE
        // ============================================================

        [StringLength(500)]
        public string? EquipmentConditionImageAfter { get; set; }
    }


    // ============================================================
    // MAINTENANCE PRIORITY
    // ============================================================

    public enum MaintenancePriority
    {
        [Display(Name = "Low")]
        Low = 0,

        [Display(Name = "Medium")]
        Medium = 1,

        [Display(Name = "High")]
        High = 2,

        [Display(Name = "Critical")]
        Critical = 3
    }


    // ============================================================
    // MAINTENANCE REQUEST STATUS
    // ============================================================

    public enum MaintenanceRequestStatus
    {
        [Display(Name = "Pending")]
        Pending = 0,

        [Display(Name = "Assigned")]
        Assigned = 1,

        [Display(Name = "In Progress")]
        InProgress = 2,

        [Display(Name = "Completed")]
        Completed = 3,

        [Display(Name = "Cancelled")]
        Cancelled = 4
    }
}