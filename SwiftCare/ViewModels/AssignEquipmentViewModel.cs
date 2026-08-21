using SwiftCare.Models;
using System.ComponentModel.DataAnnotations;

namespace SwiftCare.ViewModels
{
    public class AssignEquipmentViewModel
    {
        // ============================================================
        // RESIDENT
        // ============================================================

        [Required]
        public string AssignedToId { get; set; }


        public string? ResidentName { get; set; }

        public Room? RoomNo { get; set; }

        public string? ProfilePicture { get; set; }


        // ============================================================
        // EQUIPMENT
        // ============================================================

        public string? BarcodeValue { get; set; }


        public int? EquipmentId { get; set; }


        public string? EquipmentName { get; set; }

        public string? EquipmentImage { get; set; }

        public string? EquipmentDescription { get; set; }

        public string? EquipmentStatus { get; set; }


        // ============================================================
        // ASSIGNMENT
        // ============================================================

        public string? AssignmentNotes { get; set; }
    }
}