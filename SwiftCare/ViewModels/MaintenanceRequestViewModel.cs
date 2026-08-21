using System.ComponentModel.DataAnnotations;
using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    public class MaintenanceRequestViewModel
    {
        // ============================================================
        // EQUIPMENT INFORMATION
        // ============================================================

        public int EquipmentId { get; set; }

        public string? EquipmentName { get; set; }

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public string? BarcodeValue { get; set; }

        public EquipmentStatus EquipmentStatus { get; set; }


        // ============================================================
        // ASSIGNMENT INFORMATION
        // ============================================================

        public string? AssignedToId { get; set; }

        public string? AssignmentNotes { get; set; }

        public DateTime? AssignedAt { get; set; }


        // ============================================================
        // MAINTENANCE REQUEST
        // ============================================================

        [Required(ErrorMessage = "Please select the maintenance priority.")]
        [Display(Name = "Priority")]
        public MaintenancePriority Priority { get; set; }
            = MaintenancePriority.Medium;


        [Required(ErrorMessage = "Please describe the maintenance problem.")]
        [StringLength(
            2000,
            ErrorMessage = "The maintenance description cannot exceed 2000 characters."
        )]
        [Display(Name = "Problem Description")]
        public string? Message { get; set; }


        // ============================================================
        // OPTIONAL EQUIPMENT CONDITION IMAGE
        // ============================================================

        public IFormFile? EquipmentConditionImageBefore { get; set; }
    }
}