using Microsoft.AspNetCore.Http;
using SwiftCare.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace SwiftCare.ViewModels
{
    public class MaintenanceRequestUpdateViewModel
    {
        // ============================================================
        // REQUEST
        // ============================================================

        [Required]
        public int RequestId { get; set; }


        // ============================================================
        // EQUIPMENT INFORMATION
        // ============================================================

        public int EquipmentId { get; set; }

        public string? EquipmentName { get; set; }

        public string? EquipmentImageUrl { get; set; }

        public string? BarcodeValue { get; set; }

        public EquipmentStatus EquipmentStatus { get; set; }


        // ============================================================
        // REQUEST INFORMATION
        // ============================================================

        public string? ReferenceNumber { get; set; }

        public MaintenancePriority Priority { get; set; }

        public string? ProblemDescription { get; set; }

        public DateTime LoggedAt { get; set; }


        // ============================================================
        // UPDATE
        // ============================================================

        [Required(ErrorMessage = "Please select the maintenance status.")]
        [Display(Name = "Maintenance Status")]
        public MaintenanceRequestStatus RequestStatus { get; set; }


        [StringLength(
            3000,
            ErrorMessage = "The work performed description cannot exceed 3000 characters."
        )]
        [Display(Name = "Work Performed")]
        public string? WorkPerformed { get; set; }


        // ============================================================
        // CONDITION IMAGE AFTER MAINTENANCE
        // ============================================================

        [Display(Name = "Equipment Condition After Maintenance")]
        public IFormFile? EquipmentConditionImageAfter { get; set; }


        // ============================================================
        // EXISTING IMAGE
        // ============================================================

        public string? ExistingConditionImageAfter { get; set; }
    }
}